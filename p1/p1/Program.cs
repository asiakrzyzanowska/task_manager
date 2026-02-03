using p1.Models;
using p1.Storage;

var store = new TaskStore("tasks.json");

while (true)
{
    Console.WriteLine("\n== Task Scheduler ==");
    Console.WriteLine("1) Dodaj zadanie");
    Console.WriteLine("2) Pokaż zadania na dzień");
    Console.WriteLine("3) Pokaż wszystkie (najbliższe najpierw)");
    Console.WriteLine("4) Przełącz status zrobione / niezrobione");
    Console.WriteLine("5) Edytuj zadanie");
    Console.WriteLine("6) Usuń zadanie");
    Console.WriteLine("0) Wyjście");
    Console.Write("Wybór: ");

    var choice = Console.ReadLine()?.Trim();

    switch (choice)
    {
        case "1": AddTask(store); break;
        case "2": ShowByDay(store); break;
        case "3": ShowAll(store); break;
        case "4": ToggleDone(store); break;
        case "5": EditTask(store); break;
        case "6": Delete(store); break;
        case "0": return;
        default: Console.WriteLine("Nieznana opcja."); break;
    }
}

static void AddTask(TaskStore store)
{
    var title = ReadNonEmpty("Tytuł: ");
    var date = ReadDate("Data (YYYY-MM-DD): ");

    Console.Write("Godzina (HH:mm) opcjonalnie, Enter = brak: ");
    var time = ReadOptionalTime(Console.ReadLine());

    Console.Write("Krótka notatka (opcjonalnie): ");
    var desc = (Console.ReadLine() ?? "").Trim();

    if (desc.Length > 120)
    {
        Console.WriteLine("Notatka jest za długa (max 120 znaków).");
        return;
    }


    var item = new TaskItem
    {
        Id = Guid.NewGuid(),
        Title = title,
        Date = date,
        Time = time,
        Description = desc,
        IsDone = false,
        CreatedAt = DateTimeOffset.UtcNow
    };

    store.Add(item);
    Console.WriteLine($"Dodano zadanie: {item.Id}");
}

static void ShowByDay(TaskStore store)
{
    var day = ReadDate("Dzień (YYYY-MM-DD): ");

    var items = store.GetAll()
        .Where(t => t.Date == day)
        .OrderBy(t => t.Time ?? new TimeOnly(23, 59))
        .ThenBy(t => t.Title)
        .ToList();

    Print(items, $"Zadania na {day:yyyy-MM-dd}");
}

static void ShowAll(TaskStore store)
{
    Console.Write("tylko niewykonane? (t/n): ");
    var onlyOpen = (Console.ReadLine() ?? "").Trim().ToLower() == "t";

    var items = store.GetAll()
        .Where(t => !onlyOpen || !t.IsDone)
        .OrderBy(t => t.Date)
        .ThenBy(t => t.Time ?? new TimeOnly(23, 59))
        .ToList();

    Print(items, onlyOpen ? "Niewykonane zadania" : "Wszystkie zadania");
}

static void ToggleDone(TaskStore store)
{
    var id = ReadGuid("Podaj ID zadania: ");

    if (!store.ToggleDone(id))
    {
        Console.WriteLine("Nie znaleziono zadania.");
        return;
    }

    Console.WriteLine("Zmieniono status zadania.");
}

static void EditTask(TaskStore store)
{
    var id = ReadGuid("Podaj ID zadania: ");
    var item = store.GetById(id);

    if (item is null)
    {
        Console.WriteLine("Nie znaleziono zadania.");
        return;
    }

    Console.WriteLine("\n-- Edycja (enter = bez zmian) --");

    Console.Write($"Tytuł ({item.Title}): ");
    var title = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(title))
        item.Title = title.Trim();

    Console.Write($"Data ({item.Date:yyyy-MM-dd}): ");
    var dateInput = Console.ReadLine();
    if (!string.IsNullOrWhiteSpace(dateInput))
    {
        if (!DateOnly.TryParse(dateInput, out var newDate))
        {
            Console.WriteLine("Zła data.");
            return;
        }
        item.Date = newDate;
    }

    Console.Write($"Godzina ({item.Time?.ToString("HH:mm") ?? "brak"}, '-' usuwa): ");
    var timeInput = Console.ReadLine();
    if (timeInput == "-")
        item.Time = null;
    else if (!string.IsNullOrWhiteSpace(timeInput))
    {
        if (!TimeOnly.TryParse(timeInput, out var newTime))
        {
            Console.WriteLine("Zła godzina.");
            return;
        }
        item.Time = newTime;
    }

    Console.Write($"Opis ({item.Description}): ");
    var desc = Console.ReadLine();
    if (desc is not null)
        item.Description = desc.Trim();

    store.Update(item);
    Console.WriteLine("Zapisano zmiany.");
}

static void Delete(TaskStore store)
{
    Console.Write("Podaj słowo / fragment tytułu do wyszukania: ");
    var query = (Console.ReadLine() ?? "").Trim();

    if (string.IsNullOrWhiteSpace(query))
    {
        Console.WriteLine("Nic nie wpisano.");
        return;
    }

    var matches = store.FindByTitle(query);

    if (matches.Count == 0)
    {
        Console.WriteLine($"Nie znaleziono zadań pasujących do: \"{query}\"");
        return;
    }

    if (matches.Count == 1)
    {
        var t = matches[0];

        var when = t.Time is null
            ? $"{t.Date:yyyy-MM-dd}"
            : $"{t.Date:yyyy-MM-dd} {t.Time:HH:mm}";

        Console.WriteLine("\nZnaleziono jedno zadanie:");
        Console.WriteLine($"• {when} | {t.Title}");

        Console.Write("Czy na pewno chcesz je usunąć? (t/n): ");
        var confirm = (Console.ReadLine() ?? "").Trim().ToLower();

        if (confirm != "t")
        {
            Console.WriteLine("Anulowano.");
            return;
        }

        store.Delete(t);
        Console.WriteLine($"Usunięto: {t.Title}");
        return;
    }

    Console.WriteLine("\nZnaleziono kilka zadań:");
    for (int i = 0; i < matches.Count; i++)
    {
        var t = matches[i];

        var when = t.Time is null
            ? $"{t.Date:yyyy-MM-dd}"
            : $"{t.Date:yyyy-MM-dd} {t.Time:HH:mm}";

        var status =
            t.IsDone ? "[DONE]" :
            "[TODO]";
        Console.WriteLine($"{i + 1}) {status} {when} | {t.Title}");
    }

    Console.Write("Wybierz numer do usunięcia (0 = anuluj): ");
    if (!int.TryParse(Console.ReadLine(), out int choice))
    {
        Console.WriteLine("Nieprawidłowy wybór.");
        return;
    }

    if (choice == 0)
    {
        Console.WriteLine("Anulowano.");
        return;
    }

    if (choice < 1 || choice > matches.Count)
    {
        Console.WriteLine("Numer spoza zakresu.");
        return;
    }

    var toRemove = matches[choice - 1];
    store.Delete(toRemove);
    Console.WriteLine($"Usunięto: {toRemove.Title}");
}



static void Print(List<TaskItem> items, string header)
{
    Console.WriteLine($"\n-- {header} --");
    if (items.Count == 0)
    {
        Console.WriteLine("(brak)");
        return;
    }

    var now = DateTime.Now;
    var today = DateOnly.FromDateTime(now);
    var currentTime = TimeOnly.FromDateTime(now);

    foreach (var t in items)
    {
        var overdue = !t.IsDone &&
            (t.Date < today || (t.Date == today && t.Time is not null && t.Time < currentTime));

        var status = t.IsDone ? "[DONE]" : overdue ? "[OVERDUE]" : "[TODO]";
        var when = t.Time is null
            ? $"{t.Date:yyyy-MM-dd}"
            : $"{t.Date:yyyy-MM-dd} {t.Time:HH:mm}";

        Console.WriteLine($"{status} {when} | {t.Title}");
        Console.WriteLine($"   ID: {t.Id}");
        if (!string.IsNullOrWhiteSpace(t.Description))
            Console.WriteLine($"   {t.Description}");
    }
}

static string ReadNonEmpty(string prompt)
{
    while (true)
    {
        Console.Write(prompt);
        var text = (Console.ReadLine() ?? "").Trim();
        if (!string.IsNullOrWhiteSpace(text)) return text;
        Console.WriteLine("Pole nie może być puste.");
    }
}

static DateOnly ReadDate(string prompt)
{
    while (true)
    {
        Console.Write(prompt);
        if (DateOnly.TryParse(Console.ReadLine(), out var date))
            return date;
        Console.WriteLine("Zła data. Format: YYYY-MM-DD");
    }
}

static TimeOnly? ReadOptionalTime(string? input)
{
    if (string.IsNullOrWhiteSpace(input))
        return null;

    input = input.Trim();
    input = input.Replace(' ', ':'); 

    if (!TimeOnly.TryParse(input, out var time))
    {
        Console.WriteLine("Zła godzina. Użyj formatu HH:mm lub HH mm");
        return null;
    }

    return time;
}


static Guid ReadGuid(string prompt)
{
    while (true)
    {
        Console.Write(prompt);
        if (Guid.TryParse(Console.ReadLine(), out var id))
            return id;
        Console.WriteLine("Zły GUID.");
    }
}

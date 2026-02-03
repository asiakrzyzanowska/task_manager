using System.Text.Json;
using p1.Models;

namespace p1.Storage;

public class TaskStore
{
    private readonly string _path;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true
    };

    private List<TaskItem> _items;

    public TaskStore(string path)
    {
        _path = path;

        _jsonOptions.Converters.Add(new DateOnlyJsonConverter());
        _jsonOptions.Converters.Add(new TimeOnlyJsonConverter());

        _items = Load();
    }

    public List<TaskItem> GetAll() => _items.ToList();

    public TaskItem? GetById(Guid id)
        => _items.FirstOrDefault(x => x.Id == id);

    // ✅ NOWE: wyszukiwanie po fragmencie tytułu (ignoruje wielkość liter)
    public List<TaskItem> FindByTitle(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<TaskItem>();

        return _items
            .Where(x => x.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public void Add(TaskItem item)
    {
        _items.Add(item);
        Save();
    }

    public bool ToggleDone(Guid id)
    {
        var t = _items.FirstOrDefault(x => x.Id == id);
        if (t is null) return false;

        t.IsDone = !t.IsDone;
        Save();
        return true;
    }

    public bool Update(TaskItem updated)
    {
        var index = _items.FindIndex(x => x.Id == updated.Id);
        if (index < 0) return false;

        _items[index] = updated;
        Save();
        return true;
    }

    public bool Delete(Guid id)
    {
        var removed = _items.RemoveAll(x => x.Id == id) > 0;
        if (removed) Save();
        return removed;
    }

    // ✅ NOWE: usuwanie po obiekcie (przydatne gdy wybierasz z listy wyników)
    public bool Delete(TaskItem item)
    {
        var removed = _items.Remove(item);
        if (removed) Save();
        return removed;
    }

    private List<TaskItem> Load()
    {
        if (!File.Exists(_path))
            return new List<TaskItem>();

        try
        {
            var json = File.ReadAllText(_path);
            return JsonSerializer.Deserialize<List<TaskItem>>(json, _jsonOptions)
                   ?? new List<TaskItem>();
        }
        catch
        {
            return new List<TaskItem>();
        }
    }

    private void Save()
    {
        var json = JsonSerializer.Serialize(_items, _jsonOptions);
        File.WriteAllText(_path, json);
    }
}

using ArchStudio.Models;

namespace ArchStudio.Services;

public class AppState
{
    public string? WorkspacePath { get; private set; }
    public string? RootNamespace { get; private set; }
    public List<EntityModel> Entities { get; } = new();
    public EntityModel? ActiveEntity { get; private set; }

    public event Action? OnStateChanged;

    public void SetWorkspace(string path, string rootNamespace)
    {
        WorkspacePath = path;
        RootNamespace = rootNamespace;
        NotifyStateChanged();
    }

    public void AddEntity()
    {
        var entity = new EntityModel();
        Entities.Add(entity);
        ActiveEntity = entity;
        NotifyStateChanged();
    }

    public void LoadExistingEntities(IEnumerable<EntityModel> existingEntities)
    {
        Entities.Clear();
        Entities.AddRange(existingEntities);
        ActiveEntity = Entities.FirstOrDefault();
        NotifyStateChanged();
    }

    public void SetActiveEntity(EntityModel entity)
    {
        ActiveEntity = entity;
        NotifyStateChanged();
    }

    public void RemoveEntity(EntityModel entity)
    {
        Entities.Remove(entity);
        if (ActiveEntity == entity)
            ActiveEntity = Entities.FirstOrDefault();
        NotifyStateChanged();
    }

    public void NotifyStateChanged() => OnStateChanged?.Invoke();
}

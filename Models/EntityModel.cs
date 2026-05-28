namespace ArchStudio.Models;

public class EntityModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EntityName { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public string DbSchema { get; set; } = "dbo";

    public bool UseBaseEntity { get; set; } = true;
    public string BaseClassName { get; set; } = "BaseAuditableEntity";
    public string BaseClassNamespace { get; set; } = "Domain.Common";
    public string Interfaces { get; set; } = "ISoftDeletable";
    public List<string> Usings { get; set; } = new();

    public List<PropertyModel> Properties { get; set; } = new();
    
    public IEnumerable<PropertyModel> DtoProperties => Properties.Where(p => !p.IsAuditableProperty);
    
    // Auto-derived
    public string EntityNamePlural => string.IsNullOrWhiteSpace(EntityName)
        ? string.Empty
        : EntityName.EndsWith("y", StringComparison.OrdinalIgnoreCase)
            ? EntityName[..^1] + "ies"
            : EntityName.EndsWith("s", StringComparison.OrdinalIgnoreCase)
                ? EntityName + "es"
                : EntityName + "s";

    public string EntityNameCamelCase => string.IsNullOrWhiteSpace(EntityName)
        ? string.Empty
        : char.ToLower(EntityName[0]) + EntityName[1..];

    public bool HasCustomTypes => Properties.Any(p => !PropertyModel.SupportedTypes.Contains(p.DataType));

    public void SyncTableName()
    {
        if (string.IsNullOrWhiteSpace(TableName))
            TableName = string.Empty; // keep auto-derived via placeholder
    }
}

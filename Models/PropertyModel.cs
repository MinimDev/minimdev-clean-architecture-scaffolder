namespace ArchStudio.Models;

public class PropertyModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string DataType { get; set; } = "string";
    public bool IsNullable { get; set; } = false;
    public bool IsPrimaryKey { get; set; } = false;
    public bool IsRequired { get; set; } = true;
    public bool HasPublicSetter { get; set; } = false;
    public int? MaxLength { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }
    
    public bool IsNavigationProperty { get; set; } = false;
    public string RelationshipType { get; set; } = "None"; // None, OneToMany, ManyToOne, OneToOne, ManyToMany
    public string ForeignKeyType { get; set; } = "Guid";
    public string ForeignKeyName { get; set; } = string.Empty;
    public string PrincipalKeyName { get; set; } = string.Empty;
    
    public string ActualForeignKeyName => string.IsNullOrWhiteSpace(ForeignKeyName) ? Name + "Id" : ForeignKeyName;
    
    public bool IsAuditableProperty => Name is "IsDeleted" or "DeletedAt" or "DeletedBy" or "Created" or "CreatedBy" or "LastModified" or "LastModifiedBy";

    public string NameCamelCase => string.IsNullOrWhiteSpace(Name) 
        ? string.Empty 
        : char.ToLower(Name[0]) + Name[1..];

    public static readonly List<string> SupportedTypes = new()
    {
        "string", "int", "long", "decimal", "double", "float",
        "bool", "DateTime", "DateOnly", "TimeOnly", "Guid",
        "byte[]", "short"
    };
}

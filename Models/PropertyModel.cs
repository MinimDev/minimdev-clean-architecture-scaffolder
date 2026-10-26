namespace ArchStudio.Models;

public class PropertyModel
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;

    private string _dataType = "string";
    public string DataType
    {
        get => _dataType;
        set
        {
            _dataType = value ?? string.Empty;
            OnDataTypeChanged();
        }
    }

    public bool IsNullable { get; set; } = false;
    public bool IsPrimaryKey { get; set; } = false;
    public bool IsRequired { get; set; } = true;
    public bool HasPublicSetter { get; set; } = false;
    public int? MaxLength { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }

    private bool _isNavigationProperty = false;
    public bool IsNavigationProperty
    {
        get => !IsPrimitiveType && _isNavigationProperty;
        set
        {
            if (IsPrimitiveType)
            {
                _isNavigationProperty = false;
                RelationshipType = "None";
            }
            else
            {
                _isNavigationProperty = value;
                if (!value)
                {
                    RelationshipType = "None";
                }
                else if (RelationshipType == "None")
                {
                    RelationshipType = DataType.Contains("<") ? "OneToMany" : "ManyToOne";
                }
            }
        }
    }

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

    public bool IsPrimitiveType
    {
        get
        {
            var clean = (DataType ?? string.Empty).Trim().TrimEnd('?');
            return SupportedTypes.Any(t => t.Equals(clean, StringComparison.OrdinalIgnoreCase)) ||
                   clean.Equals("Int16", StringComparison.OrdinalIgnoreCase) ||
                   clean.Equals("Int32", StringComparison.OrdinalIgnoreCase) ||
                   clean.Equals("Int64", StringComparison.OrdinalIgnoreCase) ||
                   clean.Equals("Boolean", StringComparison.OrdinalIgnoreCase) ||
                   clean.Equals("Single", StringComparison.OrdinalIgnoreCase) ||
                   clean.Equals("Byte", StringComparison.OrdinalIgnoreCase);
        }
    }

    public void OnDataTypeChanged()
    {
        if (IsPrimitiveType)
        {
            _isNavigationProperty = false;
            RelationshipType = "None";
        }
        else if (DataType.StartsWith("ICollection<", StringComparison.OrdinalIgnoreCase) ||
                 DataType.StartsWith("IList<", StringComparison.OrdinalIgnoreCase) ||
                 DataType.StartsWith("List<", StringComparison.OrdinalIgnoreCase) ||
                 DataType.StartsWith("IEnumerable<", StringComparison.OrdinalIgnoreCase))
        {
            _isNavigationProperty = true;
            RelationshipType = "OneToMany";
        }
        else if (!string.IsNullOrWhiteSpace(DataType) && char.IsUpper(DataType[0]))
        {
            _isNavigationProperty = true;
            RelationshipType = "ManyToOne";
        }
    }
}

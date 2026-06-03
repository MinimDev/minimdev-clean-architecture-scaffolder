using ArchStudio.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ArchStudio.Services;

public class EntityParserService
{
    public EntityModel? ParseEntity(string fileContent)
    {
        try
        {
            var tree = CSharpSyntaxTree.ParseText(fileContent);
            var root = tree.GetCompilationUnitRoot();

            var classDeclaration = root.DescendantNodes().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            if (classDeclaration == null) return null;

            string tableName = string.Empty;
            string dbSchema = "dbo";

            var tableAttr = classDeclaration.AttributeLists
                .SelectMany(a => a.Attributes)
                .FirstOrDefault(a => a.Name.ToString() == "Table" || a.Name.ToString() == "TableAttribute");
            
            if (tableAttr != null && tableAttr.ArgumentList != null && tableAttr.ArgumentList.Arguments.Count > 0)
            {
                var firstArg = tableAttr.ArgumentList.Arguments[0].Expression;
                if (firstArg is LiteralExpressionSyntax literal)
                {
                    tableName = literal.Token.ValueText;
                }
                
                var schemaArg = tableAttr.ArgumentList.Arguments.FirstOrDefault(a => a.NameEquals?.Name.Identifier.Text == "Schema");
                if (schemaArg != null && schemaArg.Expression is LiteralExpressionSyntax schemaLiteral)
                {
                    dbSchema = schemaLiteral.Token.ValueText;
                }
            }

            var entity = new EntityModel
            {
                EntityName = classDeclaration.Identifier.Text,
                TableName = tableName,
                DbSchema = dbSchema
            };
            entity.SyncTableName();

            // Ekstrak Usings
            var usings = root.DescendantNodes().OfType<UsingDirectiveSyntax>()
                .Select(u => u.Name?.ToString() ?? "")
                .Where(u => !string.IsNullOrEmpty(u))
                .ToList();
            entity.Usings = usings;

            // Ekstrak Base Class & Interfaces
            if (classDeclaration.BaseList != null && classDeclaration.BaseList.Types.Any())
            {
                var types = classDeclaration.BaseList.Types.Select(t => t.Type.ToString()).ToList();
                
                // Asumsi: item pertama adalah base class, sisanya adalah interface
                // (Ini heuristic sederhana, bisa saja item pertama adalah interface jika tidak ada base class, 
                // tapi karena kita punya flag UseBaseEntity, kita asumsikan yang pertama adalah class jika tidak dimulai dengan 'I')
                var firstType = types.First();
                if (firstType.StartsWith("I") && firstType.Length > 1 && char.IsUpper(firstType[1]))
                {
                    // Item pertama sepertinya interface
                    entity.UseBaseEntity = false;
                    entity.BaseClassName = string.Empty;
                    entity.Interfaces = string.Join(", ", types);
                }
                else
                {
                    // Item pertama adalah base class
                    entity.UseBaseEntity = true;
                    entity.BaseClassName = firstType;
                    if (types.Count > 1)
                    {
                        entity.Interfaces = string.Join(", ", types.Skip(1));
                    }
                }
            }
            else
            {
                entity.UseBaseEntity = false;
                entity.BaseClassName = string.Empty;
                entity.Interfaces = string.Empty;
            }

            // Cari semua properti
            var properties = classDeclaration.Members.OfType<PropertyDeclarationSyntax>();
            foreach (var prop in properties)
            {
                var typeString = prop.Type.ToString();
                var isNullable = typeString.EndsWith("?");
                var cleanType = isNullable ? typeString[..^1] : typeString;

                var isId = prop.Identifier.Text.Equals("Id", StringComparison.OrdinalIgnoreCase);

                // Cek apakah punya public setter
                var hasPublicSetter = prop.AccessorList?.Accessors.Any(a => 
                    a.Keyword.Text == "set" && 
                    !a.Modifiers.Any(m => m.Text == "private" || m.Text == "protected" || m.Text == "internal")
                ) ?? false;

                var propertyModel = new PropertyModel
                {
                    Name = prop.Identifier.Text,
                    DataType = cleanType,
                    IsNullable = isNullable,
                    IsPrimaryKey = isId,
                    HasPublicSetter = hasPublicSetter
                };

                // Normalisasi nama tipe agar match dengan dropdown (misal "String" -> "string")
                propertyModel.DataType = propertyModel.DataType switch
                {
                    "String" => "string",
                    "Int32" => "int",
                    "Int64" => "long",
                    "Boolean" => "bool",
                    "Decimal" => "decimal",
                    "Double" => "double",
                    "Single" => "float",
                    "Int16" => "short",
                    _ => propertyModel.DataType
                };

                if (propertyModel.DataType.StartsWith("ICollection<") || propertyModel.DataType.StartsWith("IList<") || propertyModel.DataType.StartsWith("List<") || propertyModel.DataType.StartsWith("IEnumerable<"))
                {
                    propertyModel.IsNavigationProperty = true;
                    propertyModel.RelationshipType = "OneToMany";
                }
                else if (!PropertyModel.SupportedTypes.Contains(propertyModel.DataType))
                {
                    // Jika bukan tipe standar yang didukung, bisa jadi enum atau relasi ManyToOne/OneToOne.
                    // Secara default kita biarkan user menyesuaikan via UI nanti, tapi kita tandai jika bentuknya PascalCase.
                    if (!string.IsNullOrEmpty(propertyModel.DataType) && char.IsUpper(propertyModel.DataType[0]))
                    {
                        // Anggap sebagai relasi ManyToOne by default (bisa diubah di UI)
                        propertyModel.IsNavigationProperty = true;
                        propertyModel.RelationshipType = "ManyToOne";
                    }
                    PropertyModel.SupportedTypes.Add(propertyModel.DataType);
                }

                entity.Properties.Add(propertyModel);
            }

            return entity;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing entity: {ex.Message}");
            return null;
        }
    }
}

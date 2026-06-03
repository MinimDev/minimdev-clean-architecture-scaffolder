using Scriban;
using Scriban.Runtime;
using ArchStudio.Models;

namespace ArchStudio.Services;

public class TemplateEngine
{
    private readonly string _templateBasePath;

    public TemplateEngine()
    {
        _templateBasePath = Path.Combine(AppContext.BaseDirectory, "Templates");
    }

    public async Task<string> RenderAsync(string templateRelativePath, EntityModel entity, string rootNamespace, ScaffoldOptions options)
    {
        var fullPath = Path.Combine(_templateBasePath, templateRelativePath);
        var templateText = await File.ReadAllTextAsync(fullPath);

        var template = Template.Parse(templateText);
        if (template.HasErrors)
            throw new InvalidOperationException($"Template error in '{templateRelativePath}': {string.Join(", ", template.Messages)}");

        var scriptObject = new ScriptObject();
        scriptObject.Add("entity_name", entity.EntityName);
        scriptObject.Add("entity_name_plural", entity.EntityNamePlural);
        scriptObject.Add("entity_name_camel", entity.EntityNameCamelCase);
        scriptObject.Add("root_namespace", rootNamespace);
        scriptObject.Add("table_name", string.IsNullOrWhiteSpace(entity.TableName)
            ? entity.EntityNamePlural
            : entity.TableName);
        scriptObject.Add("db_schema", entity.DbSchema);
        scriptObject.Add("properties", entity.Properties);
        scriptObject.Add("dto_properties", entity.DtoProperties);
        scriptObject.Add("entity_properties", entity.EntityProperties);
        scriptObject.Add("fk_properties", entity.FkProperties);
        scriptObject.Add("collection_fk_properties", entity.CollectionFkProperties);

        // Options Support
        scriptObject.Add("require_authorization", options.RequireAuthorization);

        // Base Entity Support
        scriptObject.Add("use_base_entity", entity.UseBaseEntity);
        scriptObject.Add("base_class_name", entity.BaseClassName ?? "");
        scriptObject.Add("base_class_namespace", entity.BaseClassNamespace ?? "");
        scriptObject.Add("interfaces", entity.Interfaces ?? "");
        scriptObject.Add("usings", entity.Usings);
        scriptObject.Add("has_custom_types", entity.HasCustomTypes);
        scriptObject.Add("opts", options);

        var context = new TemplateContext();
        context.PushGlobal(scriptObject);

        return await template.RenderAsync(context);
    }
}

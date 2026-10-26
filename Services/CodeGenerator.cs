using ArchStudio.Models;

namespace ArchStudio.Services;

public record ScaffoldResult(string RelativePath, bool Success, string? Error = null);

public class CodeGenerator
{
    private readonly TemplateEngine _engine;
    private readonly FileManager _fileManager;

    public CodeGenerator(TemplateEngine engine, FileManager fileManager)
    {
        _engine = engine;
        _fileManager = fileManager;
    }

    public async IAsyncEnumerable<ScaffoldResult> ScaffoldAsync(
        string workspacePath,
        string rootNamespace,
        EntityModel entity,
        ScaffoldOptions options,
        bool overwriteExisting = false)
    {
        var tasks = BuildTaskList(entity, rootNamespace, options);

        foreach (var (templatePath, outputRelativePath) in tasks)
        {
            if (!overwriteExisting && _fileManager.FileExists(workspacePath, outputRelativePath))
            {
                yield return new ScaffoldResult(outputRelativePath, false, "File already exists (skipped)");
                continue;
            }

            string content;
            string? templateError = null;
            try
            {
                content = await _engine.RenderAsync(templatePath, entity, rootNamespace, options);
            }
            catch (Exception ex)
            {
                templateError = ex.Message;
                content = string.Empty;
            }

            if (templateError != null)
            {
                yield return new ScaffoldResult(outputRelativePath, false, $"Template error: {templateError}");
                continue;
            }

            string? writeError = null;
            try
            {
                await _fileManager.WriteFileAsync(workspacePath, outputRelativePath, content);
            }
            catch (Exception ex)
            {
                writeError = ex.Message;
            }

            if (writeError != null)
            {
                yield return new ScaffoldResult(outputRelativePath, false, $"Write error: {writeError}");
            }
            else
            {
                yield return new ScaffoldResult(outputRelativePath, true);
            }
        }

        if (options.GeneratePermissions)
        {
            var permResult = await InjectPermissionsAsync(workspacePath, rootNamespace, entity);
            yield return new ScaffoldResult("Permissions.cs", permResult.success, permResult.error);
        }

        if (options.InjectDbContext)
        {
            var ifaceResult = await InjectDbSetAsync(workspacePath, rootNamespace, entity, true);
            yield return new ScaffoldResult("IApplicationDbContext.cs", ifaceResult.success, ifaceResult.error);

            var classResult = await InjectDbSetAsync(workspacePath, rootNamespace, entity, false);
            yield return new ScaffoldResult("ApplicationDbContext.cs", classResult.success, classResult.error);
        }

        if (options.InjectWebUIDependencies)
        {
            var webUiDiResult = await InjectWebUIDependencyAsync(workspacePath, rootNamespace, entity);
            yield return new ScaffoldResult("WebUI/Program.cs (DI)", webUiDiResult.success, webUiDiResult.error);
        }

        if (options.InjectNavMenu)
        {
            var navResult = await InjectNavMenuAsync(workspacePath, rootNamespace, entity);
            yield return new ScaffoldResult("NavMenu.razor", navResult.success, navResult.error);
        }
    }

    private List<(string Template, string Output)> BuildTaskList(EntityModel e, string ns, ScaffoldOptions opts)
    {
        var list = new List<(string, string)>();
        var n = e.EntityName;
        var pl = e.EntityNamePlural;

        if (opts.GenerateDomainEntity)
            list.Add(("Domain/Entity.sbn",
                $"src/Core/{ns}.Domain/Entities/{n}.cs"));

        if (opts.GenerateDomainErrors)
            list.Add(("Domain/DomainErrors.sbn",
                $"src/Core/{ns}.Domain/Errors/{n}Errors.cs"));

        if (opts.GenerateDomainRepository)
            list.Add(("Domain/IRepository.sbn",
                $"src/Core/{ns}.Application/Common/Interfaces/I{n}Repository.cs"));

        if (opts.GenerateCreateCommand)
        {
            list.Add(("Application/CreateCommand.sbn",
                $"src/Core/{ns}.Application/Features/{pl}/Commands/Create{n}/Create{n}Command.cs"));
            list.Add(("Application/CreateCommandValidator.sbn",
                $"src/Core/{ns}.Application/Features/{pl}/Commands/Create{n}/Create{n}CommandValidator.cs"));
            list.Add(("Application/CreateCommandHandler.sbn",
                $"src/Core/{ns}.Application/Features/{pl}/Commands/Create{n}/Create{n}CommandHandler.cs"));
        }

        if (opts.GenerateUpdateCommand)
        {
            list.Add(("Application/UpdateCommand.sbn",
                $"src/Core/{ns}.Application/Features/{pl}/Commands/Update{n}/Update{n}Command.cs"));
            list.Add(("Application/UpdateCommandValidator.sbn",
                $"src/Core/{ns}.Application/Features/{pl}/Commands/Update{n}/Update{n}CommandValidator.cs"));
            list.Add(("Application/UpdateCommandHandler.sbn",
                $"src/Core/{ns}.Application/Features/{pl}/Commands/Update{n}/Update{n}CommandHandler.cs"));
        }

        if (opts.GenerateDeleteCommand)
        {
            list.Add(("Application/DeleteCommand.sbn",
                $"src/Core/{ns}.Application/Features/{pl}/Commands/Delete{n}/Delete{n}Command.cs"));
            list.Add(("Application/DeleteCommandHandler.sbn",
                $"src/Core/{ns}.Application/Features/{pl}/Commands/Delete{n}/Delete{n}CommandHandler.cs"));
        }

        if (opts.GenerateGetAllQuery)
        {
            list.Add(("Application/GetAllQuery.sbn",
                $"src/Core/{ns}.Application/Features/{pl}/Queries/GetAll{pl}/GetAll{pl}Query.cs"));
            list.Add(("Application/GetAllQueryHandler.sbn",
                $"src/Core/{ns}.Application/Features/{pl}/Queries/GetAll{pl}/GetAll{pl}QueryHandler.cs"));
        }

        if (opts.GenerateGetByIdQuery)
        {
            list.Add(("Application/GetByIdQuery.sbn",
                $"src/Core/{ns}.Application/Features/{pl}/Queries/Get{n}ById/Get{n}ByIdQuery.cs"));
            list.Add(("Application/GetByIdQueryHandler.sbn",
                $"src/Core/{ns}.Application/Features/{pl}/Queries/Get{n}ById/Get{n}ByIdQueryHandler.cs"));
        }

        if (opts.GenerateMappingConfig)
            list.Add(("Application/MappingConfig.sbn",
                $"src/Core/{ns}.Application/Features/{pl}/Mappings/{n}MappingConfig.cs"));

        if (opts.GeneratePersistenceConfig)
            list.Add(("Persistence/EfConfiguration.sbn",
                $"src/Infrastructure/{ns}.Infrastructure.Persistence/Configurations/{n}Configuration.cs"));

        if (opts.GeneratePersistenceRepository)
            list.Add(("Persistence/Repository.sbn",
                $"src/Infrastructure/{ns}.Infrastructure.Persistence/Repositories/{n}Repository.cs"));

        if (opts.GenerateController)
            list.Add(("WebAPI/Controller.sbn",
                $"src/Presentation/{ns}.WebAPI/Controllers/{pl}Controller.cs"));

        if (opts.GenerateWebUIModels)
        {
            list.Add(("WebUI/Dto.sbn",
                $"src/Presentation/{ns}.WebUI/Services/Models/{n}Dto.cs"));
            list.Add(("WebUI/CreateCommandModel.sbn",
                $"src/Presentation/{ns}.WebUI/Services/Models/Create{n}Command.cs"));
            list.Add(("WebUI/UpdateDtoModel.sbn",
                $"src/Presentation/{ns}.WebUI/Services/Models/Update{n}Dto.cs"));
        }

        if (opts.GenerateWebUIClient)
        {
            list.Add(("WebUI/ClientService.sbn",
                $"src/Presentation/{ns}.WebUI/Services/{pl}Client.cs"));
        }

        if (opts.GenerateWebUIPages)
        {
            list.Add(("WebUI/PageList.sbn",
                $"src/Presentation/{ns}.WebUI/Components/Pages/{pl}/{pl}.razor"));
            list.Add(("WebUI/CreateDialog.sbn",
                $"src/Presentation/{ns}.WebUI/Components/Pages/{pl}/Create{n}Dialog.razor"));
            list.Add(("WebUI/EditDialog.sbn",
                $"src/Presentation/{ns}.WebUI/Components/Pages/{pl}/Edit{n}Dialog.razor"));
            list.Add(("WebUI/DetailDialog.sbn",
                $"src/Presentation/{ns}.WebUI/Components/Pages/{pl}/{n}DetailDialog.razor"));
        }

        return list;
    }

    private async Task<(bool success, string? error)> InjectDbSetAsync(string workspacePath, string rootNamespace, EntityModel entity, bool isInterface)
    {
        string fileName = isInterface ? "IApplicationDbContext.cs" : "ApplicationDbContext.cs";
        var targetFiles = Directory.GetFiles(workspacePath, fileName, SearchOption.AllDirectories);
        if (targetFiles.Length == 0) return (false, $"File {fileName} not found");
        
        string filePath = targetFiles.FirstOrDefault(f => isInterface ? f.Contains(".Application") : f.Contains(".Persistence")) ?? targetFiles[0];
        string sourceCode = await File.ReadAllTextAsync(filePath);
        
        var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(sourceCode);
        var root = await tree.GetRootAsync();
        
        var typeDecl = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax>()
            .FirstOrDefault(t => t.Identifier.Text == (isInterface ? "IApplicationDbContext" : "ApplicationDbContext"));
            
        if (typeDecl == null) return (false, $"Type declaration not found in {fileName}");
        
        bool propertyExists = typeDecl.Members.OfType<Microsoft.CodeAnalysis.CSharp.Syntax.PropertyDeclarationSyntax>()
            .Any(p => p.Identifier.Text == entity.EntityNamePlural);
            
        string newSource = sourceCode;

        if (!propertyExists)
        {
            var lastProp = typeDecl.Members.OfType<Microsoft.CodeAnalysis.CSharp.Syntax.PropertyDeclarationSyntax>().LastOrDefault();
            int insertPosition = typeDecl.OpenBraceToken.Span.End;
            string indentation = "    ";
            if (lastProp != null)
            {
                insertPosition = lastProp.FullSpan.End;
                indentation = lastProp.GetLeadingTrivia().ToString().Trim('\r', '\n');
                if (string.IsNullOrEmpty(indentation)) indentation = "        ";
            }
            
            string propertyCode = isInterface
                ? $"\n{indentation}DbSet<{entity.EntityName}> {entity.EntityNamePlural} {{ get; }}"
                : $"\n{indentation}public DbSet<{entity.EntityName}> {entity.EntityNamePlural} => Set<{entity.EntityName}>();";
                
            newSource = newSource.Insert(insertPosition, propertyCode);
        }

        if (!isInterface && entity.UseBaseEntity)
        {
            var newTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(newSource);
            var newRoot = await newTree.GetRootAsync();
            var newTypeDecl = newRoot.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax>()
                .FirstOrDefault(t => t.Identifier.Text == "ApplicationDbContext");

            if (newTypeDecl != null)
            {
                var onModelCreating = newTypeDecl.Members.OfType<Microsoft.CodeAnalysis.CSharp.Syntax.MethodDeclarationSyntax>()
                    .FirstOrDefault(m => m.Identifier.Text == "OnModelCreating");

                if (onModelCreating != null && onModelCreating.Body != null)
                {
                    string filterCode = $"modelBuilder.Entity<{entity.EntityName}>().HasQueryFilter(p => !p.IsDeleted);";
                    if (!onModelCreating.Body.ToFullString().Contains(filterCode))
                    {
                        var baseCall = onModelCreating.Body.Statements.OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ExpressionStatementSyntax>()
                            .FirstOrDefault(s => s.Expression is Microsoft.CodeAnalysis.CSharp.Syntax.InvocationExpressionSyntax inv && 
                                                 inv.Expression.ToString() == "base.OnModelCreating");

                        string indentation = baseCall != null ? baseCall.GetLeadingTrivia().ToString() : "        ";
                        int filterInsertPos = baseCall != null ? baseCall.FullSpan.Start : onModelCreating.Body.CloseBraceToken.Span.Start;
                        
                        newSource = newSource.Insert(filterInsertPos, $"{indentation}{filterCode}\n");
                    }
                }
            }
        }

        if (newSource != sourceCode)
        {
            await File.WriteAllTextAsync(filePath, newSource);
            return (true, null);
        }

        return (true, "Already exists (skipped)");
    }

    private async Task<(bool success, string? error)> InjectPermissionsAsync(string workspacePath, string rootNamespace, EntityModel entity)
    {
        var pl = entity.EntityNamePlural;
        var relativePaths = new[]
        {
            $"src/Core/{rootNamespace}.Application/Common/Security/Permissions.cs",
            $"src/Presentation/{rootNamespace}.WebUI/Services/Models/Permissions.cs"
        };

        bool anyModified = false;

        foreach (var relPath in relativePaths)
        {
            var fullPath = Path.Combine(workspacePath, relPath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(fullPath)) continue;

            string source = await File.ReadAllTextAsync(fullPath);
            if (source.Contains($"class {pl}")) continue;

            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(source);
            var root = await tree.GetRootAsync();
            var classDecl = root.DescendantNodes().OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>()
                .FirstOrDefault(c => c.Identifier.Text == "Permissions");

            if (classDecl != null)
            {
                int insertPos = classDecl.OpenBraceToken.Span.End;
                string block = $"\n    public static class {pl}\n    {{\n        public const string View = \"Permissions.{pl}.View\";\n        public const string Create = \"Permissions.{pl}.Create\";\n        public const string Edit = \"Permissions.{pl}.Edit\";\n        public const string Delete = \"Permissions.{pl}.Delete\";\n    }}\n";
                
                var lastNestedClass = classDecl.Members.OfType<Microsoft.CodeAnalysis.CSharp.Syntax.ClassDeclarationSyntax>().LastOrDefault();
                if (lastNestedClass != null)
                {
                    insertPos = lastNestedClass.FullSpan.End;
                }

                string newSource = source.Insert(insertPos, block);
                await File.WriteAllTextAsync(fullPath, newSource);
                anyModified = true;
            }
        }

        return (true, anyModified ? null : "Permissions already exist or files not found (skipped)");
    }

    private async Task<(bool success, string? error)> InjectWebUIDependencyAsync(string workspacePath, string rootNamespace, EntityModel entity)
    {
        var pl = entity.EntityNamePlural;
        var relPath = $"src/Presentation/{rootNamespace}.WebUI/Program.cs";
        var fullPath = Path.Combine(workspacePath, relPath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath)) return (false, "WebUI/Program.cs not found");

        string source = await File.ReadAllTextAsync(fullPath);
        if (source.Contains($"AddScoped<{pl}Client>"))
        {
            return (true, $"{pl}Client already registered (skipped)");
        }

        int targetIndex = source.LastIndexOf("builder.Services.AddScoped<");
        if (targetIndex >= 0)
        {
            int lineEnd = source.IndexOf('\n', targetIndex);
            if (lineEnd >= 0)
            {
                string line = $"\nbuilder.Services.AddScoped<{pl}Client>();";
                string newSource = source.Insert(lineEnd, line);
                await File.WriteAllTextAsync(fullPath, newSource);
                return (true, null);
            }
        }

        return (false, "Could not find builder.Services insertion point in WebUI/Program.cs");
    }

    private async Task<(bool success, string? error)> InjectNavMenuAsync(string workspacePath, string rootNamespace, EntityModel entity)
    {
        var pl = entity.EntityNamePlural;
        var icon = string.IsNullOrWhiteSpace(entity.IconName) ? "Inventory2" : entity.IconName;
        var relPath = $"src/Presentation/{rootNamespace}.WebUI/Components/Layout/NavMenu.razor";
        var fullPath = Path.Combine(workspacePath, relPath.Replace('/', Path.DirectorySeparatorChar));
        if (!File.Exists(fullPath)) return (false, "NavMenu.razor not found");

        string source = await File.ReadAllTextAsync(fullPath);
        if (source.Contains($"Href=\"{pl.ToLower()}\"") || source.Contains($"Permissions.{pl}.View"))
        {
            return (true, "NavMenu item already exists (skipped)");
        }

        string navSnippet = $"\n        @if (TokenProvider.HasPermission(Permissions.{pl}.View))\n        {{\n            <MudNavLink Href=\"{pl.ToLower()}\" Match=\"NavLinkMatch.Prefix\" Icon=\"@Icons.Material.Rounded.{icon}\" Class=\"nav-item-modern\">\n                {pl}\n            </MudNavLink>\n        }}\n";

        int insertPos = -1;
        int productsNav = source.IndexOf("Permissions.Products.View");
        if (productsNav >= 0)
        {
            int closingBrace = source.IndexOf('}', productsNav);
            if (closingBrace >= 0) insertPos = closingBrace + 1;
        }

        if (insertPos < 0)
        {
            int mainSection = source.IndexOf("<!-- Main Navigation Section -->");
            if (mainSection >= 0)
            {
                int lineEnd = source.IndexOf('\n', mainSection);
                if (lineEnd >= 0) insertPos = lineEnd + 1;
            }
        }

        if (insertPos >= 0)
        {
            string newSource = source.Insert(insertPos, navSnippet);
            await File.WriteAllTextAsync(fullPath, newSource);
            return (true, null);
        }

        return (false, "Could not find suitable insertion point in NavMenu.razor");
    }
}

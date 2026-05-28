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

        if (options.InjectDbContext)
        {
            var ifaceResult = await InjectDbSetAsync(workspacePath, rootNamespace, entity, true);
            if (ifaceResult.success)
            {
                yield return new ScaffoldResult("IApplicationDbContext.cs", true, ifaceResult.error);
            }
            else
            {
                yield return new ScaffoldResult("IApplicationDbContext.cs", false, ifaceResult.error);
            }

            var classResult = await InjectDbSetAsync(workspacePath, rootNamespace, entity, false);
            if (classResult.success)
            {
                yield return new ScaffoldResult("ApplicationDbContext.cs", true, classResult.error);
            }
            else
            {
                yield return new ScaffoldResult("ApplicationDbContext.cs", false, classResult.error);
            }
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

        if (opts.GeneratePersistenceConfig)
            list.Add(("Persistence/EfConfiguration.sbn",
                $"src/Infrastructure/{ns}.Infrastructure.Persistence/Configurations/{n}Configuration.cs"));

        if (opts.GeneratePersistenceRepository)
            list.Add(("Persistence/Repository.sbn",
                $"src/Infrastructure/{ns}.Infrastructure.Persistence/Repositories/{n}Repository.cs"));

        if (opts.GenerateController)
            list.Add(("WebAPI/Controller.sbn",
                $"src/Presentation/{ns}.WebAPI/Controllers/{pl}Controller.cs"));

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
            if (lastProp != null)
            {
                insertPosition = lastProp.FullSpan.End;
            }
            
            string propertyCode = isInterface
                ? $"\n    DbSet<{entity.EntityName}> {entity.EntityNamePlural} {{ get; }}"
                : $"\n    public DbSet<{entity.EntityName}> {entity.EntityNamePlural} => Set<{entity.EntityName}>();";
                
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
}

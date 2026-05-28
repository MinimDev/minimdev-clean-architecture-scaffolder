using ArchStudio.Models;

namespace ArchStudio.Services;

public class FileManager
{
    public async Task WriteFileAsync(string workspacePath, string relativePath, string content)
    {
        var fullPath = Path.Combine(workspacePath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        var directory = Path.GetDirectoryName(fullPath)!;

        Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(fullPath, content, System.Text.Encoding.UTF8);
    }

    public bool FileExists(string workspacePath, string relativePath)
    {
        var fullPath = Path.Combine(workspacePath, relativePath.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(fullPath);
    }

    /// <summary>
    /// Scans the workspace root for a .sln or .csproj to detect the RootNamespace.
    /// Falls back to the folder name if none found.
    /// </summary>
    public async Task<string> DetectRootNamespaceAsync(string workspacePath)
    {
        // Look for .sln first to find the solution name
        var slnFiles = Directory.GetFiles(workspacePath, "*.sln", SearchOption.TopDirectoryOnly);
        if (slnFiles.Length > 0)
            return Path.GetFileNameWithoutExtension(slnFiles[0]);

        // Look inside src/ for the Domain .csproj
        var csprojFiles = Directory.GetFiles(workspacePath, "*.csproj", SearchOption.AllDirectories);
        var domainProj = csprojFiles.FirstOrDefault(f => f.Contains("Domain", StringComparison.OrdinalIgnoreCase));
        if (domainProj != null)
        {
            var content = await File.ReadAllTextAsync(domainProj);
            var match = System.Text.RegularExpressions.Regex.Match(content, @"<RootNamespace>(.*?)<\/RootNamespace>");
            if (match.Success) return match.Groups[1].Value.Split('.')[0];

            // Fallback: use project name prefix
            var projName = Path.GetFileNameWithoutExtension(domainProj);
            return projName.Split('.')[0];
        }

        return Path.GetFileName(workspacePath.TrimEnd(Path.DirectorySeparatorChar));
    }

    /// <summary>
    /// Reads all C# files inside the Domain/Entities directory of the workspace.
    /// </summary>
    public async Task<List<string>> GetEntityFilesAsync(string workspacePath, string rootNamespace)
    {
        var filesContent = new List<string>();
        // Construct the expected Domain/Entities path
        var entitiesDir = Path.Combine(workspacePath, "src", "Core", $"{rootNamespace}.Domain", "Entities");
        
        if (Directory.Exists(entitiesDir))
        {
            var files = Directory.GetFiles(entitiesDir, "*.cs", SearchOption.TopDirectoryOnly);
            foreach (var file in files)
            {
                var content = await File.ReadAllTextAsync(file, System.Text.Encoding.UTF8);
                filesContent.Add(content);
            }
        }
        else
        {
            // Fallback: search anywhere for an Entities folder in Domain
            var allEntitiesDirs = Directory.GetDirectories(workspacePath, "Entities", SearchOption.AllDirectories)
                .Where(d => d.Contains("Domain", StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var dir in allEntitiesDirs)
            {
                var files = Directory.GetFiles(dir, "*.cs", SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    var content = await File.ReadAllTextAsync(file, System.Text.Encoding.UTF8);
                    filesContent.Add(content);
                }
            }
        }

        return filesContent;
    }
}

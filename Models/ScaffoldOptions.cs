namespace ArchStudio.Models;

public class ScaffoldOptions
{
    public bool GenerateDomainEntity { get; set; } = true;
    public bool GenerateDomainErrors { get; set; } = true;
    public bool GenerateDomainRepository { get; set; } = false;
    public bool GenerateCreateCommand { get; set; } = true;
    public bool GenerateUpdateCommand { get; set; } = true;
    public bool GenerateDeleteCommand { get; set; } = true;
    public bool GenerateGetAllQuery { get; set; } = true;
    public bool GenerateGetByIdQuery { get; set; } = true;
    public bool GenerateMappingConfig { get; set; } = true;
    public bool GeneratePermissions { get; set; } = true;
    public bool GeneratePersistenceConfig { get; set; } = true;
    public bool GeneratePersistenceRepository { get; set; } = false;
    public bool GenerateController { get; set; } = true;
    public bool RequireAuthorization { get; set; } = true;
    public bool InjectDbContext { get; set; } = true;

    // WebUI (MudBlazor)
    public bool GenerateWebUIClient { get; set; } = true;
    public bool GenerateWebUIModels { get; set; } = true;
    public bool GenerateWebUIPages { get; set; } = true;
    public bool InjectWebUIDependencies { get; set; } = true;
    public bool InjectNavMenu { get; set; } = false;
}

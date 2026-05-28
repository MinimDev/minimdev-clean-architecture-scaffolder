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
    public bool GeneratePersistenceConfig { get; set; } = true;
    public bool GeneratePersistenceRepository { get; set; } = false;
    public bool GenerateController { get; set; } = true;
    public bool RequireAuthorization { get; set; } = false;
    public bool InjectDbContext { get; set; } = false; // Fase 5
}

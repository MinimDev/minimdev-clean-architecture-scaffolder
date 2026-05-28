# MinimDev ArchStudio User Guide

Welcome to the **MinimDev Clean Architecture Scaffolder (ArchStudio)** user guide. This guide will walk you through the process of generating a fully functional CRUD API endpoint utilizing Clean Architecture principles.

## 📌 1. Target Project Setup
Before generating code, ArchStudio needs to know where to output the files. 

By default, ArchStudio expects a Clean Architecture solution structure containing the following projects:
- `Domain` layer
- `Application` layer
- `Infrastructure.Persistence` layer
- `WebAPI` layer

Make sure the path to your target `.NET` solution is accessible.

## 📌 2. Defining the Entity
1. Open the ArchStudio desktop application.
2. In the sidebar, select the **Entity Builder**.
3. **Entity Name:** Enter the singular name of your entity (e.g., `Supplier`, `Product`, `Customer`). The engine will automatically determine the plural form.
4. **Properties:** Add properties for your entity.
   - You **do not** need to manually add auditing fields (like `IsDeleted`, `CreatedAt`, etc.). If your entity inherits from `BaseAuditableEntity` or `ISoftDeletable`, the framework handles these fields automatically.
   - Any auditable field you add will be smartly filtered out of API Data Transfer Objects (DTOs) and CQRS Commands to keep your Swagger interface clean!

## 📌 3. Scaffolding Options
Before hitting "Run Scaffold", you can fine-tune exactly what the engine generates using the checkboxes:

- **Entity class:** Generates the Domain Entity file (`Domain/Entities/Entity.cs`).
- **Domain errors:** Generates standard domain exceptions/errors.
- **Repository interface:** Generates `IEntityRepository.cs` in the Application layer.
- **CQRS - Create command:** Generates `CreateCommand`, `CreateCommandHandler`, and FluentValidation rules.
- **CQRS - Update command:** Generates `UpdateCommand`, `UpdateCommandHandler`, and FluentValidation rules. **Note:** The `UpdateCommand` binds beautifully to DTOs in the controller to avoid leaking the `Id` property into the JSON body!
- **CQRS - Delete command:** Generates `DeleteCommand` and `DeleteCommandHandler` (supports Soft Delete if your entity implements `ISoftDeletable`).
- **CQRS - Get by id query:** Generates the query to fetch a single record.
- **CQRS - Get all query:** Generates a paginated query to list all records.
- **EF Core config:** Generates `IEntityTypeConfiguration` for the entity in the Persistence layer.
- **Repository class:** Generates the concrete repository implementation.
- **REST controller:** Generates an `[ApiController]` with endpoints pointing perfectly to the MediatR commands/queries.

**Crucial Option: "Overwrite existing files"**
Check this box if you are regenerating an entity (for example, if you added a new property to `Supplier`). If unchecked, existing files will be skipped.

## 📌 4. Running the Scaffolder
Once your entity is defined and options are checked:
1. Click the **Run Scaffold** button.
2. The engine will instantly render the code using Scriban templates and write them to your target project directory.
3. If the **"Auto-inject DbSet in DbContext"** option is enabled, ArchStudio will use Roslyn Syntax Trees to safely and automatically insert `public DbSet<Entity> Entities => Set<Entity>();` directly into your existing `ApplicationDbContext.cs`!

## 📌 5. Customizing Templates
ArchStudio is powered by **Scriban** templates. You have complete control over the generated code!

- Navigate to the `Templates/` folder in the ArchStudio source code.
- You can freely edit any `.sbn` file (e.g., `Templates/Application/CreateCommandHandler.sbn`).
- ArchStudio will immediately use your modified templates the next time you run a scaffold.

Happy coding and enjoy skipping hours of boilerplate work!

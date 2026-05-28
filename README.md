# MinimDev Clean Architecture Scaffolder

![.NET](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)
![MAUI Blazor](https://img.shields.io/badge/MAUI_Blazor-Hybrid-512BD4?logo=blazor)
![Clean Architecture](https://img.shields.io/badge/Architecture-Clean-brightgreen)
![CQRS](https://img.shields.io/badge/Pattern-CQRS-blue)

**MinimDev Clean Architecture Scaffolder (ArchStudio)** is a powerful, desktop-based GUI tool built with MAUI Blazor Hybrid. It is designed to automatically generate boilerplate code for .NET 10 Web APIs adhering to **Clean Architecture**, **CQRS (Command Query Responsibility Segregation)**, and the **Mediator pattern**.

This tool dramatically reduces development time by scaffolding Domain Entities, MediatR Commands/Queries, Repositories, Entity Framework Core Configurations, and REST API Controllers in seconds!

## 🚀 Features

- **Visual Entity Builder:** Easily define your entities and their properties via a beautiful, interactive desktop UI.
- **Full CQRS Generation:** Automatically generates:
  - `CreateCommand`, `UpdateCommand`, `DeleteCommand`
  - `GetByIdQuery`, `GetAllQuery`
  - Validation using FluentValidation
- **Clean Architecture Compliant:** Separates your code into Domain, Application, Infrastructure, and Presentation layers.
- **Auditing Support:** Automatically filters out auditable properties (like `IsDeleted`, `CreatedAt`) from DTOs and update commands to ensure clean API contracts.
- **Smart REST Controllers:** Generates `[ApiController]` controllers with integrated DTO mappings, avoiding Swagger bloat.
- **Scriban Templates:** Fully customizable code generation templates using the Scriban engine.
- **Automatic EF Core Integration:** Seamlessly injects new `DbSet` properties into your `ApplicationDbContext` via Roslyn syntax tree parsing.

## 🛠️ Technologies Used

- **UI Framework:** .NET MAUI Blazor Hybrid
- **Template Engine:** Scriban
- **Syntax Parsing:** Roslyn (Microsoft.CodeAnalysis.CSharp)
- **Target Architecture:** Clean Architecture, CQRS, MediatR, EF Core

## 📦 Getting Started

1. **Clone the repository:**
   ```bash
   git clone https://github.com/MinimDev/minimdev-clean-architecture-scaffolder.git
   ```
2. **Build the project:**
   Open the folder in your terminal and run:
   ```bash
   dotnet build
   ```
3. **Run ArchStudio:**
   ```bash
   dotnet run
   ```
4. Define your entity, configure scaffolding options, and hit **Run Scaffold**!

## 📖 Documentation

For detailed instructions on how to use the scaffolding engine, manage entities, and customize the output, please refer to the [User Guide](User_Guide.md).

## 🤝 Contributing

Contributions, issues, and feature requests are welcome! Feel free to check the issues page.

## 📝 License

This project is open-source and available under the MIT License.

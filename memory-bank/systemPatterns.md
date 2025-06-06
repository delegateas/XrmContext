# System Patterns

## Architecture Overview
- Domain-driven design: TableModel, ColumnModel, RelationshipModel as core abstractions.
- Schema name is the canonical identifier for all tables/entities and columns/attributes throughout the pipeline (metadata, domain models, codegen, templates, output).
- Strict separation of metadata fetching (DataverseMetadataFetcher) and code generation (CSharpProxyGenerator).
- Internal domain classes are the only means of communication between fetchers and generators.
- Template-driven code generation using Scriban for maintainability and flexibility.
- Output writer (FileSystemOutputWriter) abstracts file system operations.
- CLI orchestration: Program.cs implements a robust CLI using System.CommandLine for option parsing, Microsoft.Extensions.DependencyInjection for service wiring, and a handler that coordinates the fetch/generate/write pipeline. The CLI supports advanced options for authentication, filtering, output, templates, and more, and provides clear user feedback.
- **Modular structure:** All large methods are split into smaller, focused helpers. Each file contains only a single class, making the codebase easy to navigate and maintain.

## Key Technical Decisions
- All identifiers for tables/entities and columns/attributes are based on schema name (not logical name) in domain models, codegen, templates, and output, to ensure consistency and alignment with Dataverse conventions.
- Use of .NET 8 and Microsoft.PowerPlatform.Dataverse.Client for Dataverse connectivity.
- CLI orchestrates end-to-end workflow, supports solution filtering.
- Attribute filtering: only main attributes (`AttributeOf == null`) are included in codegen, preventing supporting/secondary attributes from being generated.
- Enum generation: optionset values are mapped to anonymous objects before passing to the Scriban template, ensuring compatibility and correct output.
- OwnerColumnModel and owner attribute handling are not needed due to attribute filtering.
- Output structure: partial C# classes, decorated with relevant attributes, with properties for each Dataverse column and relationship.
- **Test template location pattern:** Automated tests locate template files robustly by traversing up from AppContext.BaseDirectory to find the project root (identified by the solution file), ensuring tests work regardless of working directory.
- **Automated formatting validation:** Tests validate not only property generation but also output formatting (e.g., no double newlines after the last attribute), ensuring generated code is clean and maintainable.
- **Refactor for maintainability:** All large methods are now split into smaller, focused helpers. Each file contains only a single class.

## Critical Implementation Paths
- Metadata fetcher retrieves entity metadata, filters attributes, and maps to domain models.
- Code generator consumes domain models and renders C# code using Scriban templates.
- Output writer ensures all necessary directories exist before writing files.
- CLI parses arguments using System.CommandLine, sets up dependency injection for core services, and invokes a handler that orchestrates the fetch/generate/write pipeline and provides user feedback.
- **Refactored workflow:** Each major component (fetcher, generator, CLI) is modular, with many short methods and one class per file.

## Component Relationships
- CLI → DI → DataverseMetadataFetcher → TableModel/ColumnModel/RelationshipModel → CSharpProxyGenerator → FileSystemOutputWriter → Output files.
- CLI tool coordinates all components and provides entry point for user interaction.

## Code Style Preferences
- Prefer many shorter methods over one long method. Each method should have a single responsibility and be easy to test and maintain.
- One class per file: Each class (including all column model subclasses) must be defined in its own file.
- Domain models as records: All domain model types (including TableModel, RelationshipModel, and all column model types) must be implemented as C# records.
- **Refactor discipline:** All new code and refactors must maintain the modular, one-class-per-file, many-short-methods structure for maximum maintainability.

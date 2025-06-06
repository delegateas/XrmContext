# Tech Context

## Technologies Used
- .NET 8 (C#)
- Microsoft.PowerPlatform.Dataverse.Client for Dataverse connectivity and metadata retrieval
- Scriban for template-based code generation
- xUnit for unit testing
- System.CommandLine for CLI parsing
- Microsoft.Extensions.DependencyInjection/Hosting for orchestration and dependency injection
- Modular project structure: Core library, CLI tool, and test project

## Development Setup
- Solution structure: 
  - src/DataverseProxyGenerator.Core: domain models, metadata fetcher, code generator, output writer
  - src/DataverseProxyGenerator.Tool: CLI entry point and orchestration
  - tests/DataverseProxyGenerator.Tests: unit tests
- All code is written in C# using modern best practices (records, async/await, etc.)
- Memory Bank is maintained in Markdown for project context and documentation

## Technical Constraints
- All identifiers for tables/entities and columns/attributes are based on schema name (not logical name) in domain models, codegen, templates, and output, to ensure consistency and alignment with Dataverse conventions.
- Only main attributes are included in codegen by filtering on `AttributeOf == null` (prevents supporting/secondary attributes from being generated)
- Enum generation: optionset values are mapped to anonymous objects before passing to the Scriban template, ensuring compatibility and correct output
- OwnerColumnModel and owner attribute handling are not needed due to attribute filtering
- Output writer ensures all necessary directories exist before writing files

## Dependencies
- Microsoft.PowerPlatform.Dataverse.Client
- Scriban
- xUnit

## Tool Usage Patterns
- CLI exposes a wide range of options (authentication, filtering, output, templates, localization, etc.) using System.CommandLine, and wires up core services using Microsoft.Extensions.DependencyInjection.
- CLI handler orchestrates the fetch/generate/write pipeline, providing robust argument parsing, error handling, and user feedback.
- CLI accepts connection string, output directory, template path, and optional solution unique names
- Metadata fetcher retrieves and filters entity metadata, mapping to internal domain models
- Code generator consumes domain models and renders C# code using Scriban templates
- Output writer writes generated files to disk, creating directories as needed
- **Automated tests**: Test project includes robust tests for both property generation and output formatting (e.g., no double newlines after the last attribute). Tests locate template files by traversing up from AppContext.BaseDirectory to the project root, ensuring reliability regardless of working directory.

## Additional Notes
- All major attribute types and enums are now robustly supported
- All generated code, templates, and output files now use schema names for all identifiers.
- Enum generation is now reliable for all optionsets due to anonymous object mapping for Scriban compatibility
- Filtering on AttributeOf is essential to avoid generating supporting attributes
- Documentation is continuously updated in the memory bank to reflect project state and decisions
- **Template formatting fix:** ProxyClass.scriban-cs updated to avoid double newlines after the last attribute section, validated by automated tests.

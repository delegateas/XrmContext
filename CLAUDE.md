# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

DataverseProxyGenerator is a .NET 8.0 tool that generates strongly-typed C# proxy classes and interfaces from Microsoft Dataverse metadata. The solution consists of:

- **DataverseProxyGenerator.Core**: Core generation logic, templates, and domain models
- **DataverseProxyGenerator.Tool**: Console application for running the generator
- **DataverseProxyGenerator.Tests**: Unit tests using xUnit and Verify.NET

## Development Commands

### Build and Test
```bash
# Restore dependencies
dotnet restore

# Build (Release) - ALWAYS USE RELEASE as it treats warnings as errors
dotnet build --configuration Release

# Run tests (Release) - ALWAYS USE RELEASE
dotnet test --configuration Release

# Format code (verify no changes needed)
dotnet format --verify-no-changes

# Format code (apply changes)
dotnet format
```

### Running the Tool
The tool can be run directly or configured via `appsettings.json`:

```bash
# Run with command line arguments
dotnet run --project src/DataverseProxyGenerator.Tool -- --output "output" --entities "account,contact"

# Run using appsettings.json configuration
dotnet run --project src/DataverseProxyGenerator.Tool
```

## Architecture Overview

### Code Generation Workflow
1. **Metadata Fetching** (`DataverseMetadataFetcher`): Connects to Dataverse and retrieves table/column metadata
2. **Code Generation** (`CSharpProxyGenerator`): Uses Scriban templates to generate C# code from metadata
3. **Output Writing** (`FileSystemOutputWriter`): Writes generated files to the specified directory

### Template System
The project uses embedded Scriban templates located in `src/DataverseProxyGenerator.Core/Templates/`:
- `ProxyClass.scriban-cs`: Generates entity proxy classes
- `EnumOptionset.scriban-cs`: Generates option set enums
- `IntersectionInterface.scriban-cs`: Generates intersection interfaces
- `XrmClass.scriban-cs`: Generates the main Xrm context class
- Additional attribute and helper templates

### Generated Output Structure
```
output/
├── tables/           # Entity proxy classes
├── optionsets/       # Option set enums
├── intersections/    # Intersection interfaces
├── queries/          # Xrm context class
└── attributes/       # Metadata attributes
```

### Configuration
The tool supports configuration via:
- Command line arguments (override config file settings)
- `appsettings.json` with `XrmContext` section
- Environment variables

Key configuration options:
- `OutputDirectory`: Where to write generated files
- `Solutions`: Dataverse solutions to include
- `Entities`: Specific entities to generate
- `NamespaceSetting`: Generated code namespace
- `IntersectMapping`: Interface definitions for shared columns
- `LabelMapping`: Unicode character mappings

### Domain Models
Column types are modeled using a hierarchy in `src/DataverseProxyGenerator.Core/Domain/`:
- `ColumnModel` (base)
- `StringColumnModel`, `IntegerColumnModel`, `EnumColumnModel`, etc.
- `TableModel` contains collections of columns and relationships
- `RelationshipModel` represents entity relationships

### Code Quality
The project enforces strict code quality with multiple analyzers:
- StyleCop, SonarAnalyzer, Meziantou.Analyzer
- AsyncFixer, SecurityCodeScan
- Warnings treated as errors in Release builds
- Nullable reference types enabled

## Testing
Tests use xUnit with Verify.NET for snapshot testing. Test files are located in `tests/DataverseProxyGenerator.Tests/` and include verified output files (.verified.txt) that capture expected code generation results.
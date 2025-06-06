# Product Context

## Why This Project Exists
Integrating .NET applications with Microsoft Dataverse often requires developers to manually create and maintain proxy classes that represent Dataverse tables. This process is error-prone, time-consuming, and can quickly become unmanageable as the Dataverse schema evolves.

## Problems Solved
- Eliminates manual coding of entity classes for Dataverse tables.
- Reduces risk of mismatches between Dataverse schema and .NET code.
- Accelerates development by automating proxy class generation.
- Provides a single source of truth for Dataverse schema in .NET projects.
- Lays the groundwork for supporting advanced Dataverse features and attribute types.

## How It Should Work
- The tool connects securely to a Dataverse environment.
- It fetches metadata about tables, columns, and relationships.
- It generates C# classes that accurately reflect the Dataverse schema, following best practices. Each generated class is partial, decorated with relevant attributes, and includes properties for each Dataverse column and relationship. Properties use the appropriate .NET types for their Dataverse attribute types, and the structure is designed to be extensible for additional attribute types in the future.
- The process is repeatable and can be integrated into CI/CD pipelines or developer workflows.

## User Experience Goals
- Simple, intuitive CLI interface for .NET developers.
- Clear separation between metadata fetching and code generation, allowing for future extensibility.
- Output is easy to understand, maintain, and integrate into existing .NET projects.
- Documentation and error messages are clear and actionable.
- Designed to be robust, with sensible defaults and extensibility for advanced scenarios.

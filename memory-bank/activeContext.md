# Active Context

## Current Work Focus
- Maintainable, modular codebase: all large methods are split into smaller, focused helpers for clarity and testability.
- Each file contains only a single class, following best practices for maintainability and discoverability.
- Full support for all Dataverse attribute types and robust enum generation in codegen.
- All table/entity and column/attribute identifiers use **schema name** throughout metadata, domain models, code generation, and templates.
- Only main attributes are included in codegen by filtering on `AttributeOf == null`.
- Enum generation is robust: optionset values are mapped to anonymous objects before passing to the Scriban template, ensuring correct output.
- OwnerColumnModel and owner attribute handling have been removed as they are not needed with the new attribute filtering.
- Extensible CLI: Program.cs implements a robust, extensible CLI using System.CommandLine, supporting advanced options for authentication, filtering, output, templates, and more. Dependency injection is used for core services, and the CLI handler orchestrates the fetch/generate/write pipeline.
- Maintaining and updating the Memory Bank to reflect actual project state and next steps.

## Recent Changes
- **Major refactor:** All large methods in DataverseMetadataFetcher, CSharpProxyGenerator, and Program (CLI) have been split into smaller, focused private methods. The codebase is now modular, with each method having a single responsibility.
- **One class per file:** All files now contain only a single class, enforcing maintainability and discoverability.
- CLI overhaul: Program.cs now features a comprehensive, extensible CLI with advanced options (authentication, filtering, output, templates, localization, etc.), robust argument parsing, and dependency injection for core services. The CLI handler coordinates the fetch/generate/write pipeline and provides clear user feedback.
- Swapped all usage of logical name to schema name for tables/entities and columns/attributes in metadata fetching, domain models, code generation, and templates. All generated C# class and property names now use schema names for consistency and alignment with Dataverse best practices.
- Fixed issues with abstract instantiation and missing properties in domain models.
- Removed OwnerColumnModel and all owner attribute handling.
- Fixed enum generation: OptionsetValues are now mapped to anonymous objects for template compatibility.
- Only main attributes (AttributeOf == null) are included in codegen, preventing supporting/secondary attributes from being generated.
- All attribute types and enums are now correctly supported in the generated code.
- **Template formatting fix:** Updated ProxyClass.scriban-cs to avoid double newlines after the last attribute section, ensuring clean output.
- **Automated formatting test:** Added a robust test that uses the real template to verify both property generation and that no double newlines appear after the last attribute. Test reliably locates the template file regardless of working directory.

## Next Steps
- Monitor for any issues or regressions related to the recent refactor, especially in downstream consumers or integrations.
- Ensure all new features and codegen logic use schema names exclusively.
- Monitor for edge cases in enum or attribute handling.
- Continue to ensure template and codegen compatibility as new types are added.
- Maintain up-to-date documentation in the memory bank.
- Expand automated tests to cover more formatting and output scenarios as templates evolve.
- Monitor CLI usability, argument parsing, and user feedback for further improvements.

## Active Decisions and Considerations
- All identifiers for tables and columns are now based on schema name, not logical name, to ensure clarity and alignment with Dataverse conventions.
- Strict separation of metadata fetching and code generation is enforced in the architecture.
- Internal domain classes are the only means of communication between fetchers and generators.
- Output structure: partial C# classes, decorated with relevant attributes, with properties for each Dataverse column and relationship.
- Template-driven code generation for maintainability and flexibility.
- Prefer many shorter methods over one long method. Each method should have a single responsibility and be easy to test and maintain.
- One class per file: Each class (including all column model subclasses) must be defined in its own file.

## Important Patterns and Preferences
- Domain-driven design for metadata representation.
- All codegen and templates should use schema names for identifiers.
- Extensible, modular architecture for future enhancements.
- Template-based code generation using Scriban.
- Clear, maintainable documentation in the memory bank.

## Learnings and Project Insights
- Filtering on AttributeOf is essential to avoid generating supporting attributes.
- Mapping optionset values to anonymous objects is the most robust way to ensure Scriban compatibility for enums.
- Early investment in documentation (memory bank) streamlines future development and onboarding.
- Strict adherence to separation of concerns facilitates maintainability and extensibility.
- Refactoring to smaller methods and enforcing one class per file dramatically improves maintainability and code clarity.

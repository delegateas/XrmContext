# Progress

## What Works
- End-to-end Dataverse proxy generation: fetches metadata, generates C# classes, writes output.
- All table/entity and column/attribute identifiers now use schema names throughout the pipeline (metadata, domain models, codegen, templates, output).
- CLI now supports a wide range of options (authentication, filtering, output, templates, localization, etc.) and robust orchestration using System.CommandLine and dependency injection.
- CLI supports specifying solutions for targeted entity generation.
- Domain models and codegen templates are aligned and tested.
- All major attribute types (string, int, bool, decimal, double, money, datetime, lookup, enum, guid, file, image, memo) are supported.
- Only main attributes are included in codegen by filtering on `AttributeOf == null`.
- Enum generation is robust: optionset values are mapped to anonymous objects before passing to the Scriban template, ensuring correct output.
- Output writer creates all necessary directories for generated files.
- Single Verify-based snapshot test validates codegen for all supported attribute types (string, int, bool, decimal, double, money, datetime, lookup, enum, guid, file, image, memo, partylist, etc.) in one entity, minimizing snapshot churn and maintenance.
- **Template formatting fix:** ProxyClass.scriban-cs updated to avoid double newlines after the last attribute section.
- **Automated formatting test:** Robust test added to validate both property generation and that no double newlines appear after the last attribute. Test reliably locates the template file regardless of working directory.
- Project builds and runs successfully on .NET 8.
- **Major refactor:** All large methods in DataverseMetadataFetcher, CSharpProxyGenerator, and Program (CLI) have been split into smaller, focused private methods. Each file now contains only a single class, improving maintainability and discoverability.

## What's Left to Build
- Expand codegen to support additional relationship types and advanced attribute types if needed.
- Add more comprehensive tests (output writer, CLI, integration).
- Refine CLI argument parsing and UX.
- Update documentation and usage instructions.
- Monitor for edge cases in enum or attribute handling.

## Current Status
- All major build and runtime errors resolved.
- All codegen, templates, and output now use schema names for identifiers (no logical names remain in generated code).
- CLI is architected for extensibility and maintainability, with DI and clear separation of concerns between option parsing, service wiring, and handler orchestration.
- Codegen and templates are robust for all main attribute types and enums.
- OwnerColumnModel and owner attribute handling are not needed due to attribute filtering.
- Template formatting and output structure are now validated by automated tests.
- Project is ready for further extension and real-world feedback.
- **Refactored for maintainability:** All large methods are now split into smaller, focused helpers. Each file contains only a single class, and the codebase is modular and easy to maintain.

## Known Issues
- No automated tests for output writer, CLI, edge cases, or relationship handling (see "What's Left to Build"). This is not blocking current workflow but may impact future maintainability.
- Edge cases in enum or attribute handling may arise as more entities are tested.

## Evolution of Project Decisions
- Early focus on maintainable, extensible architecture and documentation.
- Refactored metadata fetcher and codegen for clarity and testability.
- Migrated all identifier usage from logical name to schema name for tables/entities and columns/attributes, to align with Dataverse conventions and ensure consistency in generated code.
- Adopted template-driven codegen for flexibility.
- Attribute filtering (`AttributeOf == null`) is now used to avoid generating supporting attributes.
- OwnerColumnModel was added and then removed as attribute filtering made it unnecessary.
- Enum generation now uses anonymous object mapping for Scriban compatibility.
- **Major refactor:** All large methods are now split into smaller, focused helpers, and each file contains only a single class. This has dramatically improved maintainability, clarity, and testability.

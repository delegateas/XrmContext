# Project Brief

## Project Name
Dataverse Proxy Generator (.NET Tool)

## Overview
A .NET 8 command-line tool that connects to a Microsoft Dataverse environment, fetches metadata, and generates proxy classes representing Dataverse tables, including their columns and relationships.

## Core Requirements
- Connect to Microsoft Dataverse using secure authentication.
- Fetch table metadata (initially via one method, extensible to more).
- Generate C# proxy classes that mirror Dataverse tables, columns, and relationships.
- Output classes should be partial C# classes, decorated with relevant attributes, and include properties for each Dataverse column and relationship.
- Each property should use the appropriate .NET type for the Dataverse attribute type (e.g., string, Guid, int, etc.), and support for additional attribute types should be added over time.
- Initial support for string attributes; extensible to all Dataverse attribute types.
- Separation of concerns: metadata fetching and code generation are distinct, communicating via internal domain classes.
- Designed for extensibility and maintainability.

## Goals
- Simplify Dataverse integration for .NET developers.
- Ensure generated code is strongly-typed, maintainable, and up-to-date with Dataverse schema.
- Provide a foundation for future enhancements (additional metadata sources, attribute types, relationship handling, etc.).

## Scope
- CLI tool targeting .NET 8.
- Focus on core generation pipeline and domain modeling.
- Initial implementation: string attributes only, single metadata fetch method.
- Excludes UI, deployment automation, or advanced attribute types in the first iteration.

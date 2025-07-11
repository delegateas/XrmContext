namespace DataverseProxyGenerator.Core.Domain;

public record RelationshipModel
{
    public string SchemaName { get; init; } = string.Empty; // Relationship schema name

    public string? RelationshipType { get; init; } // OneToMany, ManyToOne, ManyToMany

    public string? ThisEntityRole { get; init; } // Referencing, Referenced, Entity1, Entity2

    public string? ThisEntityAttribute { get; init; } // Attribute on this entity (if applicable)

    public string? RelatedEntity { get; init; } // Logical name of related entity

    public string? RelatedEntityAttribute { get; init; } // Attribute on related entity (if applicable)

    public string? RelatedEntitySchemaName { get; init; } // Schema name of related entity
}

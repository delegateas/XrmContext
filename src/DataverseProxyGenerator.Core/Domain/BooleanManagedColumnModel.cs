namespace DataverseProxyGenerator.Core.Domain;

public record BooleanManagedColumnModel() : ManagedColumnModel("bool", IsNullable: false)
{
}
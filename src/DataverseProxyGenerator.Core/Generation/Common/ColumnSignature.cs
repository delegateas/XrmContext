namespace DataverseProxyGenerator.Core.Generation.Common;

/// <summary>
/// Helper struct for fast column comparison.
/// </summary>
public readonly record struct ColumnSignature(string SchemaName, string TypeName);

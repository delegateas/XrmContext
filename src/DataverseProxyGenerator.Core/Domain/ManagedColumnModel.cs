namespace DataverseProxyGenerator.Core.Domain;

public record ManagedColumnModel(string ReturnType, bool IsNullable) : ColumnModel
{
    public string FullReturnType => IsNullable && ReturnType != "string" ? ReturnType + "?" : ReturnType;
}
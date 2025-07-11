namespace DataverseProxyGenerator.Core.Domain;

public record EnumColumnModel : ColumnModel
{
    public string OptionsetName { get; init; } = string.Empty;

    public bool IsGlobalOptionset { get; init; }

    public bool IsMultiSelect { get; init; }

    public IDictionary<int, string> OptionsetValues { get; init; } = new Dictionary<int, string>();

    /// <summary>
    /// Maps option value to a dictionary of LCID → label.
    /// </summary>
    public IDictionary<int, Dictionary<int, string>> OptionLocalizations { get; init; } = new Dictionary<int, Dictionary<int, string>>();
}

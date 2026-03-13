using System.ComponentModel.DataAnnotations;

namespace DataverseProxyGenerator.Tool.Options;

public class GeneratorOptions
{
    [Required(ErrorMessage = "Output directory is required")]
    public string OutputDirectory { get; set; } = string.Empty;

    public IReadOnlyList<string> Solutions { get; set; } = Array.Empty<string>();

    public IReadOnlyList<string> Entities { get; set; } = Array.Empty<string>();

    public string NamespaceSetting { get; set; } = "DataverseContext";

    public string ServiceContextName { get; set; } = "Xrm";

    public string DeprecatedPrefix { get; set; } = string.Empty;

    public IReadOnlyDictionary<string, IReadOnlyList<string>> IntersectMapping { get; set; } =
        new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

    public IReadOnlyDictionary<string, string> LabelMapping { get; set; } =
        new Dictionary<string, string>(StringComparer.Ordinal);

    public bool NullableTypes { get; set; } = true;
}
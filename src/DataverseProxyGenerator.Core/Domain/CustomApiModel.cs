namespace DataverseProxyGenerator.Core.Domain;

public record CustomApiModel
{
    public string UniqueName { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public bool IsFunction { get; init; }

    public IList<CustomApiParameterModel> RequestParameters { get; init; } = new List<CustomApiParameterModel>();

    public IList<CustomApiParameterModel> ResponseProperties { get; init; } = new List<CustomApiParameterModel>();
}
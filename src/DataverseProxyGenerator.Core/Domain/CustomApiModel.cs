namespace DataverseProxyGenerator.Core.Domain;

public record CustomApiModel
{
    public Guid Id { get; init; }

    public string UniqueName { get; init; } = string.Empty;

    public string DisplayName { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public bool IsFunction { get; init; }

    public IList<CustomApiParameterModel> RequestParameters { get; init; } = [];

    public IList<CustomApiParameterModel> ResponseProperties { get; init; } = [];
}
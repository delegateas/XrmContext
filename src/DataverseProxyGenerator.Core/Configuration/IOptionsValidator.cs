namespace DataverseProxyGenerator.Core.Configuration;

public interface IOptionsValidator<in T>
{
    ValidationResult Validate(T options);
}
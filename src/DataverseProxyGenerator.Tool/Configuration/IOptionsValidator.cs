namespace DataverseProxyGenerator.Tool.Configuration;

public interface IOptionsValidator<in T>
{
    ValidationResult Validate(T options);
}
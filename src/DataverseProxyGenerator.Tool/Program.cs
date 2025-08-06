using System.Diagnostics.CodeAnalysis;
using DataverseConnection;
using DataverseProxyGenerator.Core;
using DataverseProxyGenerator.Core.Generation;
using DataverseProxyGenerator.Core.Metadata;
using DataverseProxyGenerator.Core.Output;
using DataverseProxyGenerator.Tool.Configuration;
using DataverseProxyGenerator.Tool.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DataverseProxyGenerator.Tool;

internal static class Program
{
    public static Task<int> Main(string[] args)
    {
        return RunApplication(args);
    }

    [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Command-line tool with acceptable hardcoded strings")]
    private static async Task<int> RunApplication(string[] args)
    {
        try
        {
            var baseConfig = SimpleXrmContextConfigBuilder.BuildFromConfiguration();

            // Parse command line args and merge
            var (outputDirectory, solutions, entities, namespaceSetting, serviceContextName, deprecatedPrefix, intersectMapping, labelMapping) = CommandLineParser.Parse(args);

            var config = new XrmContextConfig(
                new XrmFetchConfig(
                    (solutions.Count > 0) ? solutions : baseConfig.Fetch.Solutions,
                    (entities.Count > 0) ? entities : baseConfig.Fetch.Entities,
                    !string.IsNullOrWhiteSpace(deprecatedPrefix) ? deprecatedPrefix : baseConfig.Fetch.DeprecatedPrefix,
                    (labelMapping.Count > 0) ? labelMapping : baseConfig.Fetch.LabelMapping),
                new XrmGenerationConfig(
                    !string.IsNullOrWhiteSpace(outputDirectory) ? outputDirectory : baseConfig.Generation.OutputDirectory,
                    !string.IsNullOrWhiteSpace(namespaceSetting) ? namespaceSetting : baseConfig.Generation.NamespaceSetting ?? "DataverseContext",
                    !string.IsNullOrWhiteSpace(serviceContextName) ? serviceContextName : baseConfig.Generation.ServiceContextName ?? "Xrm",
                    (intersectMapping.Count > 0) ? intersectMapping : baseConfig.Generation.IntersectMapping));

            if (string.IsNullOrWhiteSpace(config.Generation.OutputDirectory))
            {
                Console.WriteLine("Error: Output directory is required. Specify it via command line argument --output or in appsettings.json under XrmContext:OutputDirectory");
                return 1;
            }

            var validator = new XrmContextConfigValidator();
            var validationResult = validator.Validate(config);
            if (!validationResult.IsValid)
            {
                foreach (var error in validationResult.Errors)
                {
                    Console.WriteLine($"Error: {error}");
                }

                return 1;
            }

            var host = BuildHost();
            await RunWorkflowAsync(host, config);
            return 0;
        }
        catch (Exception ex) when (ex is not OutOfMemoryException and not AccessViolationException)
        {
            Console.WriteLine($"Fatal error: {ex.Message}");
            Console.WriteLine($"Details: {ex}");
            return 1;
        }
    }

    private static IHost BuildHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
                    .AddEnvironmentVariables()
                    .Build();

                services.AddSingleton<IConfiguration>(configuration);
                services.AddDataverse();

                // Register new services
                services.AddSingleton<ICodeGenerator, CSharpProxyGenerator>();
                services.AddSingleton<IOutputWriter, FileSystemOutputWriter>();
                services.AddSingleton<IMetadataSourceFactory, DataverseMetadataSourceFactory>();
                services.AddSingleton<XrmContextConfigValidator>();
                services.AddSingleton<GeneratorOptionsValidator>();
            })
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .Build();
    }

    private static async Task RunWorkflowAsync(
        IHost host,
        XrmContextConfig config)
    {
        var loggerFactory = host.Services.GetRequiredService<ILoggerFactory>();
        var logger = loggerFactory.CreateLogger("DataverseProxyGenerator");
        var generator = host.Services.GetRequiredService<ICodeGenerator>();
        var writer = host.Services.GetRequiredService<IOutputWriter>();
        var metadataFactory = host.Services.GetRequiredService<IMetadataSourceFactory>();

        try
        {
            logger.LogInformation("Fetching Dataverse metadata...");
            var fetcher = metadataFactory.CreateFetcher(MetadataSourceType.Dataverse, config.Fetch);
            var tables = await fetcher.FetchMetadataAsync();

            logger.LogInformation("Generating proxy classes and intersection interfaces...");
            var files = generator.GenerateCode(tables, config.Generation);

            logger.LogInformation("Writing files to {OutputDirectory}...", config.Generation.OutputDirectory);
            writer.WriteFiles(files, config.Generation.OutputDirectory);

            logger.LogInformation("Proxy class and intersection interface generation complete.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during code generation: {ErrorMessage}", ex.Message);
            throw new InvalidOperationException("Code generation workflow failed. See inner exception for details.", ex);
        }
    }
}
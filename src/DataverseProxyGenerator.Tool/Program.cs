using DataverseConnection;
using DataverseProxyGenerator.Core;
using DataverseProxyGenerator.Core.Generation;
using DataverseProxyGenerator.Core.Metadata;
using DataverseProxyGenerator.Core.Output;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace DataverseProxyGenerator.Tool;

internal static class Program
{
    public static Task<int> Main(string[] args)
    {
        return RunApplication(args);
    }

    private static async Task<int> RunApplication(string[] args)
    {
        var (outputDirectory, solutions, entities, namespaceSetting, serviceContextName, deprecatedPrefix, intersectMapping, labelMapping) = CommandLineParser.Parse(args);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        // Bind flat config
        var configSection = configuration.GetSection("XrmContext");
        var config = new XrmContextConfig(
            new XrmFetchConfig(
                configSection.GetSection("Solutions").Get<string[]>() ?? System.Array.Empty<string>(),
                configSection.GetSection("Entities").Get<string[]>() ?? System.Array.Empty<string>(),
                configSection.GetValue<string>("DeprecatedPrefix") ?? string.Empty,
                configSection.GetSection("LabelMapping").Get<IReadOnlyDictionary<string, string>>() ?? new Dictionary<string, string>(StringComparer.InvariantCulture)),
            new XrmGenerationConfig(
                configSection.GetValue<string>("OutputDirectory") ?? string.Empty,
                configSection.GetValue<string>("NamespaceSetting") ?? string.Empty,
                configSection.GetValue<string>("ServiceContextName") ?? string.Empty,
                configSection.GetSection("IntersectMapping").Get<IReadOnlyDictionary<string, IReadOnlyList<string>>>() ?? new Dictionary<string, IReadOnlyList<string>>(StringComparer.InvariantCulture)));

        // Merge: command-line args override config
        var mergedConfig = new XrmContextConfig(
            new XrmFetchConfig(
                (solutions != null && solutions.Length > 0) ? solutions : config.Fetch.Solutions,
                (entities != null && entities.Length > 0) ? entities : config.Fetch.Entities,
                !string.IsNullOrWhiteSpace(deprecatedPrefix) ? deprecatedPrefix : config.Fetch.DeprecatedPrefix,
                (labelMapping != null && labelMapping.Count > 0) ? labelMapping : config.Fetch.LabelMapping),
            new XrmGenerationConfig(
                !string.IsNullOrWhiteSpace(outputDirectory) ? outputDirectory : config.Generation.OutputDirectory,
                !string.IsNullOrWhiteSpace(namespaceSetting) ? namespaceSetting : config.Generation.NamespaceSetting,
                !string.IsNullOrWhiteSpace(serviceContextName) ? serviceContextName : config.Generation.ServiceContextName,
                (intersectMapping != null && intersectMapping.Count > 0) ? intersectMapping : config.Generation.IntersectMapping));

        if (string.IsNullOrWhiteSpace(mergedConfig.Generation.OutputDirectory))
        {
            throw new InvalidOperationException("Output directory is required. Specify it via command line argument --output or in appsettings.json under XrmContext:OutputDirectory");
        }

        var host = BuildHost();
        await RunWorkflowAsync(host, mergedConfig);
        return 0;
    }

    private static IHost BuildHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory()) // Current working directory
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
                    .AddEnvironmentVariables()
                    .Build();

                services.AddSingleton<IConfiguration>(configuration);
                services.AddDataverse();
                services.AddSingleton<ICodeGenerator, CSharpProxyGenerator>();
                services.AddSingleton<IOutputWriter, FileSystemOutputWriter>();
            }).Build();
    }

    private static async Task RunWorkflowAsync(
        IHost host,
        XrmContextConfig config)
    {
        var serviceClient = host.Services.GetRequiredService<Microsoft.PowerPlatform.Dataverse.Client.ServiceClient>();
        var generator = new CSharpProxyGenerator();
        var fetcher = new DataverseMetadataFetcher(serviceClient, config.Fetch);
        var writer = host.Services.GetRequiredService<IOutputWriter>();

#pragma warning disable CA1031 // Do not catch general exception types
        try
        {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
            Console.WriteLine("Fetching Dataverse metadata...");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            var tables = await fetcher.FetchMetadataAsync();

#pragma warning disable CA1303 // Do not pass literals as localized parameters
            Console.WriteLine("Generating proxy classes and intersection interfaces...");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            var files = generator.GenerateCode(
                tables,
                config.Generation);

            Console.WriteLine($"Writing files to {config.Generation.OutputDirectory}...");
            writer.WriteFiles(files, config.Generation.OutputDirectory);

#pragma warning disable CA1303 // Do not pass literals as localized parameters
            Console.WriteLine("Proxy class and intersection interface generation complete.");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
#pragma warning restore CA1031 // Do not catch general exception types
    }
}

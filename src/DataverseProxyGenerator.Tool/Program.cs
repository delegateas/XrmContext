using DataverseConnection;
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

        var host = BuildHost();

        await RunWorkflowAsync(host, outputDirectory, solutions, entities, namespaceSetting, serviceContextName, deprecatedPrefix, intersectMapping, labelMapping);

        return 0;
    }

    private static IHost BuildHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                var configuration = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: true)
                    .AddEnvironmentVariables()
                    .Build();

                services.AddSingleton<IConfiguration>(configuration);
                services.AddDataverse();
                services.AddSingleton<IDataverseMetadataFetcher, DataverseMetadataFetcher>();
                services.AddSingleton<ICodeGenerator, CSharpProxyGenerator>();
                services.AddSingleton<IOutputWriter, FileSystemOutputWriter>();
            })
            .Build();
    }

    private static async Task RunWorkflowAsync(
        IHost host,
        string outputDirectory,
        string[] solutions,
        string[] entities,
        string namespaceSetting,
        string serviceContextName,
        string deprecatedPrefix,
        Dictionary<string, List<string>> intersectMapping,
        Dictionary<string, string> labelMapping)
    {
        var fetcher = host.Services.GetRequiredService<IDataverseMetadataFetcher>();
        var serviceClient = host.Services.GetRequiredService<Microsoft.PowerPlatform.Dataverse.Client.ServiceClient>();
        var generator = new CSharpProxyGenerator();
        var writer = host.Services.GetRequiredService<IOutputWriter>();

#pragma warning disable CA1031 // Do not catch general exception types
        try
        {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
            Console.WriteLine("Fetching Dataverse metadata...");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            var tables = await fetcher.FetchMetadataAsync(serviceClient, solutions, entities, deprecatedPrefix, labelMapping);

#pragma warning disable CA1303 // Do not pass literals as localized parameters
            Console.WriteLine("Generating proxy classes and intersection interfaces...");
#pragma warning restore CA1303 // Do not pass literals as localized parameters
            var files = generator.GenerateCode(tables, namespaceSetting, serviceContextName, intersectMapping);

            Console.WriteLine($"Writing files to {outputDirectory}...");
            writer.WriteFiles(files, outputDirectory);

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
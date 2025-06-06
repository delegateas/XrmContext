using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DataverseProxyGenerator.Core.Metadata;
using DataverseProxyGenerator.Core.Generation;
using DataverseProxyGenerator.Core.Output;
using DataverseConnection;
using Microsoft.Extensions.Configuration;

namespace DataverseProxyGenerator.Tool
{
    internal class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var rootCommand = CreateRootCommand();

            return await rootCommand.InvokeAsync(args);
        }

        private static RootCommand CreateRootCommand()
        {
            var outputDirectoryOption = new Option<string>(
                aliases: new[] { "--output", "-o" },
                description: "Output directory for generated files")
            {
                IsRequired = true
            };

            var solutionsOption = new Option<string[]>(
                aliases: new[] { "--solutions", "--ss" },
                description: "Comma-separated list of solution names. Generates code for the entities found in these solutions.")
            {
                Arity = ArgumentArity.ZeroOrMore
            };

            var entitiesOption = new Option<string[]>(
                aliases: new[] { "--entities", "--es" },
                description: "Comma-separated list of logical names of the entities to generate code for. Additive with entities from solutions.")
            {
                Arity = ArgumentArity.ZeroOrMore
            };

            var namespaceOption = new Option<string>(
                aliases: new[] { "--namespace", "--ns" },
                description: "The namespace for the generated code. Default is the global namespace.",
                getDefaultValue: () => string.Empty
            );

            var serviceContextNameOption = new Option<string>(
                aliases: new[] { "--servicecontextname", "--scn" },
                description: "The name of the generated organization service context class. If not supplied, no service context is created.",
                getDefaultValue: () => string.Empty
            );

            var deprecatedPrefixOption = new Option<string>(
                aliases: new[] { "--deprecatedprefix", "--dp" },
                description: "Marks all attributes with the given prefix in their display name as deprecated.",
                getDefaultValue: () => string.Empty
            );

            var intersectOption = new Option<string[]>(
                aliases: new[] { "--intersect", "--is" },
                description: "Comma-separated list of named semicolon-separated lists of entity logical names to intersect. Example: ICustomer:account;contact, IActivity:phonecall;email;task",
                getDefaultValue: () => Array.Empty<string>())
            {
                Arity = ArgumentArity.ZeroOrMore
            };

            var labelMappingsOption = new Option<string[]>(
                aliases: new[] { "--labelMappings", "--lm" },
                description: "Comma-separated list of mappings between unicodes and the mapped string. Example: \\u2714\\uFE0F: checkmark, \\u26D4\\uFE0F: stopsign",
                getDefaultValue: () => Array.Empty<string>())
            {
                Arity = ArgumentArity.ZeroOrMore
            };

            var localizationsOption = new Option<string[]>(
                aliases: new[] { "--localizations", "--l" },
                description: "Comma-separated list of LCIDs to include as OptionSetMetadataAttribute. Default is to use UserLocalizedLabel.",
                getDefaultValue: () => Array.Empty<string>())
            {
                Arity = ArgumentArity.ZeroOrMore
            };

            var rootCommand = new RootCommand("Dataverse Proxy Generator CLI")
            {
                outputDirectoryOption,
                solutionsOption,
                entitiesOption,
                namespaceOption,
                serviceContextNameOption,
                deprecatedPrefixOption,
                intersectOption,
                labelMappingsOption,
                localizationsOption
            };

            rootCommand.SetHandler(
                async (string outputDirectory, string[] solutions, string[] entities, string @namespace, string deprecatedPrefix, string[] intersect, string[] labelMappings) =>
                {
                    var (normalizedSolutions, normalizedEntities) = NormalizeArguments(solutions, entities);

                    var host = BuildHost();

                    var labelMappingDict = ParseLabelMappings(labelMappings);

                    var intersectMapping = ParseIntersectArgument(intersect);

                    await RunWorkflowAsync(host, outputDirectory, normalizedSolutions, normalizedEntities, @namespace, deprecatedPrefix, intersectMapping, labelMappingDict);
                },
                outputDirectoryOption, solutionsOption, entitiesOption, namespaceOption, deprecatedPrefixOption, intersectOption, labelMappingsOption);

            return rootCommand;
        }

        private static (string[] solutions, string[] entities) NormalizeArguments(string[] solutions, string[] entities)
        {
            if (solutions.Length == 1 && solutions[0]?.Contains(',') == true)
            {
                solutions = solutions[0].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }
            if (entities.Length == 1 && entities[0]?.Contains(',') == true)
            {
                entities = entities[0].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            }
            return (solutions, entities);
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
            string @namespace,
            string deprecatedPrefix,
            Dictionary<string, List<string>> intersectMapping,
            Dictionary<string, string> labelMapping)
        {
            var fetcher = host.Services.GetRequiredService<IDataverseMetadataFetcher>();
            var serviceClient = host.Services.GetRequiredService<Microsoft.PowerPlatform.Dataverse.Client.ServiceClient>();
            var generator = new CSharpProxyGenerator();
            var writer = host.Services.GetRequiredService<IOutputWriter>();

            try
            {
                Console.WriteLine("Fetching Dataverse metadata...");
                var tables = await fetcher.FetchMetadataAsync(serviceClient, solutions, entities, deprecatedPrefix, labelMapping);

                Console.WriteLine("Generating proxy classes and intersection interfaces...");
                var files = generator.GenerateCode(tables, @namespace, intersectMapping);

                Console.WriteLine($"Writing files to {outputDirectory}...");
                writer.WriteFiles(files, outputDirectory);

                Console.WriteLine("Proxy class and intersection interface generation complete.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }

        private static Dictionary<string, string> ParseLabelMappings(string[] labelMappings)
        {
            var dict = new Dictionary<string, string>();
            foreach (var mapping in labelMappings)
            {
                var parts = mapping.Split(':', 2);
                if (parts.Length == 2)
                {
                    var key = System.Text.RegularExpressions.Regex.Unescape(parts[0].Trim());
                    var value = parts[1].Trim();
                    dict[key] = value;
                }
            }
            return dict;
        }

        private static Dictionary<string, List<string>> ParseIntersectArgument(string[] intersect)
        {
            // Example input: [ "ICustomer:account;contact", "IActivity:phonecall;email;task" ]
            var result = new Dictionary<string, List<string>>();
            foreach (var entry in intersect)
            {
                var parts = entry.Split(':', 2);
                if (parts.Length == 2)
                {
                    var interfaceName = parts[0].Trim();
                    var tableList = parts[1].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    result[interfaceName] = new List<string>(tableList);
                }
            }
            return result;
        }
    }
}

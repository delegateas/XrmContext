using System.CommandLine;
using System.CommandLine.Parsing;

namespace DataverseProxyGenerator.Tool;

public static class CommandLineParser
{
    public static (string OutputDirectory,
        string[] Solutions,
        string[] Entities,
        string NamespaceSetting,
        string ServiceContextName,
        string DeprecatedPrefix,
        IReadOnlyDictionary<string, IReadOnlyList<string>> IntersectMapping,
        IReadOnlyDictionary<string, string> LabelMapping)
#pragma warning disable MA0051 // Method is too long
        Parse(string[] args)
#pragma warning restore MA0051 // Method is too long
    {
        var outputDirectoryOption = new Option<string>(
            aliases: ["--output", "-o"],
            description: "Output directory for generated files")
        {
            IsRequired = true,
        };

        var solutionsOption = new Option<string[]>(
            aliases: ["--solutions", "--ss"],
            parseArgument: GetCommaSeperatedValue,
            description: "Comma-separated list of solution names. Generates code for the entities found in these solutions.")
        {
            Arity = ArgumentArity.ZeroOrMore,
        };

        var entitiesOption = new Option<string[]>(
            aliases: ["--entities", "--es"],
            parseArgument: GetCommaSeperatedValue,
            description: "Comma-separated list of logical names of the entities to generate code for. Additive with entities from solutions.")
        {
            Arity = ArgumentArity.ZeroOrMore,
        };

        var namespaceOption = new Option<string>(
            aliases: ["--namespace", "--ns"],
            description: "The namespace for the generated code. Default is the global namespace.");

        var serviceContextNameOption = new Option<string>(
            aliases: ["--servicecontextname", "--scn"],
            description: "The name of the generated organization service context class. If not supplied, no service context is created.");

        var deprecatedPrefixOption = new Option<string>(
            aliases: ["--deprecatedprefix", "--dp"],
            description: "Marks all attributes with the given prefix in their display name as deprecated.");

        var intersectOption = new Option<Dictionary<string, IReadOnlyList<string>>>(
            aliases: ["--intersect", "--is"],
            parseArgument: result =>
            {
                var value = result.Tokens.Select(t => t.Value).ToArray();
                var dict = new Dictionary<string, IReadOnlyList<string>>(StringComparer.InvariantCulture);
                foreach (var entry in value)
                {
                    var parts = entry.Split(':', 2);
                    if (parts.Length == 2)
                    {
                        var interfaceName = parts[0].Trim();
                        var tableList = parts[1].Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                        dict[interfaceName] = new List<string>(tableList).AsReadOnly();
                    }
                }

                return dict;
            },
            description: "Comma-separated list of named semicolon-separated lists of entity logical names to intersect. Example: ICustomer:account;contact, IActivity:phonecall;email;task")
        {
            Arity = ArgumentArity.ZeroOrMore,
        };

        var labelMappingsOption = new Option<Dictionary<string, string>>(
            aliases: ["--labelMappings", "--lm"],
            parseArgument: result =>
            {
                var value = result.Tokens.Select(t => t.Value).ToArray();
                var dict = new Dictionary<string, string>(StringComparer.InvariantCulture);
                foreach (var mapping in value)
                {
                    var parts = mapping.Split(':', 2);
                    if (parts.Length == 2)
                    {
                        var key = System.Text.RegularExpressions.Regex.Unescape(parts[0].Trim());
                        var val = parts[1].Trim();
                        dict[key] = val;
                    }
                }

                return dict;
            },
            description: "Comma-separated list of mappings between unicodes and the mapped string. Example: \\u2714\\uFE0F: checkmark, \\u26D4\\uFE0F: stopsign")
        {
            Arity = ArgumentArity.ZeroOrMore,
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
        };

        var parsedResult = rootCommand.Parse(args);

        return (
            parsedResult.GetValueForOption(outputDirectoryOption) ?? throw new InvalidOperationException("Output directory is required"),
            parsedResult.GetValueForOption(solutionsOption) ?? [],
            parsedResult.GetValueForOption(entitiesOption) ?? [],
            parsedResult.GetValueForOption(namespaceOption) ?? "DataverseContext",
            parsedResult.GetValueForOption(serviceContextNameOption) ?? "Xrm",
            parsedResult.GetValueForOption(deprecatedPrefixOption) ?? string.Empty,
            (parsedResult.GetValueForOption(intersectOption) ?? []).AsReadOnly(),
            (parsedResult.GetValueForOption(labelMappingsOption) ?? []).AsReadOnly()
        );
    }

    private static string[] GetCommaSeperatedValue(System.CommandLine.Parsing.ArgumentResult result)
    {
        var value = result.Tokens.Select(t => t.Value).ToArray();
        return value.Length == 1 && value[0].Contains(',', StringComparison.InvariantCulture)
            ? value[0].Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            : value;
    }
}
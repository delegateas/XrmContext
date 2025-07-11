using DataverseProxyGenerator.Core.Domain;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk.Metadata;

namespace DataverseProxyGenerator.Core.Metadata;

public class DataverseMetadataFetcher : IDataverseMetadataFetcher
{
    private const int MaxParallelism = 8;

    public async Task<IEnumerable<TableModel>> FetchMetadataAsync(
        ServiceClient serviceClient,
        IEnumerable<string> solutionUniqueNames,
        IEnumerable<string> logicalNames,
        string? deprecatedPrefix,
        IDictionary<string, string> labelMapping)
    {
        var tables = new List<TableModel>();

        // Fetch from solutions
        var metadataFromSolution = await GetEntityMetadataFromSolutionsAsync(serviceClient, solutionUniqueNames ?? []);
        var fetchedLogicalNames = metadataFromSolution.Select(m => m.LogicalName).ToHashSet(StringComparer.InvariantCulture);

        // Fetch by logical names not already fetched
        var toolLogicalNames = new List<string>() { "activityparty" };
        var logicalNamesToFetch =
            (logicalNames ?? [])
            .Concat(toolLogicalNames)
            .Where(name => !string.IsNullOrWhiteSpace(name) && !fetchedLogicalNames.Contains(name))
            .Distinct(StringComparer.InvariantCulture)
            .ToList();
        var metadataFromLogicalNames = await GetEntityMetadataFromLogicalNamesAsync(serviceClient, logicalNamesToFetch);

        var allMetadata = metadataFromSolution.Concat(metadataFromLogicalNames).ToList();

        var logicalNameToMetadata = allMetadata.ToDictionary(m => m.LogicalName, m => m, StringComparer.InvariantCulture);

        // Merge all metadata
        foreach (var metadata in allMetadata)
        {
            var table = BuildTableModelFromMetadata(logicalNameToMetadata, metadata, deprecatedPrefix, labelMapping);
            tables.Add(table);
        }

        return tables;
    }

    private static async Task<List<EntityMetadata>> GetEntityMetadataFromSolutionsAsync(ServiceClient serviceClient, IEnumerable<string> solutionUniqueNames)
    {
        var logicalNameToMetadata = new Dictionary<string, EntityMetadata>(StringComparer.InvariantCulture);
        foreach (var solutionUniqueName in solutionUniqueNames)
        {
            var solutionId = GetSolutionId(serviceClient, solutionUniqueName);
            if (solutionId == Guid.Empty)
                continue;

            var entityIds = GetEntityIdsFromSolution(serviceClient, solutionId);

            using var semaphore = new SemaphoreSlim(MaxParallelism);
            var metadataTasks = entityIds.Select(async entityId =>
            {
                await semaphore.WaitAsync();
                try
                {
                    return await GetEntityMetadataFromIdAsync(serviceClient, entityId);
                }
                finally
                {
                    semaphore.Release();
                }
            });
            var metadata = await Task.WhenAll(metadataTasks);

            foreach (var m in metadata)
            {
                if (!string.IsNullOrEmpty(m.LogicalName) && !logicalNameToMetadata.ContainsKey(m.LogicalName))
                    logicalNameToMetadata.Add(m.LogicalName, m);
            }
        }

        return logicalNameToMetadata.Values.ToList();
    }

    private static async Task<List<EntityMetadata>> GetEntityMetadataFromLogicalNamesAsync(ServiceClient serviceClient, IEnumerable<string> logicalNames)
    {
        var metadataList = new List<EntityMetadata>();
        using var semaphore = new SemaphoreSlim(MaxParallelism);
        var tasks = logicalNames.Select(async logicalName =>
        {
            await semaphore.WaitAsync();
            try
            {
                var entityRequest = new Microsoft.Xrm.Sdk.Messages.RetrieveEntityRequest
                {
                    LogicalName = logicalName,
                    EntityFilters = EntityFilters.Entity | EntityFilters.Attributes | EntityFilters.Relationships,
                    RetrieveAsIfPublished = true,
                };

                var entityResponse = (Microsoft.Xrm.Sdk.Messages.RetrieveEntityResponse)await serviceClient.ExecuteAsync(entityRequest);
                if (entityResponse?.EntityMetadata != null)
                {
                    return entityResponse.EntityMetadata;
                }

                return null;
            }
            finally
            {
                semaphore.Release();
            }
        });

        var results = await Task.WhenAll(tasks);
        metadataList.AddRange(results.Where(m => m != null)!);
        return metadataList;
    }

    private static Guid GetSolutionId(ServiceClient serviceClient, string solutionUniqueName)
    {
        var solutionQuery = new Microsoft.Xrm.Sdk.Query.QueryExpression("solution")
        {
            ColumnSet = new Microsoft.Xrm.Sdk.Query.ColumnSet("solutionid"),
            Criteria = new Microsoft.Xrm.Sdk.Query.FilterExpression
            {
                Conditions =
                {
                    new Microsoft.Xrm.Sdk.Query.ConditionExpression("uniquename", Microsoft.Xrm.Sdk.Query.ConditionOperator.Equal, solutionUniqueName),
                },
            },
        };
        var solutionEntity = serviceClient.RetrieveMultiple(solutionQuery).Entities.FirstOrDefault();
        return solutionEntity?.Id ?? Guid.Empty;
    }

    private static List<Guid> GetEntityIdsFromSolution(ServiceClient serviceClient, Guid solutionId)
    {
        var componentQuery = new Microsoft.Xrm.Sdk.Query.QueryExpression("solutioncomponent")
        {
            ColumnSet = new Microsoft.Xrm.Sdk.Query.ColumnSet("objectid"),
            Criteria = new Microsoft.Xrm.Sdk.Query.FilterExpression
            {
                Conditions =
                {
                    new Microsoft.Xrm.Sdk.Query.ConditionExpression("solutionid", Microsoft.Xrm.Sdk.Query.ConditionOperator.Equal, solutionId),
                    new Microsoft.Xrm.Sdk.Query.ConditionExpression("componenttype", Microsoft.Xrm.Sdk.Query.ConditionOperator.Equal, 1), // 1 = Entity
                },
            },
        };
        return serviceClient.RetrieveMultiple(componentQuery).Entities
            .Where(c => c.Contains("objectid") && c["objectid"] is Guid)
            .Select(c => (Guid)c["objectid"])
            .ToList();
    }

    private static async Task<EntityMetadata> GetEntityMetadataFromIdAsync(ServiceClient serviceClient, Guid entityId)
    {
        var entityRequest = new Microsoft.Xrm.Sdk.Messages.RetrieveEntityRequest
        {
            MetadataId = entityId,
            EntityFilters = EntityFilters.Entity | EntityFilters.Attributes | EntityFilters.Relationships,
            RetrieveAsIfPublished = true,
        };
        var entityResponse = (Microsoft.Xrm.Sdk.Messages.RetrieveEntityResponse)await serviceClient.ExecuteAsync(entityRequest);
        return entityResponse.EntityMetadata;
    }

    private static TableModel BuildTableModelFromMetadata(Dictionary<string, EntityMetadata> logicalNameToMetadata, EntityMetadata entityMetadata, string? deprecatedPrefix, IDictionary<string, string> labelMapping)
    {
        var table = new TableModel
        {
            LogicalName = entityMetadata.LogicalName,
            SchemaName = entityMetadata.SchemaName,
            DisplayName = ApplyLabelMapping(entityMetadata.DisplayName?.UserLocalizedLabel?.Label ?? entityMetadata.LogicalName, labelMapping),
            EntityTypeCode = entityMetadata.ObjectTypeCode ?? 0,
            PrimaryNameAttribute = entityMetadata.PrimaryNameAttribute,
            PrimaryIdAttribute = entityMetadata.PrimaryIdAttribute,
            IsIntersect = entityMetadata.IsIntersect ?? false,
            Columns = new List<ColumnModel>(),
            Relationships = new List<RelationshipModel>(),
        };

        var validAttributes = entityMetadata.Attributes
            .Where(x => x.AttributeOf == null)
            .ToList();

        foreach (var attr in validAttributes)
        {
            var column = BuildColumnModel(attr, deprecatedPrefix, labelMapping);
            if (column != null)
            {
                table.Columns.Add(column);
            }
        }

        AddPrimaryIdColumn(table, entityMetadata, deprecatedPrefix, labelMapping);

        MapRelationships(logicalNameToMetadata, entityMetadata, table);

        return table;
    }

    private static ColumnModel? BuildColumnModel(AttributeMetadata attr, string? deprecatedPrefix, IDictionary<string, string> labelMapping)
    {
        ColumnModel? column = attr switch
        {
            StringAttributeMetadata stringAttr => BuildStringColumn(stringAttr, labelMapping),
            MemoAttributeMetadata memoAttr => BuildMemoColumn(memoAttr, labelMapping),
            IntegerAttributeMetadata intAttr => BuildIntegerColumn(intAttr, labelMapping),
            BigIntAttributeMetadata bigIntAttr => BuildBigIntColumn(bigIntAttr, labelMapping),
            BooleanAttributeMetadata boolAttr => BuildBooleanColumn(boolAttr, labelMapping),
            DateTimeAttributeMetadata dateAttr => BuildDateTimeColumn(dateAttr, labelMapping),
            DecimalAttributeMetadata decAttr => BuildDecimalColumn(decAttr, labelMapping),
            DoubleAttributeMetadata dblAttr => BuildDoubleColumn(dblAttr, labelMapping),
            MoneyAttributeMetadata moneyAttr => BuildMoneyColumn(moneyAttr, labelMapping),
            EnumAttributeMetadata enumAttr => BuildEnumColumn(enumAttr, labelMapping),
            LookupAttributeMetadata lookupAttr when lookupAttr.AttributeType == AttributeTypeCode.PartyList => BuildPartyListColumn(lookupAttr, labelMapping),
            LookupAttributeMetadata lookupAttr => BuildLookupColumn(lookupAttr, labelMapping),
            FileAttributeMetadata fileAttr => BuildFileColumn(fileAttr, labelMapping),
            ImageAttributeMetadata imageAttr => BuildImageColumn(imageAttr, labelMapping),
            _ => null,
        };

        if (column != null)
        {
            column = column with
            {
                IsObsolete =
                    !string.IsNullOrEmpty(column.DisplayName) &&
                    !string.IsNullOrEmpty(deprecatedPrefix) &&
                    column.DisplayName.StartsWith(deprecatedPrefix, StringComparison.OrdinalIgnoreCase),
            };
        }

        return column;
    }

    private static string ApplyLabelMapping(string label, IDictionary<string, string> mapping)
    {
        if (string.IsNullOrEmpty(label) || mapping == null || mapping.Count == 0)
            return label;
        foreach (var kvp in mapping)
        {
            if (!string.IsNullOrEmpty(kvp.Key))
                label = label.Replace(kvp.Key, kvp.Value, StringComparison.InvariantCulture);
        }

        return label;
    }

    private static void AddPrimaryIdColumn(TableModel table, EntityMetadata entityMetadata, string? deprecatedPrefix, IDictionary<string, string> labelMapping)
    {
        var primaryIdAttribute = Array.Find(entityMetadata.Attributes, x => x.LogicalName == entityMetadata.PrimaryIdAttribute);

        var primaryIdColumn = new PrimaryIdColumnModel
        {
            LogicalName = entityMetadata.PrimaryIdAttribute,
            SchemaName = primaryIdAttribute?.SchemaName ?? entityMetadata.PrimaryIdAttribute,
            DisplayName = ApplyLabelMapping(primaryIdAttribute?.DisplayName?.UserLocalizedLabel?.Label ?? entityMetadata.PrimaryIdAttribute, labelMapping),
            IsNullable = false,
            IsObsolete = !string.IsNullOrEmpty(entityMetadata.PrimaryNameAttribute) &&
                         !string.IsNullOrEmpty(deprecatedPrefix) &&
                         entityMetadata.PrimaryNameAttribute.StartsWith(deprecatedPrefix, StringComparison.OrdinalIgnoreCase),
        };
        table.Columns.Add(primaryIdColumn);
    }

    private static StringColumnModel BuildStringColumn(StringAttributeMetadata attr, IDictionary<string, string> labelMapping) => new StringColumnModel
    {
        LogicalName = attr.LogicalName,
        SchemaName = attr.SchemaName,
        DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName, labelMapping),
        Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty, labelMapping),
        IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired,
        MaxLength = attr.MaxLength,
    };

    private static MemoColumnModel BuildMemoColumn(MemoAttributeMetadata attr, IDictionary<string, string> labelMapping) => new MemoColumnModel
    {
        LogicalName = attr.LogicalName,
        SchemaName = attr.SchemaName,
        DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName, labelMapping),
        Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty, labelMapping),
        IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired,
        MaxLength = attr.MaxLength,
    };

    private static IntegerColumnModel BuildIntegerColumn(IntegerAttributeMetadata attr, IDictionary<string, string> labelMapping) => new IntegerColumnModel
    {
        LogicalName = attr.LogicalName,
        SchemaName = attr.SchemaName,
        DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName, labelMapping),
        Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty, labelMapping),
        IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired,
        Min = attr.MinValue ?? int.MinValue,
        Max = attr.MaxValue ?? int.MaxValue,
    };

    private static BigIntColumnModel BuildBigIntColumn(BigIntAttributeMetadata attr, IDictionary<string, string> labelMapping) => new BigIntColumnModel
    {
        LogicalName = attr.LogicalName,
        SchemaName = attr.SchemaName,
        DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName, labelMapping),
        Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty, labelMapping),
        IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired,
    };

    private static BooleanColumnModel BuildBooleanColumn(BooleanAttributeMetadata attr, IDictionary<string, string> labelMapping) => new BooleanColumnModel
    {
        LogicalName = attr.LogicalName,
        SchemaName = attr.SchemaName,
        DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName, labelMapping),
        Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty, labelMapping),
        IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired,
    };

    private static DateTimeColumnModel BuildDateTimeColumn(DateTimeAttributeMetadata attr, IDictionary<string, string> labelMapping) => new DateTimeColumnModel
    {
        LogicalName = attr.LogicalName,
        SchemaName = attr.SchemaName,
        DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName, labelMapping),
        Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty, labelMapping),
        IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired,
    };

    private static DecimalColumnModel BuildDecimalColumn(DecimalAttributeMetadata attr, IDictionary<string, string> labelMapping) => new DecimalColumnModel
    {
        LogicalName = attr.LogicalName,
        SchemaName = attr.SchemaName,
        DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName, labelMapping),
        Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty, labelMapping),
        IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired,
        Precision = attr.Precision,
    };

    private static DoubleColumnModel BuildDoubleColumn(DoubleAttributeMetadata attr, IDictionary<string, string> labelMapping) => new DoubleColumnModel
    {
        LogicalName = attr.LogicalName,
        SchemaName = attr.SchemaName,
        DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName, labelMapping),
        Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty, labelMapping),
        IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired,
    };

    private static MoneyColumnModel BuildMoneyColumn(MoneyAttributeMetadata attr, IDictionary<string, string> labelMapping) => new MoneyColumnModel
    {
        LogicalName = attr.LogicalName,
        SchemaName = attr.SchemaName,
        DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName, labelMapping),
        Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty, labelMapping),
        IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired,
        Precision = attr.Precision,
    };

    private static EnumColumnModel BuildEnumColumn(EnumAttributeMetadata attr, IDictionary<string, string> labelMapping)
    {
        var optionsetValues = attr.OptionSet?.Options?
            .Where(o => o.Value != null)
            .ToDictionary(
                o => o.Value.GetValueOrDefault(),
                o =>
                {
                    var label = o.Label?.UserLocalizedLabel?.Label;
                    label = ApplyLabelMapping(label ?? string.Empty, labelMapping);
                    if (string.IsNullOrWhiteSpace(label))
                    {
                        label = $"Option_{o.Value.GetValueOrDefault()}";
                    }

                    // Make label a valid C# identifier
                    label = label.Replace(" ", "_", StringComparison.InvariantCulture)
                                 .Replace("-", "_", StringComparison.InvariantCulture)
                                 .Replace(".", "_", StringComparison.InvariantCulture)
                                 .Replace(",", "_", StringComparison.InvariantCulture)
                                 .Replace(":", "_", StringComparison.InvariantCulture)
                                 .Replace(";", "_", StringComparison.InvariantCulture)
                                 .Replace("/", "_", StringComparison.InvariantCulture)
                                 .Replace("\\", "_", StringComparison.InvariantCulture);
                    if (char.IsDigit(label[0]))
                    {
                        label = "_" + label;
                    }

                    return label;
                }) ?? [];

        // Build OptionLocalizations: option value -> (LCID -> label)
        var optionLocalizations = BuildOptionLocalizations(attr);

        return new EnumColumnModel
        {
            LogicalName = attr.LogicalName,
            SchemaName = attr.SchemaName,
            DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName, labelMapping),
            Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty, labelMapping),
            IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired,
            OptionsetName = attr.OptionSet?.Name ?? attr.LogicalName,
            IsGlobalOptionset = attr.OptionSet?.IsGlobal ?? false,
            IsMultiSelect = attr.AttributeTypeName == "MultiSelectPicklistType",
            OptionsetValues = optionsetValues,
            OptionLocalizations = optionLocalizations,
        };
    }

    private static Dictionary<int, Dictionary<int, string>> BuildOptionLocalizations(EnumAttributeMetadata attr)
    {
        var optionLocalizations = new Dictionary<int, Dictionary<int, string>>();
        if (attr.OptionSet?.Options != null)
        {
            foreach (var o in attr.OptionSet.Options)
            {
                if (o.Value == null)
                    continue;
                var value = o.Value.GetValueOrDefault();
                var localizations = new Dictionary<int, string>();
                if (o.Label?.LocalizedLabels != null)
                {
                    foreach (var loc in o.Label.LocalizedLabels)
                    {
                        if (!string.IsNullOrWhiteSpace(loc.Label))
                        {
                            localizations[loc.LanguageCode] = loc.Label;
                        }
                    }
                }

                // Always include the userlocalized label if present
                if (o.Label?.UserLocalizedLabel != null && !string.IsNullOrWhiteSpace(o.Label.UserLocalizedLabel.Label))
                {
                    localizations[o.Label.UserLocalizedLabel.LanguageCode] = o.Label.UserLocalizedLabel.Label;
                }

                if (localizations.Count > 0)
                {
                    optionLocalizations[value] = localizations;
                }
            }
        }

        return optionLocalizations;
    }

    private static PartyListColumnModel BuildPartyListColumn(LookupAttributeMetadata attr, IDictionary<string, string> labelMapping) => new PartyListColumnModel
    {
        LogicalName = attr.LogicalName,
        SchemaName = attr.SchemaName,
        DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName, labelMapping),
        Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty, labelMapping),
        IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired,
    };

    private static LookupColumnModel BuildLookupColumn(LookupAttributeMetadata attr, IDictionary<string, string> labelMapping) => new LookupColumnModel
    {
        LogicalName = attr.LogicalName,
        SchemaName = attr.SchemaName,
        DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName, labelMapping),
        Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty, labelMapping),
        IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired,
        TargetTable = attr.Targets?.FirstOrDefault() ?? "Unknown",
    };

    private static FileColumnModel BuildFileColumn(FileAttributeMetadata attr, IDictionary<string, string> labelMapping) => new FileColumnModel
    {
        LogicalName = attr.LogicalName,
        SchemaName = attr.SchemaName,
        DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName, labelMapping),
        Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty, labelMapping),
        IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired,
    };

    private static ImageColumnModel BuildImageColumn(ImageAttributeMetadata attr, IDictionary<string, string> labelMapping) => new ImageColumnModel
    {
        LogicalName = attr.LogicalName,
        SchemaName = attr.SchemaName,
        DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName, labelMapping),
        Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty, labelMapping),
        IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired,
    };

    private static void MapRelationships(Dictionary<string, EntityMetadata> logicalNameToMetadata, EntityMetadata entityMetadata, TableModel table)
    {
        MapManyToOne(logicalNameToMetadata, entityMetadata, table);
        MapOneToMany(logicalNameToMetadata, entityMetadata, table);
        MapManyToMany(logicalNameToMetadata, entityMetadata, table);

        table = table with
        {
            Relationships = [.. table.Relationships.DistinctBy(x => x.SchemaName)],
        };
    }

    private static void MapManyToOne(Dictionary<string, EntityMetadata> logicalNameToMetadata, EntityMetadata entityMetadata, TableModel table)
    {
        foreach (var rel in entityMetadata.ManyToOneRelationships)
        {
            table.Relationships.Add(new RelationshipModel
            {
                SchemaName = rel.SchemaName,
                RelationshipType = "ManyToOne",
                ThisEntityRole = "Referencing",
                ThisEntityAttribute = rel.ReferencingAttribute,
                RelatedEntity = rel.ReferencedEntity,
                RelatedEntityAttribute = rel.ReferencedAttribute,
                RelatedEntitySchemaName = logicalNameToMetadata.TryGetValue(rel.ReferencedEntity, out var relatedMetadata) ? relatedMetadata.SchemaName : "Entity",
            });
        }
    }

    private static void MapOneToMany(Dictionary<string, EntityMetadata> logicalNameToMetadata, EntityMetadata entityMetadata, TableModel table)
    {
        foreach (var rel in entityMetadata.OneToManyRelationships.Where(x => x.ReferencingEntity != entityMetadata.LogicalName))
        {
            table.Relationships.Add(new RelationshipModel
            {
                SchemaName = rel.SchemaName,
                RelationshipType = "OneToMany",
                ThisEntityRole = "Referenced",
                ThisEntityAttribute = rel.ReferencedAttribute,
                RelatedEntity = rel.ReferencingEntity,
                RelatedEntityAttribute = rel.ReferencingAttribute,
                RelatedEntitySchemaName = logicalNameToMetadata.TryGetValue(rel.ReferencingEntity, out var relatedMetadata) ? relatedMetadata.SchemaName : "Entity",
            });
        }
    }

    private static void MapManyToMany(Dictionary<string, EntityMetadata> logicalNameToMetadata, EntityMetadata entityMetadata, TableModel table)
    {
        foreach (var rel in entityMetadata.ManyToManyRelationships)
        {
            if (rel.Entity2LogicalName != entityMetadata.LogicalName)
            {
                table.Relationships.Add(new RelationshipModel
                {
                    SchemaName = rel.SchemaName,
                    RelationshipType = "ManyToMany",
                    ThisEntityRole = "Entity1",
                    ThisEntityAttribute = rel.Entity1IntersectAttribute,
                    RelatedEntity = rel.Entity2LogicalName,
                    RelatedEntityAttribute = rel.Entity2IntersectAttribute,
                    RelatedEntitySchemaName = logicalNameToMetadata.TryGetValue(rel.Entity2LogicalName, out var relatedMetadata2) ? relatedMetadata2.SchemaName : "Entity",
                });
                continue;
            }

            table.Relationships.Add(new RelationshipModel
            {
                SchemaName = rel.SchemaName,
                RelationshipType = "ManyToMany",
                ThisEntityRole = "Entity2",
                ThisEntityAttribute = rel.Entity2IntersectAttribute,
                RelatedEntity = rel.Entity1LogicalName,
                RelatedEntityAttribute = rel.Entity1IntersectAttribute,
                RelatedEntitySchemaName = logicalNameToMetadata.TryGetValue(rel.Entity1LogicalName, out var relatedMetadata1) ? relatedMetadata1.SchemaName : "Entity",
            });
        }
    }
}
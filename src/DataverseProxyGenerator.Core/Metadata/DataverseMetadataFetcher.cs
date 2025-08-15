using DataverseProxyGenerator.Core.Domain;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using Microsoft.Xrm.Sdk.Query;

namespace DataverseProxyGenerator.Core.Metadata;

public class DataverseMetadataFetcher : IDataverseMetadataFetcher
{
    private const int MaxParallelism = 8;
    private readonly ServiceClient serviceClient;
    private readonly XrmFetchConfig config;

    public DataverseMetadataFetcher(ServiceClient serviceClient, XrmFetchConfig config)
    {
        this.serviceClient = serviceClient;
        this.config = config;
    }

    public async Task<IEnumerable<TableModel>> FetchMetadataAsync()
    {
        var tables = new List<TableModel>();

        // Fetch from solutions
        var metadataFromSolution = await GetEntityMetadataFromSolutionsAsync();
        var fetchedLogicalNames = metadataFromSolution.Select(m => m.LogicalName).ToHashSet(StringComparer.InvariantCulture);

        // Fetch by logical names not already fetched
        var toolLogicalNames = new List<string>() { "activityparty" };
        var logicalNamesToFetch =
            config.Entities
            .Concat(toolLogicalNames)
            .Where(name => !string.IsNullOrWhiteSpace(name) && !fetchedLogicalNames.Contains(name))
            .Distinct(StringComparer.InvariantCulture)
            .ToList();
        var metadataFromLogicalNames = await GetEntityMetadataFromLogicalNamesAsync(logicalNamesToFetch);

        var allMetadata = metadataFromSolution.Concat(metadataFromLogicalNames).ToList();

        var logicalNameToMetadata = allMetadata.ToDictionary(m => m.LogicalName, m => m, StringComparer.InvariantCulture);

        // Merge all metadata
        foreach (var metadata in allMetadata)
        {
            var table = BuildTableModelFromMetadata(logicalNameToMetadata, metadata);
            tables.Add(table);
        }

        return tables;
    }

    private async Task<List<EntityMetadata>> GetEntityMetadataFromSolutionsAsync()
    {
        var logicalNameToMetadata = new Dictionary<string, EntityMetadata>(StringComparer.InvariantCulture);
        foreach (var solutionUniqueName in config.Solutions)
        {
            var solutionId = GetSolutionId(solutionUniqueName);
            if (solutionId == Guid.Empty)
                continue;

            var entityIds = GetEntityIdsFromSolution(solutionId);

            using var semaphore = new SemaphoreSlim(MaxParallelism);
            var metadataTasks = entityIds.Select(async entityId =>
            {
                await semaphore.WaitAsync();
                try
                {
                    return await GetEntityMetadataFromIdAsync(entityId);
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

    private async Task<List<EntityMetadata>> GetEntityMetadataFromLogicalNamesAsync(IEnumerable<string> logicalNames)
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

    private Guid GetSolutionId(string solutionUniqueName)
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

    private List<Guid> GetEntityIdsFromSolution(Guid solutionId)
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

    private async Task<EntityMetadata> GetEntityMetadataFromIdAsync(Guid entityId)
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

    private TableModel BuildTableModelFromMetadata(Dictionary<string, EntityMetadata> logicalNameToMetadata, EntityMetadata entityMetadata)
    {
        var table = new TableModel
        {
            LogicalName = entityMetadata.LogicalName,
            SchemaName = entityMetadata.SchemaName,
            DisplayName = ApplyLabelMapping(entityMetadata.DisplayName?.UserLocalizedLabel?.Label ?? entityMetadata.LogicalName),
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
            var column = BuildColumnModel(attr);
            if (column != null)
            {
                table.Columns.Add(column);
            }
        }

        AddPrimaryIdColumn(table, entityMetadata);

        MapRelationships(logicalNameToMetadata, entityMetadata, table);

        return table;
    }

    private ColumnModel? BuildColumnModel(AttributeMetadata attr)
    {
        ColumnModel? column = attr switch
        {
            StringAttributeMetadata stringAttr => BuildStringColumn(stringAttr),
            MemoAttributeMetadata memoAttr => BuildMemoColumn(memoAttr),
            IntegerAttributeMetadata intAttr => BuildIntegerColumn(intAttr),
            BigIntAttributeMetadata bigIntAttr => BuildBigIntColumn(bigIntAttr),
            BooleanAttributeMetadata boolAttr => BuildBooleanColumn(boolAttr),
            DateTimeAttributeMetadata dateAttr => BuildDateTimeColumn(dateAttr),
            DecimalAttributeMetadata decAttr => BuildDecimalColumn(decAttr),
            DoubleAttributeMetadata dblAttr => BuildDoubleColumn(dblAttr),
            MoneyAttributeMetadata moneyAttr => BuildMoneyColumn(moneyAttr),
            EnumAttributeMetadata enumAttr => BuildEnumColumn(enumAttr),
            LookupAttributeMetadata lookupAttr when lookupAttr.AttributeType == AttributeTypeCode.PartyList => BuildPartyListColumn(lookupAttr),
            LookupAttributeMetadata lookupAttr => BuildLookupColumn(lookupAttr),
            FileAttributeMetadata fileAttr => BuildFileColumn(fileAttr),
            ImageAttributeMetadata imageAttr => BuildImageColumn(imageAttr),
            _ => null,
        };

        if (column != null)
        {
            column = column with
            {
                IsObsolete =
                    !string.IsNullOrEmpty(column.DisplayName) &&
                    !string.IsNullOrEmpty(config.DeprecatedPrefix) &&
                    column.DisplayName.StartsWith(config.DeprecatedPrefix, StringComparison.OrdinalIgnoreCase),
            };
        }

        return column;
    }

    private string ApplyLabelMapping(string label)
    {
        if (string.IsNullOrEmpty(label) || config.LabelMapping.Count == 0)
            return label;

        foreach (var kvp in config.LabelMapping)
        {
            if (!string.IsNullOrEmpty(kvp.Key))
                label = label.Replace(kvp.Key, kvp.Value, StringComparison.InvariantCulture);
        }

        return label;
    }

    private void AddPrimaryIdColumn(TableModel table, EntityMetadata entityMetadata)
    {
        var primaryIdAttribute = Array.Find(entityMetadata.Attributes, x => x.LogicalName == entityMetadata.PrimaryIdAttribute);

        var primaryIdColumn = new PrimaryIdColumnModel
        {
            LogicalName = entityMetadata.PrimaryIdAttribute,
            SchemaName = primaryIdAttribute?.SchemaName ?? entityMetadata.PrimaryIdAttribute,
            DisplayName = ApplyLabelMapping(primaryIdAttribute?.DisplayName?.UserLocalizedLabel?.Label ?? entityMetadata.PrimaryIdAttribute),
            IsObsolete = !string.IsNullOrEmpty(entityMetadata.PrimaryNameAttribute) &&
                         !string.IsNullOrEmpty(config.DeprecatedPrefix) &&
                         entityMetadata.PrimaryNameAttribute.StartsWith(config.DeprecatedPrefix, StringComparison.OrdinalIgnoreCase),
        };
        table.Columns.Add(primaryIdColumn);
    }

    private StringColumnModel BuildStringColumn(StringAttributeMetadata attr) => new StringColumnModel
    {
        LogicalName = attr.LogicalName,
        SchemaName = attr.SchemaName,
        DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName),
        Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty),
        MaxLength = attr.MaxLength,
    };

    private MemoColumnModel BuildMemoColumn(MemoAttributeMetadata attr) => new MemoColumnModel
    {
        LogicalName = attr.LogicalName,
        SchemaName = attr.SchemaName,
        DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName),
        Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty),
        MaxLength = attr.MaxLength,
    };

    private IntegerColumnModel BuildIntegerColumn(IntegerAttributeMetadata attr) => new IntegerColumnModel
    {
        LogicalName = attr.LogicalName,
        SchemaName = attr.SchemaName,
        DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName),
        Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty),
        Min = attr.MinValue ?? int.MinValue,
        Max = attr.MaxValue ?? int.MaxValue,
    };

    private BigIntColumnModel BuildBigIntColumn(BigIntAttributeMetadata attr) => new BigIntColumnModel
    {
        LogicalName = attr.LogicalName,
        SchemaName = attr.SchemaName,
        DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName),
        Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty),
    };

    private BooleanColumnModel BuildBooleanColumn(BooleanAttributeMetadata attr) => new BooleanColumnModel
    {
        LogicalName = attr.LogicalName,
        SchemaName = attr.SchemaName,
        DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName),
        Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty),
    };

    private DateTimeColumnModel BuildDateTimeColumn(DateTimeAttributeMetadata attr) => new DateTimeColumnModel
    {
        LogicalName = attr.LogicalName,
        SchemaName = attr.SchemaName,
        DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName),
        Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty),
    };

    private DecimalColumnModel BuildDecimalColumn(DecimalAttributeMetadata attr) => new DecimalColumnModel
    {
        LogicalName = attr.LogicalName,
        SchemaName = attr.SchemaName,
        DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName),
        Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty),
        Precision = attr.Precision,
    };

    private DoubleColumnModel BuildDoubleColumn(DoubleAttributeMetadata attr) => new DoubleColumnModel
    {
        LogicalName = attr.LogicalName,
        SchemaName = attr.SchemaName,
        DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName),
        Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty),
    };

    private MoneyColumnModel BuildMoneyColumn(MoneyAttributeMetadata attr) => new MoneyColumnModel
    {
        LogicalName = attr.LogicalName,
        SchemaName = attr.SchemaName,
        DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName),
        Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty),
        Precision = attr.Precision,
    };

    private EnumColumnModel BuildEnumColumn(EnumAttributeMetadata attr)
    {
        var optionsetValues = attr.OptionSet?.Options?
            .Where(o => o.Value != null)
            .ToDictionary(
                o => o.Value.GetValueOrDefault(),
                o =>
                {
                    var label = o.Label?.UserLocalizedLabel?.Label;
                    label = ApplyLabelMapping(label ?? string.Empty);
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
            DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName),
            Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty),
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

    private PartyListColumnModel BuildPartyListColumn(LookupAttributeMetadata attr) => new PartyListColumnModel
    {
        LogicalName = attr.LogicalName,
        SchemaName = attr.SchemaName,
        DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName),
        Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty),
    };

    private LookupColumnModel BuildLookupColumn(LookupAttributeMetadata attr) => new LookupColumnModel
    {
        LogicalName = attr.LogicalName,
        SchemaName = attr.SchemaName,
        DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName),
        Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty),
        TargetTable = attr.Targets?.FirstOrDefault() ?? "Unknown",
    };

    private FileColumnModel BuildFileColumn(FileAttributeMetadata attr) => new FileColumnModel
    {
        LogicalName = attr.LogicalName,
        SchemaName = attr.SchemaName,
        DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName),
        Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty),
    };

    private ImageColumnModel BuildImageColumn(ImageAttributeMetadata attr) => new ImageColumnModel
    {
        LogicalName = attr.LogicalName,
        SchemaName = attr.SchemaName,
        DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName),
        Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty),
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
            if (!logicalNameToMetadata.TryGetValue(rel.ReferencedEntity, out var relatedMetadata))
                continue;

            table.Relationships.Add(new RelationshipModel
            {
                SchemaName = rel.SchemaName,
                RelationshipType = "ManyToOne",
                ThisEntityRole = "Referencing",
                ThisEntityAttribute = rel.ReferencingAttribute,
                RelatedEntity = rel.ReferencedEntity,
                RelatedEntityAttribute = rel.ReferencedAttribute,
                RelatedEntitySchemaName = relatedMetadata.SchemaName,
            });
        }
    }

    private static void MapOneToMany(Dictionary<string, EntityMetadata> logicalNameToMetadata, EntityMetadata entityMetadata, TableModel table)
    {
        foreach (var rel in entityMetadata.OneToManyRelationships.Where(x => x.ReferencingEntity != entityMetadata.LogicalName))
        {
            if (!logicalNameToMetadata.TryGetValue(rel.ReferencingEntity, out var relatedMetadata))
                continue;

            table.Relationships.Add(new RelationshipModel
            {
                SchemaName = rel.SchemaName,
                RelationshipType = "OneToMany",
                ThisEntityRole = "Referenced",
                ThisEntityAttribute = rel.ReferencedAttribute,
                RelatedEntity = rel.ReferencingEntity,
                RelatedEntityAttribute = rel.ReferencingAttribute,
                RelatedEntitySchemaName = relatedMetadata.SchemaName,
            });
        }
    }

    private static void MapManyToMany(Dictionary<string, EntityMetadata> logicalNameToMetadata, EntityMetadata entityMetadata, TableModel table)
    {
        foreach (var rel in entityMetadata.ManyToManyRelationships.Where(x => logicalNameToMetadata.ContainsKey(x.Entity1LogicalName) && logicalNameToMetadata.ContainsKey(x.Entity2LogicalName)))
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

    public async Task<IEnumerable<CustomApiModel>> FetchCustomApisAsync()
    {
        var customApis = new List<CustomApiModel>();

        // Fetch custom APIs from solutions
        foreach (var solutionUniqueName in config.Solutions)
        {
            var solutionId = GetSolutionId(solutionUniqueName);
            if (solutionId == Guid.Empty)
                continue;

            var customApiIds = await GetCustomApiIdsFromSolutionAsync(solutionId);
            using var semaphore = new SemaphoreSlim(MaxParallelism);
            var customApiTasks = customApiIds.Select(async customApiId =>
            {
                await semaphore.WaitAsync();
                try
                {
                    return await GetCustomApiFromIdAsync(customApiId);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            var results = await Task.WhenAll(customApiTasks);
            customApis.AddRange(results.Where(api => api != null)!);
        }

        return customApis;
    }

    private async Task<List<Guid>> GetCustomApiIdsFromSolutionAsync(Guid solutionId)
    {
        var componentQuery = new QueryExpression("solutioncomponent")
        {
            ColumnSet = new ColumnSet("objectid"),
            Criteria = new FilterExpression
            {
                Conditions =
                {
                    new ConditionExpression("solutionid", ConditionOperator.Equal, solutionId),
                    new ConditionExpression("componenttype", ConditionOperator.Equal, 10026), // Custom API component type
                },
            },
        };
        var result = await serviceClient.RetrieveMultipleAsync(componentQuery);
        return result.Entities
            .Where(c => c.Contains("objectid") && c["objectid"] is Guid)
            .Select(c => (Guid)c["objectid"])
            .ToList();
    }

    private async Task<CustomApiModel?> GetCustomApiFromIdAsync(Guid customApiId)
    {
        try
        {
            // Fetch the custom API
            var customApiQuery = new QueryExpression("customapi")
            {
                ColumnSet = new ColumnSet("uniquename", "displayname", "description", "isfunction"),
                Criteria = new FilterExpression
                {
                    Conditions =
                    {
                        new ConditionExpression("customapiid", ConditionOperator.Equal, customApiId),
                    },
                },
            };

            var customApiResult = await serviceClient.RetrieveMultipleAsync(customApiQuery);
            var customApiEntity = customApiResult.Entities.FirstOrDefault();
            if (customApiEntity == null)
                return null;

            var customApi = new CustomApiModel
            {
                UniqueName = customApiEntity.GetAttributeValue<string>("uniquename") ?? string.Empty,
                DisplayName = customApiEntity.GetAttributeValue<string>("displayname") ?? string.Empty,
                Description = customApiEntity.GetAttributeValue<string>("description") ?? string.Empty,
                IsFunction = customApiEntity.GetAttributeValue<bool>("isfunction"),
            };

            var requestParameters = await FetchCustomApiRequestParametersAsync(customApiId);
            var responseProperties = await FetchCustomApiResponsePropertiesAsync(customApiId);

            return customApi with
            {
                RequestParameters = requestParameters,
                ResponseProperties = responseProperties,
            };
        }
        catch (InvalidOperationException)
        {
            // Log or handle the exception as needed
            return null;
        }
        catch (ArgumentException)
        {
            // Log or handle the exception as needed
            return null;
        }
    }

    private async Task<IList<CustomApiParameterModel>> FetchCustomApiRequestParametersAsync(Guid customApiId)
    {
        var requestParametersQuery = new QueryExpression("customapirequestparameter")
        {
            ColumnSet = new ColumnSet("name", "uniquename", "displayname", "description", "type", "isoptional", "logicalentityname"),
            Criteria = new FilterExpression
            {
                Conditions =
                {
                    new ConditionExpression("customapiid", ConditionOperator.Equal, customApiId),
                },
            },
        };

        var requestParametersResult = await serviceClient.RetrieveMultipleAsync(requestParametersQuery);
        return requestParametersResult.Entities
            .Select(param => new CustomApiParameterModel
            {
                Name = param.GetAttributeValue<string>("name") ?? string.Empty,
                UniqueName = param.GetAttributeValue<string>("uniquename") ?? string.Empty,
                DisplayName = param.GetAttributeValue<string>("displayname") ?? string.Empty,
                Description = param.GetAttributeValue<string>("description") ?? string.Empty,
                Type = (CustomApiParameterType)param.GetAttributeValue<OptionSetValue>("type").Value,
                IsOptional = param.GetAttributeValue<bool>("isoptional"),
                LogicalEntityName = param.GetAttributeValue<string>("logicalentityname"),
            })
            .ToList();
    }

    private async Task<IList<CustomApiParameterModel>> FetchCustomApiResponsePropertiesAsync(Guid customApiId)
    {
        var responsePropertiesQuery = new QueryExpression("customapiresponseproperty")
        {
            ColumnSet = new ColumnSet("name", "uniquename", "displayname", "description", "type", "logicalentityname"),
            Criteria = new FilterExpression
            {
                Conditions =
                {
                    new ConditionExpression("customapiid", ConditionOperator.Equal, customApiId),
                },
            },
        };

        var responsePropertiesResult = await serviceClient.RetrieveMultipleAsync(responsePropertiesQuery);
        return responsePropertiesResult.Entities
            .Select(prop => new CustomApiParameterModel
            {
                Name = prop.GetAttributeValue<string>("name") ?? string.Empty,
                UniqueName = prop.GetAttributeValue<string>("uniquename") ?? string.Empty,
                DisplayName = prop.GetAttributeValue<string>("displayname") ?? string.Empty,
                Description = prop.GetAttributeValue<string>("description") ?? string.Empty,
                Type = (CustomApiParameterType)prop.GetAttributeValue<OptionSetValue>("type").Value,
                IsOptional = false, // Response properties are always required
                LogicalEntityName = prop.GetAttributeValue<string>("logicalentityname"),
            })
            .ToList();
    }
}
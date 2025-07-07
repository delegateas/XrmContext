using System.Collections.Generic;
using System.Threading.Tasks;
using DataverseProxyGenerator.Core.Domain;
using Microsoft.PowerPlatform.Dataverse.Client;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System.Linq;

namespace DataverseProxyGenerator.Core.Metadata
{
    public class DataverseMetadataFetcher : IDataverseMetadataFetcher
    {
        /// <inheritdoc />
        public async Task<List<TableModel>> FetchMetadataAsync(
            ServiceClient serviceClient,
            IEnumerable<string> solutionUniqueNames,
            IEnumerable<string> logicalNames,
            string? deprecatedPrefix,
            Dictionary<string, string> labelMapping)
        {
            var tables = new List<TableModel>();

            // Fetch from solutions
            var metadataFromSolution = await GetEntityMetadataFromSolutionsAsync(serviceClient, solutionUniqueNames ?? []);
            var fetchedLogicalNames = new HashSet<string>(metadataFromSolution.Select(m => m.LogicalName));

            // Fetch by logical names not already fetched
            var logicalNamesToFetch = (logicalNames ?? []).Where(name => !string.IsNullOrWhiteSpace(name) && !fetchedLogicalNames.Contains(name)).Distinct().ToList();
            var metadataFromLogicalNames = await GetEntityMetadataFromLogicalNamesAsync(serviceClient, logicalNamesToFetch);

            var allMetadata = metadataFromSolution.Concat(metadataFromLogicalNames);

            // Merge all metadata
            foreach (var metadata in allMetadata)
            {
                var table = BuildTableModelFromMetadata(allMetadata, metadata, deprecatedPrefix, labelMapping);
                tables.Add(table);
            }

            return tables;
        }

        private async Task<List<EntityMetadata>> GetEntityMetadataFromSolutionsAsync(ServiceClient serviceClient, IEnumerable<string> solutionUniqueNames)
        {
            var logicalNameToMetadata = new Dictionary<string, EntityMetadata>();
            foreach (var solutionUniqueName in solutionUniqueNames)
            {
                var solutionId = GetSolutionId(serviceClient, solutionUniqueName);
                if (solutionId == Guid.Empty)
                    continue;

                var entityIds = GetEntityIdsFromSolution(serviceClient, solutionId);

                var metadataTasks = entityIds.Select(entityId => GetEntityMetadataFromIdAsync(serviceClient, entityId));
                var metadata = await Task.WhenAll(metadataTasks);

                foreach (var m in metadata)
                {
                    if (!string.IsNullOrEmpty(m.LogicalName) && !logicalNameToMetadata.ContainsKey(m.LogicalName))
                        logicalNameToMetadata.Add(m.LogicalName, m);
                }
            }
            return logicalNameToMetadata.Values.ToList();
        }

        private async Task<List<EntityMetadata>> GetEntityMetadataFromLogicalNamesAsync(ServiceClient serviceClient, IEnumerable<string> logicalNames)
        {
            var metadataList = new List<EntityMetadata>();
            foreach (var logicalName in logicalNames)
            {
                var entityRequest = new Microsoft.Xrm.Sdk.Messages.RetrieveEntityRequest
                {
                    LogicalName = logicalName,
                    EntityFilters = EntityFilters.Entity | EntityFilters.Attributes | EntityFilters.Relationships,
                    RetrieveAsIfPublished = true
                };

                var entityResponse = (Microsoft.Xrm.Sdk.Messages.RetrieveEntityResponse)await serviceClient.ExecuteAsync(entityRequest);
                if (entityResponse?.EntityMetadata != null)
                {
                    metadataList.Add(entityResponse.EntityMetadata);
                }
            }
            return metadataList;
        }

        private Guid GetSolutionId(ServiceClient serviceClient, string solutionUniqueName)
        {
            var solutionQuery = new Microsoft.Xrm.Sdk.Query.QueryExpression("solution")
            {
                ColumnSet = new Microsoft.Xrm.Sdk.Query.ColumnSet("solutionid"),
                Criteria =
                {
                    Conditions =
                    {
                        new Microsoft.Xrm.Sdk.Query.ConditionExpression("uniquename", Microsoft.Xrm.Sdk.Query.ConditionOperator.Equal, solutionUniqueName)
                    }
                }
            };
            var solutionEntity = serviceClient.RetrieveMultiple(solutionQuery).Entities.FirstOrDefault();
            return solutionEntity?.Id ?? Guid.Empty;
        }

        private List<Guid> GetEntityIdsFromSolution(ServiceClient serviceClient, Guid solutionId)
        {
            var entityIds = new List<Guid>();
            var componentQuery = new Microsoft.Xrm.Sdk.Query.QueryExpression("solutioncomponent")
            {
                ColumnSet = new Microsoft.Xrm.Sdk.Query.ColumnSet("objectid"),
                Criteria =
                {
                    Conditions =
                    {
                        new Microsoft.Xrm.Sdk.Query.ConditionExpression("solutionid", Microsoft.Xrm.Sdk.Query.ConditionOperator.Equal, solutionId),
                        new Microsoft.Xrm.Sdk.Query.ConditionExpression("componenttype", Microsoft.Xrm.Sdk.Query.ConditionOperator.Equal, 1) // 1 = Entity
                    }
                }
            };
            var componentEntities = serviceClient.RetrieveMultiple(componentQuery).Entities;
            foreach (var component in componentEntities)
            {
                if (component.Contains("objectid") && component["objectid"] is Guid entityId)
                    entityIds.Add(entityId);
            }
            return entityIds;
        }

        private static async Task<EntityMetadata> GetEntityMetadataFromIdAsync(ServiceClient serviceClient, Guid entityId)
        {
            var entityRequest = new Microsoft.Xrm.Sdk.Messages.RetrieveEntityRequest
            {
                MetadataId = entityId,
                EntityFilters = EntityFilters.Entity | EntityFilters.Attributes | EntityFilters.Relationships,
                RetrieveAsIfPublished = true
            };
            var entityResponse = (Microsoft.Xrm.Sdk.Messages.RetrieveEntityResponse)await serviceClient.ExecuteAsync(entityRequest);
            return entityResponse.EntityMetadata;
        }

        private TableModel BuildTableModelFromMetadata(IEnumerable<EntityMetadata> allMetadata, EntityMetadata entityMetadata, string? deprecatedPrefix, Dictionary<string, string> labelMapping)
        {
            var table = new TableModel
            {
                LogicalName = entityMetadata.LogicalName,
                SchemaName = entityMetadata.SchemaName,
                DisplayName = ApplyLabelMapping(entityMetadata.DisplayName?.UserLocalizedLabel?.Label ?? entityMetadata.LogicalName, labelMapping),
                EntityTypeCode = entityMetadata.ObjectTypeCode ?? 0,
                PrimaryNameAttribute = entityMetadata.PrimaryNameAttribute,
                PrimaryIdAttribute = entityMetadata.PrimaryIdAttribute,
                Columns = new List<ColumnModel>(),
                Relationships = new List<RelationshipModel>()
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

            MapRelationships(allMetadata, entityMetadata, table);

            return table;
        }

        private ColumnModel? BuildColumnModel(AttributeMetadata attr, string? deprecatedPrefix, Dictionary<string, string> labelMapping)
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
                LookupAttributeMetadata lookupAttr => BuildLookupColumn(lookupAttr, labelMapping),
                FileAttributeMetadata fileAttr => BuildFileColumn(fileAttr, labelMapping),
                ImageAttributeMetadata imageAttr => BuildImageColumn(imageAttr, labelMapping),
                _ => null
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

        private string ApplyLabelMapping(string label, Dictionary<string, string> mapping)
        {
            if (string.IsNullOrEmpty(label) || mapping == null || mapping.Count == 0)
                return label;
            foreach (var kvp in mapping)
            {
                if (!string.IsNullOrEmpty(kvp.Key))
                    label = label.Replace(kvp.Key, kvp.Value);
            }
            return label;
        }

        private void AddPrimaryIdColumn(TableModel table, EntityMetadata entityMetadata, string? deprecatedPrefix, Dictionary<string, string> labelMapping)
        {
            var primaryIdAttribute = entityMetadata.Attributes.Where(x => x.LogicalName == entityMetadata.PrimaryIdAttribute).FirstOrDefault();

            var primaryIdColumn = new PrimaryIdColumnModel
            {
                LogicalName = entityMetadata.PrimaryIdAttribute,
                SchemaName = primaryIdAttribute?.SchemaName ?? entityMetadata.PrimaryIdAttribute,
                DisplayName = ApplyLabelMapping(primaryIdAttribute?.DisplayName?.UserLocalizedLabel?.Label ?? entityMetadata.PrimaryIdAttribute, labelMapping),
                IsNullable = false,
                IsObsolete = !string.IsNullOrEmpty(entityMetadata.PrimaryNameAttribute) &&
                             !string.IsNullOrEmpty(deprecatedPrefix) &&
                             entityMetadata.PrimaryNameAttribute.StartsWith(deprecatedPrefix, StringComparison.OrdinalIgnoreCase)
            };
            table.Columns.Add(primaryIdColumn);
        }

        private StringColumnModel BuildStringColumn(StringAttributeMetadata attr, Dictionary<string, string> labelMapping) => new StringColumnModel
        {
            LogicalName = attr.LogicalName,
            SchemaName = attr.SchemaName,
            DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName, labelMapping),
            Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty, labelMapping),
            IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired,
            MaxLength = attr.MaxLength
        };

        private MemoColumnModel BuildMemoColumn(MemoAttributeMetadata attr, Dictionary<string, string> labelMapping) => new MemoColumnModel
        {
            LogicalName = attr.LogicalName,
            SchemaName = attr.SchemaName,
            DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName, labelMapping),
            Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty, labelMapping),
            IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired,
            MaxLength = attr.MaxLength
        };

        private IntegerColumnModel BuildIntegerColumn(IntegerAttributeMetadata attr, Dictionary<string, string> labelMapping) => new IntegerColumnModel
        {
            LogicalName = attr.LogicalName,
            SchemaName = attr.SchemaName,
            DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName, labelMapping),
            Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty, labelMapping),
            IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired,
            Min = attr.MinValue ?? int.MinValue,
            Max = attr.MaxValue ?? int.MaxValue
        };

        private BigIntColumnModel BuildBigIntColumn(BigIntAttributeMetadata attr, Dictionary<string, string> labelMapping) => new BigIntColumnModel
        {
            LogicalName = attr.LogicalName,
            SchemaName = attr.SchemaName,
            DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName, labelMapping),
            Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty, labelMapping),
            IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired
        };

        private BooleanColumnModel BuildBooleanColumn(BooleanAttributeMetadata attr, Dictionary<string, string> labelMapping) => new BooleanColumnModel
        {
            LogicalName = attr.LogicalName,
            SchemaName = attr.SchemaName,
            DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName, labelMapping),
            Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty, labelMapping),
            IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired
        };

        private DateTimeColumnModel BuildDateTimeColumn(DateTimeAttributeMetadata attr, Dictionary<string, string> labelMapping) => new DateTimeColumnModel
        {
            LogicalName = attr.LogicalName,
            SchemaName = attr.SchemaName,
            DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName, labelMapping),
            Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty, labelMapping),
            IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired
        };

        private DecimalColumnModel BuildDecimalColumn(DecimalAttributeMetadata attr, Dictionary<string, string> labelMapping) => new DecimalColumnModel
        {
            LogicalName = attr.LogicalName,
            SchemaName = attr.SchemaName,
            DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName, labelMapping),
            Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty, labelMapping),
            IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired,
            Precision = attr.Precision
        };

        private DoubleColumnModel BuildDoubleColumn(DoubleAttributeMetadata attr, Dictionary<string, string> labelMapping) => new DoubleColumnModel
        {
            LogicalName = attr.LogicalName,
            SchemaName = attr.SchemaName,
            DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName, labelMapping),
            Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty, labelMapping),
            IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired
        };

        private MoneyColumnModel BuildMoneyColumn(MoneyAttributeMetadata attr, Dictionary<string, string> labelMapping) => new MoneyColumnModel
        {
            LogicalName = attr.LogicalName,
            SchemaName = attr.SchemaName,
            DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName, labelMapping),
            Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty, labelMapping),
            IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired,
            Precision = attr.Precision
        };

        private EnumColumnModel BuildEnumColumn(EnumAttributeMetadata attr, Dictionary<string, string> labelMapping)
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
                        label = label.Replace(" ", "_")
                                     .Replace("-", "_")
                                     .Replace(".", "_")
                                     .Replace(",", "_")
                                     .Replace(":", "_")
                                     .Replace(";", "_")
                                     .Replace("/", "_")
                                     .Replace("\\", "_");
                        if (char.IsDigit(label[0]))
                        {
                            label = "_" + label;
                        }
                        return label;
                    }
                ) ?? new Dictionary<int, string>();

            // Build OptionLocalizations: option value -> (LCID -> label)
            var optionLocalizations = new Dictionary<int, Dictionary<int, string>>();
            if (attr.OptionSet?.Options != null)
            {
                foreach (var o in attr.OptionSet.Options)
                {
                    if (o.Value == null) continue;
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
                OptionLocalizations = optionLocalizations
            };
        }

        private LookupColumnModel BuildLookupColumn(LookupAttributeMetadata attr, Dictionary<string, string> labelMapping) => new LookupColumnModel
        {
            LogicalName = attr.LogicalName,
            SchemaName = attr.SchemaName,
            DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName, labelMapping),
            Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty, labelMapping),
            IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired,
            TargetTable = attr.Targets?.FirstOrDefault() ?? "Unknown",
        };

        private FileColumnModel BuildFileColumn(FileAttributeMetadata attr, Dictionary<string, string> labelMapping) => new FileColumnModel
        {
            LogicalName = attr.LogicalName,
            SchemaName = attr.SchemaName,
            DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName, labelMapping),
            Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty, labelMapping),
            IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired
        };

        private ImageColumnModel BuildImageColumn(ImageAttributeMetadata attr, Dictionary<string, string> labelMapping) => new ImageColumnModel
        {
            LogicalName = attr.LogicalName,
            SchemaName = attr.SchemaName,
            DisplayName = ApplyLabelMapping(attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName, labelMapping),
            Description = ApplyLabelMapping(attr.Description?.UserLocalizedLabel?.Label ?? string.Empty, labelMapping),
            IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired
        };

        private void MapRelationships(IEnumerable<EntityMetadata> allMetadata, EntityMetadata entityMetadata, TableModel table)
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
                    RelatedEntitySchemaName = allMetadata.FirstOrDefault(x => x.LogicalName == rel.ReferencedEntity)?.SchemaName ?? "Entity",
                });
            }
            foreach (var rel in entityMetadata.OneToManyRelationships)
            {
                table.Relationships.Add(new RelationshipModel
                {
                    SchemaName = rel.SchemaName,
                    RelationshipType = "OneToMany",
                    ThisEntityRole = "Referenced",
                    ThisEntityAttribute = rel.ReferencedAttribute,
                    RelatedEntity = rel.ReferencingEntity,
                    RelatedEntityAttribute = rel.ReferencingAttribute,
                    RelatedEntitySchemaName = allMetadata.FirstOrDefault(x => x.LogicalName == rel.ReferencedEntity)?.SchemaName ?? "Entity",
                });
            }
            foreach (var rel in entityMetadata.ManyToManyRelationships)
            {
                table.Relationships.Add(new RelationshipModel
                {
                    SchemaName = rel.SchemaName,
                    RelationshipType = "ManyToMany",
                    ThisEntityRole = "Entity1",
                    ThisEntityAttribute = rel.Entity1IntersectAttribute,
                    RelatedEntity = rel.Entity2LogicalName,
                    RelatedEntityAttribute = rel.Entity2IntersectAttribute,
                    RelatedEntitySchemaName = allMetadata.FirstOrDefault(x => x.LogicalName == rel.Entity2LogicalName)?.SchemaName ?? "Entity",
                });
                table.Relationships.Add(new RelationshipModel
                {
                    SchemaName = rel.SchemaName,
                    RelationshipType = "ManyToMany",
                    ThisEntityRole = "Entity2",
                    ThisEntityAttribute = rel.Entity2IntersectAttribute,
                    RelatedEntity = rel.Entity1LogicalName,
                    RelatedEntityAttribute = rel.Entity1IntersectAttribute,
                    RelatedEntitySchemaName = allMetadata.FirstOrDefault(x => x.LogicalName == rel.Entity1LogicalName)?.SchemaName ?? "Entity",
                });
            }
        }
    }
}

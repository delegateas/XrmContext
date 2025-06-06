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
            string? deprecatedPrefix)
        {
            var tables = new List<TableModel>();

            // Fetch from solutions
            var metadataFromSolution = await GetEntityMetadataFromSolutionsAsync(serviceClient, solutionUniqueNames ?? []);
            var fetchedLogicalNames = new HashSet<string>(metadataFromSolution.Select(m => m.LogicalName));

            // Fetch by logical names not already fetched
            var logicalNamesToFetch = (logicalNames ?? []).Where(name => !string.IsNullOrWhiteSpace(name) && !fetchedLogicalNames.Contains(name)).Distinct().ToList();
            var metadataFromLogicalNames = await GetEntityMetadataFromLogicalNamesAsync(serviceClient, logicalNamesToFetch);

            // Merge all metadata
            foreach (var metadata in metadataFromSolution.Concat(metadataFromLogicalNames))
            {
                var table = BuildTableModelFromMetadata(metadata, deprecatedPrefix);
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
        
        private TableModel BuildTableModelFromMetadata(EntityMetadata entityMetadata, string? deprecatedPrefix)
        {
            var table = new TableModel
            {
                LogicalName = entityMetadata.LogicalName,
                SchemaName = entityMetadata.SchemaName,
                DisplayName = entityMetadata.DisplayName?.UserLocalizedLabel?.Label ?? entityMetadata.LogicalName,
                Columns = new List<ColumnModel>(),
                Relationships = new List<RelationshipModel>()
            };

            var validAttributes = entityMetadata.Attributes
                .Where(x => x.AttributeOf == null)
                .ToList();

            foreach (var attr in validAttributes)
            {
                var column = BuildColumnModel(attr, deprecatedPrefix);
                if (column != null)
                {
                    table.Columns.Add(column);
                }
            }

            MapRelationships(entityMetadata, table);

            return table;
        }

        private ColumnModel? BuildColumnModel(AttributeMetadata attr, string? deprecatedPrefix)
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
                LookupAttributeMetadata lookupAttr => BuildLookupColumn(lookupAttr),
                FileAttributeMetadata fileAttr => BuildFileColumn(fileAttr),
                ImageAttributeMetadata imageAttr => BuildImageColumn(imageAttr),
                UniqueIdentifierAttributeMetadata guidAttr => BuildUniqueIdentifierColumn(guidAttr),
                _ => null
            };

            if (column != null)
            {
                column = column with
                {
                    IsObsolete =
                        !string.IsNullOrEmpty(column.DisplayName) &&
                        deprecatedPrefix != null &&
                        column.DisplayName.StartsWith(deprecatedPrefix, StringComparison.OrdinalIgnoreCase),
                };
            }

            return column;
        }

        private StringColumnModel BuildStringColumn(StringAttributeMetadata attr) => new StringColumnModel
        {
            LogicalName = attr.LogicalName,
            SchemaName = attr.SchemaName,
            DisplayName = attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName,
            Description = attr.Description?.UserLocalizedLabel?.Label,
            IsPrimaryKey = attr.IsPrimaryId ?? false,
            IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired,
            MaxLength = attr.MaxLength
        };

        private MemoColumnModel BuildMemoColumn(MemoAttributeMetadata attr) => new MemoColumnModel
        {
            LogicalName = attr.LogicalName,
            SchemaName = attr.SchemaName,
            DisplayName = attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName,
            Description = attr.Description?.UserLocalizedLabel?.Label,
            IsPrimaryKey = false,
            IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired,
            MaxLength = attr.MaxLength
        };

        private IntegerColumnModel BuildIntegerColumn(IntegerAttributeMetadata attr) => new IntegerColumnModel
        {
            LogicalName = attr.LogicalName,
            SchemaName = attr.SchemaName,
            DisplayName = attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName,
            Description = attr.Description?.UserLocalizedLabel?.Label,
            IsPrimaryKey = false,
            IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired
        };

        private BigIntColumnModel BuildBigIntColumn(BigIntAttributeMetadata attr) => new BigIntColumnModel
        {
            LogicalName = attr.LogicalName,
            SchemaName = attr.SchemaName,
            DisplayName = attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName,
            Description = attr.Description?.UserLocalizedLabel?.Label,
            IsPrimaryKey = false,
            IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired
        };

        private BooleanColumnModel BuildBooleanColumn(BooleanAttributeMetadata attr) => new BooleanColumnModel
        {
            LogicalName = attr.LogicalName,
            SchemaName = attr.SchemaName,
            DisplayName = attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName,
            Description = attr.Description?.UserLocalizedLabel?.Label,
            IsPrimaryKey = false,
            IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired
        };

        private DateTimeColumnModel BuildDateTimeColumn(DateTimeAttributeMetadata attr) => new DateTimeColumnModel
        {
            LogicalName = attr.LogicalName,
            SchemaName = attr.SchemaName,
            DisplayName = attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName,
            Description = attr.Description?.UserLocalizedLabel?.Label,
            IsPrimaryKey = false,
            IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired
        };

        private DecimalColumnModel BuildDecimalColumn(DecimalAttributeMetadata attr) => new DecimalColumnModel
        {
            LogicalName = attr.LogicalName,
            SchemaName = attr.SchemaName,
            DisplayName = attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName,
            Description = attr.Description?.UserLocalizedLabel?.Label,
            IsPrimaryKey = false,
            IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired,
            Precision = attr.Precision
        };

        private DoubleColumnModel BuildDoubleColumn(DoubleAttributeMetadata attr) => new DoubleColumnModel
        {
            LogicalName = attr.LogicalName,
            SchemaName = attr.SchemaName,
            DisplayName = attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName,
            Description = attr.Description?.UserLocalizedLabel?.Label,
            IsPrimaryKey = false,
            IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired
        };

        private MoneyColumnModel BuildMoneyColumn(MoneyAttributeMetadata attr) => new MoneyColumnModel
        {
            LogicalName = attr.LogicalName,
            SchemaName = attr.SchemaName,
            DisplayName = attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName,
            Description = attr.Description?.UserLocalizedLabel?.Label,
            IsPrimaryKey = false,
            IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired,
            Precision = attr.Precision
        };

        private EnumColumnModel BuildEnumColumn(EnumAttributeMetadata attr) => new EnumColumnModel
        {
            LogicalName = attr.LogicalName,
            SchemaName = attr.SchemaName,
            DisplayName = attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName,
            Description = attr.Description?.UserLocalizedLabel?.Label,
            IsPrimaryKey = false,
            IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired,
            OptionsetName = attr.OptionSet?.Name ?? attr.LogicalName,
            IsGlobalOptionset = attr.OptionSet?.IsGlobal ?? false,
            OptionsetValues = attr.OptionSet?.Options?
                .Where(o => o.Value != null)
                .ToDictionary(
                    o => o.Value.GetValueOrDefault(),
                    o =>
                    {
                        var label = o.Label?.UserLocalizedLabel?.Label;
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
                ) ?? new Dictionary<int, string>()
        };

        private LookupColumnModel BuildLookupColumn(LookupAttributeMetadata attr) => new LookupColumnModel
        {
            LogicalName = attr.LogicalName,
            SchemaName = attr.SchemaName,
            DisplayName = attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName,
            Description = attr.Description?.UserLocalizedLabel?.Label,
            IsPrimaryKey = false,
            IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired,
            TargetTable = attr.Targets?.FirstOrDefault() ?? "Unknown",
        };

        private FileColumnModel BuildFileColumn(FileAttributeMetadata attr) => new FileColumnModel
        {
            LogicalName = attr.LogicalName,
            SchemaName = attr.SchemaName,
            DisplayName = attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName,
            Description = attr.Description?.UserLocalizedLabel?.Label,
            IsPrimaryKey = false,
            IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired
        };

        private ImageColumnModel BuildImageColumn(ImageAttributeMetadata attr) => new ImageColumnModel
        {
            LogicalName = attr.LogicalName,
            SchemaName = attr.SchemaName,
            DisplayName = attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName,
            Description = attr.Description?.UserLocalizedLabel?.Label,
            IsPrimaryKey = false,
            IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired
        };

        private UniqueIdentifierColumnModel BuildUniqueIdentifierColumn(UniqueIdentifierAttributeMetadata attr) => new UniqueIdentifierColumnModel
        {
            LogicalName = attr.LogicalName,
            SchemaName = attr.SchemaName,
            DisplayName = attr.DisplayName?.UserLocalizedLabel?.Label ?? attr.LogicalName,
            Description = attr.Description?.UserLocalizedLabel?.Label,
            IsPrimaryKey = attr.IsPrimaryId ?? false,
            IsNullable = attr.RequiredLevel?.Value != AttributeRequiredLevel.ApplicationRequired,
            IsUniqueIdentifier = true
        };

        private void MapRelationships(EntityMetadata entityMetadata, TableModel table)
        {
            foreach (var rel in entityMetadata.ManyToOneRelationships)
            {
                table.Relationships.Add(new RelationshipModel
                {
                    RelationshipName = rel.SchemaName,
                    RelatedTable = rel.ReferencedEntity,
                    RelationshipType = "ManyToOne"
                });
            }
            foreach (var rel in entityMetadata.OneToManyRelationships)
            {
                table.Relationships.Add(new RelationshipModel
                {
                    RelationshipName = rel.SchemaName,
                    RelatedTable = rel.ReferencedEntity,
                    RelationshipType = "OneToMany"
                });
            }
            foreach (var rel in entityMetadata.ManyToManyRelationships)
            {
                table.Relationships.Add(new RelationshipModel
                {
                    RelationshipName = rel.SchemaName,
                    RelatedTable = rel.Entity1LogicalName,
                    RelationshipType = "ManyToMany"
                });
            }
        }
    }
}

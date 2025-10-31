using DataverseProxyGenerator.Core.Domain;

namespace DataverseProxyGenerator.Core.Generation.Utilities;

public static class TypeSignatureHelper
{
    /// <summary>
    /// Gets the C# type signature for a column model.
    /// </summary>
    /// <param name="column">The column model.</param>
    /// <returns>The C# type signature string.</returns>
    public static string GetPropertyTypeSignature(ColumnModel column)
    {
        ArgumentNullException.ThrowIfNull(column);

        return column switch
        {
            StringColumnModel or MemoColumnModel => "string?",
            IntegerColumnModel => "int?",
            BigIntColumnModel => "long?",
            BooleanColumnModel => "bool?",
            DateTimeColumnModel => "DateTime?",
            DecimalColumnModel => "decimal?",
            DoubleColumnModel => "double?",
            MoneyColumnModel => "decimal?",
            EnumColumnModel enumColumnModel => GetEnumTypeSignature(enumColumnModel),
            LookupColumnModel => "EntityReference?",
            PartyListColumnModel => "IEnumerable<ActivityParty>",
            FileColumnModel or ImageColumnModel => "byte[]",
            PrimaryIdColumnModel => "Guid",
            BooleanManagedColumnModel => "BooleanManagedProperty",
            ManagedColumnModel managedColumnModel => $"ManagedProperty<{managedColumnModel.FullReturnType}>",
            UniqueIdentifierColumnModel => "Guid?",
            _ => "object",
        };
    }

    /// <summary>
    /// Gets the C# type signature for an enum column.
    /// </summary>
    /// <param name="enumColumn">The enum column model.</param>
    /// <returns>The C# enum type signature.</returns>
    private static string GetEnumTypeSignature(EnumColumnModel enumColumn)
    {
        var enumName = NameSanitizer.SanitizeName(enumColumn.OptionsetName, "UnknownOptionSet");
        return enumColumn.IsMultiSelect ? $"IEnumerable<{enumName}>" : $"{enumName}?";
    }
}
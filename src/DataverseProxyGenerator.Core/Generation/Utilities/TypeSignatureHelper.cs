using DataverseProxyGenerator.Core.Domain;

namespace DataverseProxyGenerator.Core.Generation.Utilities;

public static class TypeSignatureHelper
{
    /// <summary>
    /// Gets the C# type signature for a column model.
    /// </summary>
    /// <param name="column">The column model.</param>
    /// <param name="nullable">When true, value types are annotated as nullable (e.g. <c>int?</c>). When false, types omit the <c>?</c> suffix.</param>
    /// <returns>The C# type signature string.</returns>
    public static string GetPropertyTypeSignature(ColumnModel column, bool nullable = true)
    {
        ArgumentNullException.ThrowIfNull(column);

        return column switch
        {
            StringColumnModel or MemoColumnModel => nullable ? "string?" : "string",
            IntegerColumnModel => nullable ? "int?" : "int",
            BigIntColumnModel => nullable ? "long?" : "long",
            BooleanColumnModel => nullable ? "bool?" : "bool",
            DateTimeColumnModel => nullable ? "DateTime?" : "DateTime",
            DecimalColumnModel => nullable ? "decimal?" : "decimal",
            DoubleColumnModel => nullable ? "double?" : "double",
            MoneyColumnModel => nullable ? "decimal?" : "decimal",
            EnumColumnModel enumColumnModel => GetEnumTypeSignature(enumColumnModel, nullable),
            LookupColumnModel => nullable ? "EntityReference?" : "EntityReference",
            PartyListColumnModel => "IEnumerable<ActivityParty>",
            FileColumnModel or ImageColumnModel => "byte[]",
            PrimaryIdColumnModel => nullable ? "Guid?" : "Guid",
            BooleanManagedColumnModel => "BooleanManagedProperty",
            ManagedColumnModel managedColumnModel => GetManagedTypeSignature(managedColumnModel, nullable),
            UniqueIdentifierColumnModel => nullable ? "Guid?" : "Guid",
            _ => "object",
        };
    }

    /// <summary>
    /// Gets the C# type signature for an enum column.
    /// </summary>
    /// <param name="enumColumn">The enum column model.</param>
    /// <param name="nullable">When true, single-value enum types are annotated as nullable.</param>
    /// <returns>The C# enum type signature.</returns>
    private static string GetEnumTypeSignature(EnumColumnModel enumColumn, bool nullable)
    {
        var enumName = NameSanitizer.SanitizeName(enumColumn.OptionsetName, "UnknownOptionSet");
        if (enumColumn.IsMultiSelect)
            return $"IEnumerable<{enumName}>";
        return nullable ? $"{enumName}?" : enumName;
    }

    /// <summary>
    /// Gets the C# type signature for a managed property column.
    /// </summary>
    /// <param name="managedColumn">The managed column model.</param>
    /// <param name="nullable">When true, the inner type is annotated as nullable if the column's <c>IsNullable</c> flag is set.</param>
    /// <returns>The C# managed property type signature.</returns>
    private static string GetManagedTypeSignature(ManagedColumnModel managedColumn, bool nullable)
    {
        var innerType = nullable && managedColumn.IsNullable && managedColumn.ReturnType != "string"
            ? managedColumn.ReturnType + "?"
            : managedColumn.ReturnType;
        return $"ManagedProperty<{innerType}>";
    }
}
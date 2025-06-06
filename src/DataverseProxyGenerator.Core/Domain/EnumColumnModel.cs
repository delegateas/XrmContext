using System.Collections.Generic;

namespace DataverseProxyGenerator.Core.Domain
{
    public record EnumColumnModel : ColumnModel
    {
        public string OptionsetName { get; init; }
        public bool IsGlobalOptionset { get; init; }
        public Dictionary<int, string> OptionsetValues { get; init; }
        public override string TypeName => "EnumColumnModel";
    }
}

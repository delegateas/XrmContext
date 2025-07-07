using System.Collections.Generic;

namespace DataverseProxyGenerator.Core.Domain
{
    public record EnumColumnModel : ColumnModel
    {
        public string OptionsetName { get; init; }
        public bool IsGlobalOptionset { get; init; }
        public Dictionary<int, string> OptionsetValues { get; init; }
        /// <summary>
        /// Maps option value to a dictionary of LCID → label.
        /// </summary>
        public Dictionary<int, Dictionary<int, string>> OptionLocalizations { get; init; }
    }
}

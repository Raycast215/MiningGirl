using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Data
{
    [Serializable]
    [DataFile("CharacterStatGrowthDataTable")]  
    public class CharacterStatGrowthDataRow : DataTableRowBase
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public EStatType StatType { get; set; }
        public float GrowthValue { get; set; }
        public float MinValue { get; set; }
        public float MaxValue { get; set; }
    }
    
    public class CharacterStatGrowthDataTable : DataTableBase<CharacterStatGrowthDataRow>
    {
        public CharacterStatGrowthDataTable(IReadOnlyList<CharacterStatGrowthDataRow> rows) : base(rows)
        {
        }
    }
}
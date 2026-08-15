using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Data
{
    [Serializable]
    [DataFile("SkillCardDataTable")]  
    public class SkillCardDataTableRow : DataTableRowBase
    {
        public string Name { get; set; }
        public string Desc { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public ESkillCategoryType SkillCategoryType { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public ESkillType SkillType { get; set; }
        public int Cost { get; set; }
        public float DurationTime { get; set; }
        public float EffectRange { get; set; }
        public float EffectValue { get; set; }
        public int Weight { get; set; }
    }
    
    public class SkillCardDataTable : DataTableBase<SkillCardDataTableRow>
    {
        public SkillCardDataTable(IReadOnlyList<SkillCardDataTableRow> rows) : base(rows)
        {
        }
    }
}

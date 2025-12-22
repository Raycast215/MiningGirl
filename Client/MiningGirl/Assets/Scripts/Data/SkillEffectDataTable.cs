using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Data
{
    [Serializable]
    [DataFile("SkillEffectDataTable")]  
    public class SkillEffectDataRowTable : DataTableRowBase
    {
        [JsonConverter(typeof(StringEnumConverter))] 
        public ESkillEffectType EffectType { get; set; }
        public float EffectValue { get; set; }
    }
    
    public class SkillEffectDataTable : DataTableBase<SkillEffectDataRowTable>
    {
        public SkillEffectDataTable(IReadOnlyList<SkillEffectDataRowTable> rows) : base(rows)
        {
        }
    }
}

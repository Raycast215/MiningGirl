using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Data
{
    [Serializable]
    [DataFile("LevelUpBonusSkillDataTable")]  
    public class LevelUpBonusSkillDataTableRow : DataTableRowBase
    {
       
        public string Name { get; set; }
        public string Desc { get; set; }
        public float EffectValue { get; set; }
        public int Weight { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public EEffectValueType ValueType { get; set; }
        // 이 보너스가 무엇에 작용하는지. 코드는 Id 대신 이 값을 보고 동작합니다.
        [JsonConverter(typeof(StringEnumConverter))]
        public ELevelUpBonusEffectType EffectType { get; set; }
        public int MaxLevel { get; set; }
    }
    
    public class LevelUpBonusSkillDataTable : DataTableBase<LevelUpBonusSkillDataTableRow>
    {
        public LevelUpBonusSkillDataTable(IReadOnlyList<LevelUpBonusSkillDataTableRow> rows) : base(rows)
        {
        }
    }
}

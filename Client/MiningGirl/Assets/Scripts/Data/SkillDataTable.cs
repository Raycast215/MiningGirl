using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Data
{
    [Serializable]
    [DataFile("SkillDataTable")]  
    public class SkillDataRowTable : DataTableRowBase
    {
        public float Cost { get; set; }                  // 스킬 사용 비용
        [JsonConverter(typeof(StringEnumConverter))] 
        public ESkillType SkillType { get; set; }
        [JsonConverter(typeof(StringEnumConverter))] 
        public ESkillRank SkillRank { get; set; }
        [JsonConverter(typeof(ListFromStringConverter<float>))]
        public List<float> EffectValueList { get; set; } // 스킬 효과값 리스트
        public bool Chainable { get; set; }              // 스킬 연속사용 스택 사용 여부
        public string IconAssetKey { get; set; }         // 아이콘 에셋 어드레서블 키
        public int Weight { get; set; }                  // 드로우 가중치
        
        public string NameKey { get; set; }
        public string DescKey { get; set; }
    }

    public class SkillDataTable : DataTableBase<SkillDataRowTable>
    {
        public SkillDataTable(IReadOnlyList<SkillDataRowTable> rows) : base(rows)
        {
        }
    }
}
#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Data
{
    [Serializable]
    [DataFile("CharacterStatDataTable")]  
    public class CharacterStatDataRow : DataTableRowBase
    {
        public float Damage { get; set; }
        public float AttackDistance { get; set; }
        public float AttackDelay { get; set; }
        public float MoveSpeed { get; set; }
        public float CriDamage { get; set; }
        public float CriRate { get; set; }
        public float ExtraHitRate { get; set; }
        // 최대 체력
        public float MaxHealth { get; set; }
        // 피격 후 무적 시간(초)
        public float InvincibleDuration { get; set; }

        // 시작 시 미리 1레벨 받고 시작할 레벨업 스킬 타입들 (콤마 구분)
        [JsonConverter(typeof(ListFromStringConverter<ELevelUpBonusEffectType>))]
        public List<ELevelUpBonusEffectType>? StartSkillTypeList { get; set; }
    }
    
    public class CharacterStatDataTable : DataTableBase<CharacterStatDataRow>
    {
        public CharacterStatDataTable(IReadOnlyList<CharacterStatDataRow> rows) : base(rows)
        {
        }
    }
}
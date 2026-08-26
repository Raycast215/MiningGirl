#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Data
{
    [Serializable]
    [DataFile("MonsterDataTable")]
    public class MonsterDataTableRow : DataTableRowBase
    {
        // Id가 곧 프리팹 이름입니다(Monster_001). 별도 AssetId를 두지 않습니다.
        public string Name { get; set; }

        [JsonConverter(typeof(StringEnumConverter))]
        public EMonsterType MonsterType { get; set; }

        public float MaxHealth { get; set; }

        // 타워에 주는 피해.
        public float Damage { get; set; }

        public float MoveSpeed { get; set; }
        public float AttackDelay { get; set; }
        public float AttackDistance { get; set; }

        // 경험치 상대 비중. 절대값이 아니라 스테이지 총량을 나누는 몫입니다.
        public float ExpWeight { get; set; }

        public int Gold { get; set; }

        // 웨이브가 아니라 몬스터의 성질로 보고 여기 둡니다.
        public float SpawnInterval { get; set; }
        public float SpawnDelay { get; set; }

        // 0~1. 1이면 넉백 무효.
        public float KnockbackResist { get; set; }
    }

    public class MonsterDataTable : DataTableBase<MonsterDataTableRow>
    {
        public MonsterDataTable(IReadOnlyList<MonsterDataTableRow> rows) : base(rows)
        {
        }
    }
}

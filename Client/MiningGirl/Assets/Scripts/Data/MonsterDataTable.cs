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

        public int Gold { get; set; }

        // 웨이브 안에서 이 종류가 등장하기 시작하는 시차(초).
        // 스폰 간격은 몬스터 고정값이 아니라 WaveDataTable.Duration ÷ Count 로 나옵니다.
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

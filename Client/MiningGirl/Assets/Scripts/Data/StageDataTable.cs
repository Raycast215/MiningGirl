#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Data
{
    [Serializable]
    [DataFile("StageDataTable")]
    public class StageDataTableRow : DataTableRowBase
    {
        public string Name { get; set; }

        // 이 스테이지의 웨이브 수. 보통 20.
        public int WaveCount { get; set; }

        // 몬스터 체력·공격력에 곱하는 스테이지 난이도 배율.
        public float MonsterStatMultiplier { get; set; }

        // 별 1·2·3개일 때 주는 클리어 골드. 시트에는 "200,350,500" 처럼 적습니다.
        // 별 0개(실패)는 지급이 없어 배열에 들어가지 않습니다.
        [JsonConverter(typeof(ListFromStringConverter<int>))]
        public List<int>? ClearGoldList { get; set; }

        public string? BgAssetId { get; set; }
        public string? BgmId { get; set; }
    }

    public class StageDataTable : DataTableBase<StageDataTableRow>
    {
        public StageDataTable(IReadOnlyList<StageDataTableRow> rows) : base(rows)
        {
        }
    }
}

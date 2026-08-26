#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Data
{
    [Serializable]
    [DataFile("WaveDataTable")]
    public class WaveDataTableRow : DataTableRowBase
    {
        public string StageId { get; set; }

        // 1부터 StageDataTable.WaveCount 까지.
        public int WaveNo { get; set; }

        // 이 웨이브에 나올 몬스터 종류. 시트에는 "Monster_001,Monster_002" 처럼 적습니다.
        [JsonConverter(typeof(ListFromStringConverter<string>))]
        public List<string>? MonsterIds { get; set; }

        // 종류별 마리 수. MonsterIds와 순서·길이가 같아야 합니다.
        [JsonConverter(typeof(ListFromStringConverter<int>))]
        public List<int>? Counts { get; set; }
    }

    public class WaveDataTable : DataTableBase<WaveDataTableRow>
    {
        public WaveDataTable(IReadOnlyList<WaveDataTableRow> rows) : base(rows)
        {
        }
    }
}

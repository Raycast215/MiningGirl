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

        // 이 웨이브가 지속되는 시간(초).
        // 이 시간이 지나면 몬스터가 남아 있어도 다음 웨이브로 넘어갑니다.
        public float Duration { get; set; }
    }

    public class WaveDataTable : DataTableBase<WaveDataTableRow>
    {
        public WaveDataTable(IReadOnlyList<WaveDataTableRow> rows) : base(rows)
        {
        }

        // 한 스테이지에 걸려 있는 경험치 총합.
        //
        // 여기서 계산하는 이유는 시트에 따로 적으면 값이 두 곳으로 갈라지기 때문입니다.
        // 구성을 한 줄 고칠 때마다 총합도 같이 고쳐야 하는데, 어긋나도 아무도 안 알려줍니다.
        //
        // 경험치 게이지의 마지막 구간 분모에만 쓰입니다. 레벨 계산에는 안 씁니다.
        public int SumExp(string stageId, MonsterDataTable monsters)
        {
            if (Rows == null || monsters == null || string.IsNullOrEmpty(stageId))
                return 0;

            var total = 0;

            for (var i = 0; i < Rows.Count; i++)
            {
                var row = Rows[i];

                if (row == null || row.StageId != stageId || row.MonsterIds == null || row.Counts == null)
                    continue;

                // 종류와 마리 수는 길이가 같아야 하지만, 시트가 어긋나 있어도
                // 여기서 터지지는 않게 짧은 쪽에 맞춥니다.
                var count = Math.Min(row.MonsterIds.Count, row.Counts.Count);

                for (var k = 0; k < count; k++)
                {
                    var monster = monsters.GetRow(row.MonsterIds[k]);

                    if (monster != null)
                        total += monster.Exp * row.Counts[k];
                }
            }

            return total;
        }

        // 한 스테이지에 나오는 몬스터 총 마리 수.
        //
        // 예전에는 StageDataTable에 열로 적혀 있었습니다. 구성을 고칠 때마다 두 곳을
        // 맞춰야 했고, 실제로 어긋났는데 아무도 안 알려줬습니다 - 시트는 200인데
        // 실제는 182였고, 측정 로그가 "처치 182/200"으로 판을 덜 끝낸 것처럼 찍혔습니다.
        public int SumMonsterCount(string stageId)
        {
            if (Rows == null || string.IsNullOrEmpty(stageId))
                return 0;

            var total = 0;

            for (var i = 0; i < Rows.Count; i++)
            {
                var row = Rows[i];

                if (row == null || row.StageId != stageId || row.Counts == null)
                    continue;

                // 종류 목록보다 마리 수가 길면 그 뒤는 붙을 종류가 없어 안 나옵니다.
                var count = row.MonsterIds != null
                    ? Math.Min(row.MonsterIds.Count, row.Counts.Count)
                    : 0;

                for (var k = 0; k < count; k++)
                    total += row.Counts[k];
            }

            return total;
        }
    }
}

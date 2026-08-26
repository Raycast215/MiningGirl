#nullable enable
using System;
using System.Collections.Generic;

namespace Data
{
    [Serializable]
    [DataFile("ExpDataTable")]
    public class ExpDataTableRow : DataTableRowBase
    {
        public int Level { get; set; }

        // 이 레벨에서 다음 레벨까지 필요한 경험치.
        // 곡선은 150 + (Level - 1) x 41 입니다.
        public int RequiredExp { get; set; }
    }

    public class ExpDataTable : DataTableBase<ExpDataTableRow>
    {
        public ExpDataTable(IReadOnlyList<ExpDataTableRow> rows) : base(rows)
        {
        }
    }
}

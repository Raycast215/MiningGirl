using System;
using System.Collections.Generic;

namespace Data
{
    [Serializable]
    [DataFile("StageDataTable")]  
    public class StageDataRowTable : DataTableRowBase
    {
        public int Chapter { get; set; }
        public int Stage { get; set; }
        public int TargetMiningCount { get; set; }
        public string DefaultRewardId { get; set; }
        public int DefaultRewardCount { get; set; }
    }
        
    public class StageDataTable : DataTableBase<StageDataRowTable>
    {
        public StageDataTable(IReadOnlyList<StageDataRowTable> rows) : base(rows)
        {
        }
    }
}
using System;
using System.Collections.Generic;

namespace Data
{
    [Serializable]
    [DataFile("StartingSkillDataTable")]  
    public class StartingSkillDataRowTable : DataTableRowBase
    {
        public string SkillId { get; set; }
        public int Count { get; set; }
    }
    
    public class StartingSkillDataTable : DataTableBase<StartingSkillDataRowTable>
    {
        public StartingSkillDataTable(IReadOnlyList<StartingSkillDataRowTable> rows) : base(rows)
        {
        }
    }
}
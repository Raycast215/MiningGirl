#nullable enable
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Data
{
    [Serializable]
    [DataFile("StageInfoTable")]  
    public class StageInfoTableRow : DataTableRowBase
    {
        public int Index { get; set; }
        public int StageNumber { get; set; }
        public float PlayTime { get; set; }
        
        [JsonConverter(typeof(StringEnumConverter))]
        public EStageType StageType { get; set; }
        
        [JsonConverter(typeof(ListFromStringConverter<EItemType>))]
        public List<EItemType>? ClearRewardTypeList { get; set; }
        
        [JsonConverter(typeof(ListFromStringConverter<uint>))]
        public List<uint>? ClearRewardCountList { get; set; }
    }
    
    public class StageInfoTable : DataTableBase<StageInfoTableRow>
    {
        public StageInfoTable(IReadOnlyList<StageInfoTableRow> rows) : base(rows)
        {
        }
    }
}

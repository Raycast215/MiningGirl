using System;
using Common;

namespace Data
{
    [Serializable]
    [DataFile("PlayerStatTable")]  
    public class PlayerStatTable : DataTableBase
    {
        public EUnitRank UnitRank { get; set; }
        public int Str { get; set; }
        public int Dex { get; set; }
        public int Luk { get; set; }
        public float StrGrowthRate { get; set; }
        public float DexGrowthRate { get; set; }
        public float LukGrowthRate { get; set; }
    }
}
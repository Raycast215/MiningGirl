using System;
using Common;

namespace Data
{
    [Serializable]
    public abstract class DataTableBase
    {
        public string Id { get; set; }
        public EVisibleType VisibleType { get; set; }
    }
}
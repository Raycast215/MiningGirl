using System;
using System.Collections.Generic;
using System.Linq;

namespace Data
{
    [Serializable]
    public abstract class DataTableRowBase
    {
        public string Id { get; set; }
        public bool IsVisible { get; set; }
    }
    
    [Serializable]
    public class DataTableBase<T> where T : DataTableRowBase
    {
        public IReadOnlyList<T> Rows { get; set; }
        
        public DataTableBase(IReadOnlyList<T> rows)
        {
            Rows = rows;
        }

        public T GetRow(string id)
        {
            return Rows.FirstOrDefault(x => x.Id == id);
        }
    }
}
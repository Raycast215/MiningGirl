using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Data
{
    [Serializable]
    public abstract class DataTableRowBase
    {
        public string Id { get; set; }
        
        [JsonConverter(typeof(StringEnumConverter))] 
        public EVisibleType VisibleType { get; set; }
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
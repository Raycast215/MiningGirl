using System;

namespace Data
{
    [Serializable]
    [DataFile("TestTable")]  
    public class TestTable
    {
        public int Id;
        public string Name;
    }
}
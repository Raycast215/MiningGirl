using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Data;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Manager
{
    public class DataTableManager : SingletonBase<DataTableManager>
    {
        public SkillDataTable SkillDataTable { get; private set; }
        public StartingSkillDataTable StartingSkillDataTable { get; private set; }
        
        protected override void Initialized()
        {
            base.Initialized();

            IsInitialized = false;
            
            PreLoadData().Forget();
        }

        private async UniTaskVoid PreLoadData()
        {
            await AddressableSheetsDataManager.LoadLabelAsync("DataTable", location: LoadDataTable);
        }
        
        private void LoadDataTable(IList<IResourceLocation> list)
        {
            if (list == null || list.Count == 0)
                return;

            foreach (var location in list)
            {
                switch (location.PrimaryKey)
                {
                    case "SkillDataTable":
                        SkillDataTable = new SkillDataTable(AddressableSheetsDataManager.GetAll<SkillDataRowTable>());
                        break;
                    
                    case "StartingSkillDataTable":
                        StartingSkillDataTable = new StartingSkillDataTable(AddressableSheetsDataManager.GetAll<StartingSkillDataRowTable>());
                        break;
                }
            }
            
            IsInitialized = true;
        }
    }
}
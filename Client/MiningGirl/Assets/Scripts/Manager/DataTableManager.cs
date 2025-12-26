using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Data;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Manager
{
    public class DataTableManager : SingletonBase<DataTableManager>
    {
        public SkillDataTable SkillDataTable { get; private set; }
        public StartingSkillDataTable StartingSkillDataTable { get; private set; }
        public SkillEffectDataTable SkillEffectDataTable { get; private set; }
        public StageDataTable StageDataTable { get; private set; }
        
        protected override void Initialized()
        {
            base.Initialized();
        }

        public async UniTaskVoid PreLoadData()
        {
            if (IsInitialized)
                return;
            
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
                    case "SkillEffectDataTable":
                        SkillEffectDataTable = new SkillEffectDataTable(AddressableSheetsDataManager.GetAll<SkillEffectDataRowTable>());
                        break;
                    case "StageDataTable":
                        StageDataTable = new StageDataTable(AddressableSheetsDataManager.GetAll<StageDataRowTable>());
                        break;
                }
            }
            
            IsInitialized = true;
        }
    }
}
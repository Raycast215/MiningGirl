using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Data;
using UnityEngine;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace Manager
{
    public class DataTableManager : SingletonBase<DataTableManager>
    {
        public CharacterStatDataTable CharacterStatDataTable { get; private set; }
        public CharacterStatGrowthDataTable CharacterStatGrowthDataTable { get; private set; }
        public StageInfoTable StageInfoTable { get; private set; }
        public LevelUpBonusSkillDataTable LevelUpBonusSkillDataTable { get; private set; }

        public async UniTaskVoid PreLoadData()
        {
            if (IsInitialized)
                return;

            bool success = await AddressableSheetsDataManager.LoadLabelAsync("DataTable");

            if (!success)
            {
                Debug.LogError("Data load failed");
                return;
            }

            CharacterStatDataTable =
                new CharacterStatDataTable(AddressableSheetsDataManager.GetAll<CharacterStatDataRow>());

            CharacterStatGrowthDataTable =
                new CharacterStatGrowthDataTable(AddressableSheetsDataManager.GetAll<CharacterStatGrowthDataRow>());

            StageInfoTable =
                new StageInfoTable(AddressableSheetsDataManager.GetAll<StageInfoTableRow>());
            
            LevelUpBonusSkillDataTable =
                new LevelUpBonusSkillDataTable(AddressableSheetsDataManager.GetAll<LevelUpBonusSkillDataTableRow>());
            
            Debug.Log($"CharacterStatDataRow count = {CharacterStatDataTable.Rows.Count}");

            IsInitialized = true;
        }
        
        // public async UniTaskVoid PreLoadData()
        // {
        //     if (IsInitialized)
        //         return;
        //
        //     var result = await AddressableSheetsDataManager.LoadLabelAsync("DataTable");
        //     if (result != ELoadResponseType.Success)
        //     {
        //         Debug.LogError("DataTable load failed.");
        //         return;
        //     }
        //
        //     CharacterStatDataTable = new CharacterStatDataTable(AddressableSheetsDataManager.GetAll<CharacterStatDataRow>());
        //     CharacterStatGrowthDataTable = new CharacterStatGrowthDataTable(AddressableSheetsDataManager.GetAll<CharacterStatGrowthDataRow>());
        //
        //     Debug.Log($"CharacterStatDataRow Count = {CharacterStatDataTable.Rows.Count}");
        //     Debug.Log($"CharacterStatGrowthDataRow Count = {CharacterStatGrowthDataTable.Rows.Count}");
        //
        //     IsInitialized = true;
        // }
        
        // public async UniTaskVoid PreLoadData()
        // {
        //     if (IsInitialized)
        //         return;
        //
        //     var result = await AddressableSheetsDataManager.LoadLabelAsync("DataTable");
        //     if (result != ELoadResponseType.Success)
        //     {
        //         Debug.LogError("DataTable load failed.");
        //         return;
        //     }
        //
        //     CharacterStatDataTable = new CharacterStatDataTable(AddressableSheetsDataManager.GetAll<CharacterStatDataRow>());
        //     CharacterStatGrowthDataTable = new CharacterStatGrowthDataTable(AddressableSheetsDataManager.GetAll<CharacterStatGrowthDataRow>());
        //
        //     Debug.Log($"CharacterStatDataTable Loaded: {CharacterStatDataTable != null}");
        //     Debug.Log($"CharacterStatGrowthDataTable Loaded: {CharacterStatGrowthDataTable != null}");
        //
        //     IsInitialized = true;
        // }
        
        // public async UniTaskVoid PreLoadData()
        // {
        //     if (IsInitialized)
        //         return;
        //     
        //     var result = await AddressableSheetsDataManager.LoadLabelAsync("DataTable");
        //     
        //     if (result != ELoadResponseType.Success)
        //     {
        //         Debug.LogError("DataTable load failed.");
        //         return;
        //     }
        //     
        //     BuildDataTables();
        // }
        
        private void BuildDataTables()
        {
            CharacterStatDataTable = new CharacterStatDataTable(AddressableSheetsDataManager.GetAll<CharacterStatDataRow>());
            CharacterStatGrowthDataTable = new CharacterStatGrowthDataTable(AddressableSheetsDataManager.GetAll<CharacterStatGrowthDataRow>());
            
            IsInitialized = true;
        }
        
        private void LoadDataTable(IList<IResourceLocation> list)
        {
            if (list == null || list.Count == 0)
                return;

            foreach (var location in list)
            {
                switch (location.PrimaryKey)
                {
                    case "CharacterStatDataTable":
                        Debug.Log($"Load: {location.PrimaryKey}");
                        CharacterStatDataTable = new CharacterStatDataTable(AddressableSheetsDataManager.GetAll<CharacterStatDataRow>());
                        break;
                    case "CharacterStatGrowthDataTable":
                        Debug.Log($"Load: {location.PrimaryKey}");
                        CharacterStatGrowthDataTable = new CharacterStatGrowthDataTable(AddressableSheetsDataManager.GetAll<CharacterStatGrowthDataRow>());
                        break;
                }
            }
            
            IsInitialized = true;
        }
    }
}
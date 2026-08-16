using Cysharp.Threading.Tasks;
using Data;
using UnityEngine;

namespace Manager
{
    public class DataTableManager : SingletonBase<DataTableManager>
    {
        public CharacterStatDataTable CharacterStatDataTable { get; private set; }
        public CharacterStatGrowthDataTable CharacterStatGrowthDataTable { get; private set; }
        public StageInfoTable StageInfoTable { get; private set; }
        public LevelUpBonusSkillDataTable LevelUpBonusSkillDataTable { get; private set; }
        public SkillCardDataTable SkillCardDataTable { get; private set; }
        public DefaultSkillCardDataTable DefaultSkillCardDataTable { get; private set; }
        public GameConstantDataTable GameConstantDataTable { get; private set; }

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
            
            SkillCardDataTable =
                new SkillCardDataTable(AddressableSheetsDataManager.GetAll<SkillCardDataTableRow>());

            DefaultSkillCardDataTable =
                new DefaultSkillCardDataTable(AddressableSheetsDataManager.GetAll<DefaultSkillCardDataTableRow>());

            GameConstantDataTable =
                new GameConstantDataTable(AddressableSheetsDataManager.GetAll<GameConstantDataTableRow>());

            Debug.Log($"CharacterStatDataRow count = {CharacterStatDataTable.Rows.Count}");

            IsInitialized = true;
        }
    }
}
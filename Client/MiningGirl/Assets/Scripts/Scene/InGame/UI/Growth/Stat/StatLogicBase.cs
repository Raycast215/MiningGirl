using UnityEngine;

namespace Scene.InGame.UI.Growth.Stat
{
    public abstract class StatLogicBase
    {
        protected StatGrowthInfoUI UI { get; set; }
        protected IInGameDataHandler DataHandler { get; set; }
        protected IInGameUIHandler UIHandler { get; set; }
        
        protected StatLogicBase(StatGrowthInfoUI ui, IInGameUIHandler inGameUIHandler, IInGameDataHandler inGameDataDataHandler)
        {
            UI = ui;
            UIHandler = inGameUIHandler;
            DataHandler = inGameDataDataHandler;
        }
        
        protected virtual EStatType StatType => default;
        protected virtual ETextType TextType => default;

        public void RefreshUI()
        {
            // 강화 버튼 상태 갱신.
            UI.SetEnhanceState(DataHandler.CheckLevelUpState(StatType));
        }
        
        protected void TryLevelUp()
        {
            if (!DataHandler.CheckLevelUpState(StatType))
            {
                Debug.Log("재화가 부족합니다.");
                return;
            }

            if (DataHandler.GetStatData(StatType).IsMaxLevel)
            {
                Debug.Log("최대레벨!");
                return;
            }
            
            // 레벨업 후 데이터 갱신.
            DataHandler.LevelUpStat(StatType);
            
            // 스탯 UI 갱신.
            UI.Set(DataHandler.GetStatData(StatType).Value, TextType);
            
            // 비용 UI 갱신.
            UI.SetCost(DataHandler.GetStatData(StatType).Cost);
            
            // 레벨 UI 갱신.
            UI.SetLevel(DataHandler.GetStatData(StatType).Level);

            RefreshUI();
        }
    }
}
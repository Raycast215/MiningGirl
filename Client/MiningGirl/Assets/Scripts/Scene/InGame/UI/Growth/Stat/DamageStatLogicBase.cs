using UnityEngine;

namespace Scene.InGame.UI.Growth.Stat
{
    public abstract class DamageStatLogicBase
    {
        protected StatGrowthInfoUI UI { get; set; }
        protected IInGameDataHandler DataHandler { get; set; }
        protected IInGameUIHandler UIHandler { get; set; }
        
        protected DamageStatLogicBase(StatGrowthInfoUI ui, IInGameUIHandler inGameUIHandler, IInGameDataHandler inGameDataDataHandler)
        {
            UI = ui;
            UIHandler = inGameUIHandler;
            DataHandler = inGameDataDataHandler;
        }
        
        protected virtual EStatType StatType => default;
        
        protected void TryLevelUp()
        {
            if (!DataHandler.CheckLevelUpState(StatType))
            {
                Debug.Log($"Gold: {DataHandler.GetItemCount(EItemType.Gold)}");
                Debug.Log($"Cost: {DataHandler.GetStatData(StatType).Cost}");
                Debug.Log("재화가 부족합니다.");
                return;
            }
            
            // 레벨업 후 데이터 갱신.
            DataHandler.LevelUpStat(StatType);
            
            // 갱신된 데이터로 UI 갱신.
            UI.Set(DataHandler.GetStatData(StatType).Value);
            
            // 갱신된 데이터로 비용 UI 갱신.
            UI.SetCost((int)DataHandler.GetStatData(StatType).Cost);
        }
    }
}
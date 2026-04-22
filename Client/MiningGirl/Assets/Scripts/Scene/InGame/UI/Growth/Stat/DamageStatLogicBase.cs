using UnityEngine;

namespace Scene.InGame.UI.Growth.Stat
{
    public abstract class DamageStatLogicBase
    {
        protected StatGrowthInfoUI UI { get; set; }
        protected IInGameDataHandler Handler { get; set; }
        
        protected DamageStatLogicBase(StatGrowthInfoUI ui, IInGameDataHandler inGameDataHandler)
        {
            UI = ui;
            Handler = inGameDataHandler;
        }
        
        protected virtual EStatType StatType => default;
        
        protected void TryLevelUp()
        {
            if (!Handler.CheckLevelUpState(StatType))
            {
                Debug.Log("재화가 부족합니다.");
                return;
            }
            
            Handler.LevelUpStat(StatType);
            UI.Set(Handler.GetStatData(StatType).Value);
        }
    }
}
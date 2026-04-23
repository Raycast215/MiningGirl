namespace Scene.InGame.UI.Growth.Stat
{
    public sealed class ExtraHitRateStatLogic : DamageStatLogicBase
    {
        protected override EStatType StatType => EStatType.ExtraHitRate;
    
        public ExtraHitRateStatLogic(StatGrowthInfoUI ui, IInGameUIHandler inGameUIHandler, IInGameDataHandler inGameDataDataHandler) : base(ui, inGameUIHandler, inGameDataDataHandler)
        {
            ui.Init("추가타 확률", TryLevelUp);
            ui.Set(DataHandler.GetStatData(StatType).Value);
            ui.SetCost((int)DataHandler.GetStatData(StatType).Cost);
        }
    }
}
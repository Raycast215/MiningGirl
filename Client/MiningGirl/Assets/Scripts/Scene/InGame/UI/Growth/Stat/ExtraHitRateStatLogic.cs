namespace Scene.InGame.UI.Growth.Stat
{
    public sealed class ExtraHitRateStatLogic : StatLogicBase
    {
        protected override EStatType StatType => EStatType.ExtraHitRate;
        protected override ETextType TextType => ETextType.Percent;
    
        public ExtraHitRateStatLogic(StatGrowthInfoUI ui, IInGameUIHandler inGameUIHandler, IInGameDataHandler inGameDataDataHandler) : base(ui, inGameUIHandler, inGameDataDataHandler)
        {
            ui.Init("추가타 확률", TryLevelUp);
            ui.Set(DataHandler.GetStatData(StatType).Value, TextType);
            ui.SetCost(DataHandler.GetStatData(StatType).Cost);
            ui.SetLevel(DataHandler.GetStatData(StatType).Level);
            ui.SetEnhanceState(DataHandler.CheckLevelUpState(StatType));
        }
    }
}
namespace Scene.InGame.UI.Growth.Stat
{
    public sealed class CriRateStatLogic : StatLogicBase
    {
        protected override EStatType StatType => EStatType.CriRate;
        protected override ETextType TextType => ETextType.Percent;
    
        public CriRateStatLogic(StatGrowthInfoUI ui, IInGameUIHandler inGameUIHandler, IInGameDataHandler inGameDataDataHandler) : base(ui, inGameUIHandler, inGameDataDataHandler)
        {
            ui.Init("치명타 확률", TryLevelUp);
            ui.Set(DataHandler.GetStatData(StatType).Value, TextType);
            ui.SetCost(DataHandler.GetStatData(StatType).Cost);
            ui.SetLevel(DataHandler.GetStatData(StatType).Level);
            ui.SetEnhanceState(DataHandler.CheckLevelUpState(StatType));
        }
    }
}
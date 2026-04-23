namespace Scene.InGame.UI.Growth.Stat
{
    public sealed class CriRateStatLogic : DamageStatLogicBase
    {
        protected override EStatType StatType => EStatType.CriRate;
    
        public CriRateStatLogic(StatGrowthInfoUI ui, IInGameUIHandler inGameUIHandler, IInGameDataHandler inGameDataDataHandler) : base(ui, inGameUIHandler, inGameDataDataHandler)
        {
            ui.Init("치명타 확률", TryLevelUp);
            ui.Set(DataHandler.GetStatData(StatType).Value);
            ui.SetCost((int)DataHandler.GetStatData(StatType).Cost);
        }
    }
}
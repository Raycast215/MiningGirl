namespace Scene.InGame.UI.Growth.Stat
{
    public sealed class CriRateStatLogic : DamageStatLogicBase
    {
        protected override EStatType StatType => EStatType.CriRate;
    
        public CriRateStatLogic(StatGrowthInfoUI ui, IInGameDataHandler inGameDataHandler) : base(ui, inGameDataHandler)
        {
            ui.Init("치명타 확률", TryLevelUp);
            ui.Set(Handler.GetStatData(StatType).Value);
        }
    }
}
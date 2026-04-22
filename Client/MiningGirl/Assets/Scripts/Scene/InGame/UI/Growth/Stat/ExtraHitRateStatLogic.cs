namespace Scene.InGame.UI.Growth.Stat
{
    public sealed class ExtraHitRateStatLogic : DamageStatLogicBase
    {
        protected override EStatType StatType => EStatType.ExtraHitRate;
    
        public ExtraHitRateStatLogic(StatGrowthInfoUI ui, IInGameDataHandler inGameDataHandler) : base(ui, inGameDataHandler)
        {
            ui.Init("추가타 확률", TryLevelUp);
            ui.Set(Handler.GetStatData(StatType).Value);
        }
    }
}
namespace Scene.InGame.UI.Growth.Stat
{
    public sealed class AttackDelayStatLogic : DamageStatLogicBase
    {
        protected override EStatType StatType => EStatType.AttackDelay;
    
        public AttackDelayStatLogic(StatGrowthInfoUI ui, IInGameUIHandler inGameUIHandler, IInGameDataHandler inGameDataDataHandler) : base(ui, inGameUIHandler, inGameDataDataHandler)
        {
            ui.Init("공격속도", TryLevelUp);
            ui.Set(DataHandler.GetStatData(StatType).Value);
            ui.SetCost((int)DataHandler.GetStatData(StatType).Cost);
        }
    }
}
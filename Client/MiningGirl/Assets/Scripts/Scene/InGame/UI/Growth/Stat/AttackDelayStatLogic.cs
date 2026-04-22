namespace Scene.InGame.UI.Growth.Stat
{
    public sealed class AttackDelayStatLogic : DamageStatLogicBase
    {
        protected override EStatType StatType => EStatType.AttackDelay;
    
        public AttackDelayStatLogic(StatGrowthInfoUI ui, IInGameDataHandler inGameDataHandler) : base(ui, inGameDataHandler)
        {
            ui.Init("공격속도", TryLevelUp);
            ui.Set(Handler.GetStatData(StatType).Value);
        }
    }
}
namespace Scene.InGame.UI.Growth.Stat
{
    public sealed class DamageStatLogic : DamageStatLogicBase
    {
        protected override EStatType StatType => EStatType.Damage;
        
        public DamageStatLogic(StatGrowthInfoUI ui, IInGameDataHandler inGameDataHandler) : base(ui, inGameDataHandler)
        {
            ui.Init("데미지", TryLevelUp);
            ui.Set(Handler.GetStatData(StatType).Value);
        }
    }
}
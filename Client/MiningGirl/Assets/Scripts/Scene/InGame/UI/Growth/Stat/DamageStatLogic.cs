namespace Scene.InGame.UI.Growth.Stat
{
    public sealed class DamageStatLogic : DamageStatLogicBase
    {
        protected override EStatType StatType => EStatType.Damage;
        
        public DamageStatLogic(StatGrowthInfoUI ui, IInGameUIHandler inGameUIHandler, IInGameDataHandler inGameDataDataHandler) : base(ui, inGameUIHandler, inGameDataDataHandler)
        {
            ui.Init("데미지", TryLevelUp);
            ui.Set(DataHandler.GetStatData(StatType).Value);
            ui.SetCost((int)DataHandler.GetStatData(StatType).Cost);
        }
    }
}
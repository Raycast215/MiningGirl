namespace Scene.InGame.UI.Growth.Stat
{
    public sealed class CriDamageStatLogic : DamageStatLogicBase
    {
        protected override EStatType StatType => EStatType.CriDamage;
    
        public CriDamageStatLogic(StatGrowthInfoUI ui, IInGameUIHandler inGameUIHandler, IInGameDataHandler inGameDataDataHandler) : base(ui, inGameUIHandler, inGameDataDataHandler)
        {
            ui.Init("치명타 데미지", TryLevelUp);
            ui.Set(DataHandler.GetStatData(StatType).Value);
            ui.SetCost((int)DataHandler.GetStatData(StatType).Cost);
        }
    }
}
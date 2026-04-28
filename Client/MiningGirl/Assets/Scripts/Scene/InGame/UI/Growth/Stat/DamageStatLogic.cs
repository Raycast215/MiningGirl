namespace Scene.InGame.UI.Growth.Stat
{
    public sealed class DamageStatLogic : StatLogicBase
    {
        protected override EStatType StatType => EStatType.Damage;
        protected override ETextType TextType => ETextType.Int;
        
        public DamageStatLogic(StatGrowthInfoUI ui, IInGameUIHandler inGameUIHandler, IInGameDataHandler inGameDataDataHandler) : base(ui, inGameUIHandler, inGameDataDataHandler)
        {
            ui.Init("데미지", TryLevelUp);
            ui.Set(DataHandler.GetStatData(StatType).Value, TextType);
            ui.SetCost(DataHandler.GetStatData(StatType).Cost);
            ui.SetLevel(DataHandler.GetStatData(StatType).Level);
            ui.SetEnhanceState(DataHandler.CheckLevelUpState(StatType));
        }
    }
}
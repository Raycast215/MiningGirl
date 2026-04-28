namespace Scene.InGame.UI.Growth.Stat
{
    public sealed class CriStatLogic : StatLogicBase
    {
        protected override EStatType StatType => EStatType.CriDamage;
        protected override ETextType TextType => ETextType.Float;
    
        public CriStatLogic(StatGrowthInfoUI ui, IInGameUIHandler inGameUIHandler, IInGameDataHandler inGameDataDataHandler) : base(ui, inGameUIHandler, inGameDataDataHandler)
        {
            ui.Init("치명타 데미지", TryLevelUp);
            ui.Set(DataHandler.GetStatData(StatType).Value, TextType);
            ui.SetCost(DataHandler.GetStatData(StatType).Cost);
            ui.SetLevel(DataHandler.GetStatData(StatType).Level);
            ui.SetEnhanceState(DataHandler.CheckLevelUpState(StatType));
        }
    }
}
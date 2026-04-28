namespace Scene.InGame.UI.Growth.Stat
{
    public sealed class MoveSpeedStatLogic : StatLogicBase
    {
        protected override EStatType StatType => EStatType.MoveSpeed;
        protected override ETextType TextType => ETextType.Float;
    
        public MoveSpeedStatLogic(StatGrowthInfoUI ui, IInGameUIHandler inGameUIHandler, IInGameDataHandler inGameDataDataHandler) : base(ui, inGameUIHandler, inGameDataDataHandler)
        {
            ui.Init("이동속도", TryLevelUp);
            ui.Set(DataHandler.GetStatData(StatType).Value, TextType);
            ui.SetCost(DataHandler.GetStatData(StatType).Cost);
            ui.SetLevel(DataHandler.GetStatData(StatType).Level);
            ui.SetEnhanceState(DataHandler.CheckLevelUpState(StatType));
        }
    }
}
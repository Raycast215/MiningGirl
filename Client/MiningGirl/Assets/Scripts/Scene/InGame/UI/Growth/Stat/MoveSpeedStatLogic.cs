namespace Scene.InGame.UI.Growth.Stat
{
    public sealed class MoveSpeedStatLogic : DamageStatLogicBase
    {
        protected override EStatType StatType => EStatType.MoveSpeed;
    
        public MoveSpeedStatLogic(StatGrowthInfoUI ui, IInGameUIHandler inGameUIHandler, IInGameDataHandler inGameDataDataHandler) : base(ui, inGameUIHandler, inGameDataDataHandler)
        {
            ui.Init("이동속도", TryLevelUp);
            ui.Set(DataHandler.GetStatData(StatType).Value);
            ui.SetCost((int)DataHandler.GetStatData(StatType).Cost);
        }
    }
}
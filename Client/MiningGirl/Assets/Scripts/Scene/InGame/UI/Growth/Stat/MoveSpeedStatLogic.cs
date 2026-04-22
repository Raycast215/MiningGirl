namespace Scene.InGame.UI.Growth.Stat
{
    public sealed class MoveSpeedStatLogic : DamageStatLogicBase
    {
        protected override EStatType StatType => EStatType.MoveSpeed;
    
        public MoveSpeedStatLogic(StatGrowthInfoUI ui, IInGameDataHandler inGameDataHandler) : base(ui, inGameDataHandler)
        {
            ui.Init("이동속도", TryLevelUp);
            ui.Set(Handler.GetStatData(StatType).Value);
        }
    }
}
namespace Scene.InGame.UI.Growth.Stat
{
    public sealed class CriDamageStatLogic : DamageStatLogicBase
    {
        protected override EStatType StatType => EStatType.CriDamage;
    
        public CriDamageStatLogic(StatGrowthInfoUI ui, IInGameDataHandler inGameDataHandler) : base(ui, inGameDataHandler)
        {
            ui.Init("치명타 데미지", TryLevelUp);
            ui.Set(Handler.GetStatData(StatType).Value);
        }
    }
}
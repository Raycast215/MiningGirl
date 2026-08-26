using Data;
using Manager;

namespace Legacy.Scene.InGame.State
{
    // 런 하나를 굴리는 데 필요한 튜닝 수치 묶음.
    //
    // 예전에는 이 값들이 StaminaUI, CostUI, MiningProgressUI 의 [SerializeField]에
    // 흩어져 있었습니다. 그러면 밸런싱을 하려고 씬을 열어야 하고,
    // 계산이 맞는지 확인하려면 게임을 실행해야 했습니다.
    //
    // 순수 C# 클래스라 테스트에서 그냥 new 해서 값을 넣어보면 됩니다.
    public class RunSettings
    {
        // ── 코스트 ──
        public int MaxCost = 10;
        public float CostRegenInterval = 3f;
        public float CostLateSpeedMultiplier = 2f;
        public float CostSpeedUpProgress = 0.5f;

        // ── 클리어 조건(채굴) ──
        public int MiningGoalBase = 10;
        public int MiningGoalPerStage = 5;

        // ── 스태미나 ──
        public float MaxStamina = 100f;
        public float MiningStaminaCost = 10f;
        public float HitStaminaCost = 1f;

        // 상수 테이블에서 읽어옵니다. 테이블이 없으면(테스트 등) 위 기본값을 그대로 씁니다.
        public static RunSettings FromTable(GameConstantDataTable table)
        {
            var s = new RunSettings();

            if (table == null)
                return s;

            s.MaxCost = table.GetInt(EGameConstantType.MaxCost, s.MaxCost);
            s.CostRegenInterval = table.GetValue(EGameConstantType.CostRegenInterval, s.CostRegenInterval);
            s.CostLateSpeedMultiplier = table.GetValue(EGameConstantType.CostLateSpeedMultiplier, s.CostLateSpeedMultiplier);
            s.CostSpeedUpProgress = table.GetValue(EGameConstantType.CostSpeedUpProgress, s.CostSpeedUpProgress);

            s.MiningGoalBase = table.GetInt(EGameConstantType.MiningGoalBase, s.MiningGoalBase);
            s.MiningGoalPerStage = table.GetInt(EGameConstantType.MiningGoalPerStage, s.MiningGoalPerStage);

            s.MaxStamina = table.GetValue(EGameConstantType.MaxStamina, s.MaxStamina);
            s.MiningStaminaCost = table.GetValue(EGameConstantType.MiningStaminaCost, s.MiningStaminaCost);
            s.HitStaminaCost = table.GetValue(EGameConstantType.HitStaminaCost, s.HitStaminaCost);

            return s;
        }

        // 편의용 — 지금 로드된 테이블에서 바로 만듭니다.
        public static RunSettings FromCurrentTable()
        {
            return FromTable(DataTableManager.Instance?.GameConstantDataTable);
        }
    }
}

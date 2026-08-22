using System;
using UnityEngine;

namespace Scene.InGame.State
{
    // 런 하나의 게임 상태 전부. MonoBehaviour가 아니고 씬에 존재하지 않습니다.
    //
    // 예전에는 이 값들이 UI 컴포넌트에 흩어져 있었습니다.
    //   스테이지 번호 → StageUI.Stage
    //   스태미나      → StaminaUI.Current
    //   코스트        → CostUI.Cost
    //   골드          → MainGameUIController._gold
    // 그래서 세이브가 UI에서 값을 읽어가는 모양이었고, 규칙을 검증하려면
    // 씬을 통째로 띄워야 했습니다.
    //
    // 지금은 게임플레이와 세이브가 이 클래스만 보고, UI는 이 값을 읽어 그리기만 합니다.
    public class RunState
    {
        public RunSettings Settings { get; }

        public StaminaState Stamina { get; }
        public CostState Cost { get; }
        public MiningState Mining { get; }

        // 진행 중인 스테이지 번호(1부터)
        public int Stage { get; private set; } = 1;

        // 이번 런에서 누적된 골드. 스테이지 재시작에도 초기화되지 않습니다.
        public int Gold { get; private set; }

        // 이번 스테이지에서 번 골드만. 스테이지가 시작될 때 0으로 돌아갑니다.
        // (강화로 쓴 금액은 빼지 않습니다 — '이번 판에 얼마를 벌었나'를 보여주는 값입니다.)
        public int StageGold { get; private set; }

        public event Action OnStageChanged;
        public event Action OnGoldChanged;

        // 목표 채굴량을 채워 스테이지가 끝났을 때 한 번만.
        public event Action OnFinished;

        // 스태미나가 바닥나 실패했을 때.
        public event Action OnStaminaEmpty;

        // 진행 중인지(GameStart ~ 스테이지 종료). false면 코스트가 차오르지 않습니다.
        private bool _isRunning;
        private bool _isPaused;

        // 한 스테이지에서 종료 처리는 한 번만 돌아야 합니다.
        // (목표 달성과 스태미나 소진이 같은 프레임에 겹칠 수 있습니다.)
        private bool _isFinished;

        public RunState(RunSettings settings = null)
        {
            Settings = settings ?? new RunSettings();

            Stamina = new StaminaState(Settings);
            Cost = new CostState(Settings);
            Mining = new MiningState(Settings);

            Mining.OnGoalReached += Finish;
            Stamina.OnEmpty += () => OnStaminaEmpty?.Invoke();

            Mining.SetGoalByStage(Stage);
        }

        public void SetStage(int stage)
        {
            Stage = Mathf.Max(1, stage);

            OnStageChanged?.Invoke();
        }

        public void AddGold(int amount)
        {
            if (amount <= 0)
                return;

            Gold += amount;
            StageGold += amount;

            OnGoldChanged?.Invoke();
        }

        // 강화 구매용. 모자라면 아무것도 하지 않고 false를 돌려줍니다.
        public bool TrySpendGold(int amount)
        {
            if (amount <= 0)
                return true;

            if (Gold < amount)
                return false;

            Gold -= amount;

            OnGoldChanged?.Invoke();

            return true;
        }

        // 저장된 진행으로 되돌립니다(게임 시작 시 1회).
        public void RestoreProgress(int stage, int gold)
        {
            Gold = Mathf.Max(0, gold);
            StageGold = 0;

            SetStage(stage);

            Mining.SetGoalByStage(Stage);
            Stamina.Reset();

            OnGoldChanged?.Invoke();
        }

        // 스테이지가 새로 시작될 때. advanceStage가 false면 같은 스테이지 재도전입니다.
        public void ResetStage(bool advanceStage = true)
        {
            _isFinished = false;
            StageGold = 0;

            if (advanceStage)
                SetStage(Stage + 1);

            Cost.Reset(0);
            Stamina.Reset();
            Mining.SetGoalByStage(Stage);
        }

        // 실제 진행 시작 — 이 시점부터 코스트가 차오릅니다.
        public void Start()
        {
            _isFinished = false;
            _isRunning = true;
        }

        public void SetPaused(bool paused)
        {
            _isPaused = paused;
        }

        // 스테이지 종료(클리어). 두 번 불려도 한 번만 처리합니다.
        public void Finish()
        {
            if (_isFinished)
                return;

            _isFinished = true;
            _isRunning = false;

            OnFinished?.Invoke();
        }

        // 매 프레임 시간을 흘려보냅니다. MonoBehaviour 쪽에서 Time.deltaTime을 넘겨줍니다.
        // 테스트에서는 원하는 값을 직접 넣으면 됩니다.
        public void Tick(float deltaTime)
        {
            if (!_isRunning || _isPaused)
                return;

            // 채굴 진행도가 절반을 넘기면 코스트 회복을 가속합니다.
            // 후반에 몬스터가 몰릴수록 카드를 더 자주 쓸 수 있게 하려는 의도입니다.
            Cost.SetSpeedUp(Mining.Progress >= Settings.CostSpeedUpProgress);

            Cost.Tick(deltaTime);
        }

        // 광물 하나를 다 캤을 때 — 채굴 진행도만 올립니다.
        // 스태미나는 '캐는 시도'마다 따로 나갑니다(AddMiningAttempt).
        public void AddMinedCount(int amount = 1)
        {
            if (amount <= 0)
                return;

            Mining.Add(amount);
        }

        // 광물을 한 번 내려칠 때마다 호출됩니다. 다 캤는지와 무관하게 소모합니다.
        // (예전에는 광물 하나를 완전히 캤을 때 한 번만 소모했습니다.)
        public void AddMiningAttempt(int count = 1)
        {
            for (var i = 0; i < count; i++)
                Stamina.ConsumeByMining();
        }
    }
}

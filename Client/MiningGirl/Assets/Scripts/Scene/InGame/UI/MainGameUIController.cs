using System;
using Cysharp.Threading.Tasks;
using Data;
using MainGame.Bonus;
using MainGame.Entity;
using MainGame.UI;
using Manager;
using UnityEngine;

namespace MainGame
{
    public class MainGameUIController : GameMonoInitializer
    {
        private event Action OnNextGameExecuted;

        // 이번 런에서 누적된 골드. 스테이지 재시작(Next)에도 초기화되지 않습니다.
        private int _gold;

        public int Gold => _gold;

        [SerializeField]
        private StageUI stageUI;
        [SerializeField]
        private BuffListUI buffListUI;

        [SerializeField]
        private StaminaUI staminaUI;
        [SerializeField]
        private MiningProgressUI miningProgressUI;

        [SerializeField]
        [Tooltip("이 진행도를 넘기면 코스트 회복이 빨라집니다(0.5 = 절반 경과)")]
        [Range(0f, 1f)]
        private float costSpeedUpProgress = 0.5f;
        [SerializeField]
        private CostUI costUI;
        [SerializeField]
        [Tooltip("채굴로 획득한 골드를 표시합니다")]
        private Scene.InGame.UI.Resource.CountViewerUI goldCountViewer;
        [SerializeField]
        private CharacterSelectPopup characterSelectPopup;

        public async UniTask InitAsync(Action onNextGameExecuted)
        {
            OnNextGameExecuted = null;
            OnNextGameExecuted += onNextGameExecuted;

            stageUI.Init();
            costUI.Init();

            // 클리어 조건 — 목표 채굴량을 채우면 스테이지가 끝납니다.
            staminaUI?.Init();
            miningProgressUI?.Init(GameFinish);
            miningProgressUI?.SetGoalByStage(Stage);

            // 골드는 런 전체에서 누적되므로 여기(최초 진입)에서만 0으로 시작합니다.
            _gold = 0;
            goldCountViewer.SetCount(_gold);

            IsInitialized = true;
        }

        // 스테이지가 새로 시작될 때 호출합니다(advanceStage가 false면 같은 스테이지 재도전).
        public void ResetStage(bool advanceStage = true)
        {
            // 새 스테이지가 시작되므로 종료 상태를 풉니다.
            _isFinished = false;

            if (advanceStage)
                stageUI.NextStage();

            costUI.SetCost(0);

            staminaUI?.Reset();
            miningProgressUI?.SetGoalByStage(Stage);
        }

        public void GameStart()
        {
            costUI.Execute().Forget();
        }

        public bool CanAffordCost(int amount) => costUI.CanAfford(amount);
        public bool TrySpendCost(int amount) => costUI.TrySpend(amount);
        public void AddCost(int amount, bool allowOvercharge = false) => costUI.Add(amount, allowOvercharge);

        // 골드 획득(양수만 받습니다). 소모는 TrySpendGold를 씁니다.
        public void AddGold(int amount)
        {
            if (amount <= 0)
                return;

            _gold += amount;
            goldCountViewer.AddCount(amount);
        }

        // 캐릭터 선택 팝업을 띄웁니다. 선택된 캐릭터 데이터가 onSelected로 전달됩니다.
        public void ShowCharacterSelect(Action<CharacterStatDataRow> onSelected)
        {
            characterSelectPopup.Show(onSelected);
        }

        // 스테이지가 절반을 넘기면 코스트 회복을 가속합니다.
        // 후반에 몬스터가 몰릴수록 카드를 더 자주 쓸 수 있게 하려는 의도입니다.
        private void Update()
        {
            if (!IsInitialized || costUI == null)
                return;

            // 채굴 진행도를 기준으로 후반 가속을 판단합니다.
            var progress = miningProgressUI != null ? miningProgressUI.Progress : 0f;

            costUI.SetSpeedUp(progress >= costSpeedUpProgress);
        }

        // 카드 버프 표시를 시작합니다.
        public void InitBuffList(MainGame.Bonus.TemporaryBuffState buffs)
        {
            if (buffListUI != null)
                buffListUI.Init(buffs);
        }

        // 현재 스테이지 번호
        public int Stage => stageUI.Stage;

        // 아직 보여줄 레벨업이 남아 있는지 (팝업 이후 게임 재개 판단용)
        // 광물을 하나 캘 때마다 호출합니다(클리어 조건).
        // 강화 구매에 쓸 골드를 소모합니다. 모자라면 아무것도 하지 않고 false를 돌려줍니다.
        // 저장된 스테이지·골드로 되돌립니다(게임 시작 시 1회).
        public void RestoreProgress(int stage, int gold)
        {
            _gold = Mathf.Max(0, gold);
            goldCountViewer.SetCount(_gold);

            stageUI.SetStage(Mathf.Max(1, stage));
            miningProgressUI?.SetGoalByStage(Stage);

            // 강화 복원이 끝난 뒤 최대치를 다시 계산합니다.
            // (InitAsync에서 이미 Init을 했지만 그때는 아직 강화가 반영되기 전입니다.)
            staminaUI?.Reset();
        }

        public bool TrySpendGold(int amount)
        {
            if (amount <= 0)
                return true;

            if (Gold < amount)
                return false;

            // AddGold는 획득 전용(양수만 받음)이라 소모는 직접 처리합니다.
            _gold -= amount;
            goldCountViewer.SetCount(_gold);

            return true;
        }

        // 테스트용 — 남은 채굴량을 한 번에 채웁니다(스태미나는 쓰지 않습니다).
        public void ForceCompleteMining()
        {
            if (miningProgressUI == null)
                return;

            miningProgressUI.Add(miningProgressUI.Goal - miningProgressUI.Current);
        }

        // 테스트용 — 스태미나를 바닥내 실패시킵니다.
        public void ForceDrainStamina()
        {
            staminaUI?.Consume(float.MaxValue);
        }

        public void AddMinedCount(int amount = 1)
        {
            miningProgressUI?.Add(amount);

            // 채굴은 스태미나를 씁니다. 많이 캘수록 몬스터를 버틸 여력이 줄어듭니다.
            for (var i = 0; i < amount; i++)
                staminaUI?.ConsumeByMining();
        }

        // 몬스터에게 맞았을 때 호출합니다.
        // 회복 카드용 — 최대 스태미나의 비율만큼 회복합니다.
        // (체력 시스템을 걷어내면서 힐 카드의 회복 대상이 스태미나로 바뀌었습니다.)
        public void RecoverStaminaByRatio(float ratio)
        {
            if (staminaUI == null || ratio <= 0f)
                return;

            staminaUI.Recover(staminaUI.Max * ratio);
        }

        public void ConsumeStaminaByHit()
        {
            staminaUI?.ConsumeByHit();
        }

        // 스태미나가 바닥났을 때 실행할 처리를 등록합니다.
        // 강화 보정치 조회를 스태미나에 넘겨줍니다.
        public void SetStaminaBonusProvider(System.Func<(float, float, float, float)> provider)
        {
            staminaUI?.SetBonusProvider(provider);

            // 보정치가 연결된 뒤에야 강화가 반영된 최대치를 계산할 수 있습니다.
            staminaUI?.Reset();
        }

        public void SetStaminaEmptyHandler(System.Action handler)
        {
            staminaUI?.SetEmptyHandler(handler);
        }


        // 팝업 등으로 게임을 멈출 때 코스트 회복과 스태미나를 함께 멈춥니다.
        public void SetPaused(bool paused)
        {
            staminaUI?.SetPaused(paused);
            miningProgressUI?.SetPaused(paused);
            costUI.SetPaused(paused);
        }

        // 스테이지가 이미 끝났는지. 한 스테이지에서 종료 처리는 한 번만 돌아야 합니다.
        // (목표 달성과 스태미나 소진이 겹치거나, 테스트 버튼을 연타하면 두 번 불립니다.
        //  두 번째 호출이 새 스테이지의 코스트 진행을 멈춰 게임이 정지하는 문제가 있었습니다.)
        private bool _isFinished;

        public void GameFinish()
        {
            if (_isFinished)
                return;

            _isFinished = true;

            costUI.StopProcess();
            OnNextGameExecuted?.Invoke();
        }
    }
}

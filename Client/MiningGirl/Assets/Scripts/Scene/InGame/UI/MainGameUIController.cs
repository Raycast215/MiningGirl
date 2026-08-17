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
        [Tooltip("스테이지 제한 시간(초)")]
        private float stageTimeSeconds = 60f;

        [SerializeField]
        private TimerUI timerUI;
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

            timerUI.Init(GetStageTime(), GameFinish);
            stageUI.Init();
            costUI.Init();

            // 새 클리어 조건: 목표 채굴량을 채우면 스테이지 종료
            staminaUI?.Init();
            miningProgressUI?.Init(GameFinish);
            miningProgressUI?.SetGoalByStage(Stage);

            // 골드는 런 전체에서 누적되므로 여기(최초 진입)에서만 0으로 시작합니다.
            _gold = 0;
            goldCountViewer.SetCount(_gold);

            IsInitialized = true;
        }

        // 스테이지 제한 시간은 게임 상수 테이블에서 가져옵니다.
        // 테이블에 값이 없으면 인스펙터 값으로 대체합니다.
        private float GetStageTime()
        {
            var table = Manager.DataTableManager.Instance?.GameConstantDataTable;

            return table != null ? table.GetValue(EGameConstantType.StageTime, stageTimeSeconds) : stageTimeSeconds;
        }

        // 스테이지가 새로 시작될 때 호출합니다(advanceStage가 false면 같은 스테이지 재도전).
        public void SetTime(bool advanceStage = true)
        {
            if (advanceStage)
                stageUI.NextStage();

            timerUI.SetTime(GetStageTime());
            costUI.SetCost(0);

            staminaUI?.Reset();
            miningProgressUI?.SetGoalByStage(Stage);
        }

        public void GameStart()
        {
            timerUI.Execute().Forget();
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

            // 제한 시간이 사라졌으므로 채굴 진행도를 기준으로 후반 가속을 판단합니다.
            var progress = miningProgressUI != null ? miningProgressUI.Progress
                : (timerUI != null ? timerUI.Progress : 0f);

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

        public void AddMinedCount(int amount = 1)
        {
            miningProgressUI?.Add(amount);

            // 채굴은 스태미나를 씁니다. 많이 캘수록 몬스터를 버틸 여력이 줄어듭니다.
            for (var i = 0; i < amount; i++)
                staminaUI?.ConsumeByMining();
        }

        // 몬스터에게 맞았을 때 호출합니다.
        public void ConsumeStaminaByHit()
        {
            staminaUI?.ConsumeByHit();
        }

        // 스태미나가 바닥났을 때 실행할 처리를 등록합니다.
        // 강화 보정치 조회를 스태미나에 넘겨줍니다.
        public void SetStaminaBonusProvider(System.Func<(float, float, float, float)> provider)
        {
            staminaUI?.SetBonusProvider(provider);
        }

        public void SetStaminaEmptyHandler(System.Action handler)
        {
            staminaUI?.SetEmptyHandler(handler);
        }


        // 팝업 등으로 게임을 멈출 때 타이머와 코스트 회복을 함께 멈춥니다.
        public void SetPaused(bool paused)
        {
            timerUI.SetPaused(paused);
            staminaUI?.SetPaused(paused);
            miningProgressUI?.SetPaused(paused);
            costUI.SetPaused(paused);
        }

        public void GameFinish()
        {
            costUI.StopProcess();
            OnNextGameExecuted?.Invoke();
            Debug.Log("게임 종료");
        }
    }
}

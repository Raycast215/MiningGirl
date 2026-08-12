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
        private Action<int, Action> _onLevelUp;

        // 이번 런에서 누적된 골드. 스테이지 재시작(Next)에도 초기화되지 않습니다.
        private int _gold;

        public int Gold => _gold;

        [SerializeField]
        [Tooltip("스테이지 제한 시간(초)")]
        private float stageTimeSeconds = 60f;

        [SerializeField]
        private TimerUI timerUI;
        [SerializeField]
        private CostUI costUI;
        [SerializeField]
        private LevelExpUI levelExpUI;
        [SerializeField]
        [Tooltip("채굴로 획득한 골드를 표시합니다")]
        private Scene.InGame.UI.Resource.CountViewerUI goldCountViewer;
        [SerializeField]
        private LevelUpBonusSelectPopup levelUpBonusPopup;
        [SerializeField]
        private CharacterSelectPopup characterSelectPopup;

        public async UniTask InitAsync(Action onNextGameExecuted, Action<int, Action> onLevelUp = null)
        {
            OnNextGameExecuted = null;
            OnNextGameExecuted += onNextGameExecuted;

            _onLevelUp = onLevelUp;
            
            timerUI.Init(stageTimeSeconds, GameFinish);
            costUI.Init();
            levelExpUI.Init(OnLevelUp);

            // 골드는 런 전체에서 누적되므로 여기(최초 진입)에서만 0으로 시작합니다.
            _gold = 0;
            goldCountViewer.SetCount(_gold);
            
            IsInitialized = true;
        }

        public void SetTime()
        {
            timerUI.SetTime(stageTimeSeconds);
            costUI.SetCost(0);
            levelExpUI.Reset();
        }

        public void GameStart()
        {
            timerUI.Execute().Forget();
            costUI.Execute().Forget();
        }
        
        // 카드 사용 등 외부에서 코스트를 다룰 때 쓰는 진입점입니다.
        public bool CanAffordCost(int amount) => costUI.CanAfford(amount);
        public bool TrySpendCost(int amount) => costUI.TrySpend(amount);
        public void AddCost(int amount, bool allowOvercharge = false) => costUI.Add(amount, allowOvercharge);

        // 레벨업 시 호출됩니다. 한 번에 여러 레벨이 올라도 레벨당 한 번씩 호출됩니다.
        // TODO: 추후 이 시점에 레벨업 보너스 선택 UI를 띄웁니다.
        // 레벨업 연출이 한 단계 끝난 시점에 호출됩니다.
        // onContinue를 호출해야 다음 레벨 연출이 이어집니다.
        private void OnLevelUp(int newLevel, Action onContinue)
        {
            Debug.Log($"[LevelUp] Lv.{newLevel} 달성");

            if (_onLevelUp == null)
            {
                onContinue?.Invoke();
                return;
            }

            _onLevelUp.Invoke(newLevel, onContinue);
        }

        public void AddGold(int amount)
        {
            if (amount <= 0)
                return;

            _gold += amount;
            goldCountViewer.AddCount(amount);
        }

        // 경험치 지급(외부 컨트롤러가 호출)
        public void AddExp(int amount)
        {
            levelExpUI.AddExp(amount);
        }

        // 레벨업 보너스 팝업을 띄웁니다. 선택된 보너스는 onSelected로 전달됩니다.
        public void ShowLevelUpBonus(int level, LevelUpBonusState state, Action<LevelUpBonusSkillDataTableRow> onSelected)
        {
            levelUpBonusPopup.Show(level, state, onSelected);
        }

        // 캐릭터 선택 팝업을 띄웁니다. 선택된 캐릭터 데이터가 onSelected로 전달됩니다.
        public void ShowCharacterSelect(Action<CharacterStatDataRow> onSelected)
        {
            characterSelectPopup.Show(onSelected);
        }

        // 아직 보여줄 레벨업이 남아 있는지 (팝업 이후 게임 재개 판단용)
        public bool HasPendingLevelUp => levelExpUI.HasPendingLevelUp;

        // 팝업 등으로 게임을 멈출 때 타이머와 코스트 회복을 함께 멈춥니다.
        public void SetPaused(bool paused)
        {
            timerUI.SetPaused(paused);
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
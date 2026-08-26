using System;
using Cysharp.Threading.Tasks;
using Data;
using Legacy.MainGame.UI;
using Legacy.Scene.InGame.State;
using UnityEngine;

namespace Legacy.MainGame
{
    // 게임 상태를 화면에 그리는 일만 합니다.
    //
    // 값의 주인은 RunState이고 이 클래스는 그것을 읽어 각 View에 넘길 뿐입니다.
    // 게임플레이 코드는 이 클래스를 거치지 않고 RunState를 직접 씁니다.
    // (예전에는 골드·스테이지·스태미나·코스트가 전부 여기와 하위 UI에 들어 있었고,
    //  세이브까지 이 클래스에서 값을 읽어갔습니다.)
    public class MainGameUIController : GameMonoInitializer
    {
        [SerializeField]
        private StageUI stageUI;
        [SerializeField]
        private BuffListUI buffListUI;

        [SerializeField]
        private StaminaUI staminaUI;
        [SerializeField]
        private MiningProgressUI miningProgressUI;

        [SerializeField]
        private CostUI costUI;
        [SerializeField]
        [Tooltip("채굴로 획득한 골드를 표시합니다")]
        private Legacy.Scene.InGame.UI.Resource.CountViewerUI goldCountViewer;
        [SerializeField]
        private CharacterSelectPopup characterSelectPopup;

        private RunState _state;

        public async UniTask InitAsync()
        {
            stageUI.Init();
            costUI.Init();

            IsInitialized = true;

            await UniTask.CompletedTask;
        }

        // 그릴 대상을 받습니다. 값이 바뀌면 알림을 받아 해당 부분만 다시 그립니다.
        public void Bind(RunState state)
        {
            if (_state != null)
                Unbind();

            _state = state;

            if (_state == null)
                return;

            _state.OnStageChanged += RenderStage;
            _state.OnGoldChanged += RenderGold;
            _state.Stamina.OnChanged += RenderStamina;
            _state.Mining.OnChanged += RenderMining;
            _state.Cost.OnChanged += RenderCost;

            RenderAll();
        }

        public void Unbind()
        {
            if (_state == null)
                return;

            _state.OnStageChanged -= RenderStage;
            _state.OnGoldChanged -= RenderGold;
            _state.Stamina.OnChanged -= RenderStamina;
            _state.Mining.OnChanged -= RenderMining;
            _state.Cost.OnChanged -= RenderCost;

            _state = null;
        }

        private void OnDestroy()
        {
            Unbind();
        }

        public void RenderAll()
        {
            RenderStage();
            RenderGold();
            RenderStamina();
            RenderMining();
            RenderCost();
        }

        private void RenderStage()
        {
            stageUI?.SetStage(_state.Stage);
        }

        private void RenderGold()
        {
            goldCountViewer?.SetCount(_state.Gold);
        }

        private void RenderStamina()
        {
            staminaUI?.SetValue(_state.Stamina.Current, _state.Stamina.Max);
        }

        private void RenderMining()
        {
            miningProgressUI?.SetValue(_state.Mining.Current, _state.Mining.Goal);
        }

        private void RenderCost()
        {
            costUI?.SetValue(_state.Cost.Cost, _state.Cost.RegenProgress, _state.Cost.Max);
        }

        // 코스트 오브는 '다음 1까지의 진행도'가 매 프레임 조금씩 차오르므로
        // 이벤트가 아니라 여기서 계속 그려 줍니다.
        private void Update()
        {
            if (_state == null)
                return;

            RenderCost();
        }

        // 팝업 등으로 게임을 멈출 때 게이지 연출도 함께 멈춥니다.
        public void SetPaused(bool paused)
        {
            staminaUI?.SetPaused(paused);
            miningProgressUI?.SetPaused(paused);
        }

        // 카드 버프 표시를 시작합니다.
        public void InitBuffList(Legacy.MainGame.Bonus.TemporaryBuffState buffs)
        {
            if (buffListUI != null)
                buffListUI.Init(buffs);
        }

        // 캐릭터 선택 팝업이 데이터를 직접 뒤지지 않도록 조회 함수를 넘겨줍니다.
        public void InitCharacterSelect(
            Func<System.Collections.Generic.IReadOnlyList<CharacterStatDataRow>> getCharacters,
            Func<ELevelUpBonusEffectType, LevelUpBonusSkillDataTableRow> findBonusRow)
        {
            characterSelectPopup?.Init(getCharacters, findBonusRow);
        }

        // 캐릭터 선택 팝업을 띄웁니다. 선택된 캐릭터 데이터가 onSelected로 전달됩니다.
        public void ShowCharacterSelect(Action<CharacterStatDataRow> onSelected)
        {
            characterSelectPopup.Show(onSelected);
        }
    }
}

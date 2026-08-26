using Scene.MainGameScene.ViewModel;
using TMPro;
using UI.Common;
using UnityEngine;

namespace Scene.MainGameScene.UI
{
    // 인게임 상시 표시. ViewModel을 구독해 그리기만 합니다.
    // 계산과 포맷은 InGameHudViewModel에 있습니다.
    public class InGameHudUI : MonoBehaviour
    {
        [Header("Top")]
        [SerializeField]
        private TMP_Text stageText;

        [SerializeField]
        private TMP_Text waveText;

        [SerializeField]
        [Tooltip("다음 레벨업까지의 진행도. 웨이브 남은 시간이 아닙니다.")]
        private GaugeBarView expGauge;

        [SerializeField]
        private TMP_Text elapsedText;

        [Header("Bottom")]
        [SerializeField]
        private GaugeBarView towerHealthGauge;

        [SerializeField]
        [Tooltip("왼쪽부터 채웁니다. 칸 수가 SkillSlotMax보다 적으면 넘치는 스킬은 안 보입니다.")]
        private SkillSlotView[] skillSlots;

        public int SlotViewCount => skillSlots?.Length ?? 0;

        // 하단 UI 띠(타워 체력바 + 스킬 슬롯)의 윗선을 월드 좌표로 돌려줍니다.
        //
        // 타워와 캐릭터 배치를 여기서 역산합니다. 상수로 박으면 SafeArea가 다른 기기에서
        // 타워가 UI 뒤로 묻히거나 붕 뜹니다.
        public bool TryGetBottomBandWorldTopY(out float worldY)
        {
            worldY = 0f;

            if (towerHealthGauge == null)
                return false;

            var rect = towerHealthGauge.transform as RectTransform;

            if (rect == null)
                return false;

            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);

            // 0=좌하 1=좌상 2=우상 3=우하
            worldY = corners[1].y;

            return true;
        }

        private InGameHudViewModel _viewModel;

        // 타워 체력은 첫 표시만 트윈 없이 즉시 채웁니다.
        private bool _towerFirstDraw = true;

        // 경험치가 줄어들었는지 보려면 직전 값이 필요합니다.
        private bool _expFirstDraw = true;
        private float _lastExp;

        private void OnDestroy()
        {
            Unbind();
        }

        // 쿨다운은 매 프레임 바뀌어 알림으로 받으면 낭비라 그릴 때 읽어 옵니다.
        private void LateUpdate()
        {
            if (_viewModel == null || skillSlots == null)
                return;

            var slots = _viewModel.Slots;

            for (var i = 0; i < skillSlots.Length && i < slots.Count; i++)
            {
                if (!slots[i].HasSkill)
                    continue;

                skillSlots[i].SetCooldown(_viewModel.GetCooldownRatio(i));
            }
        }

        public void Bind(InGameHudViewModel viewModel)
        {
            Unbind();

            _viewModel = viewModel;

            if (_viewModel == null)
                return;

            _viewModel.StageText.Bind(OnStageTextChanged);
            _viewModel.WaveText.Bind(OnWaveTextChanged);
            _viewModel.ElapsedText.Bind(OnElapsedTextChanged);
            _viewModel.Exp.Bind(OnExpChanged);
            _viewModel.TowerHealth.Bind(OnTowerHealthChanged);
            _viewModel.SlotRevision.Bind(OnSlotsChanged);
            _viewModel.IsPaused.Bind(OnPausedChanged);
        }

        private void Unbind()
        {
            if (_viewModel == null)
                return;

            _viewModel.StageText.Unbind(OnStageTextChanged);
            _viewModel.WaveText.Unbind(OnWaveTextChanged);
            _viewModel.ElapsedText.Unbind(OnElapsedTextChanged);
            _viewModel.Exp.Unbind(OnExpChanged);
            _viewModel.TowerHealth.Unbind(OnTowerHealthChanged);
            _viewModel.SlotRevision.Unbind(OnSlotsChanged);
            _viewModel.IsPaused.Unbind(OnPausedChanged);

            _viewModel = null;
        }

#region 바인딩 대상

        private void OnStageTextChanged(string value)
        {
            if (stageText != null)
                stageText.text = value;
        }

        private void OnWaveTextChanged(string value)
        {
            if (waveText != null)
                waveText.text = value;
        }

        private void OnElapsedTextChanged(string value)
        {
            if (elapsedText != null)
                elapsedText.text = value;
        }

        private void OnExpChanged(GaugeValue value)
        {
            if (expGauge == null)
                return;

            // 레벨업하면 경험치가 0으로 돌아갑니다. 이때 트윈을 태우면 게이지가
            // 주르륵 줄어들어 "경험치를 잃었다"로 보입니다. 오른쪽 끝까지 찼다가
            // 사라지는 게 맞는 그림이라, 줄어드는 방향은 트윈 없이 즉시 반영합니다.
            var reset = value.Current < _lastExp;

            expGauge.SetValue(value.Current, value.Max, reset || _expFirstDraw);

            _lastExp = value.Current;
            _expFirstDraw = false;
        }

        private void OnTowerHealthChanged(GaugeValue value)
        {
            if (towerHealthGauge == null)
                return;

            towerHealthGauge.SetValue(value.Current, value.Max, _towerFirstDraw);

            _towerFirstDraw = false;
        }

        private void OnSlotsChanged(int revision)
        {
            if (skillSlots == null || _viewModel == null)
                return;

            var slots = _viewModel.Slots;

            for (var i = 0; i < skillSlots.Length; i++)
            {
                if (i >= slots.Count || !slots[i].HasSkill)
                {
                    skillSlots[i].SetEmpty();

                    continue;
                }

                skillSlots[i].SetSkill(slots[i].IconAssetId, slots[i].Level);
            }
        }

        // 3택이 떠 있는 동안에는 게이지 트윈도 멈춥니다.
        private void OnPausedChanged(bool paused)
        {
            if (expGauge != null)
                expGauge.SetPaused(paused);

            if (towerHealthGauge != null)
                towerHealthGauge.SetPaused(paused);
        }

#endregion
    }
}

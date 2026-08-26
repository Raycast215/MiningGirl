using Scene.MainGameScene.ViewModel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scene.MainGameScene.UI
{
    // 스테이지 결과 오버레이. ViewModel을 구독해 그리고, 버튼은 커맨드를 부릅니다.
    public class StageResultUI : MonoBehaviour
    {
        [SerializeField]
        private GameObject root;

        [SerializeField]
        [Tooltip("왼쪽부터 채웁니다. 실패해도 숨기지 않고 빈 별로 남겨 둡니다.")]
        private StarIconView[] stars;

        [SerializeField]
        private TMP_Text titleText;

        [SerializeField]
        private TMP_Text stageText;

        [Header("Rows")]
        [SerializeField]
        [Tooltip("라벨은 우측 정렬, 값은 좌측 정렬로 두어야 세로줄이 가운데에서 맞습니다.")]
        private TMP_Text waveLabel;

        [SerializeField]
        private TMP_Text waveValue;

        [SerializeField]
        private TMP_Text elapsedLabel;

        [SerializeField]
        private TMP_Text elapsedValue;

        [SerializeField]
        private TMP_Text towerLabel;

        [SerializeField]
        private TMP_Text towerValue;

        [SerializeField]
        private Button retryButton;

        private StageResultViewModel _viewModel;

        private void Awake()
        {
            if (retryButton != null)
                retryButton.onClick.AddListener(() => _viewModel?.Retry());
        }

        private void OnDestroy()
        {
            Unbind();
        }

        public void Bind(StageResultViewModel viewModel)
        {
            Unbind();

            _viewModel = viewModel;

            if (_viewModel == null)
                return;

            _viewModel.IsVisible.Bind(OnVisibleChanged);
            _viewModel.StarCount.Bind(OnStarCountChanged);
            _viewModel.TitleText.Bind(OnTitleChanged);
            _viewModel.StageText.Bind(OnStageChanged);
            _viewModel.WaveLabel.Bind(OnWaveLabelChanged);
            _viewModel.WaveValue.Bind(OnWaveValueChanged);
            _viewModel.ElapsedLabel.Bind(OnElapsedLabelChanged);
            _viewModel.ElapsedValue.Bind(OnElapsedValueChanged);
            _viewModel.TowerLabel.Bind(OnTowerLabelChanged);
            _viewModel.TowerValue.Bind(OnTowerValueChanged);
        }

        private void Unbind()
        {
            if (_viewModel == null)
                return;

            _viewModel.IsVisible.Unbind(OnVisibleChanged);
            _viewModel.StarCount.Unbind(OnStarCountChanged);
            _viewModel.TitleText.Unbind(OnTitleChanged);
            _viewModel.StageText.Unbind(OnStageChanged);
            _viewModel.WaveLabel.Unbind(OnWaveLabelChanged);
            _viewModel.WaveValue.Unbind(OnWaveValueChanged);
            _viewModel.ElapsedLabel.Unbind(OnElapsedLabelChanged);
            _viewModel.ElapsedValue.Unbind(OnElapsedValueChanged);
            _viewModel.TowerLabel.Unbind(OnTowerLabelChanged);
            _viewModel.TowerValue.Unbind(OnTowerValueChanged);

            _viewModel = null;
        }

        private void OnVisibleChanged(bool visible)
        {
            if (root != null)
                root.SetActive(visible);
        }

        private void OnStarCountChanged(int count)
        {
            if (stars == null)
                return;

            for (var i = 0; i < stars.Length; i++)
                stars[i].SetFilled(i < count);
        }

        private void OnTitleChanged(string value)
        {
            if (titleText != null)
                titleText.text = value;
        }

        private void OnStageChanged(string value)
        {
            if (stageText != null)
                stageText.text = value;
        }

        private void OnWaveLabelChanged(string value)
        {
            if (waveLabel != null)
                waveLabel.text = value;
        }

        private void OnWaveValueChanged(string value)
        {
            if (waveValue != null)
                waveValue.text = value;
        }

        private void OnElapsedLabelChanged(string value)
        {
            if (elapsedLabel != null)
                elapsedLabel.text = value;
        }

        private void OnElapsedValueChanged(string value)
        {
            if (elapsedValue != null)
                elapsedValue.text = value;
        }

        private void OnTowerLabelChanged(string value)
        {
            if (towerLabel != null)
                towerLabel.text = value;
        }

        private void OnTowerValueChanged(string value)
        {
            if (towerValue != null)
                towerValue.text = value;
        }
    }
}

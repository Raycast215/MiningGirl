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
        private TMP_Text waveText;

        [SerializeField]
        private TMP_Text elapsedText;

        [SerializeField]
        private TMP_Text towerText;

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
            _viewModel.WaveText.Bind(OnWaveChanged);
            _viewModel.ElapsedText.Bind(OnElapsedChanged);
            _viewModel.TowerText.Bind(OnTowerChanged);
        }

        private void Unbind()
        {
            if (_viewModel == null)
                return;

            _viewModel.IsVisible.Unbind(OnVisibleChanged);
            _viewModel.StarCount.Unbind(OnStarCountChanged);
            _viewModel.TitleText.Unbind(OnTitleChanged);
            _viewModel.WaveText.Unbind(OnWaveChanged);
            _viewModel.ElapsedText.Unbind(OnElapsedChanged);
            _viewModel.TowerText.Unbind(OnTowerChanged);

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

        private void OnWaveChanged(string value)
        {
            if (waveText != null)
                waveText.text = value;
        }

        private void OnElapsedChanged(string value)
        {
            if (elapsedText != null)
                elapsedText.text = value;
        }

        private void OnTowerChanged(string value)
        {
            if (towerText != null)
                towerText.text = value;
        }
    }
}

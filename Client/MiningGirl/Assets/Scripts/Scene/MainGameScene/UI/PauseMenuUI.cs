using Scene.MainGameScene.ViewModel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scene.MainGameScene.UI
{
    // 인게임 메뉴와 배속 버튼. ViewModel을 구독해 그리고 버튼은 커맨드를 부릅니다.
    public class PauseMenuUI : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField]
        [Tooltip("메뉴를 여는 버튼. 상시 표시입니다.")]
        private Button menuButton;

        [SerializeField]
        [Tooltip("배속 버튼. 메뉴를 열지 않고 바로 누릅니다.")]
        private Button speedButton;

        [SerializeField]
        private TMP_Text speedLabel;

        [Header("Overlay")]
        [SerializeField]
        private GameObject root;

        [SerializeField]
        private Button resumeButton;

        [SerializeField]
        private Button surrenderButton;

        [SerializeField]
        private TMP_Text surrenderLabel;

        [SerializeField]
        [Tooltip("포기를 한 번 눌렀을 때 뜨는 되묻기 안내")]
        private GameObject confirmHint;

        [SerializeField]
        [Tooltip("되묻기 상태에서 포기 버튼에 넣을 문구")]
        private string surrenderConfirmText = "정말 포기합니다";

        [SerializeField]
        private string surrenderText = "게임 포기하기";

        // 오버레이 바깥을 눌러도 닫히게 하는 뒷판. 없으면 무시합니다.
        [SerializeField]
        private Button dimmedBackground;

        private PauseMenuViewModel _viewModel;

        private void Awake()
        {
            if (menuButton != null)
                menuButton.onClick.AddListener(() => _viewModel?.Open());

            if (speedButton != null)
                speedButton.onClick.AddListener(() => _viewModel?.ToggleSpeed());

            if (resumeButton != null)
                resumeButton.onClick.AddListener(() => _viewModel?.Close());

            if (surrenderButton != null)
                surrenderButton.onClick.AddListener(() => _viewModel?.RequestSurrender());

            // 되묻는 중에 바깥을 누르면 포기가 아니라 되묻기만 취소합니다.
            if (dimmedBackground != null)
                dimmedBackground.onClick.AddListener(HandleBackgroundClicked);
        }

        private void OnDestroy()
        {
            Unbind();
        }

        public void Bind(PauseMenuViewModel viewModel)
        {
            Unbind();

            _viewModel = viewModel;

            if (_viewModel == null)
                return;

            _viewModel.IsOpen.Bind(OnOpenChanged);
            _viewModel.IsConfirmingSurrender.Bind(OnConfirmChanged);
            _viewModel.SpeedText.Bind(OnSpeedTextChanged);
            _viewModel.IsAvailable.Bind(OnAvailableChanged);
        }

        private void Unbind()
        {
            if (_viewModel == null)
                return;

            _viewModel.IsOpen.Unbind(OnOpenChanged);
            _viewModel.IsConfirmingSurrender.Unbind(OnConfirmChanged);
            _viewModel.SpeedText.Unbind(OnSpeedTextChanged);
            _viewModel.IsAvailable.Unbind(OnAvailableChanged);

            _viewModel = null;
        }

        private void HandleBackgroundClicked()
        {
            if (_viewModel == null)
                return;

            if (_viewModel.IsConfirmingSurrender.Value)
            {
                _viewModel.CancelSurrender();

                return;
            }

            _viewModel.Close();
        }

#region 바인딩 대상

        private void OnOpenChanged(bool open)
        {
            if (root != null)
                root.SetActive(open);
        }

        private void OnConfirmChanged(bool confirming)
        {
            if (confirmHint != null)
                confirmHint.SetActive(confirming);

            if (surrenderLabel != null)
                surrenderLabel.text = confirming ? surrenderConfirmText : surrenderText;
        }

        private void OnSpeedTextChanged(string value)
        {
            if (speedLabel != null)
                speedLabel.text = value;
        }

        // 결과 화면이 뜨면 메뉴 버튼과 배속 버튼을 잠급니다.
        // 끝난 판의 속도를 바꾸거나 메뉴를 여는 건 의미가 없습니다.
        private void OnAvailableChanged(bool available)
        {
            if (menuButton != null)
                menuButton.interactable = available;

            if (speedButton != null)
                speedButton.interactable = available;
        }

#endregion
    }
}

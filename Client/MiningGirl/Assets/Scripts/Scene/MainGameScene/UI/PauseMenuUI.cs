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

        // 버튼을 이미 연결했는지. Bind는 여러 번 불릴 수 있습니다.
        private bool _buttonsWired;

        // 버튼 연결을 Awake가 아니라 Bind에서 합니다.
        //
        // 이 컴포넌트는 처음에 꺼져 있는 PauseMenu 오브젝트에 붙어 있어 Awake가
        // 돌지 않습니다. 그런데 메뉴 버튼과 배속 버튼은 HUD에 있어 늘 켜져 있으므로,
        // Awake에서 연결하면 버튼은 멀쩡히 보이는데 눌러도 아무 일이 없습니다.
        // Bind는 컨트롤러가 직접 부르므로 오브젝트가 꺼져 있어도 실행됩니다.
        private void WireButtons()
        {
            if (_buttonsWired)
                return;

            _buttonsWired = true;

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
            WireButtons();

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

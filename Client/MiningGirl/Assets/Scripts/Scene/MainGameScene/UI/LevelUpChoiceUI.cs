using Scene.MainGameScene.ViewModel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scene.MainGameScene.UI
{
    // 레벨업 3택 오버레이. ViewModel을 구독해 그리고, 누른 칸 번호만 되돌려 줍니다.
    //
    // 확인 버튼을 두지 않습니다. 한 판에 열대여섯 번 뜨는데 매번 두 번 누르게 하면 번거롭습니다.
    public class LevelUpChoiceUI : MonoBehaviour
    {
        [SerializeField]
        private GameObject root;

        [SerializeField]
        private TMP_Text headerText;

        [SerializeField]
        [Tooltip("세로 3장. 가로로 놓으면 장당 폭이 좁아 이름과 수치가 안 들어갑니다.")]
        private LevelUpChoiceItemUI[] items;

        [Header("다시 뽑기")]
        [SerializeField]
        [Tooltip("카드 목록 아래에 놓습니다")]
        private Button rerollButton;

        [SerializeField]
        [Tooltip("남은 횟수. 쓴 횟수가 아니라 남은 횟수를 적습니다")]
        private TMP_Text rerollLabel;

        [SerializeField]
        [Tooltip("{0}에 남은 횟수가 들어갑니다")]
        private string rerollFormat = "다시 뽑기 {0}";

        [SerializeField]
        [Tooltip("후보가 모자라 다시 뽑아도 소용없을 때. 횟수 소진과 구분됩니다")]
        private string notEnoughPoolText = "다시 뽑을 카드 없음";

        [SerializeField]
        [Tooltip("끈 상태의 글자색")]
        private Color disabledLabelColor = new Color32(128, 120, 138, 255);

        [SerializeField]
        private Color labelColor = new Color32(255, 248, 232, 255);

        private LevelUpChoiceViewModel _viewModel;

        private void Awake()
        {
            if (rerollButton != null)
                rerollButton.onClick.AddListener(() => _viewModel?.Reroll());
        }

        private void OnDestroy()
        {
            Unbind();
        }

        public void Bind(LevelUpChoiceViewModel viewModel)
        {
            Unbind();

            _viewModel = viewModel;

            if (_viewModel == null)
                return;

            _viewModel.IsVisible.Bind(OnVisibleChanged);
            _viewModel.HeaderText.Bind(OnHeaderChanged);
            _viewModel.ItemRevision.Bind(OnItemsChanged);
            _viewModel.RemainingRerolls.Bind(OnRerollCountChanged);
            _viewModel.RerollBlockReason.Bind(OnRerollBlockChanged);
        }

        private void Unbind()
        {
            if (_viewModel == null)
                return;

            _viewModel.IsVisible.Unbind(OnVisibleChanged);
            _viewModel.HeaderText.Unbind(OnHeaderChanged);
            _viewModel.ItemRevision.Unbind(OnItemsChanged);
            _viewModel.RemainingRerolls.Unbind(OnRerollCountChanged);
            _viewModel.RerollBlockReason.Unbind(OnRerollBlockChanged);

            _viewModel = null;
        }

        private void OnVisibleChanged(bool visible)
        {
            if (root != null)
                root.SetActive(visible);
        }

        private void OnHeaderChanged(string value)
        {
            if (headerText != null)
                headerText.text = value;
        }

        private void OnItemsChanged(int revision)
        {
            if (items == null || _viewModel == null)
                return;

            var source = _viewModel.Items;

            for (var i = 0; i < items.Length; i++)
            {
                if (i >= source.Count)
                {
                    items[i].Hide();

                    continue;
                }

                var index = i;

                items[i].Bind(source[i], () => _viewModel.Select(index));
            }
        }

        private void OnRerollCountChanged(int remaining)
        {
            RefreshRerollLabel();
        }

        // 0회가 되어도 버튼을 숨기지 않고 끄기만 합니다.
        // 사라지면 원래 없는 기능으로 읽힙니다.
        private void OnRerollBlockChanged(ERerollBlockReason reason)
        {
            if (rerollButton != null)
                rerollButton.interactable = reason == ERerollBlockReason.None;

            RefreshRerollLabel();
        }

        private void RefreshRerollLabel()
        {
            if (rerollLabel == null || _viewModel == null)
                return;

            var reason = _viewModel.RerollBlockReason.Value;

            // 후보 부족은 남은 횟수와 무관한 상태라 숫자를 보여 주면 오해합니다.
            // 횟수가 남았는데도 못 누르는 이유가 숫자에 있는 것처럼 읽힙니다.
            rerollLabel.text = reason == ERerollBlockReason.NotEnoughPool
                ? notEnoughPoolText
                : string.Format(rerollFormat, _viewModel.RemainingRerolls.Value);

            rerollLabel.color = reason == ERerollBlockReason.None ? labelColor : disabledLabelColor;
        }
    }
}

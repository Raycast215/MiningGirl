using Scene.MainGameScene.ViewModel;
using TMPro;
using UnityEngine;

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

        private LevelUpChoiceViewModel _viewModel;

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
        }

        private void Unbind()
        {
            if (_viewModel == null)
                return;

            _viewModel.IsVisible.Unbind(OnVisibleChanged);
            _viewModel.HeaderText.Unbind(OnHeaderChanged);
            _viewModel.ItemRevision.Unbind(OnItemsChanged);

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
    }
}

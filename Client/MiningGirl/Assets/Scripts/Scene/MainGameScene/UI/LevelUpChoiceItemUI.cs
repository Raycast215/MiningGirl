using System;
using Manager;
using Scene.MainGameScene.ViewModel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scene.MainGameScene.UI
{
    // 3택 카드 한 장.
    //
    // 부모가 값을 밀어넣는 순수 표시 컴포넌트라 ViewModel을 따로 두지 않았습니다.
    public class LevelUpChoiceItemUI : MonoBehaviour
    {
        [SerializeField]
        private Button button;

        [SerializeField]
        private Image icon;

        [SerializeField]
        private TMP_Text titleText;

        [SerializeField]
        private TMP_Text subtitleText;

        [SerializeField]
        private TMP_Text detailText;

        [SerializeField]
        [Tooltip("새로 얻는 스킬에만 켜지는 배지")]
        private GameObject newBadge;

        private Action _onClicked;

        private void Awake()
        {
            if (button != null)
                button.onClick.AddListener(() => _onClicked?.Invoke());
        }

        public void Bind(LevelUpChoiceItem item, Action onClicked)
        {
            _onClicked = onClicked;

            gameObject.SetActive(true);

            if (titleText != null)
                titleText.text = item.Title;

            if (subtitleText != null)
                subtitleText.text = item.Subtitle;

            if (detailText != null)
                detailText.text = item.Detail;

            if (newBadge != null)
                newBadge.SetActive(item.IsNew);

            AddressableManager.Instance.ApplySprite(item.IconAssetId, icon);
        }

        public void Hide()
        {
            _onClicked = null;

            gameObject.SetActive(false);
        }
    }
}

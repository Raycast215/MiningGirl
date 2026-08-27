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

        [Header("강화스킬")]
        [SerializeField]
        [Tooltip("런당 한 번뿐인 선택이라 일반 카드와 다르게 보이는 테두리")]
        private GameObject masteryFrame;

        [SerializeField]
        [Tooltip("조건 진행도를 담는 줄. 조건을 진행시키는 강화 카드에만 켜집니다")]
        private GameObject masteryProgressRoot;

        [SerializeField]
        private Image masteryHintIcon;

        [SerializeField]
        [Tooltip("조건 두 줄. 왼쪽부터 채웁니다")]
        private TMP_Text[] masteryProgressTexts;

        [SerializeField]
        [Tooltip("이 카드가 올리는 조건에 쓰는 색")]
        private Color advancingColor = new Color(1f, 0.85f, 0.35f, 1f);

        [SerializeField]
        [Tooltip("이미 채운 조건에 쓰는 색")]
        private Color metColor = new Color(0.45f, 0.85f, 0.55f, 1f);

        [SerializeField]
        [Tooltip("아직 못 채운 조건에 쓰는 색")]
        private Color pendingColor = new Color(0.62f, 0.66f, 0.72f, 1f);

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

            if (masteryFrame != null)
                masteryFrame.SetActive(item.IsMastery);

            ApplyMasteryProgress(item);

            AddressableManager.Instance.ApplySprite(item.IconAssetId, icon);
        }

        // 강화스킬 조건 진행도를 그립니다.
        //
        // 두 줄을 다 보여줍니다. 이 카드가 올리는 쪽만 보여주면 나머지 절반을
        // 모른 채로 "다 채웠다"고 오해하게 됩니다.
        private void ApplyMasteryProgress(LevelUpChoiceItem item)
        {
            var progress = item.MasteryProgress;
            var show = progress != null && progress.Length > 0;

            if (masteryProgressRoot != null)
                masteryProgressRoot.SetActive(show);

            if (!show || masteryProgressTexts == null)
                return;

            if (masteryHintIcon != null)
                AddressableManager.Instance.ApplySprite(item.MasteryHintIconAssetId, masteryHintIcon);

            for (var i = 0; i < masteryProgressTexts.Length; i++)
            {
                var label = masteryProgressTexts[i];

                if (label == null)
                    continue;

                if (i >= progress.Length)
                {
                    label.gameObject.SetActive(false);

                    continue;
                }

                var entry = progress[i];

                label.gameObject.SetActive(true);
                label.text = entry.Text;

                // 이 카드가 올리는 쪽을 강조합니다. 셋 중 하나만 칠해야 어느 쪽이
                // 오르는지 헷갈리지 않습니다.
                label.color = entry.IsAdvancing
                    ? advancingColor
                    : entry.IsMet ? metColor : pendingColor;
            }
        }

        public void Hide()
        {
            _onClicked = null;

            gameObject.SetActive(false);
        }
    }
}

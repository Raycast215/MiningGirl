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
        // 조건 알약 한 개. 아이콘이 종류를, 숫자가 진행도를 말합니다.
        [Serializable]
        private class MasteryPill
        {
            public GameObject root;
            public Image background;
            public Image icon;
            public TMP_Text label;

            [Tooltip("강조된 알약 아래 깔리는 금색 선. 색만으로는 약해서 선을 더합니다")]
            public GameObject underline;
        }

        // 조건 종류별 아이콘. 종류가 늘면 여기에 한 줄 더합니다.
        [Serializable]
        private class ConditionIcon
        {
            public ESkillUpgradeType type;
            public Sprite sprite;
        }

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
        [Tooltip("조건 알약. 왼쪽부터 채웁니다. 좌우 순서가 종류를 읽는 단서라 흔들리면 안 됩니다")]
        private MasteryPill[] masteryPills;

        [SerializeField]
        private ConditionIcon[] conditionIcons;

        [Header("강화스킬 색")]
        [SerializeField]
        [Tooltip("이 카드가 올리는 조건의 알약 배경")]
        private Color pillOnColor = new Color32(96, 84, 116, 255);

        [SerializeField]
        [Tooltip("올리지 않는 조건의 알약 배경")]
        private Color pillOffColor = new Color32(40, 36, 47, 255);

        [SerializeField]
        [Tooltip("이 카드가 올리는 조건의 글자")]
        private Color textOnColor = new Color32(255, 248, 232, 255);

        [SerializeField]
        [Tooltip("올리지 않는 조건의 글자")]
        private Color textOffColor = new Color32(128, 120, 138, 255);

        [SerializeField]
        [Tooltip("다 채운 조건. 완료는 진행도와 다른 상태라 색을 따로 씁니다")]
        private Color doneColor = new Color32(126, 214, 132, 255);

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
        // 두 조건을 다 보여줍니다. 이 카드가 올리는 쪽만 보여주면 나머지 절반을
        // 모른 채로 "다 채웠다"고 오해하게 됩니다.
        private void ApplyMasteryProgress(LevelUpChoiceItem item)
        {
            var progress = item.MasteryProgress;
            var show = progress != null && progress.Length > 0;

            if (masteryProgressRoot != null)
                masteryProgressRoot.SetActive(show);

            if (!show || masteryPills == null)
                return;

            for (var i = 0; i < masteryPills.Length; i++)
            {
                var pill = masteryPills[i];

                if (pill == null)
                    continue;

                if (i >= progress.Length)
                {
                    if (pill.root != null)
                        pill.root.SetActive(false);

                    continue;
                }

                ApplyPill(pill, progress[i]);
            }
        }

        private void ApplyPill(MasteryPill pill, MasteryProgressItem entry)
        {
            if (pill.root != null)
                pill.root.SetActive(true);

            // 다 채운 조건은 올리는 쪽이든 아니든 완료 색입니다.
            // 완료와 진행 중은 강조보다 먼저 갈라져야 읽힙니다.
            var color = entry.IsMet
                ? doneColor
                : entry.IsAdvancing ? textOnColor : textOffColor;

            if (pill.background != null)
                pill.background.color = entry.IsAdvancing ? pillOnColor : pillOffColor;

            if (pill.underline != null)
                pill.underline.SetActive(entry.IsAdvancing);

            if (pill.label != null)
            {
                pill.label.text = entry.Text;
                pill.label.color = color;
            }

            if (pill.icon == null)
                return;

            var sprite = FindConditionIcon(entry.Type);

            pill.icon.sprite = sprite;
            pill.icon.enabled = sprite != null;

            // 강조된 알약에서는 아이콘을 글자와 같은 밝기로 둡니다. 완료 초록은
            // 숫자에만 남겨야 "무엇이 끝났는가"가 숫자에서 읽힙니다.
            pill.icon.color = entry.IsAdvancing ? textOnColor : color;
        }

        private Sprite FindConditionIcon(ESkillUpgradeType type)
        {
            if (conditionIcons == null)
                return null;

            for (var i = 0; i < conditionIcons.Length; i++)
            {
                if (conditionIcons[i] != null && conditionIcons[i].type == type)
                    return conditionIcons[i].sprite;
            }

            return null;
        }

        public void Hide()
        {
            _onClicked = null;

            gameObject.SetActive(false);
        }
    }
}

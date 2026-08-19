using System;
using Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MainGame.UI
{
    // 카드 정리 화면의 카드 한 장.
    //
    // 아이콘과 카테고리 색으로 카드 성격을 구분하고,
    // 버릴 카드로 고르면 붉게 바뀌며 X 표시가 붙습니다.
    // (색만으로 구분하면 알아보기 어려워 표시를 함께 씁니다.)
    public class CardCleanupItemView : MonoBehaviour
    {
        private const string IconPathFormat = "Icon/{0}";

        [SerializeField]
        private Button selectButton;

        [SerializeField]
        [Tooltip("카드 테두리. 카테고리 색이 들어갑니다")]
        private Image frame;

        [SerializeField]
        [Tooltip("카드 배경")]
        private Image background;

        [SerializeField]
        [Tooltip("아이콘 뒤 원형 판. 카테고리 색이 옅게 들어갑니다")]
        private Image iconBase;

        [SerializeField]
        private Image iconImage;

        [SerializeField]
        private TextMeshProUGUI nameText;

        [SerializeField]
        private TextMeshProUGUI costText;

        [SerializeField]
        [Tooltip("이번에 새로 받은 카드에만 켜집니다")]
        private GameObject newBadge;

        [SerializeField]
        [Tooltip("버릴 카드로 골랐을 때 켜집니다")]
        private GameObject discardMark;

        [Header("Category Colors")]
        [SerializeField]
        private Color attackColor = new Color(0.11f, 0.62f, 0.46f, 1f);

        [SerializeField]
        private Color supportColor = new Color(0.22f, 0.54f, 0.87f, 1f);

        [SerializeField]
        private Color assistColor = new Color(0.93f, 0.62f, 0.15f, 1f);

        [SerializeField]
        [Tooltip("버릴 카드로 고른 색")]
        private Color discardColor = new Color(0.85f, 0.25f, 0.25f, 1f);

        [Header("Background")]
        [SerializeField]
        private Color normalBackColor = new Color(0.16f, 0.16f, 0.18f, 0.9f);

        [SerializeField]
        private Color discardBackColor = new Color(0.32f, 0.12f, 0.12f, 0.95f);

        private Action _onClick;

        private void Awake()
        {
            if (selectButton == null)
                return;

            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => _onClick?.Invoke());
        }

        [SerializeField]
        [Tooltip("마지막으로 눌러 설명을 보고 있는 카드에 켜집니다")]
        private GameObject focusMark;

        public void SetData(SkillCardDataTableRow row, bool isNew, bool isDiscard, bool isFocused, Action onClick)
        {
            _onClick = onClick;

            if (row == null)
                return;

            if (nameText != null)
                nameText.text = row.Name;

            if (costText != null)
                costText.text = row.Cost.ToString();

            if (newBadge != null)
                newBadge.SetActive(isNew);

            if (discardMark != null)
                discardMark.SetActive(isDiscard);

            if (focusMark != null)
                focusMark.SetActive(isFocused);

            // 버릴 카드는 카테고리와 상관없이 붉게 칠합니다.
            // 지금 무엇을 버리는지가 이 화면에서 가장 중요한 정보입니다.
            var color = isDiscard ? discardColor : GetCategoryColor(row.SkillCategoryType);

            if (frame != null)
                frame.color = color;

            if (background != null)
                background.color = isDiscard ? discardBackColor : normalBackColor;

            if (iconBase != null)
                iconBase.color = new Color(color.r, color.g, color.b, isDiscard ? 0.35f : 0.4f);

            SetIcon(row.AssetId);
        }

        private Color GetCategoryColor(ESkillCategoryType type)
        {
            return type switch
            {
                ESkillCategoryType.Attack => attackColor,
                ESkillCategoryType.Assist => assistColor,
                _ => supportColor,
            };
        }

        // 아이콘이 없는 카드는 원형 판만 보여줍니다(정식 아이콘이 채워지면 자동으로 나옵니다).
        private void SetIcon(string assetId)
        {
            if (iconImage == null)
                return;

            if (string.IsNullOrEmpty(assetId))
            {
                iconImage.enabled = false;

                return;
            }

            var sprite = Resources.Load<Sprite>(string.Format(IconPathFormat, assetId));

            iconImage.sprite = sprite;
            iconImage.enabled = sprite != null;
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}

using System;
using Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MainGame.UI
{
    // 카드 정리 화면의 카드 한 장.
    // 이름과 코스트만 보여주고, 버릴 카드로 고르면 표시가 바뀝니다.
    public class CardCleanupItemView : MonoBehaviour
    {
        [SerializeField]
        private Button selectButton;

        [SerializeField]
        private Image background;

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

        [Header("Colors")]
        [SerializeField]
        private Color normalColor = new Color(0.16f, 0.16f, 0.18f, 0.9f);

        [SerializeField]
        [Tooltip("이번에 새로 받은 카드 배경")]
        private Color newColor = new Color(0.11f, 0.45f, 0.38f, 0.9f);

        [SerializeField]
        [Tooltip("버릴 카드로 고른 배경")]
        private Color discardColor = new Color(0.55f, 0.18f, 0.18f, 0.95f);

        private Action _onClick;

        private void Awake()
        {
            if (selectButton == null)
                return;

            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => _onClick?.Invoke());
        }

        public void SetData(SkillCardDataTableRow row, bool isNew, bool isDiscard, Action onClick)
        {
            _onClick = onClick;

            if (row == null)
                return;

            if (nameText != null)
                nameText.text = row.Name;

            if (costText != null)
                costText.text = row.Cost.ToString();

            if (newBadge != null)
                newBadge.SetActive(isNew && !isDiscard);

            if (discardMark != null)
                discardMark.SetActive(isDiscard);

            // 버릴 카드 표시가 새 카드 표시보다 우선입니다.
            // (지금 무엇을 버리는지가 이 화면에서 가장 중요한 정보입니다.)
            if (background != null)
                background.color = isDiscard ? discardColor : isNew ? newColor : normalColor;
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}

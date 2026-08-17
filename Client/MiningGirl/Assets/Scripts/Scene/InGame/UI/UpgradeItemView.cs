using System;
using Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MainGame.UI
{
    // 강화 팝업의 항목 한 줄.
    // '지금 값 → 다음 값'과 레벨, 가격을 한 줄에 담습니다.
    public class UpgradeItemView : MonoBehaviour
    {
        [SerializeField]
        private Button buyButton;
        [SerializeField]
        private TextMeshProUGUI nameText;
        [SerializeField]
        private TextMeshProUGUI detailText;
        [SerializeField]
        private TextMeshProUGUI priceText;
        [SerializeField]
        private Image background;

        [Header("Colors")]
        [SerializeField]
        private Color affordableColor = new Color(0.11f, 0.62f, 0.46f, 1f);
        [SerializeField]
        [Tooltip("골드가 모자라거나 최대 레벨일 때 가격 버튼 색")]
        private Color disabledColor = new Color(0.35f, 0.35f, 0.33f, 1f);

        private Action _onBuy;

        private void Awake()
        {
            if (buyButton == null)
                return;

            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(() => _onBuy?.Invoke());
        }

        // row: 강화 항목 / level: 이번에 사면 도달할 레벨 / gold: 보유 골드
        public void SetData(LevelUpBonusSkillDataTableRow row, int level, int gold, string detail, Action onBuy)
        {
            _onBuy = onBuy;

            if (row == null)
                return;

            if (nameText != null)
                nameText.text = row.Name;

            var isMax = row.MaxLevel >= 0 && level > row.MaxLevel;
            var price = row.GetPrice(level);
            var canBuy = !isMax && gold >= price;

            if (detailText != null)
            {
                // 최대 레벨이면 더 살 수 없다는 것만 알려줍니다.
                detailText.text = isMax
                    ? $"Lv.{row.MaxLevel} / {row.MaxLevel} · 최대"
                    : $"{detail} · Lv.{level} / {(row.MaxLevel < 0 ? "-" : row.MaxLevel.ToString())}";
            }

            if (priceText != null)
                priceText.text = isMax ? "최대" : $"{price} 골드";

            // 항목 전체를 어둡게 덮으면 글자 대비가 무너지므로 가격 버튼 색만 바꿉니다.
            if (background != null)
                background.color = canBuy ? affordableColor : disabledColor;

            if (buyButton != null)
                buyButton.interactable = canBuy;
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}

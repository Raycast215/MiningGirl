using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Legacy.MainGame.UI
{
    // 강화 팝업의 항목 한 줄. 받은 문자열을 그리기만 합니다.
    //
    // 예전에는 이 뷰가 row.GetPrice(level)로 가격을 조회하고
    // canBuy 판정까지 직접 했습니다. 그런데 같은 판정이 UpgradePopup.Buy에도 있어서,
    // 한쪽만 고치면 '버튼은 눌리는데 안 사지는' 상태가 될 수 있었습니다.
    // 판정은 팝업 한 곳에서 하고 여기로는 결과만 넘어옵니다.
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

        // 전부 팝업이 계산해서 넘겨준 값입니다.
        public void SetData(string name, string detail, string price, bool canBuy, Action onBuy)
        {
            _onBuy = onBuy;

            if (nameText != null)
                nameText.text = name;

            if (detailText != null)
                detailText.text = detail;

            if (priceText != null)
                priceText.text = price;

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

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MainGame.UI
{
    // 레벨업 시 보너스를 선택하는 팝업.
    // 지금은 임시 항목 3개만 제공하고, 선택하면 콜백으로 인덱스를 넘긴 뒤 스스로 닫힙니다.
    public class LevelUpBonusSelectPopup : MonoBehaviour
    {
        [Serializable]
        public class BonusSlot
        {
            public Button button;
            public TextMeshProUGUI titleText;
            public TextMeshProUGUI descText;
        }

        [SerializeField]
        private TextMeshProUGUI levelText;
        [SerializeField]
        private BonusSlot[] slots = new BonusSlot[3];

        [SerializeField]
        [Tooltip("임시 보너스 이름 (슬롯 수와 같아야 합니다)")]
        private string[] tempBonusNames = { "채굴 속도 증가", "터치 데미지 증가", "코스트 회복 가속" };
        [SerializeField]
        [Tooltip("임시 보너스 설명")]
        private string[] tempBonusDescs = { "광물을 더 빠르게 캡니다", "터치 공격이 강해집니다", "코스트가 더 빨리 찹니다" };

        private Action<int> _onSelected;

        // 주의: 여기서 SetActive(false)를 호출하면 안 됩니다.
        // 오브젝트가 비활성으로 시작하므로 Awake는 '처음 켜지는 순간'에 실행되는데,
        // 거기서 다시 끄면 Show()로 켜자마자 꺼져버립니다.
        private void Awake()
        {
            BindButtons();
        }

        private void BindButtons()
        {
            if (slots == null)
                return;

            for (var i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null || slot.button == null)
                    continue;

                var index = i;
                slot.button.onClick.RemoveAllListeners();
                slot.button.onClick.AddListener(() => Select(index));
            }
        }

        // 팝업을 띄웁니다. onSelected는 선택된 보너스 인덱스를 받습니다.
        public void Show(int level, Action<int> onSelected)
        {
            _onSelected = onSelected;

            if (levelText != null)
                levelText.text = $"Lv.{level} 보너스 선택";

            ApplyTempBonusTexts();
            BindButtons();

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void ApplyTempBonusTexts()
        {
            if (slots == null)
                return;

            for (var i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null)
                    continue;

                if (slot.titleText != null)
                    slot.titleText.text = tempBonusNames != null && i < tempBonusNames.Length ? tempBonusNames[i] : $"보너스 {i + 1}";

                if (slot.descText != null)
                    slot.descText.text = tempBonusDescs != null && i < tempBonusDescs.Length ? tempBonusDescs[i] : string.Empty;
            }
        }

        private void Select(int index)
        {
            var name = tempBonusNames != null && index < tempBonusNames.Length ? tempBonusNames[index] : $"보너스 {index + 1}";
            Debug.Log($"[LevelUpBonus] 선택됨 — index={index} ({name})");

            var callback = _onSelected;
            _onSelected = null;

            Hide();

            callback?.Invoke(index);
        }
    }
}

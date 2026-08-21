using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scene.InGame.UI
{
    // 스테이지가 끝난 직후 강화 팝업보다 먼저 잠깐 뜨는 결과 창.
    // 이번 판에서 번 골드를 '플레이로 번 것'과 '클리어 보상'으로 나눠 보여주고,
    // 버튼을 누르면 강화로 넘어갑니다.
    //
    // 강화 팝업이 결과까지 겸하면 '무슨 일이 있었는지'와 '무엇을 살지'가 한 화면에 섞입니다.
    // 그래서 결과는 여기서 끊어 보여주고, 강화 팝업은 강화에만 집중하게 했습니다.
    public class StageResultPopup : MonoBehaviour
    {
        [SerializeField]
        private RectTransform panel;

        [SerializeField]
        private TextMeshProUGUI titleText;

        [Header("Rows")]
        [SerializeField]
        [Tooltip("플레이로 번 골드 줄 — 클리어 보상이 없으면 통째로 숨깁니다")]
        private GameObject minedRow;
        [SerializeField]
        private TextMeshProUGUI minedValueText;

        [SerializeField]
        [Tooltip("클리어 보상 줄 — 실패했을 때는 숨깁니다")]
        private GameObject clearBonusRow;
        [SerializeField]
        private TextMeshProUGUI clearBonusValueText;

        [SerializeField]
        private GameObject divider;

        [SerializeField]
        private TextMeshProUGUI totalLabelText;
        [SerializeField]
        private TextMeshProUGUI totalValueText;

        [SerializeField]
        private TextMeshProUGUI ownedGoldText;

        [SerializeField]
        private Button confirmButton;
        [SerializeField]
        private TextMeshProUGUI confirmButtonText;

        [Header("Colors")]
        [SerializeField]
        private Color clearedTitleColor = new Color(0.896f, 0.646f, 0.148f, 1f);
        [SerializeField]
        private Color failedTitleColor = new Color(0.851f, 0.325f, 0.325f, 1f);

        [Header("Animation")]
        [SerializeField]
        private float popDuration = 0.22f;
        [SerializeField]
        private float popFromScale = 0.85f;

        private event Action OnConfirm;

        private void Awake()
        {
            if (confirmButton == null)
                return;

            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(Confirm);
        }

        // minedGold: 채굴·처치로 번 골드, clearBonus: 스테이지 클리어 보상(실패면 0),
        // ownedGold: 지금 보유한 전체 골드
        public void Show(int stage, bool isCleared, int minedGold, int clearBonus, int ownedGold, Action onConfirm)
        {
            OnConfirm = null;
            OnConfirm += onConfirm;

            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            SetInputEnabled(true);

            minedGold = Mathf.Max(0, minedGold);
            clearBonus = Mathf.Max(0, clearBonus);

            if (titleText != null)
            {
                titleText.text = isCleared ? $"스테이지 {stage} 클리어" : $"스테이지 {stage} 실패";
                titleText.color = isCleared ? clearedTitleColor : failedTitleColor;
            }

            // 클리어 보상이 없으면 나눠 보여줄 것이 없습니다.
            // 그때는 합계 줄 하나만 남기고 '획득 골드'로 이름을 바꿔 답니다.
            var hasBreakdown = clearBonus > 0;

            if (minedRow != null)
                minedRow.SetActive(hasBreakdown);

            if (clearBonusRow != null)
                clearBonusRow.SetActive(hasBreakdown);

            if (divider != null)
                divider.SetActive(hasBreakdown);

            if (minedValueText != null)
                minedValueText.text = $"+{minedGold}";

            if (clearBonusValueText != null)
                clearBonusValueText.text = $"+{clearBonus}";

            if (totalLabelText != null)
                totalLabelText.text = hasBreakdown ? "합계" : "획득 골드";

            if (totalValueText != null)
                totalValueText.text = $"+{minedGold + clearBonus}";

            if (ownedGoldText != null)
                ownedGoldText.text = $"보유 골드 {Mathf.Max(0, ownedGold)}";

            if (confirmButtonText != null)
                confirmButtonText.text = "업그레이드";

            PlayPop();
        }

        public void Hide()
        {
            if (panel != null)
                panel.DOKill();

            gameObject.SetActive(false);
        }

        // 팝업이 사라지는 시점은 호출한 쪽이 정합니다.
        // (강화 팝업이 먼저 떠 있어야 그 사이에 인게임 화면이 비치지 않습니다.)
        private void Confirm()
        {
            SetInputEnabled(false);

            OnConfirm?.Invoke();
        }

        private void SetInputEnabled(bool enabled)
        {
            if (confirmButton != null)
                confirmButton.interactable = enabled;
        }

        private void PlayPop()
        {
            if (panel == null || popDuration <= 0f)
                return;

            panel.DOKill();
            panel.localScale = Vector3.one * popFromScale;
            panel.DOScale(1f, popDuration).SetEase(Ease.OutBack);
        }
    }
}

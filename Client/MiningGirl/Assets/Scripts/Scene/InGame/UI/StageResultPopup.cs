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
        [Tooltip("광물을 캐서 번 골드 줄")]
        private GameObject minedRow;
        [SerializeField]
        private TextMeshProUGUI minedValueText;

        [SerializeField]
        [Tooltip("몬스터를 잡아서 번 골드 줄")]
        private GameObject killRow;
        [SerializeField]
        private TextMeshProUGUI killValueText;

        [SerializeField]
        [Tooltip("목표 달성 보상 줄 — 목표를 못 채웠을 때는 숨깁니다")]
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
        [Tooltip("목표를 채웠을 때")]
        private Color clearedTitleColor = new Color(0.896f, 0.646f, 0.148f, 1f);
        [SerializeField]
        [Tooltip("스태미나가 다 되어 끝났을 때. 경고색이 아니라 중립색을 씁니다")]
        private Color failedTitleColor = new Color(0.620f, 0.700f, 0.820f, 1f);

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

        // minedGold: 광물 채굴로 번 골드, killGold: 몬스터 처치로 번 골드,
        // clearBonus: 목표 달성 보상(못 채웠으면 0), ownedGold: 지금 보유한 전체 골드
        public void Show(int stage, bool isCleared, int minedGold, int killGold, int clearBonus, int ownedGold, Action onConfirm)
        {
            OnConfirm = null;
            OnConfirm += onConfirm;

            gameObject.SetActive(true);
            transform.SetAsLastSibling();

            SetInputEnabled(true);

            minedGold = Mathf.Max(0, minedGold);
            killGold = Mathf.Max(0, killGold);
            clearBonus = Mathf.Max(0, clearBonus);

            if (titleText != null)
            {
                // 목표를 채웠든 아니든 '채굴이 끝났다'는 사실만 알려줍니다.
                // 성패는 아래 '목표 달성 보상' 줄이 있고 없고로 드러납니다.
                titleText.text = $"스테이지 {stage} 채굴 종료";
                titleText.color = isCleared ? clearedTitleColor : failedTitleColor;
            }

            // 채굴·처치 줄은 0이어도 그대로 보여줍니다.
            // '이번 판에 무엇으로 벌었나'가 다음 판에 무엇을 늘릴지 정하는 근거라,
            // 0이라는 사실 자체가 정보입니다.
            if (minedRow != null)
                minedRow.SetActive(true);

            if (killRow != null)
                killRow.SetActive(true);

            // 목표를 못 채우면 보상 줄만 사라집니다.
            if (clearBonusRow != null)
                clearBonusRow.SetActive(clearBonus > 0);

            if (divider != null)
                divider.SetActive(true);

            if (minedValueText != null)
                minedValueText.text = $"+{minedGold}";

            if (killValueText != null)
                killValueText.text = $"+{killGold}";

            if (clearBonusValueText != null)
                clearBonusValueText.text = $"+{clearBonus}";

            if (totalLabelText != null)
                totalLabelText.text = "합계";

            if (totalValueText != null)
                totalValueText.text = $"+{minedGold + killGold + clearBonus}";

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

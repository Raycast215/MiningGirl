using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MainGame.UI
{
    // 스테이지 맵의 칸 하나.
    //
    // 색만으로 상태를 구분하면 알아보기 어려워서 모양도 함께 씁니다.
    // 일반 스테이지는 원, 카드 정리 스테이지는 사각형입니다.
    public class StageMapNodeView : MonoBehaviour
    {
        public enum EState
        {
            Cleared, // 지나온 칸
            Current, // 지금 칸
            Locked,  // 아직 안 간 칸
        }

        [SerializeField]
        private Image background;

        [SerializeField]
        private TextMeshProUGUI numberText;

        [SerializeField]
        [Tooltip("카드 정리 스테이지에만 켜지는 표시")]
        private GameObject cardMark;

        [Header("Colors")]
        [SerializeField]
        private Color clearedColor = new Color(0.11f, 0.62f, 0.46f, 1f);

        [SerializeField]
        private Color currentColor = new Color(0.22f, 0.54f, 0.87f, 1f);

        [SerializeField]
        private Color lockedColor = new Color(0.28f, 0.28f, 0.26f, 1f);

        [Header("Size")]
        [SerializeField]
        [Tooltip("현재 칸은 조금 크게 그려 어디 있는지 바로 보이게 합니다")]
        private float currentScale = 1.25f;

        private RectTransform _rect;

        private RectTransform Rect => _rect ??= (RectTransform)transform;

        public void SetData(int stageNumber, EState state, bool isCardStage)
        {
            if (numberText != null)
                numberText.text = stageNumber.ToString();

            if (background != null)
            {
                background.color = state switch
                {
                    EState.Cleared => clearedColor,
                    EState.Current => currentColor,
                    _ => lockedColor,
                };
            }

            if (cardMark != null)
                cardMark.SetActive(isCardStage);

            Rect.localScale = Vector3.one * (state == EState.Current ? currentScale : 1f);
        }
    }
}

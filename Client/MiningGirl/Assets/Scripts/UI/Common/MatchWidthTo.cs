using UnityEngine;
using UnityEngine.UI;

namespace UI.Common
{
    // 다른 RectTransform의 실제 폭을 따라갑니다.
    //
    // 카드 목록처럼 화면 폭에 따라 크기가 변하는 영역 아래에
    // 같은 폭으로 붙어야 하는 요소에 씁니다.
    // 앵커만으로는 부모 전체 폭을 따라가서, 카드가 차지하는 폭보다 넓어집니다.
    [ExecuteAlways]
    public class MatchWidthTo : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("폭을 따라갈 대상")]
        private RectTransform target;

        [SerializeField]
        [Tooltip("그리드가 붙어 있으면 셀과 간격으로 실제 내용 폭을 계산합니다")]
        private GridLayoutGroup targetGrid;

        [SerializeField]
        [Tooltip("대상 폭에서 더하거나 뺄 값")]
        private float offset;

        private RectTransform _rect;

        private RectTransform Rect => _rect ??= (RectTransform)transform;

        private void OnEnable()
        {
            Apply();
        }

        private void Update()
        {
            Apply();
        }

        private void Apply()
        {
            if (target == null)
                return;

            var width = target.rect.width;

            // 그리드는 셀이 다 안 차면 남는 공간이 생기므로 실제 내용 폭을 계산합니다.
            if (targetGrid != null && targetGrid.constraint == GridLayoutGroup.Constraint.FixedColumnCount)
            {
                var columns = Mathf.Max(1, targetGrid.constraintCount);
                var content = targetGrid.cellSize.x * columns + targetGrid.spacing.x * (columns - 1);

                width = Mathf.Min(width, content);
            }

            width = Mathf.Max(0f, width + offset);

            if (Mathf.Approximately(Rect.rect.width, width))
                return;

            // 좌우 앵커가 늘어나는 형태여도 폭을 직접 지정할 수 있도록 가운데로 모읍니다.
            Rect.anchorMin = new Vector2(0.5f, Rect.anchorMin.y);
            Rect.anchorMax = new Vector2(0.5f, Rect.anchorMax.y);
            Rect.sizeDelta = new Vector2(width, Rect.sizeDelta.y);
        }
    }
}

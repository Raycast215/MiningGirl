using UnityEngine;
using UnityEngine.UI;

namespace UI.Common
{
    // 부모 폭에 맞춰 그리드 셀 크기를 매번 다시 계산합니다.
    // 셀 크기를 고정해두면 화면 비율이 달라질 때 항목이 화면 밖으로 나갑니다.
    [RequireComponent(typeof(GridLayoutGroup))]
    public class ResponsiveGridLayout : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("한 줄에 놓을 칸 수")]
        private int columns = 2;

        [SerializeField]
        [Tooltip("칸 사이 간격")]
        private Vector2 spacing = new Vector2(40f, 18f);

        [SerializeField]
        [Tooltip("셀 높이(가로만 화면에 맞춰 늘어납니다). 비율 유지를 켜면 무시됩니다.")]
        private float cellHeight = 150f;

        [SerializeField]
        [Tooltip("켜면 높이를 폭 x 비율로 계산합니다. 카드처럼 모양이 정해진 항목에 씁니다.")]
        private bool keepAspect;

        [SerializeField]
        [Tooltip("높이 / 폭 비율. 카드 400x550이면 1.375")]
        private float aspectRatio = 1.375f;

        [SerializeField]
        [Tooltip("셀 높이의 상한. 0이면 제한 없음. 세로가 좁은 화면에서 넘치는 것을 막습니다.")]
        private float maxCellHeight;

        [SerializeField]
        [Tooltip("한 칸의 최대 폭. 화면이 아주 넓을 때 항목이 지나치게 길어지는 것을 막습니다.")]
        private float maxCellWidth = 1500f;

        private GridLayoutGroup _grid;
        private RectTransform _rect;
        private float _lastWidth = -1f;

        private void Awake()
        {
            _grid = GetComponent<GridLayoutGroup>();
            _rect = (RectTransform)transform;
        }

        private void OnEnable()
        {
            // 켜질 때마다 새로 계산합니다(해상도가 바뀐 뒤 다시 열릴 수 있습니다).
            _lastWidth = -1f;

            Apply();
        }

        private void Update()
        {
            Apply();
        }

        private void Apply()
        {
            if (_grid == null || _rect == null)
                return;

            var width = _rect.rect.width;

            if (width <= 0f || Mathf.Approximately(width, _lastWidth))
                return;

            _lastWidth = width;

            var count = Mathf.Max(1, columns);

            // 칸 사이 간격을 뺀 나머지를 균등 분배합니다.
            var usable = width - spacing.x * (count - 1);
            var cellWidth = Mathf.Min(maxCellWidth, usable / count);

            _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _grid.constraintCount = count;
            _grid.spacing = spacing;
            var height = cellHeight;

            if (keepAspect)
            {
                height = cellWidth * Mathf.Max(0.1f, aspectRatio);

                // 세로가 좁은 화면에서는 높이를 먼저 맞추고 폭을 거기에 맞춥니다.
                if (maxCellHeight > 0f && height > maxCellHeight)
                {
                    height = maxCellHeight;
                    cellWidth = height / Mathf.Max(0.1f, aspectRatio);
                }
            }

            _grid.cellSize = new Vector2(Mathf.Max(1f, cellWidth), Mathf.Max(1f, height));
        }
    }
}

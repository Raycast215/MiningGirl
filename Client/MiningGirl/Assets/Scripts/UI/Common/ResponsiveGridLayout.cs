using UnityEngine;
using UnityEngine.UI;

namespace UI.Common
{
    // 부모 크기에 맞춰 그리드 셀 크기(와 필요하면 열 수)를 매번 다시 계산합니다.
    // 셀 크기를 고정해두면 화면 비율이 달라질 때 항목이 화면 밖으로 나갑니다.
    [RequireComponent(typeof(GridLayoutGroup))]
    public class ResponsiveGridLayout : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("한 줄에 놓을 칸 수. 자동 열 수를 켜면 무시됩니다.")]
        private int columns = 2;

        [Header("Auto Columns")]
        [SerializeField]
        [Tooltip("켜면 세로 공간까지 보고 칸이 가장 커지는 열 수를 고릅니다.")]
        private bool autoColumns;

        [SerializeField]
        [Tooltip("자동일 때 허용할 최소 열 수")]
        private int minColumns = 4;

        [SerializeField]
        [Tooltip("자동일 때 허용할 최대 열 수")]
        private int maxColumns = 7;

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
        [Tooltip("셀 높이의 상한. 0이면 제한 없음.")]
        private float maxCellHeight;

        [SerializeField]
        [Tooltip("한 칸의 최대 폭. 화면이 아주 넓을 때 항목이 지나치게 커지는 것을 막습니다.")]
        private float maxCellWidth = 1500f;

        private GridLayoutGroup _grid;
        private RectTransform _rect;
        private float _lastWidth = -1f;
        private float _lastHeight = -1f;
        private int _lastCount = -1;

        private void Awake()
        {
            _grid = GetComponent<GridLayoutGroup>();
            _rect = (RectTransform)transform;
        }

        private void OnEnable()
        {
            // 켜질 때마다 새로 계산합니다(해상도가 바뀐 뒤 다시 열릴 수 있습니다).
            _lastWidth = -1f;
            _lastHeight = -1f;
            _lastCount = -1;

            Apply();
        }

        private void Update()
        {
            Apply();
        }

        private int CountActiveChildren()
        {
            var count = 0;

            for (var i = 0; i < transform.childCount; i++)
            {
                if (transform.GetChild(i).gameObject.activeSelf)
                    count++;
            }

            return count;
        }

        private void Apply()
        {
            if (_grid == null || _rect == null)
                return;

            var width = _rect.rect.width;
            var height = _rect.rect.height;

            if (width <= 0f || height <= 0f)
                return;

            var itemCount = CountActiveChildren();

            // 크기나 개수가 바뀌었을 때만 다시 계산합니다.
            if (Mathf.Approximately(width, _lastWidth)
                && Mathf.Approximately(height, _lastHeight)
                && itemCount == _lastCount)
                return;

            _lastWidth = width;
            _lastHeight = height;
            _lastCount = itemCount;

            var column = autoColumns && itemCount > 0
                ? PickBestColumns(width, height, itemCount)
                : Mathf.Max(1, columns);

            var size = Measure(width, column);

            _grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            _grid.constraintCount = column;
            _grid.spacing = spacing;
            _grid.cellSize = size;
        }

        // 열 수가 적을수록 칸이 커집니다.
        // 세로에 다 들어가는 것 중 가장 적은 열 수를 고릅니다.
        private int PickBestColumns(float width, float height, int itemCount)
        {
            var min = Mathf.Max(1, Mathf.Min(minColumns, maxColumns));
            var max = Mathf.Max(min, maxColumns);
            var best = max;

            for (var c = min; c <= max; c++)
            {
                var size = Measure(width, c);
                var rows = Mathf.CeilToInt(itemCount / (float)c);
                var totalHeight = size.y * rows + spacing.y * (rows - 1);

                if (totalHeight > height)
                    continue;

                best = c;

                break;
            }

            return best;
        }

        private Vector2 Measure(float width, int column)
        {
            var usable = width - spacing.x * (column - 1);
            var cellWidth = Mathf.Min(maxCellWidth, usable / column);
            var cellSize = cellHeight;

            if (keepAspect)
            {
                cellSize = cellWidth * Mathf.Max(0.1f, aspectRatio);

                if (maxCellHeight > 0f && cellSize > maxCellHeight)
                {
                    cellSize = maxCellHeight;
                    cellWidth = cellSize / Mathf.Max(0.1f, aspectRatio);
                }
            }

            return new Vector2(Mathf.Max(1f, cellWidth), Mathf.Max(1f, cellSize));
        }
    }
}

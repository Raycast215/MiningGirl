using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Common
{
    // 카드를 조준하는 동안 '어디로 던지는지'와 '어디까지 닿는지'를 그립니다.
    //
    //  - 손패 자리에서 카드 중앙으로 이어지는 포물선
    //  - 카드 중앙을 중심으로 한 사거리 원(점선 테두리 + 옅은 채움)
    //
    // 선도 원도 작은 사각형 이미지를 늘어놓아 그립니다.
    // UI에 진짜 선을 그리려면 메시를 직접 만들어야 하는데, 조준 표시 하나 때문에
    // 그 비용을 치를 이유가 없고 점선 모양과도 오히려 잘 맞습니다.
    //
    // 길이 단위는 전부 캔버스 로컬 단위(레퍼런스 해상도 1080 기준)입니다.
    // 화면 픽셀이 아니라서 기기 해상도가 바뀌어도 같은 비율로 보입니다.
    public class AimIndicator : MonoBehaviour
    {
        [Header("Arc")]
        [SerializeField]
        [Tooltip("포물선이 솟아오르는 높이. 0이면 직선에 가깝습니다.")]
        private float arcHeight = 330f;

        [SerializeField]
        [Tooltip("포물선 굵기")]
        private float arcThickness = 9f;

        [SerializeField]
        [Tooltip("점선 한 칸 길이")]
        private float arcDashLength = 27f;

        [SerializeField]
        [Tooltip("점선 사이 간격")]
        private float arcDashGap = 24f;

        [Header("Range")]
        [SerializeField]
        [Tooltip("사거리 원 테두리 굵기")]
        private float ringThickness = 6f;

        [SerializeField]
        [Tooltip("사거리 원 점선 한 칸 길이")]
        private float ringDashLength = 18f;

        [SerializeField]
        [Tooltip("사거리 원 점선 간격")]
        private float ringDashGap = 18f;

        [SerializeField]
        [Tooltip("사거리 원 안쪽 채움 진하기. 0이면 테두리만 그립니다.")]
        [Range(0f, 0.3f)]
        private float fillAlpha = 0.07f;

        [Header("Colors")]
        [SerializeField]
                [Tooltip("지금 놓으면 쓰이는 상황일 때")]
        private Color okColor = new Color(0.949f, 0.643f, 0.149f, 1f);

        [SerializeField]
                [Tooltip("대상이 없거나 코스트가 모자랄 때 — 놓아도 헛방이라고 색으로 알립니다.")]
        private Color noColor = new Color(1f, 0.353f, 0.302f, 1f);

        [Header("Limit")]
        [SerializeField]
        [Tooltip("한 번에 그릴 조각 수 상한. 넘으면 거기서 그만 그립니다(무한 루프 방지).")]
        private int maxSegments = 200;

        private RectTransform _rect;
        private readonly List<Image> _pool = new List<Image>();
        private Image _fill;
        private int _used;

        // 조각 하나에 쓰는 흰색 스프라이트. 색은 Image.color로 입히므로 하나면 충분합니다.
        private static Sprite _dashSprite;
        private static Sprite _discSprite;

        private RectTransform Rect => _rect ??= (RectTransform)transform;

        private void Awake()
        {
            Hide();
        }

        // fromScreen : 손패에서 이 카드의 자리 (포물선 시작점)
        // toScreen   : 지금 카드 중앙 (포물선 끝점 겸 사거리 원 중심)
                // edgeScreen : 사거리 끝 지점을 화면으로 옮긴 것. 여기까지의 거리를 반지름으로 씁니다.
        //              toScreen과 같은 점을 넣으면 반지름이 0이 돼 사거리 원을 건너뜁니다(버프·지원 카드).
        //              (월드 사거리를 픽셀로 직접 환산하면 캔버스 스케일을 또 나눠야 해서,
        //               두 점을 같은 방식으로 변환한 뒤 거리를 재는 편이 정확합니다.)
                public void Show(Vector2 fromScreen, Vector2 toScreen, Vector2 edgeScreen, bool isUsable, Camera uiCamera)
        {
            if (!gameObject.activeSelf)
                gameObject.SetActive(true);

            if (!TryToLocal(fromScreen, uiCamera, out var from)
                || !TryToLocal(toScreen, uiCamera, out var to)
                || !TryToLocal(edgeScreen, uiCamera, out var edge))
            {
                Hide();

                return;
            }

            _used = 0;

                        var color = isUsable ? okColor : noColor;
            var radius = Vector2.Distance(to, edge);

            DrawFill(to, radius, color);
            DrawRing(to, radius, color);
            DrawArc(from, to, color);

            // 이번에 안 쓴 조각은 꺼둡니다(파괴하지 않고 다음 프레임에 재사용).
            for (var i = _used; i < _pool.Count; i++)
            {
                if (_pool[i].gameObject.activeSelf)
                    _pool[i].gameObject.SetActive(false);
            }
        }

        public void Hide()
        {
            if (_fill != null && _fill.gameObject.activeSelf)
                _fill.gameObject.SetActive(false);

            for (var i = 0; i < _pool.Count; i++)
            {
                if (_pool[i].gameObject.activeSelf)
                    _pool[i].gameObject.SetActive(false);
            }

            _used = 0;
        }

#region Draw

        private void DrawArc(Vector2 from, Vector2 to, Color color)
        {
            var control = (from + to) * 0.5f + Vector2.up * arcHeight;
            var step = Mathf.Max(1f, arcDashLength + arcDashGap);

            // 베지어는 t를 일정하게 늘려도 실제 이동 거리가 들쭉날쭉합니다.
            // 촘촘히 샘플링해 '지나온 길이'를 재고, 그 길이를 기준으로 놓아야 간격이 고릅니다.
            const int Samples = 48;

            var travelled = 0f;
            var next = 0f;
            var prev = Bezier(from, control, to, 0f);

            for (var i = 1; i <= Samples; i++)
            {
                var point = Bezier(from, control, to, i / (float)Samples);
                var length = Vector2.Distance(prev, point);

                while (travelled + length >= next)
                {
                    var t = length <= 0f ? 0f : (next - travelled) / length;

                    if (!Place(Vector2.Lerp(prev, point, t), point - prev, arcDashLength, arcThickness, color))
                        return;

                    next += step;
                }

                travelled += length;
                prev = point;
            }
        }

        private void DrawRing(Vector2 center, float radius, Color color)
        {
            if (radius <= 1f)
                return;

            var step = Mathf.Max(1f, ringDashLength + ringDashGap);
            var count = Mathf.Max(8, Mathf.RoundToInt(2f * Mathf.PI * radius / step));

            for (var i = 0; i < count; i++)
            {
                var angle = i / (float)count * Mathf.PI * 2f;
                var offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

                // 조각을 원의 접선 방향으로 눕혀야 테두리처럼 보입니다.
                var tangent = new Vector2(-offset.y, offset.x);

                if (!Place(center + offset * radius, tangent, ringDashLength, ringThickness, color))
                    return;
            }
        }

        private void DrawFill(Vector2 center, float radius, Color color)
        {
            if (fillAlpha <= 0f || radius <= 1f)
            {
                if (_fill != null && _fill.gameObject.activeSelf)
                    _fill.gameObject.SetActive(false);

                return;
            }

            if (_fill == null)
            {
                _fill = CreatePiece("AimRangeFill", GetDiscSprite());

                // 채움은 항상 맨 아래에 깔립니다. 만들 때 한 번만 정해두면 됩니다.
                _fill.rectTransform.SetAsFirstSibling();
            }

            var rect = _fill.rectTransform;

            rect.anchoredPosition = center;
            rect.localRotation = Quaternion.identity;
            rect.sizeDelta = new Vector2(radius * 2f, radius * 2f);

            _fill.color = new Color(color.r, color.g, color.b, fillAlpha);

            if (!_fill.gameObject.activeSelf)
                _fill.gameObject.SetActive(true);
        }

        // 조각 하나를 놓습니다. 상한에 걸리면 false를 돌려 호출부가 멈추게 합니다.
        private bool Place(Vector2 position, Vector2 direction, float length, float thickness, Color color)
        {
            if (_used >= maxSegments)
                return false;

            var image = GetPiece(_used++);
            var rect = image.rectTransform;

            rect.anchoredPosition = position;
            rect.sizeDelta = new Vector2(length, thickness);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);

            image.color = color;

            if (!image.gameObject.activeSelf)
                image.gameObject.SetActive(true);

            return true;
        }

#endregion

#region Pool

        private Image GetPiece(int index)
        {
            while (_pool.Count <= index)
                _pool.Add(CreatePiece("AimDash", GetDashSprite()));

            return _pool[index];
        }

        private Image CreatePiece(string pieceName, Sprite sprite)
        {
            var go = new GameObject(pieceName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = (RectTransform)go.transform;

            rect.SetParent(Rect, false);

            // 부모 중앙 기준으로 놓습니다. ScreenPointToLocalPointInRectangle이
            // 피벗(=중앙) 기준 좌표를 주기 때문에 그대로 anchoredPosition에 넣을 수 있습니다.
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);

            var image = go.GetComponent<Image>();

            image.sprite = sprite;
            image.raycastTarget = false;

            go.SetActive(false);

            return image;
        }

#endregion

#region Utility

        private bool TryToLocal(Vector2 screenPoint, Camera uiCamera, out Vector2 local)
        {
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(Rect, screenPoint, uiCamera, out local);
        }

        private static Vector2 Bezier(Vector2 p0, Vector2 p1, Vector2 p2, float t)
        {
            var u = 1f - t;

            return u * u * p0 + 2f * u * t * p1 + t * t * p2;
        }

        private static Sprite GetDashSprite()
        {
            if (_dashSprite != null)
                return _dashSprite;

            var texture = new Texture2D(4, 4, TextureFormat.ARGB32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
            };

            var pixels = new Color32[16];

            for (var i = 0; i < pixels.Length; i++)
                pixels[i] = new Color32(255, 255, 255, 255);

            texture.SetPixels32(pixels);
            texture.Apply();

            _dashSprite = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));

            return _dashSprite;
        }

        // 사거리 채움용 원. 가장자리 몇 픽셀만 부드럽게 해서 계단이 보이지 않게 합니다.
        private static Sprite GetDiscSprite()
        {
            if (_discSprite != null)
                return _discSprite;

            const int Size = 128;

            var texture = new Texture2D(Size, Size, TextureFormat.ARGB32, false)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave,
            };

            var center = (Size - 1) * 0.5f;
            var pixels = new Color32[Size * Size];

            for (var y = 0; y < Size; y++)
            {
                for (var x = 0; x < Size; x++)
                {
                    var dx = x - center;
                    var dy = y - center;
                    var distance = Mathf.Sqrt(dx * dx + dy * dy) / center;
                    var alpha = Mathf.Clamp01((1f - distance) * center * 0.5f);

                    pixels[y * Size + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            _discSprite = Sprite.Create(texture, new Rect(0f, 0f, Size, Size), new Vector2(0.5f, 0.5f));

            return _discSprite;
        }

#endregion
    }
}

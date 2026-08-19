using System.Collections.Generic;
using UnityEngine;

namespace UI.Common
{
    // 자식들을 부모 폭에 맞춰 가로로 나눠 놓습니다.
    //
    // 카드처럼 크기가 정해진 항목을 손패에 늘어놓을 때 씁니다.
    // GridLayoutGroup은 자식의 RectTransform 크기를 바꿔버려서
    // 안쪽 요소가 원본 비율대로 짜인 카드에는 맞지 않습니다.
    // 여기서는 크기를 건드리지 않고 위치와 배율만 정합니다.
    [ExecuteAlways]
    public class HandCardLayout : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("카드 한 장의 원본 크기")]
        private Vector2 cardSize = new Vector2(400f, 550f);

        [SerializeField]
        [Tooltip("카드 사이 최소 간격(배율 적용 전 기준)")]
        private float spacing = 40f;

        [SerializeField]
        [Tooltip("좌우 여백")]
        private float sidePadding = 40f;

        [SerializeField]
        [Tooltip("이 배율보다 크게는 키우지 않습니다")]
        private float maxScale = 0.75f;

        [SerializeField]
        [Tooltip("세로 위치. 0이면 가운데")]
        private float verticalOffset;

        [Header("Fan")]
        [SerializeField]
        [Tooltip("양 끝 카드가 기울어지는 각도(도). 0이면 부채꼴 없이 나란히 놓입니다.")]
        private float fanAngle = 8f;

        [SerializeField]
        [Tooltip("양 끝 카드가 아래로 내려가는 정도. 가운데가 가장 높습니다.")]
        private float fanArc = 40f;

        [SerializeField]
        [Tooltip("카드가 겹치는 정도. 1이면 간격 그대로, 작을수록 더 겹칩니다.")]
        [Range(0.3f, 1f)]
        private float overlap = 1f;

        private RectTransform _rect;

        // 마지막으로 계산했을 때의 폭과 카드 수.
        private float _lastWidth = -1f;
        private int _lastCount = -1;

        private RectTransform Rect => _rect ??= (RectTransform)transform;

        private void OnEnable()
        {
            _lastWidth = -1f;
            _lastCount = -1;

            Apply();
        }

        private void Update()
        {
            // 매 프레임 위치를 덮어쓰면 드래그 중인 카드가 제자리로 끌려갑니다.
            // 부모 크기나 카드 수가 바뀌었을 때만 다시 계산합니다.
            var width = Rect != null ? Rect.rect.width : 0f;
            var count = CountActiveChildren();

            if (Mathf.Approximately(width, _lastWidth) && count == _lastCount)
                return;

            Apply();
        }

        private int CountActiveChildren()
        {
            if (Rect == null)
                return 0;

            var count = 0;

            for (var i = 0; i < Rect.childCount; i++)
            {
                var child = Rect.GetChild(i);

                if (child != null && child.gameObject.activeSelf)
                    count++;
            }

            return count;
        }

        // 슬롯 하나의 위치·각도·배율. 손패 컨트롤러가 카드를 옮길 때 씁니다.
        public struct Slot
        {
            public Vector2 Position;
            public float Angle;
            public float Scale;
        }

        // index번째 슬롯 정보를 돌려줍니다. count는 지금 손패에 있는 카드 수입니다.
        public Slot GetSlot(int index, int count)
        {
            var slot = new Slot { Position = Vector2.zero, Angle = 0f, Scale = 1f };

            if (Rect == null || count <= 0)
                return slot;

            var width = Rect.rect.width;
            var height = Rect.rect.height;

            if (width <= 0f || height <= 0f)
                return slot;

            var needWidth = cardSize.x + (cardSize.x * overlap + spacing) * (count - 1);
            var usableWidth = Mathf.Max(1f, width - sidePadding * 2f);
            var tiltHeight = cardSize.y + Mathf.Abs(fanArc)
                + cardSize.x * Mathf.Sin(Mathf.Abs(fanAngle) * Mathf.Deg2Rad);

            var scale = Mathf.Min(Mathf.Min(usableWidth / needWidth, height / tiltHeight), maxScale);
            var step = (cardSize.x * overlap + spacing) * scale;
            var totalWidth = cardSize.x * scale + step * (count - 1);

            var startX = (width - totalWidth) * 0.5f + cardSize.x * scale * 0.5f;
            var centerY = -height * 0.5f + verticalOffset;

            // 가운데를 0으로 두고 -1 ~ 1 범위로 위치를 나타냅니다.
            var t01 = count <= 1 ? 0f : index / (float)(count - 1) * 2f - 1f;

            slot.Angle = -fanAngle * t01;
            slot.Scale = scale;
            slot.Position = new Vector2(startX + step * index, centerY - fanArc * t01 * t01 * scale);

            return slot;
        }

        // 카드가 늘거나 줄면 호출합니다.
        public void Refresh()
        {
            _lastWidth = -1f;
            _lastCount = -1;

            Apply();
        }

        private void Apply()
        {
            if (Rect == null)
                return;

            var children = new List<RectTransform>();

            for (var i = 0; i < Rect.childCount; i++)
            {
                var child = Rect.GetChild(i) as RectTransform;

                if (child == null || !child.gameObject.activeSelf)
                    continue;

                children.Add(child);
            }

            if (children.Count == 0)
                return;

            var width = Rect.rect.width;
            var height = Rect.rect.height;

            if (width <= 0f || height <= 0f)
                return;

            var count = children.Count;

            // 카드 전부 + 간격이 들어갈 배율을 구합니다.
            // 겹치는 만큼 실제로 필요한 폭이 줄어듭니다.
            var needWidth = cardSize.x + (cardSize.x * overlap + spacing) * (count - 1);
            var usableWidth = Mathf.Max(1f, width - sidePadding * 2f);

            // 기울어지면 세로로 조금 더 차지합니다.
            var tiltHeight = cardSize.y + Mathf.Abs(fanArc) + cardSize.x * Mathf.Sin(Mathf.Abs(fanAngle) * Mathf.Deg2Rad);

            var scale = Mathf.Min(usableWidth / needWidth, height / tiltHeight);

            scale = Mathf.Min(scale, maxScale);

            // 겹치는 만큼 간격을 줄입니다.
            var step = (cardSize.x * overlap + spacing) * scale;
            var totalWidth = cardSize.x * scale + step * (count - 1);

            // 가운데 정렬을 위해 왼쪽 시작점을 구합니다.
            var startX = (width - totalWidth) * 0.5f + cardSize.x * scale * 0.5f;
            var centerY = -height * 0.5f + verticalOffset;

            _lastWidth = width;
            _lastCount = count;

            for (var i = 0; i < count; i++)
            {
                var child = children[i];

                // 앵커를 좌상단으로 맞춰야 좌표 계산이 단순해집니다.
                child.anchorMin = new Vector2(0f, 1f);
                child.anchorMax = new Vector2(0f, 1f);
                child.pivot = new Vector2(0.5f, 0.5f);
                child.sizeDelta = cardSize;
                child.localScale = Vector3.one * scale;
                // 가운데를 0으로 두고 -1 ~ 1 범위로 위치를 나타냅니다.
                var t01 = count <= 1 ? 0f : i / (float)(count - 1) * 2f - 1f;

                // 양 끝일수록 바깥으로 기울고, 아래로 내려갑니다.
                var angle = -fanAngle * t01;
                var arc = -fanArc * t01 * t01 * scale;

                child.localRotation = Quaternion.Euler(0f, 0f, angle);
                child.anchoredPosition = new Vector2(startX + step * i, centerY + arc);
            }
        }
    }
}

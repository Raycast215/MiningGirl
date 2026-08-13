using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MainGame.Card
{
    // 손패 카드 한 장. 드래그 입력만 담당하고, 판정(사용/버리기)은 CardHandController가 합니다.
    //
    // 실제로 움직이는 것은 자기 자신이 아니라 자식 Contents입니다.
    // (슬롯 위치는 그대로 두고 내용물만 끌어야 손패 정렬이 흐트러지지 않습니다.)
    public class CardView : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField]
        [Tooltip("실제로 끌려다니는 내용물")]
        private RectTransform contents;
        [SerializeField]
        private CanvasGroup canvasGroup;

        [Header("Texts (없어도 동작)")]
        [SerializeField]
        private TMP_Text nameText;
        [SerializeField]
        private TMP_Text costText;

        [Header("Motion")]
        [SerializeField]
        [Tooltip("드래그 중 확대 배율")]
        private float dragScale = 1.1f;
        [SerializeField]
        private float returnDuration = 0.2f;
        [SerializeField]
        [Tooltip("드로우될 때 아래에서 올라오는 거리")]
        private float drawFromOffsetY = -400f;
        [SerializeField]
        private float drawDuration = 0.25f;

        public int SlotIndex { get; private set; }
        public bool IsDragging { get; private set; }

        // 드래그 중 내용물의 화면상 위치
        public RectTransform Contents => contents;

        private Action<CardView> _onDragBegin;
        private Action<CardView> _onDrag;
        private Action<CardView> _onDragEnd;

        private Vector2 _homePosition;
        private Vector2 _pointerOffset;
        private Tween _moveTween;
        private Tween _slotTween;
        private RectTransform _rect;
        private Canvas _canvas;
        private bool _isInteractable = true;

        public void Init(int slotIndex, Canvas canvas, Action<CardView> onDragBegin, Action<CardView> onDrag, Action<CardView> onDragEnd)
        {
            SlotIndex = slotIndex;
            _canvas = canvas;
            _onDragBegin = onDragBegin;
            _onDrag = onDrag;
            _onDragEnd = onDragEnd;

            _rect = transform as RectTransform;

            if (contents == null)
                contents = transform.GetChild(0) as RectTransform;

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            // 슬롯 기준 원위치를 기억합니다.
            _homePosition = contents != null ? contents.anchoredPosition : Vector2.zero;
        }

        public void SetInteractable(bool value)
        {
            _isInteractable = value;
        }

        // 카드 내용 표시. 데이터가 붙기 전까지는 임시 문자열을 받습니다.
        public void SetContent(string cardName, string cost)
        {
            if (nameText != null)
                nameText.text = cardName;

            if (costText != null)
                costText.text = cost;
        }

        // 아직 드로우되지 않은 상태로 감춥니다.
        public void SetHidden()
        {
            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.alpha = 0f;
            }
        }

        // 손패에서의 자리(슬롯)를 옮깁니다. duration이 0이면 즉시 이동합니다.
        public void SetSlotPosition(Vector2 slotPosition, float duration)
        {
            if (_rect == null)
                _rect = transform as RectTransform;

            _slotTween?.Kill();

            if (duration <= 0f)
            {
                _rect.anchoredPosition = slotPosition;
                return;
            }

            _slotTween = _rect.DOAnchorPos(slotPosition, duration).SetEase(Ease.OutCubic);
        }

        // 새 카드가 손패로 들어오는 연출
        public void PlayDraw(float delay = 0f)
        {
            if (contents == null)
                return;

            _moveTween?.Kill();

            contents.anchoredPosition = _homePosition + new Vector2(0f, drawFromOffsetY);
            contents.localScale = Vector3.one;

            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.alpha = 0f;
                canvasGroup.DOFade(1f, drawDuration).SetDelay(delay);
            }

            _moveTween = contents.DOAnchorPos(_homePosition, drawDuration).SetEase(Ease.OutCubic).SetDelay(delay);
        }

        // 판정에 걸리지 않았을 때 원래 자리로 되돌립니다.
        public void ReturnHome()
        {
            if (contents == null)
                return;

            _moveTween?.Kill();
            _moveTween = contents.DOAnchorPos(_homePosition, returnDuration).SetEase(Ease.OutCubic);
            contents.DOScale(Vector3.one, returnDuration);
        }

        // 사용/버리기로 손패에서 빠질 때의 연출. 끝나면 onComplete 호출.
        public void PlayConsume(bool toBottom, Action onComplete)
        {
            if (contents == null)
            {
                onComplete?.Invoke();
                return;
            }

            _moveTween?.Kill();

            var target = contents.anchoredPosition + new Vector2(0f, toBottom ? -300f : 300f);

            _moveTween = contents.DOAnchorPos(target, 0.18f).SetEase(Ease.InCubic);
            contents.DOScale(Vector3.one * 0.8f, 0.18f);

            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.DOFade(0f, 0.18f).OnComplete(() => onComplete?.Invoke());
            }
            else
            {
                _moveTween.OnComplete(() => onComplete?.Invoke());
            }
        }

#region Drag

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!_isInteractable || contents == null)
                return;

            IsDragging = true;

            _moveTween?.Kill();
            contents.DOScale(Vector3.one * dragScale, 0.1f);

            // 손가락과 카드의 간격을 유지하기 위한 오프셋
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                contents.parent as RectTransform, eventData.position, GetEventCamera(), out var localPoint);

            _pointerOffset = contents.anchoredPosition - localPoint;

            // 드래그 중인 카드가 다른 카드 위에 그려지도록
            transform.SetAsLastSibling();

            _onDragBegin?.Invoke(this);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!IsDragging || contents == null)
                return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    contents.parent as RectTransform, eventData.position, GetEventCamera(), out var localPoint))
            {
                contents.anchoredPosition = localPoint + _pointerOffset;
            }

            _onDrag?.Invoke(this);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!IsDragging)
                return;

            IsDragging = false;

            _onDragEnd?.Invoke(this);
        }

        private Camera GetEventCamera()
        {
            if (_canvas == null)
                return null;

            return _canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _canvas.worldCamera;
        }

#endregion
    }
}

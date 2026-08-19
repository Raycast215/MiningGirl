using System;
using Data;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
        [SerializeField]
        private TMP_Text descText;

        [Header("Availability")]
        [SerializeField]
        [Tooltip("코스트 배지 (부족할 때 붉게)")]
        private Image costBadgeImage;
        [SerializeField]
        [Tooltip("사용 실패 사유를 잠깐 띄우는 텍스트")]
        private TMP_Text failText;

        [SerializeField]
        private Color costNormalColor = Color.white;
        [SerializeField]
        private Color costLackColor = new Color(1f, 0.35f, 0.35f);
        [SerializeField]
        [Tooltip("드래그 중 사용 가능할 때 프레임 색")]
        private Color dragOkColor = Color.white;
        [SerializeField]
        [Tooltip("드래그 중 사용 불가일 때 프레임 색")]
        private Color dragBlockColor = new Color(1f, 0.45f, 0.45f);

        [Header("Category")]
        [SerializeField]
        [Tooltip("카테고리 이름 (공격 / 보조 / 서포트)")]
        private TMP_Text typeText;
        [SerializeField]
        [Tooltip("카테고리별 아이콘 — 하나만 켜집니다")]
        private GameObject attackSymbol;
        [SerializeField]
        private GameObject assistSymbol;
        [SerializeField]
        private GameObject supportSymbol;

        [SerializeField]
        [Tooltip("코스트를 낼 수 있을 때 켜지는 이펙트")]
        private GameObject costEffect;

        [SerializeField]
        [Tooltip("드래그하는 동안 켜지는 선택 이펙트")]
        private GameObject selectEffect;

        [Header("Unavailable Shake")]
        [SerializeField]
        [Tooltip("쓸 수 없는 카드를 잡았을 때 흔들리는 세기(px)")]
        private float unavailableShakeStrength = 10f;
        [SerializeField]
        [Tooltip("흔들리는 시간(초)")]
        private float unavailableShakeDuration = 0.22f;

        [Header("Frame")]
        [SerializeField]
        [Tooltip("카드 타입별로 색이 바뀌는 테두리")]
        private Image frameImage;
        [SerializeField]
        private Sprite attackFrame;
        [SerializeField]
        private Sprite assistFrame;
        [SerializeField]
        private Sprite supportFrame;

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
        private bool _isAvailable = true;

        // 아직 드로우되지 않아 감춰둔 상태.
        // 이 동안에는 사용 가능 판정이 알파를 되살리면 안 됩니다.
        private bool _isHidden;

        // 드로우 연출이 도는 동안은 조작을 막습니다.
        // (연출 시작과 동시에 잡히면 카드가 날아오는 중에 드래그가 걸립니다.)
        private bool _isDrawing;
        private Tween _failTween;

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

        // 카드 내용 표시. 데이터가 없을 때(테스트)는 문자열만 넣습니다.
        public void SetContent(string cardName, string cost)
        {
            if (nameText != null)
                nameText.text = cardName;

            if (costText != null)
                costText.text = cost;
        }

        // 평소 표시 — 코스트가 되는지만 봅니다.
        // (대상 유무는 몬스터가 화면을 들락날락해서 깜빡이므로 여기서 판정하지 않습니다.)
        // 드로우 페이드인이 끝나야 할 밝기.
        //
        // 사용 불가를 카드 투명도로 표현하면 어두운 카드 배경이 비쳐서 오히려
        // 하얗게 뜨고 글자가 안 읽힙니다. 그래서 밝기는 항상 1로 두고,
        // 쓸 수 없다는 건 코스트 배지 색·코스트 이펙트·진동으로만 알립니다.
        private float TargetAlpha => 1f;

        public void SetAvailable(bool value)
        {
            _isAvailable = value;

            if (costBadgeImage != null)
                costBadgeImage.color = value ? costNormalColor : costLackColor;

            // 코스트를 낼 수 있을 때만 배지 뒤 이펙트를 켭니다.
            if (costEffect != null && costEffect.activeSelf != value)
                costEffect.SetActive(value);

            // 아직 드로우 전이면 감춰둔 상태를 유지합니다.
            if (_isHidden || canvasGroup == null || IsDragging)
                return;

            // 드로우 페이드가 도는 중에는 건드리지 않습니다(연출이 끊깁니다).
            if (DOTween.IsTweening(canvasGroup))
                return;

            canvasGroup.alpha = TargetAlpha;
        }

        // 쓸 수 없는 카드를 잡았을 때의 짧은 진동.
        //
        // 내용물(contents)은 드래그로 매 프레임 위치가 갱신되므로 건드리면 충돌합니다.
        // 그래서 카드 본체(슬롯 위치)를 흔들고, 끝나면 제자리로 돌아오게 합니다.
        public void PlayUnavailableShake()
        {
            if (_rect == null)
                _rect = transform as RectTransform;

            if (_rect == null)
                return;

            var home = _rect.anchoredPosition;

            _slotTween?.Kill();

            _slotTween = _rect
                .DOShakeAnchorPos(unavailableShakeDuration, new Vector2(unavailableShakeStrength, 0f), 14, 0f)
                .OnComplete(() => _rect.anchoredPosition = home);
        }

        // 드래그하는 동안 카드를 강조하는 이펙트
        private void SetSelectEffect(bool value)
        {
            if (selectEffect != null && selectEffect.activeSelf != value)
                selectEffect.SetActive(value);
        }

        // 드래그 중에만 '지금 놓으면 되는지'를 프레임 색으로 알려줍니다.
        public void SetDragFeedback(bool canUse)
        {
            if (frameImage != null)
                frameImage.color = canUse ? dragOkColor : dragBlockColor;
        }

        // 사용 실패 — 흔들고 이유를 잠깐 띄웁니다.
        public void PlayFail(string reason)
        {
            if (contents != null)
            {
                // 흔들기는 '원위치에서' 시작해야 합니다.
                // 끌던 자리에서 흔들면 그 자리를 시작점으로 기억해 카드가 화면에 남습니다.
                _moveTween?.Kill();
                contents.DOKill();

                contents.anchoredPosition = _homePosition;
                contents.localScale = Vector3.one;

                _moveTween = contents.DOShakeAnchorPos(0.3f, new Vector2(24f, 0f), 12, 0f)
                    .OnComplete(() => contents.anchoredPosition = _homePosition);
            }

            if (failText == null)
                return;

            _failTween?.Kill();

            failText.text = reason;
            failText.alpha = 1f;
            failText.gameObject.SetActive(true);

            _failTween = failText.DOFade(0f, 0.9f)
                .SetDelay(0.4f)
                .OnComplete(() => failText.gameObject.SetActive(false));
        }

        // 스킬 카드 데이터를 그대로 반영합니다(이름 / 코스트 / 설명 / 타입별 프레임).
        public void SetCardData(SkillCardDataTableRow row)
        {
            if (row == null)
                return;

            SetContent(row.Name, row.Cost.ToString());

            if (descText != null)
                descText.text = BuildDescription(row);

            ApplyCategory(row.SkillCategoryType);
            ApplyFrame(row.SkillCategoryType);
        }

        // 설명문의 {0}은 효과 값, {1}은 지속시간입니다.
        private string BuildDescription(SkillCardDataTableRow row)
        {
            if (string.IsNullOrEmpty(row.Desc))
                return string.Empty;

            var value = FormatNumber(row.EffectValue);
            var duration = FormatNumber(row.DurationTime);

            return string.Format(row.Desc, value, duration);
        }

        private static string FormatNumber(float value)
        {
            return Mathf.Approximately(value, Mathf.Round(value))
                ? Mathf.RoundToInt(value).ToString()
                : value.ToString("0.##");
        }

        // 카테고리에 맞는 이름과 아이콘을 표시합니다.
        private void ApplyCategory(ESkillCategoryType category)
        {
            if (typeText != null)
                typeText.text = GetCategoryName(category);

            if (attackSymbol != null)
                attackSymbol.SetActive(category == ESkillCategoryType.Attack);

            if (assistSymbol != null)
                assistSymbol.SetActive(category == ESkillCategoryType.Assist);

            if (supportSymbol != null)
                supportSymbol.SetActive(category == ESkillCategoryType.Support);
        }

        private static string GetCategoryName(ESkillCategoryType category)
        {
            switch (category)
            {
                case ESkillCategoryType.Attack: return "공격";
                case ESkillCategoryType.Assist: return "보조";
                case ESkillCategoryType.Support: return "서포트";
            }

            return string.Empty;
        }

        private void ApplyFrame(ESkillCategoryType category)
        {
            if (frameImage == null)
                return;

            var sprite = category switch
            {
                ESkillCategoryType.Attack => attackFrame,
                ESkillCategoryType.Assist => assistFrame,
                ESkillCategoryType.Support => supportFrame,
                _ => attackFrame
            };

            if (sprite != null)
                frameImage.sprite = sprite;
        }

        // 아직 드로우되지 않은 상태로 감춥니다.
        public void SetHidden()
        {
            _isHidden = true;
            _isDrawing = false;

            SetSelectEffect(false);

            _moveTween?.Kill();

            if (contents != null)
                contents.DOKill();

            if (canvasGroup == null)
                return;

            canvasGroup.DOKill();
            canvasGroup.alpha = 0f;
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

            _isHidden = false;

            _moveTween?.Kill();

            contents.anchoredPosition = _homePosition + new Vector2(0f, drawFromOffsetY);
            contents.localScale = Vector3.one;

            if (canvasGroup != null)
            {
                canvasGroup.DOKill();
                canvasGroup.alpha = 0f;
                canvasGroup.DOFade(TargetAlpha, drawDuration).SetDelay(delay);
            }

            _isDrawing = true;

            _moveTween = contents.DOAnchorPos(_homePosition, drawDuration)
                .SetEase(Ease.OutCubic)
                .SetDelay(delay)
                .OnComplete(() => _isDrawing = false);
        }

        // 판정에 걸리지 않았을 때 원래 자리로 되돌립니다.
        public void ReturnHome()
        {
            if (contents == null)
                return;

            _moveTween?.Kill();
            contents.DOKill();

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
            if (!_isInteractable || _isDrawing || _isHidden || contents == null)
                return;

            IsDragging = true;

            SetSelectEffect(true);

            // 끌고 있는 동안은 잘 보이도록 되돌립니다.
            if (canvasGroup != null)
                canvasGroup.alpha = 1f;

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

            SetSelectEffect(false);

            // 프레임 색과 밝기를 평소 상태로 되돌립니다.
            SetDragFeedback(true);

            if (canvasGroup != null)
                canvasGroup.alpha = TargetAlpha;

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

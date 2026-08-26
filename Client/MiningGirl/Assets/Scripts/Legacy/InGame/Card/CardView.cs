using System;
using Data;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Legacy.MainGame.Card
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
        [Tooltip("카드 그림. 어드레서블에서 불러옵니다.")]
        private Image iconImage;
        [SerializeField]
        [Tooltip("Attack / Assist / Support 순서. 지금은 셋 다 같은 프레임을 씁니다.")]
        private Sprite[] categoryFrames;

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

        [Header("Aim")]
        [SerializeField]
        [Tooltip("조준 중(사용 판정선 위) 카드 투명도. 카드가 조준점을 덮어 대상 표시가 안 보이던 문제 때문입니다.")]
        [Range(0.05f, 1f)]
        private float aimAlpha = 0.4f;

        [SerializeField]
        [Tooltip("조준 중 카드 크기")]
        [Range(0.3f, 1.5f)]
        private float aimScale = 0.85f;

        [SerializeField]
        [Tooltip("조준 자세로 바뀌는 시간(초). 0이면 즉시 바뀝니다.")]
        private float aimTweenDuration = 0.12f;

        [SerializeField]
        [Tooltip("흐려질 때도 진하게 남길 요소(테두리·이름·코스트). CanvasGroup의 Ignore Parent Groups가 켜져 있어야 합니다.")]
        private CanvasGroup[] aimKeepGroups;

        private bool _isAiming;
        private Tween _aimFadeTween;
        private Tween _aimScaleTween;

        public bool IsAiming => _isAiming;

        // 사용 판정선을 넘었는지에 따라 조준 자세로 바꿉니다.
        //
        // 카드가 조준점 위에 그대로 엉혀서 대상 표시가 하나도 안 보이던 문제 때문입니다.
        // 흐리고 작게 만들어 아래가 비치게 하되, 무엇을 쥐고 있는지는 알 수 있게
        // 테두리·이름·코스트만 aimKeepGroups로 진하게 남깁니다.
        public void SetAiming(bool value)
        {
            if (_isAiming == value)
                return;

            _isAiming = value;

            ApplyAimPose(false);
        }

        // 드래그가 끝나거나 카드가 감췄짐 때는 연출 없이 즉시 되돌립니다.
        private void ResetAim()
        {
            KillAimTweens();

            if (!_isAiming)
                return;

            _isAiming = false;

            ApplyAimPose(true);
        }

        private void KillAimTweens()
        {
            _aimFadeTween?.Kill();
            _aimFadeTween = null;

            _aimScaleTween?.Kill();
            _aimScaleTween = null;
        }

        private void ApplyAimPose(bool instant)
        {
            KillAimTweens();

            // 조준 중에는 카드 전체를 덮는 선택 이펙트를 끕니다.
            //
            //             // 카드 전면을 덮는 연출이면 '비쳐 보이게' 하려는 의도와 정면으로 부딪힙니다. 셀이더 머티리얼(400x550 전면 발광)입니다.
            //             // (기본 프리팩에는 지금 이펙트가 없어서 이 호출은 아무 일도 하지 않습니다. 알파를 그대로 따르지 않는 경우가 많고,
            //             //  나중에 새 강조 연출을 달면 그때부터 자동으로 적용됩니다.) '비쳐 보이게' 하려는 의도와 부딪힙니다.
            SetSelectEffect(IsDragging && !_isAiming);

            var alpha = _isAiming ? aimAlpha : TargetAlpha;
            var scale = _isAiming ? aimScale : (IsDragging ? dragScale : 1f);
            var duration = instant ? 0f : aimTweenDuration;

            if (canvasGroup != null)
            {
                if (duration <= 0f)
                    canvasGroup.alpha = alpha;
                else
                    _aimFadeTween = canvasGroup.DOFade(alpha, duration);
            }

            if (contents == null)
                return;

            if (duration <= 0f)
                contents.localScale = Vector3.one * scale;
            else
                _aimScaleTween = contents.DOScale(Vector3.one * scale, duration);
        }

        // aimKeepGroups는 Ignore Parent Groups라 카드 전체 알파를 따라오지 않습니다.
        // 조준 중에는 1로 남기고, 그 외에는 카드와 같은 밝기로 맞춰 줍니다.
        // (드로우 페이드인·사용 후 사라짐에서 이 요소들만 남아 보이지 않도록)
        private void LateUpdate()
        {
            if (aimKeepGroups == null || aimKeepGroups.Length == 0 || canvasGroup == null)
                return;

            var alpha = _isAiming ? 1f : canvasGroup.alpha;

            for (var i = 0; i < aimKeepGroups.Length; i++)
            {
                if (aimKeepGroups[i] == null)
                    continue;

                if (!Mathf.Approximately(aimKeepGroups[i].alpha, alpha))
                    aimKeepGroups[i].alpha = alpha;
            }
        }

        public int SlotIndex { get; private set; }
        public bool IsDragging { get; private set; }

        // 지금 끌고 있는 위치를 슬롯과 같은 기준(카드 루트)으로 돌려줍니다.
        // contents는 카드 안쪽 좌표라 슬롯 좌표와 그대로 비교하면 어긋납니다.
        public Vector2 DragLocalPosition
        {
            get
            {
                var rect = (RectTransform)transform;

                if (contents == null)
                    return rect.anchoredPosition;

                // 카드 루트 위치 + 안쪽에서 끌린 거리
                return rect.anchoredPosition + contents.anchoredPosition * rect.localScale.x;
            }
        }

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
        // 사용 실패 — 흔들고,         // 사용 실패 — 흔들고, reason이 있을 때만 이유를 잠깐 띄웁니다. 잠긐 띄우거나 합니다.
        //
        // '대상 없음'은 놓기 전에 이미 조준 표시(포물선·사거리 원이 붉게)로 알려주므로
        // 같은 말을 카드 안에 또 쓰지 않습니다. 그럴 때는 reason 없이 부릅니다.
        // 사용 실패 — 카드를 짧게 흔듭니다.
        //
        // 실패 '사유'는 카드가 아니라 손패 위 공통 문구(CardMessageUI)가 말합니다.
        // 카드 안에 두면 작고, 흔들리고, 3장이 각자 다른 말을 할 수 있었습니다.
        public void PlayFail()
        {
            if (contents == null)
                return;

            // 흔들기는 '원위치에서' 시작해야 합니다.
            // 끌던 자리에서 흔들면 그 자리를 시작점으로 기억해 카드가 화면에 남습니다.
            _moveTween?.Kill();
            contents.DOKill();

            contents.anchoredPosition = _homePosition;
            contents.localScale = Vector3.one;

            _moveTween = contents.DOShakeAnchorPos(0.3f, new Vector2(24f, 0f), 12, 0f)
                .OnComplete(() => contents.anchoredPosition = _homePosition);
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

            // 아이콘은 어드레서블에 있습니다. 캐시에 있으면 즉시 들어갑니다.
            Manager.AddressableManager.Instance?.ApplySprite(row.AssetId, iconImage);
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
            if (frameImage == null || categoryFrames == null)
                return;

            var index = (int)category;
            var sprite = index >= 0 && index < categoryFrames.Length ? categoryFrames[index] : null;

            if (sprite != null)
                frameImage.sprite = sprite;
        }

        // 아직 드로우되지 않은 상태로 감춥니다.
        public void SetHidden()
        {
                        ResetAim();

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

        // 위치·각도·배율을 한 번에 맞춥니다(부채꼴 손패 배치용).
        public void SetSlotPose(Vector2 slotPosition, float angle, float scale, float duration)
        {
            if (_rect == null)
                _rect = transform as RectTransform;

            _slotTween?.Kill();

            if (duration <= 0f)
            {
                _rect.anchoredPosition = slotPosition;
                _rect.localRotation = Quaternion.Euler(0f, 0f, angle);
                _rect.localScale = Vector3.one * scale;

                return;
            }

            _slotTween = _rect.DOAnchorPos(slotPosition, duration).SetEase(Ease.OutCubic);

            _rect.DOLocalRotate(new Vector3(0f, 0f, angle), duration).SetEase(Ease.OutCubic);
            _rect.DOScale(scale, duration).SetEase(Ease.OutCubic);
        }

        // 드래그 도중 슬롯을 옮깁니다.
        // 카드 루트는 새 슬롯으로 가고, 안쪽(contents)은 그만큼 반대로 밀어
        // 손끝에 붙어 있던 화면상 위치가 그대로 유지됩니다.
        public void MoveSlotKeepingDrag(Vector2 slotPosition)
        {
            var rect = (RectTransform)transform;
            var delta = slotPosition - rect.anchoredPosition;

            _slotTween?.Kill();

            rect.anchoredPosition = slotPosition;

            if (contents == null)
                return;

            var scale = Mathf.Approximately(rect.localScale.x, 0f) ? 1f : rect.localScale.x;

            contents.anchoredPosition -= delta / scale;
        }

        // 각도만 바꿉니다. 드래그 중에는 0도로 세워 두었다가
        // 놓을 때 슬롯 각도로 돌아갑니다.
        public void SetSlotAngle(float angle, float duration)
        {
            if (_rect == null)
                _rect = transform as RectTransform;

            _rect.DOKill(false);

            if (duration <= 0f)
            {
                _rect.localRotation = Quaternion.Euler(0f, 0f, angle);

                return;
            }

            _rect.DOLocalRotate(new Vector3(0f, 0f, angle), duration).SetEase(Ease.OutCubic);
        }


        // 새 카드가 손패로 들어오는 연출
        public void PlayDraw(float delay = 0f)
        {
            if (contents == null)
                return;

                        ResetAim();

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

            // 조준 자세를 먼저 되돌려야 아래의 밝기 복구와 서로 싸우지 않습니다.
            ResetAim();

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

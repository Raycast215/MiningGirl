using System;
using System.Collections.Generic;
using Data;
using InGame.System.Skill.UI;
using MainGame.Card;
using Manager;
using Scene.InGame.Entity.Interface;
using UnityEngine;

namespace Scene.InGame.Card
{
    // 손패(카드 3장)의 드래그 판정과 드로우를 관리합니다.
    //
    // 판정 규칙:
    //  - 카드를 아래쪽(버리기 영역)으로 끌어서 놓으면 → 버리고 새 카드 드로우
    //  - 그 외 위치에 놓으면 → 사용 시도 (지금은 로그만, 추후 카드 효과 연결)
    //  - 거의 안 움직였으면 → 원위치 복귀
    //
    // 아직 카드 데이터가 없어서 내용은 임시 문자열로 채웁니다.
    public class CardHandController : GameMonoInitializer
    {
        [SerializeField]
        [Tooltip("손패 카드들 (씬의 Skill Card Group 자식)")]
        private List<CardView> cards = new List<CardView>();

        [SerializeField]
        [Tooltip("드래그 좌표 계산에 쓸 캔버스")]
        private Canvas canvas;

        [SerializeField]
        [Tooltip("손패 배치를 계산하는 컴포넌트(카드들의 부모)")]
        private global::UI.Common.HandCardLayout handLayout;

        [SerializeField]
        [Tooltip("아래로 버릴 때 표시되는 영역 UI (선택)")]
                private SkillCardRemoveUI removeUI;

        [SerializeField]
        [Tooltip("조준 중 포물선과 사거리 원을 그리는 표시기 (선택)")]
                private global::UI.Common.AimIndicator aimIndicator;

        [SerializeField]
        [Tooltip("손패 위에 사용 실패 사유를 띄우는 공통 문구 (선택)")]
        private Scene.InGame.UI.CardMessageUI cardMessage;

        [Header("Judge")]
        // 카드는 화면 맨 아래에 있어서 '절대 위치'로 판정하면 기본 자세가 이미 하단이라
        // 살짝만 건드려도 버리기가 됩니다. 그래서 '처음 잡은 곳에서 얼마나 끌었는지'로 판정합니다.
        [SerializeField]
        [Tooltip("아래로 이 비율(화면 높이 대비)만큼 끌어내리면 '버리기'")]
        [Range(0.01f, 0.3f)]
        private float discardDragRatio = 0.05f;

        [SerializeField]
        [Tooltip("위로 이 비율(화면 높이 대비)만큼 끌어올리면 '사용'")]
        [Range(0.02f, 0.5f)]
        private float useDragRatio = 0.12f;

        [Header("Draw Motion")]
        [SerializeField]
        [Tooltip("처음 손패를 채울 때 카드 사이의 시간차(초)")]
        private float drawStagger = 0.12f;

        [SerializeField]
        [Tooltip("카드가 빠진 뒤 남은 카드들이 좌측으로 밀리는 시간(초)")]
        private float shiftDuration = 0.2f;

        [Header("Reorder")]
        [SerializeField]
        [Tooltip("끌고 있는 카드가 옆 카드 자리에 이만큼 들어오면 자리를 바꿉니다(슬롯 간격 대비 비율)")]
        [Range(0.2f, 1f)]
        private float reorderThreshold = 0.5f;

        [SerializeField]
        [Tooltip("자리를 비켜주는 카드가 움직이는 시간(초)")]
        private float reorderDuration = 0.15f;

        [Header("Cost")]
        [SerializeField]
        [Tooltip("카드 사용 시 소모하는 코스트")]
        private int useCost = 3;

        [SerializeField]
        [Tooltip("카드를 버리고 새로 뽑을 때 소모하는 코스트")]
        private int discardCost = 1;

        // 카드 리롤(버리기) 비용은 게임 상수 테이블에서 가져옵니다(없으면 인스펙터 값).
        private int GetDiscardCost()
        {
            var table = DataTableManager.Instance?.GameConstantDataTable;

            return table != null ? table.GetInt(EGameConstantType.CardRerollCost, discardCost) : discardCost;
        }

        private readonly Dictionary<CardView, Vector2> _dragStartScreenPos = new Dictionary<CardView, Vector2>();
        // 코스트 확인/소모는 UI 컨트롤러에 위임합니다(LevelUpController와 같은 주입 방식).
        private Func<int, bool> _canAffordCost;
        private Func<int, bool> _trySpendCost;

        // 슬롯 위치(좌 -> 우). 씬에 배치된 카드들의 원래 좌표에서 가져옵니다.
        private readonly List<Vector2> _slotPositions = new List<Vector2>();

        // 현재 손패 순서(좌 -> 우). 카드가 빠지면 남은 카드가 앞으로 당겨집니다.
        private readonly List<CardView> _order = new List<CardView>();

        // 지금 끌고 있는 카드. 멀티터치로 두 장이 동시에 끌리지 않도록 한 장만 허용합니다.
                private CardView _draggingCard;

        // 드래그 중 조준 표시를 켜 둔 대상들.
        private readonly List<IEntity> _markedTargets = new List<IEntity>();

        // 카드마다 지금 들고 있는 스킬 데이터
        private readonly Dictionary<CardView, SkillCardDataTableRow> _cardData = new Dictionary<CardView, SkillCardDataTableRow>();

        // 스킬 효과 실행에 필요한 것들
        private SkillCardContext _skillContext;

        // 런 전체에서 유지되는 덱(드로우/버린 더미 포함)
        public SkillDeck Deck { get; } = new SkillDeck();

                private int _drawCount;

        // 지금 끌고 있는 카드가 사용 판정선을 넘어 조준 중인지.
        // OnDrag는 손끝이 움직인 프레임에만 오기 때문에,
        // 멈춰 있는 동안에도 표시를 다시 그리려고 따로 들고 있습니다.
        private bool _isAimingDrag;
        private bool _isPaused;

        public void Init(Func<int, bool> canAffordCost = null, Func<int, bool> trySpendCost = null, SkillCardContext skillContext = null)
        {
            if (IsInitialized)
                return;

            _canAffordCost = canAffordCost;
            _trySpendCost = trySpendCost;
            _skillContext = skillContext;

            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();

            // 슬롯 좌표는 HandCardLayout이 손패 수에 맞춰 계산합니다.
            if (handLayout == null && cards.Count > 0 && cards[0] != null)
                handLayout = cards[0].transform.parent.GetComponent<global::UI.Common.HandCardLayout>();

            for (var i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (card == null)
                    continue;

                card.Init(i, canvas, OnDragBegin, OnDragging, OnDragEnd);
            }

            // 아직 깔지 않고 숨겨둡니다. 실제 드로우는 게임 시작(StartHand) 시점에 합니다.
            HideAll();

            HideRemoveUIImmediate();

            IsInitialized = true;
        }

        // 재시작 등으로 판을 리셋할 때, 화면이 덮여 있는 동안 손패를 즉시 감춥니다.
        // (감추지 않으면 화면이 밝아질 때 이전 카드가 남아 있다가 사라지는 게 보입니다.)
        public void HideHand()
        {
            HideAll();

            HideRemoveUIImmediate();

            ClearTargetPreview();
        }

        private void Update()
        {
            if (!IsInitialized)
                return;

            RefreshAvailability();

            // 조준 중에는 손가락이 멈춰 있어도 표시를 계속 다시 그립니다.
            // 드래그 이벤트만 믿으면 코스트가 회복되거나 대상이 움직여도
            // 포물선이 직전 상태(예: 붉은색)에 멈춰 있게 됩니다.
            if (!_isPaused && _isAimingDrag && _draggingCard != null)
                RefreshTargetPreview(_draggingCard);
        }

        // 평소에는 '코스트가 되는지'만 갱신합니다.
        // 대상 유무까지 보면 몬스터가 화면을 드나들 때마다 카드가 깜빡입니다.
        private void RefreshAvailability()
        {
            foreach (var card in cards)
            {
                if (card == null)
                    continue;

                var cost = GetCardCost(card);

                card.SetAvailable(!_isPaused && CanAfford(cost));
            }
        }

        // 카드에 매겨진 코스트. 0도 유효한 값이므로 '값이 없을 때'만 기본값으로 대체합니다.
        // (0을 빈 값으로 취급하면 공짜 카드가 기본 코스트로 막혀버립니다.)
        private int GetCardCost(CardView card)
        {
            if (_cardData.TryGetValue(card, out var row) && row != null)
                return Mathf.Max(0, row.Cost);

            return useCost;
        }

        // 게임 시작 시점에 손패를 좌측부터 순차로 깔아줍니다.
        // 스테이지 시작 — 덱을 준비하고 손패를 깝니다.
        public void StartHand()
        {
            // 첫 스테이지에서만 기본 덱을 만들고, 이후에는 카드를 모두 되돌려 섞습니다.
            if (Deck.DeckCount == 0)
                Deck.InitFromDefaultTable();
            else
                Deck.ResetPiles();

            DealAll();
        }

        // 시작 전에는 카드를 감춰둡니다.
        private void HideAll()
        {
            foreach (var card in cards)
            {
                if (card == null)
                    continue;

                card.SetHidden();
            }
        }

        // 팝업 등으로 게임이 멈춘 동안에는 카드도 만질 수 없게 합니다.
        public void SetPaused(bool paused)
        {
            _isPaused = paused;

            foreach (var card in cards)
            {
                if (card == null)
                    continue;

                card.SetInteractable(!paused);
            }

            if (paused)
            {
                _draggingCard = null;

                HideRemoveUI();
                ClearTargetPreview();
            }
        }

        // 손패 전체를 새로 드로우합니다(스테이지 재시작 등).
        public void ResetHand()
        {
            DealAll();

            HideRemoveUI();
        }

#region Drag Callbacks

        // 손패 수에 맞춰 슬롯 좌표를 다시 구하고 카드들을 배치합니다.
        // 배치 계산은 HandCardLayout이, 어느 카드가 어느 슬롯인지는 여기가 정합니다.
        private void RefreshSlots(float duration)
        {
            if (handLayout == null)
                return;

            var count = _order.Count;

            _slotPositions.Clear();

            for (var i = 0; i < count; i++)
            {
                var slot = handLayout.GetSlot(i, count);

                _slotPositions.Add(slot.Position);

                var card = _order[i];

                if (card == null)
                    continue;

                // 끌고 있는 카드는 맨 위에 떠 있어야 하므로 자리·깊이를 건드리지 않습니다.
                if (card.IsDragging)
                    continue;

                // 오른쪽 카드가 위에 오도록 순서대로 그립니다.
                card.transform.SetSiblingIndex(i);

                card.SetSlotPose(slot.Position, slot.Angle, slot.Scale, duration);
            }

            // 다른 카드가 자리를 잡으면서 순서가 밀리므로,
            // 끌고 있는 카드는 마지막에 다시 맨 위로 올립니다.
            if (_draggingCard != null)
                _draggingCard.transform.SetAsLastSibling();
        }

        private void OnDragBegin(CardView card)
        {
            if (_isPaused)
                return;

            _draggingCard = card;

            // 코스트가 모자란 카드를 잡으면 짧게 흔들어 알려줍니다.
            // (끌어서 버리는 건 가능하므로 드래그 자체를 막지는 않습니다.)
            if (!CanAfford(GetCardCost(card)))
                card.PlayUnavailableShake();

            // 다른 카드는 잠가서 두 번째 손가락이 다른 카드를 집지 못하게 합니다.
            SetOthersInteractable(card, false);

            // 끌고 있는 동안은 똑바로 세웁니다(기울어진 채 끌리면 손끝과 어긋나 보입니다).
            card.SetSlotAngle(0f, reorderDuration);

            // 끌고 있는 카드는 맨 위에 그려 다른 카드에 가리지 않게 합니다.
            card.transform.SetAsLastSibling();

            _dragStartScreenPos[card] = GetScreenPosition(card);
        }

        private void OnDragging(CardView card)
        {
            if (_isPaused)
                return;

            // 아래로 충분히 끌어내렸으면 안내 UI를 띄웁니다.
            var isDiscard = IsDiscardDrag(card);

            if (isDiscard)
                ShowRemoveUI();
            else
                HideRemoveUI();

            // 끌고 있는 동안에만 '지금 놓으면 되는지'를 프레임 색으로 알려줍니다.
            card.SetDragFeedback(isDiscard ? CanAfford(GetDiscardCost()) : CanUseNow(card));

            // 사용 판정선을 넘은 동안만 조준 상태입니다.
            // 이때 카드를 흐리고 작게 만들고(대상 표시가 카드에 가리지 않게)
            // 대상 표시와 포물선·사거리 원을 띄웁니다.
            var isAiming = !isDiscard && IsUseDrag(card);

            card.SetAiming(isAiming);

            if (isAiming)
            {
                // 먼저 켜둔 뒤에 그려야, 그리다 쓸 것이 없어 표시를 지우는 경우
                // 그 결과(꺼짐)가 그대로 남습니다.
                _isAimingDrag = true;

                RefreshTargetPreview(card);
            }
            else
            {
                ClearTargetPreview();
            }

            // 버리려고 아래로 끄는 중이 아니면 자리 바꾸기를 살핍니다.
            if (!isDiscard)
                TryReorder(card);
        }

        // 위로 사용 판정선을 넘었는지
        private bool IsUseDrag(CardView card)
        {
            return GetDragDeltaY(card) >= Screen.height * useDragRatio;
        }

        // 지금 카드 위치 기준으로 적중할 대상의 머리 위에 표시를 띄우고,
        // 손패 자리에서 카드 중앙으로 포물선을 그립니다.
        // 사거리 원과 머리 위 표시는 대상을 직접 고르는 스킬에만 붙습니다.
        // 버프·지원 카드는 포물선만 그려서 '어디에 놓는 중'인지만 보여줍니다.
        //
        // 매 프레임 전부 껐다 켜면 깜빡이므로, 이전 프레임과 비교해
        // 빠진 대상만 끄고 새로 들어온 대상만 켭니다.
        private void RefreshTargetPreview(CardView card)
        {
            if (_skillContext == null || !_cardData.TryGetValue(card, out var row) || row == null)
            {
                ClearTargetPreview();

                return;
            }

            var effect = SkillCardEffectFactory.Get(row.SkillType);

            if (effect == null)
            {
                ClearTargetPreview();

                return;
            }

            var dropScreen = GetScreenPosition(card);

            _skillContext.SetDropScreenPosition(dropScreen);

            // 코스트가 모자라도 포물선은 그립니다. 대신 붉게 칠해 못 쓴다고 알려줍니다.
            // (머리 위 표시까지 켜면 쓸 수 있는 것처럼 보여서 대상은 잡지 않습니다.)
            var canAfford = CanAfford(GetCardCost(card));

            var preview = effect as ITargetPreviewEffect;
            var targets = preview != null && canAfford ? preview.CollectTargets(_skillContext, row) : null;

            ApplyTargetPreview(targets);

            var usable = canAfford
                && (preview != null ? targets.Count > 0 : effect.CanExecute(_skillContext, row));

            var range = preview != null ? preview.GetPreviewRange(row) : 0f;

            ShowAimIndicator(card, dropScreen, range, usable);
        }

        // 포물선 시작점은 이 카드의 손패 자리입니다.
        // 드래그 중에도 카드 루트는 슬롯에 그대로 있고 안쪽(Contents)만 움직입니다.
        //
        // isUsable이 false면(대상 없음·코스트 부족) 포물선을 붉게 그립니다.
        // worldRange가 0 이하면 사거리 원은 빼고 포물선만 그립니다.
        private void ShowAimIndicator(CardView card, Vector2 dropScreen, float worldRange, bool isUsable)
        {
            if (aimIndicator == null)
                return;

            var uiCamera = GetUICamera();
            var fromScreen = RectTransformUtility.WorldToScreenPoint(uiCamera, card.transform.position);

            // 사거리는 월드 단위라, 끝 지점을 같은 방식으로 화면에 옮긴 뒤 거리로 잽니다.
            // (픽셀로 직접 환산하면 캔버스 스케일을 또 나눠야 해서 틀리기 쉽습니다.)
            var worldCamera = _skillContext != null && _skillContext.Camera != null ? _skillContext.Camera : Camera.main;
            var edgeScreen = worldRange > 0f && worldCamera != null
                ? (Vector2)worldCamera.WorldToScreenPoint(_skillContext.DropWorldPosition + Vector3.right * worldRange)
                : dropScreen;

            aimIndicator.Show(fromScreen, dropScreen, edgeScreen, isUsable, uiCamera);
        }

        private Camera GetUICamera()
        {
            if (canvas == null)
                return null;

            return canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        }

        private void ApplyTargetPreview(IReadOnlyList<IEntity> targets)
        {
            // 이번에 빠진 대상만 끕니다.
            for (var i = _markedTargets.Count - 1; i >= 0; i--)
            {
                var marked = _markedTargets[i];

                if (ContainsTarget(targets, marked))
                    continue;

                SetTargetMark(marked, false);

                _markedTargets.RemoveAt(i);
            }

            if (targets == null)
                return;

            for (var i = 0; i < targets.Count; i++)
            {
                var target = targets[i];

                if (target == null || ContainsTarget(_markedTargets, target))
                    continue;

                SetTargetMark(target, true);

                _markedTargets.Add(target);
            }
        }

        // 드래그가 끝나거나 게임이 멈추면 표시를 모두 지웁니다.
        // 드래그가 끝나거나 게임이 멈추면 표시를 모두 지웁니다.
        public void ClearTargetPreview()
        {
            _isAimingDrag = false;

            for (var i = 0; i < _markedTargets.Count; i++)
                SetTargetMark(_markedTargets[i], false);

            _markedTargets.Clear();

            if (aimIndicator != null)
                aimIndicator.Hide();
        }

        private static bool ContainsTarget(IReadOnlyList<IEntity> list, IEntity item)
        {
            if (list == null)
                return false;

            for (var i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], item))
                    return true;
            }

            return false;
        }

        // 몬스터든 광물이든 머리 위 표시는 EntityBase가 들고 있습니다.
        private static void SetTargetMark(IEntity entity, bool value)
        {
            var target = entity as global::Scene.InGame.Entity.EntityBase;

            if (target != null)
                target.SetTargetMark(value);
        }

        // 끌고 있는 카드가 옆 슬롯 가까이 오면 그 자리의 카드와 순서를 바꿉니다.
        // 끌리는 카드는 손끝을 따라가고, 비켜주는 카드만 부드럽게 움직입니다.
        private void TryReorder(CardView card)
        {
            var from = _order.IndexOf(card);

            if (from < 0 || _order.Count < 2 || _slotPositions.Count < 2)
                return;

            var rect = (RectTransform)card.transform;
            var current = card.DragLocalPosition;

            // 슬롯 간격. 이 간격의 일정 비율만큼 넘어오면 자리를 바꿉니다.
            var step = Mathf.Abs(_slotPositions[1].x - _slotPositions[0].x);

            if (step <= 0f)
                return;

            var threshold = step * reorderThreshold;
            var to = from;

            // 오른쪽으로 넘어갔는지
            if (from + 1 < _order.Count
                && current.x - _slotPositions[from].x > threshold)
                to = from + 1;
            // 왼쪽으로 넘어갔는지
            else if (from - 1 >= 0
                && _slotPositions[from].x - current.x > threshold)
                to = from - 1;

            if (to == from)
                return;

            // 순서를 맞바꿉니다.
            var other = _order[to];

            _order[to] = card;
            _order[from] = other;

            // 비켜주는 카드는 새 자리로 부드럽게 이동합니다.
            // 끌고 있는 카드는 RefreshSlots가 건너뛰므로 손끝에 그대로 붙어 있습니다.
            RefreshSlots(reorderDuration);

            // 놓았을 때 돌아갈 자리를 새 슬롯으로 맞춰 둡니다.
            if (handLayout != null)
            {
                var slot = handLayout.GetSlot(to, _order.Count);

                card.MoveSlotKeepingDrag(slot.Position);
            }
        }

        private void OnDragEnd(CardView card)
        {
            _draggingCard = null;

            // 손을 뗄 때 조준 표시도 함께 지웁니다.
            // (아래의 TryUseCard보다 먼저 지워야 사용 직후에 표시가 남지 않습니다.)
            ClearTargetPreview();

            // 끌던 카드도 자기 슬롯 자세(위치·각도·배율)로 돌아갑니다.
            // 드래그 중에는 RefreshSlots가 이 카드를 건너뛰므로 여기서 맞춰 줍니다.
            RefreshSlots(reorderDuration);

            // 잠갔던 다른 카드를 다시 풀어줍니다.
            if (!_isPaused)
                SetOthersInteractable(card, true);

            HideRemoveUI();

            if (_isPaused)
            {
                card.ReturnHome();
                return;
            }

            var deltaY = GetDragDeltaY(card);

            // 아래로 충분히 끌어내림 → 버리고 새로 뽑기
            if (deltaY <= -Screen.height * discardDragRatio)
            {
                if (!TrySpend(GetDiscardCost()))
                {
                    Debug.Log($"[Card] 버리기 실패 — 코스트 부족 (필요 {GetDiscardCost()})");

                                        card.PlayFail();
                    ShowMessage($"코스트가 부족합니다 ({GetDiscardCost()} 필요)");
                    return;
                }

                Debug.Log($"[Card] 버리기 — 슬롯 {card.SlotIndex} (코스트 -{discardCost})");

                card.PlayConsume(true, () => OnCardConsumed(card));
                return;
            }

            // 위로 충분히 끌어올림 → 사용
            if (deltaY >= Screen.height * useDragRatio)
            {
                TryUseCard(card);
                return;
            }

            // 어느 쪽도 아니면 되돌립니다.
            card.ReturnHome();
        }

#endregion

        // 코스트를 소모합니다. 주입이 없으면(테스트 등) 항상 성공으로 봅니다.
        private bool TrySpend(int amount)
        {
            if (amount <= 0)
                return true;

            if (_trySpendCost == null)
                return true;

            return _trySpendCost.Invoke(amount);
        }

        // 지금 이 비용을 낼 수 있는지 (드래그 중 안내용)
        private bool CanAfford(int amount)
        {
            if (amount <= 0 || _canAffordCost == null)
                return true;

            return _canAffordCost.Invoke(amount);
        }

        // 지정한 카드를 제외한 나머지의 입력 허용 여부를 바꿉니다.
        private void SetOthersInteractable(CardView except, bool value)
        {
            foreach (var card in cards)
            {
                if (card == null || card == except)
                    continue;

                card.SetInteractable(value);
            }
        }

        // 처음 잡은 위치에서 세로로 얼마나 이동했는지 (양수=위로, 음수=아래로)
        private float GetDragDeltaY(CardView card)
        {
            if (!_dragStartScreenPos.TryGetValue(card, out var startPos))
                return 0f;

            return GetScreenPosition(card).y - startPos.y;
        }

        private bool IsDiscardDrag(CardView card)
        {
            return GetDragDeltaY(card) <= -Screen.height * discardDragRatio;
        }

        private Vector2 GetScreenPosition(CardView card)
        {
            if (card.Contents == null)
                return Vector2.zero;

            var cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;

            return RectTransformUtility.WorldToScreenPoint(cam, card.Contents.position);
        }

        // 지금 이 카드를 쓸 수 있는지(코스트 + 대상 조건)
        private bool CanUseNow(CardView card)
        {
            if (!_cardData.TryGetValue(card, out var row) || row == null)
                return false;

            if (!CanAfford(GetCardCost(card)))
                return false;

            // 소환 계열 스킬이 '놓은 자리'를 알 수 있게 먼저 알려줍니다.
            _skillContext?.SetDropScreenPosition(GetScreenPosition(card));

            var effect = SkillCardEffectFactory.Get(row.SkillType);

            return effect != null && effect.CanExecute(_skillContext, row);
        }

        // 카드 사용 시도. 순서가 중요합니다.
        //  1) 효과를 쓸 수 있는 상황인지 확인 (예: 때릴 적이 화면에 있는지)
        //  2) 코스트 확인·소모
        //  3) 효과 실행
        // 1이나 2에서 걸리면 카드도 코스트도 소모되지 않고 제자리로 돌아갑니다.
        private void TryUseCard(CardView card)
        {
            _cardData.TryGetValue(card, out var row);

            if (row == null)
            {
                Debug.LogWarning("[Card] 카드 데이터가 없어 사용할 수 없습니다.");

                card.ReturnHome();
                return;
            }

            // 소환 계열 스킬이 '놓은 자리'를 알 수 있게 먼저 알려줍니다.
            _skillContext?.SetDropScreenPosition(GetScreenPosition(card));

            var effect = SkillCardEffectFactory.Get(row.SkillType);

            if (effect == null || !effect.CanExecute(_skillContext, row))
            {
                Debug.Log($"[Card] 사용 실패 — {row.Name} 를 쓸 대상/조건이 없습니다.");

                // 대상이 없다는 건 놓기 전에 조준 표시가 붉게 변해 이미 알려줍니다.
                // 또 말하지 않고 흔들기만 합니다.
                card.PlayFail();
                return;
            }

            var cost = GetCardCost(card);

            if (!TrySpend(cost))
            {
                Debug.Log($"[Card] 사용 실패 — 코스트 부족 (필요 {cost})");

                card.PlayFail();
                ShowMessage($"코스트가 부족합니다 ({cost} 필요)");
                return;
            }

            effect.Execute(_skillContext, row);

            Debug.Log($"[Card] 사용 — {row.Name} (코스트 -{cost})");

            card.PlayConsume(false, () => OnCardConsumed(card));
        }

        // 손패 위 공통 문구로 실패 사유를 알립니다.
        private void ShowMessage(string message)
        {
            if (cardMessage != null)
                cardMessage.Show(message);
        }

        // 손패 전체를 좌측부터 순차적으로 깔아줍니다(게임 시작 / 스테이지 재시작).
        private void DealAll()
        {
            _order.Clear();

            foreach (var card in cards)
            {
                if (card == null)
                    continue;

                _order.Add(card);
            }

            // 순서가 정해진 뒤 자리를 잡아야 각도가 어긋나지 않습니다.
            RefreshSlots(0f);

            for (var i = 0; i < _order.Count; i++)
                DrawTo(_order[i], true, i * drawStagger);
        }

        // 카드가 손패에서 빠진 뒤 처리.
        // 남은 카드는 좌측으로 당겨지고, 빠진 카드는 새 카드가 되어 맨 우측에 들어옵니다.
        private void OnCardConsumed(CardView card)
        {
            // 사용했든 버렸든 덱의 '버린 더미'로 돌아갑니다.
            if (_cardData.TryGetValue(card, out var used) && used != null)
                Deck.Discard(used.Id);

            _order.Remove(card);

            // 빠진 카드는 맨 뒤로 다시 들어옵니다.
            card.transform.SetAsLastSibling();

            _order.Add(card);

            // 남은 카드와 새 카드를 한 번에 제자리로 보냅니다.
            // (순서를 확정한 뒤 배치해야 각도·위치가 서로 맞습니다.)
            RefreshSlots(shiftDuration);

            DrawTo(card, true, shiftDuration * 0.5f);
        }

        // 새 카드를 뽑아 슬롯을 채웁니다. 스킬 카드 테이블에서 가중치로 고릅니다.
        private void DrawTo(CardView card, bool playAnimation, float delay = 0f)
        {
            _drawCount++;

            var row = PickRandomCard();
            _cardData[card] = row;

            if (row != null)
                card.SetCardData(row);
            else
                card.SetContent($"CARD {_drawCount}", "-");

            // 새 카드의 코스트로 먼저 판정해야 드로우 페이드가 올바른 밝기로 끝납니다.
            card.SetAvailable(!_isPaused && CanAfford(GetCardCost(card)));

            if (playAnimation)
                card.PlayDraw(delay);
        }

        // 덱에서 카드 한 장을 뽑습니다.
        private SkillCardDataTableRow PickRandomCard()
        {
            return Deck.Draw();
        }

        private void ShowRemoveUI()
        {
            if (removeUI != null)
                removeUI.ShowCardRemoveUI();
        }

        // 시작 시점에는 연출 없이 즉시 끕니다.
        //
        // SkillCardRemoveUI.HideCardRemoveUI는 '이미 표시 중'이 아니면 아무것도 하지 않아서,
        // 씬에서 켜진 채 시작하면 영영 꺼지지 않습니다. 그래서 오브젝트를 직접 비활성화합니다.
        private void HideRemoveUIImmediate()
        {
            if (removeUI == null)
                return;

            removeUI.gameObject.SetActive(false);
        }

        private void HideRemoveUI()
        {
            if (removeUI != null)
                removeUI.HideCardRemoveUI();
        }
    }
}

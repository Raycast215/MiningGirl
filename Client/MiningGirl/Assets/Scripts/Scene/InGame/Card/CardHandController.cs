using System;
using Data;
using Manager;
using System.Collections.Generic;
using InGame.System.Skill.UI;
using UnityEngine;

namespace MainGame.Card
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
        [Tooltip("아래로 버릴 때 표시되는 영역 UI (선택)")]
        private SkillCardRemoveUI removeUI;

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

        [Header("Cost")]
        [SerializeField]
        [Tooltip("카드 사용 시 소모하는 코스트")]
        private int useCost = 3;

        [SerializeField]
        [Tooltip("카드를 버리고 새로 뽑을 때 소모하는 코스트")]
        private int discardCost = 1;

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

        // 카드마다 지금 들고 있는 스킬 데이터
        private readonly Dictionary<CardView, SkillCardDataTableRow> _cardData = new Dictionary<CardView, SkillCardDataTableRow>();

        // 스킬 효과 실행에 필요한 것들
        private SkillCardContext _skillContext;

        private int _drawCount;
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

            // 씬에 배치된 순서를 좌 -> 우 슬롯 위치로 기억해 둡니다.
            _slotPositions.Clear();

            for (var i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (card == null)
                    continue;

                card.Init(i, canvas, OnDragBegin, OnDragging, OnDragEnd);
                _slotPositions.Add(((RectTransform)card.transform).anchoredPosition);
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
        }

        private void Update()
        {
            if (!IsInitialized)
                return;

            RefreshAvailability();
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

        private int GetCardCost(CardView card)
        {
            if (_cardData.TryGetValue(card, out var row) && row != null && row.Cost > 0)
                return row.Cost;

            return useCost;
        }

        // 게임 시작 시점에 손패를 좌측부터 순차로 깔아줍니다.
        public void StartHand()
        {
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
            }
        }

        // 손패 전체를 새로 드로우합니다(스테이지 재시작 등).
        public void ResetHand()
        {
            DealAll();

            HideRemoveUI();
        }

#region Drag Callbacks

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
            card.SetDragFeedback(isDiscard ? CanAfford(discardCost) : CanUseNow(card));
        }

        private void OnDragEnd(CardView card)
        {
            _draggingCard = null;

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
                if (!TrySpend(discardCost))
                {
                    Debug.Log($"[Card] 버리기 실패 — 코스트 부족 (필요 {discardCost})");

                    card.PlayFail("코스트 부족");
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

            var effect = SkillCardEffectFactory.Get(row.SkillType);

            if (effect == null || !effect.CanExecute(_skillContext, row))
            {
                Debug.Log($"[Card] 사용 실패 — {row.Name} 를 쓸 대상/조건이 없습니다.");

                card.PlayFail("대상 없음");
                return;
            }

            var cost = row.Cost > 0 ? row.Cost : useCost;

            if (!TrySpend(cost))
            {
                Debug.Log($"[Card] 사용 실패 — 코스트 부족 (필요 {cost})");

                card.PlayFail("코스트 부족");
                return;
            }

            effect.Execute(_skillContext, row);

            Debug.Log($"[Card] 사용 — {row.Name} (코스트 -{cost})");

            card.PlayConsume(false, () => OnCardConsumed(card));
        }

        // 손패 전체를 좌측부터 순차적으로 깔아줍니다(게임 시작 / 스테이지 재시작).
        private void DealAll()
        {
            _order.Clear();

            for (var i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (card == null)
                    continue;

                card.SetSlotPosition(_slotPositions[i], 0f);
                DrawTo(card, true, i * drawStagger);

                _order.Add(card);
            }
        }

        // 카드가 손패에서 빠진 뒤 처리.
        // 남은 카드는 좌측으로 당겨지고, 빠진 카드는 새 카드가 되어 맨 우측에 들어옵니다.
        private void OnCardConsumed(CardView card)
        {
            _order.Remove(card);

            // 남은 카드들을 앞 슬롯으로 밀어줍니다.
            for (var i = 0; i < _order.Count; i++)
                _order[i].SetSlotPosition(_slotPositions[i], shiftDuration);

            // 빈 맨 뒷자리에 새 카드를 놓습니다.
            var lastIndex = Mathf.Min(_order.Count, _slotPositions.Count - 1);

            card.SetSlotPosition(_slotPositions[lastIndex], 0f);
            card.transform.SetAsLastSibling();

            DrawTo(card, true, shiftDuration * 0.5f);

            _order.Add(card);
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

        // 가중치 기반으로 카드 한 장을 고릅니다.
        private SkillCardDataTableRow PickRandomCard()
        {
            var table = DataTableManager.Instance?.SkillCardDataTable;
            if (table?.Rows == null || table.Rows.Count == 0)
                return null;

            var total = 0;
            foreach (var row in table.Rows)
                total += Mathf.Max(0, row.Weight);

            if (total <= 0)
                return table.Rows[UnityEngine.Random.Range(0, table.Rows.Count)];

            var pick = UnityEngine.Random.Range(0, total);
            var acc = 0;

            foreach (var row in table.Rows)
            {
                acc += Mathf.Max(0, row.Weight);

                if (pick < acc)
                    return row;
            }

            return table.Rows[table.Rows.Count - 1];
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

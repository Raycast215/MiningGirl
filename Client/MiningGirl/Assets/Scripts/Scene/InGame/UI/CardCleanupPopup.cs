using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Data;
using MainGame.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scene.InGame.UI
{
    // 카드 정리 화면.
    //
    // 새 카드 몇 장을 덱에 얹어 전부 늘어놓고, 넘치는 만큼 버리게 합니다.
    // 받은 카드와 원래 카드를 같은 자리에서 비교하게 되므로
    // "새 카드가 좋은가"가 아니라 "이 중 최선의 10장은 무엇인가"를 묻게 됩니다.
    public class CardCleanupPopup : MonoBehaviour
    {
        [SerializeField]
        private CardCleanupItemView itemPrefab;

        [SerializeField]
        private RectTransform itemRoot;

        [SerializeField]
        private TextMeshProUGUI titleText;

        [SerializeField]
        private TextMeshProUGUI descText;

        [SerializeField]
        private Button confirmButton;

        [SerializeField]
        [Tooltip("현재 장수 / 목표 장수를 보여줍니다")]
        private TextMeshProUGUI confirmButtonText;

        [SerializeField]
        [Tooltip("제시된 새 카드를 다시 뽑는 버튼")]
        private Button rerollButton;

        [SerializeField]
        [Tooltip("남은 갱신 횟수 표시")]
        private TextMeshProUGUI rerollButtonText;

        [Header("Colors")]
        [SerializeField]
        [Tooltip("목표 장수에 맞췄을 때 버튼 색")]
        private Color confirmReadyColor = new Color(0.11f, 0.62f, 0.46f, 1f);

        [SerializeField]
        [Tooltip("아직 더 골라야 할 때 버튼 색")]
        private Color confirmDisabledColor = new Color(0.39f, 0.39f, 0.39f, 1f);

        [Header("Draw")]
        [SerializeField]
        [Tooltip("화면이 뜬 뒤 새 카드가 나타나기까지 기다리는 시간(초). 기존 카드를 먼저 훑어볼 여유를 줍니다.")]
        private float drawStartDelay = 1f;

        [SerializeField]
        [Tooltip("새로 받은 카드가 나타나는 시간(초)")]
        private float drawDuration = 0.35f;

        [SerializeField]
        [Tooltip("새 카드 사이의 간격(초)")]
        private float drawInterval = 0.15f;

        private readonly List<CardCleanupItemView> _items = new List<CardCleanupItemView>();

        // 화면에 늘어놓은 카드 전체(기존 덱 + 새 카드)
        private readonly List<SkillCardDataTableRow> _cards = new List<SkillCardDataTableRow>();

        // 이번에 받은 카드의 위치(드로우 연출용)
        private readonly HashSet<int> _rewardIndexes = new HashSet<int>();

        // 그중 처음 얻어보는 카드의 위치(NEW 표시용)
        private readonly HashSet<int> _newIndexes = new HashSet<int>();

        // 버리기로 고른 카드의 위치
        private readonly HashSet<int> _discardIndexes = new HashSet<int>();

        // 드로우 연출이 도는 동안은 조작을 막습니다.
        private bool _isDrawing;

        private int _deckSize;
        private Action<List<string>> _onConfirm;

        // 갱신할 때 기존 덱은 그대로 두고 새 카드만 다시 뽑아야 하므로,
        // 이번에 받은 원본 덱 구성을 따로 들고 있습니다.
        private readonly List<string> _deckCards = new List<string>();

        // 남은 갱신 횟수 조회 / 실제 갱신(새 카드 목록을 돌려줍니다. 못 쓰면 null)
        private Func<int> _getRemainReroll;
        private Func<IReadOnlyList<SkillCardDataTableRow>> _reroll;

        // 카드 데이터 조회와 'NEW 붙일지' 판단을 밖에서 받습니다.
        // 예전에는 이 팝업이 DataTableManager와 GameDataManager를 직접 썼습니다.
        // 특히 세이브 기록(MarkCardSeen)까지 여기서 했어서,
        // '카드 고르는 화면'이 저장 시점을 정하는 모양이었습니다.
        private Func<string, SkillCardDataTableRow> _getCard;
        private Func<string, bool> _isNewCard;

        // 버려야 하는 장수
        private int RequiredDiscardCount => Mathf.Max(0, _cards.Count - _deckSize);

        // 지금 남길 장수
        private int RemainCount => _cards.Count - _discardIndexes.Count;

        public void Init(Func<string, SkillCardDataTableRow> getCard = null,
            Func<string, bool> isNewCard = null,
            Func<int> getRemainReroll = null,
            Func<IReadOnlyList<SkillCardDataTableRow>> reroll = null)
        {
            _getCard = getCard;
            _isNewCard = isNewCard;
            _getRemainReroll = getRemainReroll;
            _reroll = reroll;

            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(Confirm);
            }

            if (rerollButton != null)
            {
                rerollButton.onClick.RemoveAllListeners();
                rerollButton.onClick.AddListener(Reroll);
            }

            Hide();
        }

        // deckCards: 현재 덱, rewards: 이번에 받은 카드, deckSize: 유지할 장수
        public void Show(IReadOnlyList<string> deckCards, IReadOnlyList<SkillCardDataTableRow> rewards,
            int deckSize, Action<List<string>> onConfirm)
        {
            _onConfirm = onConfirm;
            _deckSize = Mathf.Max(1, deckSize);

            // 갱신하면 새 카드만 다시 뽑고 기존 덱은 그대로 둬야 하므로 따로 보관합니다.
            _deckCards.Clear();

            if (deckCards != null)
            {
                foreach (var id in deckCards)
                    _deckCards.Add(id);
            }

            BuildCards(rewards);

            _isDrawing = true;

            gameObject.SetActive(true);

            // 스테이지 맵 연출이 화면을 덮은 채 이 화면을 엽니다.
            // 맵이 계층상 뒤에 있어서, 올려주지 않으면 맵에 가려 보이지 않습니다.
            transform.SetAsLastSibling();

            if (titleText != null)
                titleText.text = "카드 정리";

            // 처음 채울 때는 트윈 없이 즉시 반영합니다.
            // 카드를 재사용하므로, 지난번에 버리기로 골랐던 칸은 기울어진 각도가
            // 남아 있어서, 트윈을 쓰면 화면이 뜨고 난 뒤에 천천히 바로서는 것이 보입니다.
            Refresh(animate: false);

            PlayDrawAsync(drawStartDelay).Forget();
        }

        // 기존 덱 + 이번에 제시된 새 카드로 화면에 늘어놓을 목록을 만듭니다.
        // Show와 갱신이 같은 경로를 쓰도록 뽑아냈습니다.
        private void BuildCards(IReadOnlyList<SkillCardDataTableRow> rewards)
        {
            _cards.Clear();
            _rewardIndexes.Clear();
            _newIndexes.Clear();

            // 카드가 통째로 바뀌므로 골라둔 '버릴 카드'는 의미를 잃습니다.
            _discardIndexes.Clear();

            if (_getCard != null)
            {
                foreach (var id in _deckCards)
                {
                    var row = _getCard.Invoke(id);

                    if (row != null)
                        _cards.Add(row);
                }
            }

            if (rewards == null)
                return;

            foreach (var row in rewards)
            {
                if (row == null)
                    continue;

                // 새로 받은 카드는 뒤에 붙이고 위치를 기억해 연출에 씁니다.
                _rewardIndexes.Add(_cards.Count);

                // NEW는 이번에 '처음 얻은' 카드에만 붙입니다.
                if (_isNewCard != null && _isNewCard.Invoke(row.Id))
                    _newIndexes.Add(_cards.Count);

                _cards.Add(row);
            }
        }

        // 제시된 새 카드만 다시 뽑습니다. 남은 횟수는 바깥이 셉니다.
        private void Reroll()
        {
            // 연출 중에는 누를 수 없습니다(뽑히는 중에 목록이 바뀌면 인덱스가 어긋납니다).
            if (_isDrawing || _reroll == null)
                return;

            var rewards = _reroll.Invoke();

            // null이면 남은 횟수가 없다는 뜻입니다.
            if (rewards == null)
            {
                RefreshRerollButton();

                return;
            }

            BuildCards(rewards);

            _isDrawing = true;

            Refresh(animate: false);

            // 갱신은 이미 화면을 보고 있는 상태라, 처음처럼 뜸을 들일 이유가 없습니다.
            PlayDrawAsync(0f).Forget();
        }

        private void RefreshRerollButton()
        {
            var remain = _getRemainReroll != null ? _getRemainReroll.Invoke() : 0;

            if (rerollButtonText != null)
                rerollButtonText.text = $"갱신 {remain}";

            if (rerollButton != null)
            {
                // 연출 중에도 잠급니다. 남은 횟수가 0이면 계속 잠깁니다.
                rerollButton.interactable = remain > 0 && !_isDrawing;

                var image = rerollButton.GetComponent<Image>();

                if (image != null)
                    image.color = remain > 0 ? confirmReadyColor : confirmDisabledColor;
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        // 새로 받은 카드를 한 장씩 나타나게 합니다.
        private async UniTaskVoid PlayDrawAsync(float startDelay)
        {
            var order = new List<int>(_rewardIndexes);

            order.Sort();

            if (order.Count == 0)
            {
                _isDrawing = false;

                RefreshRerollButton();

                return;
            }

            RefreshRerollButton();

            // 먼저 감춰두고
            foreach (var index in order)
            {
                if (index < _items.Count)
                    _items[index].PrepareDraw();
            }

            // 잠깐 기다렸다가 한 장씩 나타냅니다.
            if (startDelay > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(startDelay), ignoreTimeScale: true);

            // 기다리는 사이에 화면이 닫혔을 수 있습니다.
            if (this == null || !gameObject.activeInHierarchy)
                return;

            foreach (var index in order)
            {
                if (index >= _items.Count)
                    continue;

                _items[index].PlayDraw(drawDuration);

                await UniTask.Delay(TimeSpan.FromSeconds(drawInterval), ignoreTimeScale: true);
            }

            // 마지막 카드가 다 나타날 때까지 기다린 뒤 조작을 엽니다.
            await UniTask.Delay(TimeSpan.FromSeconds(drawDuration), ignoreTimeScale: true);

            _isDrawing = false;

            RefreshRerollButton();
        }

        private void Toggle(int index)
        {
            // 연출 중에는 고를 수 없습니다.
            if (_isDrawing)
                return;

            if (_discardIndexes.Contains(index))
            {
                _discardIndexes.Remove(index);
            }
            else if (_discardIndexes.Count < RequiredDiscardCount)
            {
                _discardIndexes.Add(index);
            }
            else
            {
                // 이미 목표 장수만큼 골랐으면 더 고를 수 없습니다.
                return;
            }

            Refresh();
        }

        // animate: 선택 표시를 트윈으로 바꿀지 여부. 고를 때만 켭니다.
        private void Refresh(bool animate = true)
        {
            if (descText != null)
                descText.text = $"버릴 카드 {RequiredDiscardCount}장을 고르세요";

            EnsureItems(_cards.Count);

            for (var i = 0; i < _items.Count; i++)
            {
                if (i >= _cards.Count)
                {
                    _items[i].SetVisible(false);
                    continue;
                }

                var index = i;

                _items[i].SetVisible(true);
                _items[i].SetData(_cards[i], _newIndexes.Contains(i), _discardIndexes.Contains(i),
                    CountInDeck(_cards[i].Id), () => Toggle(index), animate);
            }

            RefreshConfirmText();
            RefreshRerollButton();
        }

        // 남길 장수 / 목표 장수. 목표를 넘으면 붉고 크게 보여줍니다.
        private void RefreshConfirmText()
        {
            if (confirmButtonText == null)
                return;

            var remain = RemainCount;
            var ready = remain <= _deckSize;

            // 크기는 그대로 두고 색만 바꿉니다(숫자가 커졌다 작아지면 흔들려 보입니다).
            confirmButtonText.text = ready
                ? $"<size=150%>{remain}</size> / {_deckSize}"
                : $"<color=#ff0000><size=150%>{remain}</size></color> / {_deckSize}";

            // 목표 장수에 맞춰야 넘어갈 수 있으므로 색으로도 알려줍니다.
            if (confirmButton != null)
            {
                var image = confirmButton.GetComponent<Image>();

                if (image != null)
                    image.color = ready ? confirmReadyColor : confirmDisabledColor;
            }
        }

        private void Confirm()
        {
            if (_isDrawing)
                return;

            // 목표 장수보다 많으면 넘어가지 않습니다.
            if (RemainCount > _deckSize)
                return;

            // 남길 카드만 모아 넘깁니다.
            var result = new List<string>();

            for (var i = 0; i < _cards.Count; i++)
            {
                if (_discardIndexes.Contains(i))
                    continue;

                result.Add(_cards[i].Id);
            }

            // 남긴 카드를 '얻어본 것'으로 기록하는 건 밖에서 합니다.
            // 화면은 고른 결과만 넘기고, 저장할지는 받는 쪽이 정합니다.

            var callback = _onConfirm;
            _onConfirm = null;

            Hide();

            callback?.Invoke(result);
        }

        // 같은 카드가 덱에 몇 장 남을지 셉니다(버리기로 고른 것은 뺍니다).
        private int CountInDeck(string id)
        {
            var count = 0;

            for (var i = 0; i < _cards.Count; i++)
            {
                if (_cards[i].Id != id || _discardIndexes.Contains(i))
                    continue;

                count++;
            }

            return count;
        }

        private void EnsureItems(int count)
        {
            if (itemPrefab == null || itemRoot == null)
                return;

            while (_items.Count < count)
                _items.Add(Instantiate(itemPrefab, itemRoot));
        }
    }
}

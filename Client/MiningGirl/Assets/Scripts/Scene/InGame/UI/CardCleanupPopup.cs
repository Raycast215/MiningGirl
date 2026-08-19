using System;
using System.Collections.Generic;
using Data;
using MainGame.Card;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MainGame.UI
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
        private TextMeshProUGUI countText;

        [SerializeField]
        private Button confirmButton;

        [SerializeField]
        private TextMeshProUGUI confirmButtonText;

        [SerializeField]
        [Tooltip("덜 고른 채로 진행하려 할 때 잠깐 보여주는 안내")]
        private TextMeshProUGUI warningText;

        [SerializeField]
        [Tooltip("안내가 보이는 시간(초)")]
        private float warningDuration = 1.5f;

        [Header("Colors")]
        [SerializeField]
        private Color confirmReadyColor = new Color(0.11f, 0.62f, 0.46f, 1f);

        [SerializeField]
        private Color confirmDisabledColor = new Color(0.35f, 0.35f, 0.33f, 1f);

        private readonly List<CardCleanupItemView> _items = new List<CardCleanupItemView>();

        // 화면에 늘어놓은 카드 전체(기존 덱 + 새 카드)
        private readonly List<SkillCardDataTableRow> _cards = new List<SkillCardDataTableRow>();

        // 새로 받은 카드의 위치(표시용)
        private readonly HashSet<int> _newIndexes = new HashSet<int>();

        // 버리기로 고른 카드의 위치
        private readonly HashSet<int> _discardIndexes = new HashSet<int>();

        private int _deckSize;
        private Action<List<string>> _onConfirm;

        // 버려야 하는 장수
        private int RequiredDiscardCount => Mathf.Max(0, _cards.Count - _deckSize);

        public void Init()
        {
            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveAllListeners();
                confirmButton.onClick.AddListener(Confirm);
            }

            Hide();
        }

        // deckCards: 현재 덱, rewards: 이번에 받은 카드, deckSize: 유지할 장수
        public void Show(IReadOnlyList<string> deckCards, IReadOnlyList<SkillCardDataTableRow> rewards,
            int deckSize, Action<List<string>> onConfirm)
        {
            _onConfirm = onConfirm;
            _deckSize = Mathf.Max(1, deckSize);

            _cards.Clear();
            _newIndexes.Clear();
            _discardIndexes.Clear();

            var table = Manager.DataTableManager.Instance?.SkillCardDataTable;

            if (deckCards != null && table != null)
            {
                foreach (var id in deckCards)
                {
                    var row = table.GetRow(id);

                    if (row != null)
                        _cards.Add(row);
                }
            }

            if (rewards != null)
            {
                foreach (var row in rewards)
                {
                    if (row == null)
                        continue;

                    // 새로 받은 카드는 뒤에 붙이고 위치를 기억해 표시에 씁니다.
                    _newIndexes.Add(_cards.Count);
                    _cards.Add(row);
                }
            }

            gameObject.SetActive(true);

            HideWarning();

            if (titleText != null)
                titleText.text = "카드 정리";

            Refresh();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void Toggle(int index)
        {
            if (_discardIndexes.Contains(index))
                _discardIndexes.Remove(index);
            else if (_discardIndexes.Count < RequiredDiscardCount)
                _discardIndexes.Add(index);
            else
                return;

            Refresh();
        }

        private void Refresh()
        {
            var required = RequiredDiscardCount;

            if (descText != null)
                descText.text = $"버릴 카드 {required}장을 고르세요";

            if (countText != null)
                countText.text = $"선택 {_discardIndexes.Count} / {required}";

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
                    () => Toggle(index));
            }

            // 필요한 만큼 다 골라야 넘어갈 수 있습니다.
            var ready = _discardIndexes.Count >= required;

            if (confirmButton != null)
            {
                // 버튼은 항상 누를 수 있게 둡니다. 비활성 버튼은 왜 못 누르는지 알려주지 못하니
                // 눌렀을 때 안내를 띄우는 편이 낫습니다.
                confirmButton.interactable = true;

                var image = confirmButton.GetComponent<Image>();

                if (image != null)
                    image.color = ready ? confirmReadyColor : confirmDisabledColor;
            }

            if (confirmButtonText != null)
                confirmButtonText.text = ready ? "다음 스테이지" : $"{required - _discardIndexes.Count}장 더 선택";
        }

        private void Confirm()
        {
            var required = RequiredDiscardCount;

            if (_discardIndexes.Count < required)
            {
                ShowWarning($"버릴 카드를 {required - _discardIndexes.Count}장 더 골라주세요");

                return;
            }

            // 남길 카드만 모아 넘깁니다.
            var result = new List<string>();

            for (var i = 0; i < _cards.Count; i++)
            {
                if (_discardIndexes.Contains(i))
                    continue;

                result.Add(_cards[i].Id);
            }

            var callback = _onConfirm;
            _onConfirm = null;

            Hide();

            callback?.Invoke(result);
        }

        // 안내를 잠깐 띄웠다가 지웁니다.
        private void ShowWarning(string message)
        {
            if (warningText == null)
                return;

            warningText.text = message;
            warningText.gameObject.SetActive(true);

            CancelInvoke(nameof(HideWarning));
            Invoke(nameof(HideWarning), warningDuration);
        }

        private void HideWarning()
        {
            if (warningText != null)
                warningText.gameObject.SetActive(false);
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

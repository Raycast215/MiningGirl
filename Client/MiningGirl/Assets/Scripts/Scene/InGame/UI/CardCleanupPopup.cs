using System;
using System.Collections.Generic;
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
        private TextMeshProUGUI countText;

        [SerializeField]
        [Tooltip("고른 카드를 한 번에 되돌립니다")]
        private Button resetButton;

        [SerializeField]
        private Button confirmButton;

        [SerializeField]
        private TextMeshProUGUI confirmButtonText;

        [Header("Detail")]
        [SerializeField]
        [Tooltip("누른 카드의 아이콘")]
        private Image detailIcon;

        [SerializeField]
        [Tooltip("아이콘 뒤 원판(카테고리 색이 들어갑니다)")]
        private Image detailIconBase;

        [SerializeField]
        [Tooltip("누른 카드의 이름")]
        private TextMeshProUGUI detailNameText;
        [SerializeField]
        [Tooltip("카테고리 심볼(인게임 카드와 같은 아이콘)")]
        private Image detailTypeSymbol;

        [SerializeField]
        [Tooltip("Attack / Assist / Support 순서로 넣습니다")]
        private Sprite[] categorySymbols;


        [SerializeField]
        [Tooltip("카테고리와 코스트")]
        private TextMeshProUGUI detailTagText;

        [SerializeField]
        [Tooltip("효과 설명")]
        private TextMeshProUGUI detailDescText;

        [SerializeField]
        [Tooltip("덱에 몇 장 있는지")]
        private TextMeshProUGUI detailCountText;

        [SerializeField]
        [Tooltip("아직 아무 카드도 안 눌렀을 때 보여주는 안내")]
        private GameObject detailEmpty;

        [SerializeField]
        [Tooltip("카드를 눌렀을 때 켜지는 상세 묶음")]
        private GameObject detailRoot;

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

        // 마지막으로 눌러 설명을 보고 있는 카드. -1이면 아직 아무것도 안 누른 상태입니다.
        private int _focusIndex = -1;

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

            if (resetButton != null)
            {
                resetButton.onClick.RemoveAllListeners();
                resetButton.onClick.AddListener(ResetSelection);
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
            _focusIndex = -1;

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

        // 고른 것을 전부 해제합니다. 하나씩 다시 누르지 않아도 되도록.
        private void ResetSelection()
        {
            if (_discardIndexes.Count == 0)
                return;

            _discardIndexes.Clear();

            HideWarning();
            Refresh();
        }

        private void Toggle(int index)
        {
            // 누른 카드의 설명을 아래에 띄웁니다(선택 여부와 별개).
            _focusIndex = index;

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
                // 이미 다 골랐으면 선택은 늘리지 않지만, 설명은 보여줘야 합니다.
                // (여기서 그냥 빠져나가면 다른 카드를 눌러도 설명이 안 바뀝니다.)
                ShowWarning($"버릴 카드는 {RequiredDiscardCount}장까지입니다");
            }

            Refresh();
        }

        private void Refresh()
        {
            var required = RequiredDiscardCount;

            if (descText != null)
                descText.text = $"버릴 카드 {required}장을 고르세요";

            if (countText != null)
                countText.text = $"{_discardIndexes.Count} / {required}";

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
                    i == _focusIndex, () => Toggle(index));
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

            RefreshDetail();
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

        // 누른 카드의 설명을 하단에 표시합니다.
        private void RefreshDetail()
        {
            var hasFocus = _focusIndex >= 0 && _focusIndex < _cards.Count;

            if (detailEmpty != null)
                detailEmpty.SetActive(!hasFocus);

            if (detailRoot != null)
                detailRoot.SetActive(hasFocus);

            if (!hasFocus)
                return;

            var row = _cards[_focusIndex];

            // 아이콘과 카테고리 색을 카드와 똑같이 맞춥니다.
            if (detailIconBase != null)
            {
                var color = GetCategoryColor(row.SkillCategoryType);

                detailIconBase.color = new Color(color.r, color.g, color.b, 0.45f);
            }

            if (detailIcon != null)
            {
                var sprite = string.IsNullOrEmpty(row.AssetId)
                    ? null
                    : Resources.Load<Sprite>($"Icon/{row.AssetId}");

                detailIcon.sprite = sprite;
                detailIcon.enabled = sprite != null;
            }

            if (detailNameText != null)
                detailNameText.text = row.Name;

            // 인게임 카드에 붙는 것과 같은 카테고리 심볼을 보여줍니다.
            if (detailTypeSymbol != null)
            {
                var index = (int)row.SkillCategoryType;
                var sprite = categorySymbols != null && index >= 0 && index < categorySymbols.Length
                    ? categorySymbols[index]
                    : null;

                detailTypeSymbol.sprite = sprite;
                detailTypeSymbol.enabled = sprite != null;
            }

            if (detailTagText != null)
                detailTagText.text = $"{GetCategoryName(row.SkillCategoryType)}   코스트 {row.Cost}";

            if (detailDescText != null)
                detailDescText.text = BuildDesc(row);

            // 같은 카드가 덱에 몇 장인지 셉니다.
            // 카드가 여러 칸으로 흩어져 있어 직접 세지 않으면 알기 어렵습니다.
            if (detailCountText != null)
            {
                var count = 0;

                for (var i = 0; i < _cards.Count; i++)
                {
                    if (_cards[i].Id != row.Id || _discardIndexes.Contains(i))
                        continue;

                    count++;
                }

                detailCountText.text = $"덱에 {count}장";
            }
        }

        // 카드 뷰와 같은 기준의 카테고리 색입니다.
        private static Color GetCategoryColor(ESkillCategoryType type)
        {
            return type switch
            {
                ESkillCategoryType.Attack => new Color(0.11f, 0.62f, 0.46f, 1f),
                ESkillCategoryType.Assist => new Color(0.93f, 0.62f, 0.15f, 1f),
                _ => new Color(0.22f, 0.54f, 0.87f, 1f),
            };
        }

        private static string GetCategoryName(ESkillCategoryType type)
        {
            return type switch
            {
                ESkillCategoryType.Attack => "공격",
                ESkillCategoryType.Assist => "보조",
                _ => "서포트",
            };
        }

        // 설명은 '{0} 데미지로 공격' 같은 서식이라 실제 값을 채워 보여줍니다.
        private static string BuildDesc(SkillCardDataTableRow row)
        {
            if (string.IsNullOrEmpty(row.Desc))
                return string.Empty;

            try
            {
                return string.Format(row.Desc, row.EffectValue, row.DurationTime, row.EffectRange);
            }
            catch
            {
                return row.Desc;
            }
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

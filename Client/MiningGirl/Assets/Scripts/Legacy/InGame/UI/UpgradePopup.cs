using System;
using System.Collections.Generic;
using Data;
using Legacy.MainGame.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Legacy.Scene.InGame.UI
{
    // 스테이지가 끝날 때(클리어·실패 모두) 뜨는 강화 팝업.
    // 처치·채굴로 번 골드를 여기서 씁니다. 경험치·레벨을 대신하는 성장 창구입니다.
    public class UpgradePopup : MonoBehaviour
    {
        [SerializeField]
        private UpgradeItemView itemPrefab;
        [SerializeField]
        private RectTransform itemRoot;

        [SerializeField]
        private TextMeshProUGUI goldText;
        [SerializeField]
        private TextMeshProUGUI titleText;
        [SerializeField]
        private TextMeshProUGUI descText;
        [SerializeField]
        private TextMeshProUGUI emptyText;
        [SerializeField]
        private Button closeButton;
        [SerializeField]
        private TextMeshProUGUI closeButtonText;

        [Header("Tabs")]
        [SerializeField]
        private Button[] tabButtons;
        [SerializeField]
        private Color selectedTabColor = new Color(0.11f, 0.62f, 0.46f, 1f);
        [SerializeField]
        private Color normalTabColor = new Color(0.28f, 0.28f, 0.26f, 1f);

        private readonly List<UpgradeItemView> _items = new List<UpgradeItemView>();
        private readonly List<LevelUpBonusSkillDataTableRow> _rows = new List<LevelUpBonusSkillDataTableRow>();

        private EUpgradeTabType _tab = EUpgradeTabType.Character;
        private int _stage;
        private bool _isCleared;

        // 바깥에서 주입: 보유 골드 조회 / 골드 소모 / 현재 레벨 조회 / 구매 적용 / 닫기
        private Func<int> _getGold;
        private Func<int, bool> _trySpendGold;
        private Func<LevelUpBonusSkillDataTableRow, int> _getLevel;
        private Action<LevelUpBonusSkillDataTableRow> _onPurchase;
        private Action _onClose;

        // 강화 항목 목록을 밖에서 받습니다.
        // (예전에는 팝업이 DataTableManager를 직접 뒤졌습니다.)
        private Func<IReadOnlyList<LevelUpBonusSkillDataTableRow>> _getAllRows;

        // 닫기를 누른 뒤에도 팝업은 잠시 떠 있습니다.
        // 그 동안 강화를 더 사거나 탭을 바꾸지 못하게 입력만 잠가는 용도입니다.
        private CanvasGroup _canvasGroup;

        public void Init(Func<int> getGold, Func<int, bool> trySpendGold,
            Func<LevelUpBonusSkillDataTableRow, int> getLevel,
            Action<LevelUpBonusSkillDataTableRow> onPurchase,
            Func<IReadOnlyList<LevelUpBonusSkillDataTableRow>> getAllRows = null)
        {
            _getAllRows = getAllRows;
            _getGold = getGold;
            _trySpendGold = trySpendGold;
            _getLevel = getLevel;
            _onPurchase = onPurchase;

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(Close);
            }

            BindTabs();
            Hide();
        }

        private void BindTabs()
        {
            if (tabButtons == null)
                return;

            for (var i = 0; i < tabButtons.Length; i++)
            {
                if (tabButtons[i] == null)
                    continue;

                var tab = (EUpgradeTabType)i;

                tabButtons[i].onClick.RemoveAllListeners();
                tabButtons[i].onClick.AddListener(() => SelectTab(tab));
            }
        }

        // isCleared가 false면 실패 후 재도전입니다(강화는 양쪽 모두 가능).
        public void Show(int stage, bool isCleared, Action onClose)
        {
            _stage = stage;
            _isCleared = isCleared;
            _onClose = onClose;
            _tab = EUpgradeTabType.Character;

            gameObject.SetActive(true);

            SetInputEnabled(true);

            // 클리어/실패와 획득 골드는 앞의 결과 창(StageResultPopup)이 알려줍니다.
            // 여기는 강화라는 목적만 남깁니다.
            if (titleText != null)
                titleText.text = "업그레이드";

            if (descText != null)
                descText.text = "골드를 사용해 능력을 강화하세요";

            if (closeButtonText != null)
                // '다시 도전'은 졌다는 뉘앙스라 이어서 한다는 표현으로 바꿉니다.
                closeButtonText.text = isCleared ? "다음 스테이지" : "계속 채굴";

            Refresh();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        // 닫기를 누르면 바로 사라지지 않고, 다음 흐름부터 진행합니다.
        // 먼저 닫으면 인게임이 잠깐 드러났다가 다음 화면(맵 연출·화면 덮개)이
        // 덮는 모양이 됩니다. 화면이 가려진 뒤에 바깥에서 Hide를 부릅니다.
        // (InGameController.PrepareNextStage 참고)
        private void Close()
        {
            var callback = _onClose;
            _onClose = null;

            // 더 사거나 닫기를 한 번 더 누르지 못하게 입력만 잠가 둡니다.
            SetInputEnabled(false);

            callback?.Invoke();
        }

        // 보이는 것은 그대로 두고 입력만 여닫습니다.
        private void SetInputEnabled(bool value)
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();

                if (_canvasGroup == null)
                    _canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            _canvasGroup.interactable = value;
        }

        private void SelectTab(EUpgradeTabType tab)
        {
            _tab = tab;

            Refresh();
        }

        // 살 수 있는 항목이 하나도 없으면 바깥에서 팝업을 건너뛸 수 있게 알려줍니다.
        public bool HasAnyAffordable(int stage)
        {
            // 표시용 _rows와 섞이지 않도록 여기서는 전체 목록을 다시 받아 훑습니다.
            var rows = _getAllRows?.Invoke();

            if (rows == null)
                return false;

            var gold = _getGold?.Invoke() ?? 0;

            foreach (var row in rows)
            {
                if (row == null)
                    continue;

                // 해금 판정은 이번에 끝난 스테이지 기준입니다.
                if (row.UnlockStage > 0 && stage < row.UnlockStage)
                    continue;

                var level = (_getLevel?.Invoke(row) ?? 0) + 1;

                if (row.MaxLevel >= 0 && level > row.MaxLevel)
                    continue;

                if (gold >= row.GetPrice(level))
                    return true;
            }

            return false;
        }

        // tab이 null이면 모든 탭에서 모읍니다(구매 가능 여부 판단용).
        // tab이 null이면 모든 탭에서 모읍니다(구매 가능 여부 판단용).
        private void CollectRows(EUpgradeTabType? tab)
        {
            _rows.Clear();

            var rows = _getAllRows?.Invoke();

            if (rows == null)
                return;

            foreach (var row in rows)
            {
                if (row == null)
                    continue;

                if (tab.HasValue && row.TabType != tab.Value)
                    continue;

                // 아직 해금되지 않은 항목은 숨깁니다.
                if (row.UnlockStage > 0 && _stage < row.UnlockStage)
                    continue;

                _rows.Add(row);
            }
        }

        private void Refresh()
        {
            CollectRows(_tab);
            RefreshTabColors();

            if (goldText != null)
                goldText.text = $"보유 골드 {_getGold?.Invoke() ?? 0}";

            var gold = _getGold?.Invoke() ?? 0;

            EnsureItems(_rows.Count);

            for (var i = 0; i < _items.Count; i++)
            {
                if (i >= _rows.Count)
                {
                    _items[i].SetVisible(false);
                    continue;
                }

                var row = _rows[i];
                var level = (_getLevel?.Invoke(row) ?? 0) + 1;

                // 가격·최대레벨·구매가능 판정은 여기서 한 번만 합니다.
                // 뷰는 결과 문자열만 받아 그립니다.
                var isMax = row.MaxLevel >= 0 && level > row.MaxLevel;
                var price = row.GetPrice(level);
                var canBuy = !isMax && gold >= price;

                var detail = isMax
                    ? $"Lv.{row.MaxLevel} / {row.MaxLevel} (최대)"
                    : $"{BuildDetail(row, level)}   Lv.{level} / {(row.MaxLevel < 0 ? "-" : row.MaxLevel.ToString())}";

                _items[i].SetVisible(true);
                _items[i].SetData(row.Name, detail, isMax ? "최대" : $"{price} 골드", canBuy, () => Buy(row));
            }

            if (emptyText != null)
            {
                emptyText.gameObject.SetActive(_rows.Count == 0);

                // 모든 탭에 항목이 있으므로, 비어 보이는 이유는 해금 대기뿐입니다.
                emptyText.text = "아직 열리지 않은 항목입니다";
            }
        }

        // '지금 값 → 다음 값'을 만들어 줍니다. 값이 없으면 효과만 적습니다.
        private string BuildDetail(LevelUpBonusSkillDataTableRow row, int level)
        {
            var current = (level - 1) * row.EffectValue;
            var next = level * row.EffectValue;

            string Format(float v)
            {
                return Mathf.Approximately(v, Mathf.Round(v))
                    ? Mathf.RoundToInt(v).ToString()
                    : v.ToString("0.##");
            }

            return $"+{Format(current)} → +{Format(next)}";
        }

        private void Buy(LevelUpBonusSkillDataTableRow row)
        {
            if (row == null)
                return;

            var level = (_getLevel?.Invoke(row) ?? 0) + 1;

            if (row.MaxLevel >= 0 && level > row.MaxLevel)
                return;

            var price = row.GetPrice(level);

            // 골드를 실제로 냈을 때만 강화가 적용됩니다.
            if (_trySpendGold == null || !_trySpendGold.Invoke(price))
                return;

            _onPurchase?.Invoke(row);

            Refresh();
        }

        private void EnsureItems(int count)
        {
            if (itemPrefab == null || itemRoot == null)
                return;

            while (_items.Count < count)
                _items.Add(Instantiate(itemPrefab, itemRoot));
        }

        private void RefreshTabColors()
        {
            if (tabButtons == null)
                return;

            for (var i = 0; i < tabButtons.Length; i++)
            {
                if (tabButtons[i] == null)
                    continue;

                var image = tabButtons[i].GetComponent<Image>();

                if (image != null)
                    image.color = (EUpgradeTabType)i == _tab ? selectedTabColor : normalTabColor;
            }
        }
    }
}

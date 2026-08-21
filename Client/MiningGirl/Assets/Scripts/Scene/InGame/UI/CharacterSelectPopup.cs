using System;
using System.Collections.Generic;
using Data;
using TMPro;
using UnityEngine;

namespace MainGame.UI
{
    // 게임 시작 시 캐릭터를 고르는 팝업.
    //
    // 후보 목록과 강화 항목 조회는 밖에서 주입받습니다.
    // (예전에는 팝업과 슬롯 뷰가 각자 DataTableManager를 직접 뒤졌습니다.)
    public class CharacterSelectPopup : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI titleText;

        [Header("Slots")]
        [SerializeField]
        [Tooltip("캐릭터 선택 항목 프리팹")]
        private CharacterSelectSlotView slotPrefab;
        [SerializeField]
        [Tooltip("선택 항목이 생성될 부모")]
        private Transform slotRoot;

        [Header("Display")]
        [SerializeField]
        [Tooltip("표시할 캐릭터 이름 (데이터 순서대로 대응)")]
        private string[] characterNames = { "밸런스형", "치명타형", "추가타형" };

        private Action<CharacterStatDataRow> _onSelected;
        private readonly List<CharacterStatDataRow> _rows = new List<CharacterStatDataRow>();
        private readonly List<CharacterSelectSlotView> _slots = new List<CharacterSelectSlotView>();

        private Func<IReadOnlyList<CharacterStatDataRow>> _getCharacters;
        private Func<ELevelUpBonusEffectType, LevelUpBonusSkillDataTableRow> _findBonusRow;

        public void Init(Func<IReadOnlyList<CharacterStatDataRow>> getCharacters,
            Func<ELevelUpBonusEffectType, LevelUpBonusSkillDataTableRow> findBonusRow)
        {
            _getCharacters = getCharacters;
            _findBonusRow = findBonusRow;
        }

        // 팝업을 띄웁니다. 선택하면 onSelected로 해당 캐릭터 데이터가 전달됩니다.
        public void Show(Action<CharacterStatDataRow> onSelected)
        {
            _onSelected = onSelected;

            if (titleText != null)
                titleText.text = "캐릭터 선택";

            _rows.Clear();

            var rows = _getCharacters?.Invoke();

            if (rows != null)
                _rows.AddRange(rows);

            if (_rows.Count == 0)
                Debug.LogError("[CharacterSelect] 캐릭터 스탯 데이터가 없습니다.");

            BuildSlots();

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void BuildSlots()
        {
            if (slotPrefab == null || slotRoot == null)
            {
                Debug.LogError("[CharacterSelect] 슬롯 프리팹 또는 부모가 연결되지 않았습니다.");
                return;
            }

            while (_slots.Count < _rows.Count)
                _slots.Add(Instantiate(slotPrefab, slotRoot));

            for (var i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];

                if (i >= _rows.Count)
                {
                    slot.SetVisible(false);
                    continue;
                }

                var row = _rows[i];
                var index = i;
                var displayName = characterNames != null && i < characterNames.Length
                    ? characterNames[i]
                    : $"캐릭터 {i + 1}";

                slot.SetVisible(true);
                slot.SetData(row, displayName, BuildStartSkills(row), () => Select(index));
            }
        }

        // 캐릭터가 미리 들고 시작하는 스킬을 (이름, 레벨)로 정리합니다.
        // 같은 타입이 여러 번 들어 있으면 그 개수가 곧 시작 레벨입니다.
        // 시트에 최대 레벨보다 많이 적혀 있어도 실제 부여는 최대 레벨까지이므로 여기서 잘라줍니다.
        private List<(string name, int level)> BuildStartSkills(CharacterStatDataRow row)
        {
            var result = new List<(string, int)>();
            var types = row?.StartSkillTypeList;

            if (types == null || types.Count == 0 || _findBonusRow == null)
                return result;

            // 등장 순서를 유지하면서 타입별 개수를 셉니다.
            var order = new List<ELevelUpBonusEffectType>();
            var counts = new Dictionary<ELevelUpBonusEffectType, int>();

            foreach (var type in types)
            {
                if (counts.ContainsKey(type))
                {
                    counts[type]++;
                    continue;
                }

                counts[type] = 1;
                order.Add(type);
            }

            foreach (var type in order)
            {
                var found = _findBonusRow.Invoke(type);

                if (found == null)
                    continue;

                var level = counts[type];

                if (found.MaxLevel >= 0)
                    level = Mathf.Min(level, found.MaxLevel);

                result.Add((found.Name, level));
            }

            return result;
        }

        private void Select(int index)
        {
            if (index < 0 || index >= _rows.Count)
                return;

            var row = _rows[index];
            Debug.Log($"[CharacterSelect] 선택됨 — Id={row.Id}");

            var callback = _onSelected;
            _onSelected = null;

            Hide();

            callback?.Invoke(row);
        }
    }
}

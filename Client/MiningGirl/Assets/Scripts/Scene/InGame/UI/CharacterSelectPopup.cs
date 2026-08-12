using System;
using System.Collections.Generic;
using Data;
using Manager;
using TMPro;
using UnityEngine;

namespace MainGame.UI
{
    // 게임 시작 시 캐릭터를 고르는 팝업.
    // 후보는 CharacterStatDataTable의 행을 그대로 사용합니다.
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

        // 팝업을 띄웁니다. 선택하면 onSelected로 해당 캐릭터 데이터가 전달됩니다.
        public void Show(Action<CharacterStatDataRow> onSelected)
        {
            _onSelected = onSelected;

            if (titleText != null)
                titleText.text = "캐릭터 선택";

            _rows.Clear();

            var table = DataTableManager.Instance?.CharacterStatDataTable;
            if (table?.Rows != null)
                _rows.AddRange(table.Rows);

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
                slot.SetData(row, displayName, () => Select(index));
            }
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

using System;
using System.Collections.Generic;
using Data;
using MainGame.Bonus;
using TMPro;
using UnityEngine;

namespace MainGame.UI
{
    // 레벨업 시 보너스를 선택하는 팝업.
    // 선택 항목은 프리팹(LevelUpBonusSlotView)을 필요한 개수만큼 생성해서 재사용합니다.
    public class LevelUpBonusSelectPopup : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI levelText;

        [Header("Slots")]
        [SerializeField]
        [Tooltip("선택 항목 프리팹")]
        private LevelUpBonusSlotView slotPrefab;
        [SerializeField]
        [Tooltip("선택 항목이 생성될 부모")]
        private Transform slotRoot;
        [SerializeField]
        [Tooltip("한 번에 제시할 후보 수")]
        private int slotCount = 3;

        private Action<LevelUpBonusSkillDataTableRow> _onSelected;
        private readonly List<LevelUpBonusSkillDataTableRow> _current = new List<LevelUpBonusSkillDataTableRow>();
        private readonly List<LevelUpBonusSlotView> _slots = new List<LevelUpBonusSlotView>();

        // 팝업을 띄웁니다. 후보는 현재 획득 상태를 기준으로 뽑습니다.
        public void Show(int level, LevelUpBonusState state, Action<LevelUpBonusSkillDataTableRow> onSelected)
        {
            _onSelected = onSelected;

            if (levelText != null)
                levelText.text = $"Lv.{level} 보너스 선택";

            _current.Clear();
            _current.AddRange(LevelUpBonusPicker.Pick(state, slotCount));

            BuildSlots(state);

            gameObject.SetActive(true);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void BuildSlots(LevelUpBonusState state)
        {
            if (slotPrefab == null || slotRoot == null)
            {
                Debug.LogError("[LevelUpBonus] 슬롯 프리팹 또는 부모가 연결되지 않았습니다.");
                return;
            }

            // 부족한 만큼만 새로 만들고, 나머지는 재사용합니다.
            while (_slots.Count < _current.Count)
                _slots.Add(Instantiate(slotPrefab, slotRoot));

            for (var i = 0; i < _slots.Count; i++)
            {
                var slot = _slots[i];

                if (i >= _current.Count)
                {
                    slot.SetVisible(false);
                    continue;
                }

                var row = _current[i];
                var index = i;

                slot.SetVisible(true);
                slot.SetData(row, state.GetLevel(row.Id), () => Select(index));
            }
        }

        private void Select(int index)
        {
            if (index < 0 || index >= _current.Count)
                return;

            var row = _current[index];
            Debug.Log($"[LevelUpBonus] 선택됨 — Id={row.Id} ({row.Name})");

            var callback = _onSelected;
            _onSelected = null;

            Hide();

            callback?.Invoke(row);
        }
    }
}

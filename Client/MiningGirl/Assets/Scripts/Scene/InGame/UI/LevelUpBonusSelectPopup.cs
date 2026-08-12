using System;
using Cysharp.Threading.Tasks;
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

        [Header("Input")]
        [SerializeField]
        [Tooltip("팝업이 뜬 직후 이 시간(초) 동안 클릭을 막습니다. 몬스터를 연타하다 레벨업하면 그 탭이 버튼으로 들어가는 것을 방지합니다.")]
        private float inputBlockDuration = 0.4f;
        [SerializeField]
        private CanvasGroup canvasGroup;

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

            BlockInputTemporarily().Forget();
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

        // 팝업이 뜬 직후 잠깐 클릭을 막습니다.
        private async UniTaskVoid BlockInputTemporarily()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (canvasGroup == null || inputBlockDuration <= 0f)
                return;

            canvasGroup.interactable = false;

            try
            {
                await UniTask.WaitForSeconds(inputBlockDuration, cancellationToken: this.GetCancellationTokenOnDestroy());
            }
            catch (Exception _)
            {
                return;
            }

            canvasGroup.interactable = true;
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

using System;
using Data;
using MainGame.Bonus;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MainGame.UI
{
    // 레벨업 보너스 선택 항목 하나를 표시하는 뷰.
    // 팝업이 이 프리팹을 필요한 개수만큼 생성해서 사용합니다.
    public class LevelUpBonusSlotView : MonoBehaviour
    {
        [SerializeField]
        private Button button;
        [SerializeField]
        private TextMeshProUGUI titleText;
        [SerializeField]
        private TextMeshProUGUI levelText;
        [SerializeField]
        private TextMeshProUGUI descText;

        [Header("Colors")]
        [SerializeField]
        private Color levelColor = new Color(0.62f, 0.68f, 0.80f, 1f);
        [SerializeField]
        [Tooltip("다음 획득으로 최대 레벨에 도달할 때 표시할 색")]
        private Color maxLevelColor = new Color(0.98f, 0.78f, 0.35f, 1f);

        private Action _onClick;

        private void Awake()
        {
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => _onClick?.Invoke());
            }
        }

        // row: 표시할 스킬 데이터 / currentLevel: 지금까지 획득한 횟수
        public void SetData(LevelUpBonusSkillDataTableRow row, int currentLevel, Action onClick)
        {
            _onClick = onClick;

            if (row == null)
                return;

            if (titleText != null)
                titleText.text = row.Name;

            if (descText != null)
                descText.text = LevelUpBonusPicker.BuildDescription(row);

            if (levelText != null)
            {
                // 최대 레벨이 없는(-1) 즉시 효과 스킬은 레벨 개념이 없어 표시하지 않습니다.
                if (row.MaxLevel < 0)
                {
                    levelText.text = string.Empty;
                }
                else
                {
                    // 이번에 획득하면 도달할 레벨을 표시합니다(1부터 시작).
                    var nextLevel = currentLevel + 1;
                    levelText.text = $"Lv.{nextLevel} / {row.MaxLevel}";

                    // 이번에 선택하면 최대 레벨에 도달하는 경우 강조합니다.
                    levelText.color = nextLevel >= row.MaxLevel ? maxLevelColor : levelColor;
                }
            }
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}

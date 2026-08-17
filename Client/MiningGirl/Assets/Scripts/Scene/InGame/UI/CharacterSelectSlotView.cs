using System;
using System.Collections.Generic;
using System.Text;
using Data;
using Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MainGame.UI
{
    // 캐릭터 선택 항목 하나를 표시하는 뷰.
    // 팝업이 이 프리팹을 데이터 수만큼 생성해서 사용합니다.
    public class CharacterSelectSlotView : MonoBehaviour
    {
        [SerializeField]
        private Button button;
        [SerializeField]
        private TextMeshProUGUI nameText;
        [SerializeField]
        private TextMeshProUGUI statText;

        [Header("Colors")]
        [SerializeField]
        private Color valueColor = new Color(0.30f, 0.85f, 0.39f, 1f);

        private Action _onClick;

        private void Awake()
        {
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => _onClick?.Invoke());
            }
        }

        public void SetData(CharacterStatDataRow row, string displayName, Action onClick)
        {
            _onClick = onClick;

            if (row == null)
                return;

            if (nameText != null)
                nameText.text = displayName;

            if (statText != null)
                statText.text = BuildStatText(row);
        }

        // 주요 스탯을 한눈에 비교할 수 있게 정리합니다. 수치는 색으로 강조합니다.
        private string BuildStatText(CharacterStatDataRow row)
        {
            var hex = ColorUtility.ToHtmlStringRGB(valueColor);

            string V(float value, string suffix = "")
            {
                var text = Mathf.Approximately(value, Mathf.Round(value))
                    ? Mathf.RoundToInt(value).ToString()
                    : value.ToString("0.##");

                return $"<color=#{hex}>{text}{suffix}</color>";
            }

            // 치명타 데미지는 '추가 배율'이라 +표기로 따로 보여줍니다.
            // (확률 옆에 괄호로 붙이면 확률이 150%인 것처럼 읽혀서 분리했습니다.)
            // 세로로 긴 카드형 슬롯이라 항목마다 한 줄씩 내려 적습니다.
            // 가로로 붙여 쓰면 좁은 폭에서 줄바꿈이 제멋대로 일어나 읽기 어렵습니다.
            var lines = new List<string>
            {
                $"채굴 데미지 {V(row.Damage)}",
                $"채굴 주기 {V(row.AttackDelay, "초")}",
                $"이동속도 {V(row.MoveSpeed)}",
                $"치명타 확률 {V(row.CriRate, "%")}",
                $"치명타 데미지 +{V(Mathf.RoundToInt(row.CriDamage * 100f), "%")}",
                $"추가타 확률 {V(row.ExtraHitRate, "%")}",
                $"체력 {V(row.MaxHealth)}",
                $"피격 무적 시간 {V(row.InvincibleDuration, "초")}",
            };

            return string.Join("\n", lines) + BuildStartSkillText(row, hex);
        }

        // 캐릭터가 미리 들고 시작하는 스킬을 '이름 Lv.N' 형태로 보여줍니다.
        // 같은 타입이 여러 번 들어 있으면 그 개수가 곧 시작 레벨입니다.
        private string BuildStartSkillText(CharacterStatDataRow row, string hex)
        {
            var types = row?.StartSkillTypeList;
            if (types == null || types.Count == 0)
                return string.Empty;

            var table = DataTableManager.Instance?.LevelUpBonusSkillDataTable;
            if (table?.Rows == null)
                return string.Empty;

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

            var text = new StringBuilder();

            foreach (var type in order)
            {
                LevelUpBonusSkillDataTableRow found = null;

                foreach (var skill in table.Rows)
                {
                    if (skill.EffectType != type)
                        continue;

                    found = skill;
                    break;
                }

                if (found == null)
                    continue;

                if (text.Length > 0)
                    text.Append("\n");

                // 시트에 최대 레벨보다 많이 적혀 있어도 실제 부여는 최대 레벨까지만 되므로
                // 표시도 최대 레벨을 넘지 않게 잘라줍니다.
                var level = counts[type];
                if (found.MaxLevel >= 0)
                    level = Mathf.Min(level, found.MaxLevel);

                // 스킬 이름과 레벨을 통째로 강조합니다.
                text.Append($"<color=#{hex}>{found.Name} Lv.{level}</color>");
            }

            if (text.Length == 0)
                return string.Empty;

            return $"\n\n시작 스킬\n{text}";
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}

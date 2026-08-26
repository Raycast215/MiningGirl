using System;
using System.Collections.Generic;
using System.Text;
using Data;
using Manager;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Legacy.MainGame.UI
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

        // startSkills: 팝업이 계산해 넘겨준 (스킬 이름, 시작 레벨) 목록.
        // 예전에는 이 뷰가 LevelUpBonusSkillDataTable을 직접 뒤지고
        // 최대 레벨 clamp 규칙까지 여기서 다시 구현했습니다.
        public void SetData(CharacterStatDataRow row, string displayName,
            IReadOnlyList<(string name, int level)> startSkills, Action onClick)
        {
            _onClick = onClick;

            if (row == null)
                return;

            if (nameText != null)
                nameText.text = displayName;

            if (statText != null)
                statText.text = BuildStatText(row, startSkills);
        }

        // 주요 스탯을 한눈에 비교할 수 있게 정리합니다. 수치는 색으로 강조합니다.
        private string BuildStatText(CharacterStatDataRow row, IReadOnlyList<(string name, int level)> startSkills)
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

                $"피격 무적 시간 {V(row.InvincibleDuration, "초")}",
            };

            return string.Join("\n", lines) + BuildStartSkillText(startSkills, hex);
        }

        // 받은 목록을 '이름 Lv.N' 형태로 늘어놓기만 합니다.
        private string BuildStartSkillText(IReadOnlyList<(string name, int level)> startSkills, string hex)
        {
            if (startSkills == null || startSkills.Count == 0)
                return string.Empty;

            var text = new StringBuilder();

            foreach (var skill in startSkills)
            {
                if (text.Length > 0)
                    text.Append("\n");

                text.Append($"<color=#{hex}>{skill.name} Lv.{skill.level}</color>");
            }

            return $"\n\n시작 스킬\n{text}";
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}

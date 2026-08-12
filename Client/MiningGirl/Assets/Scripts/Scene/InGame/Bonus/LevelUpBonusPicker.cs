using System.Collections.Generic;
using Data;
using Manager;
using UnityEngine;

namespace MainGame.Bonus
{
    // 레벨업 시 제시할 보너스 후보를 데이터 테이블에서 가중치로 뽑습니다.
    public static class LevelUpBonusPicker
    {
        // 최대 레벨에 도달하지 않은 스킬 중에서 중복 없이 count개를 가중치 기반으로 뽑습니다.
        public static List<LevelUpBonusSkillDataTableRow> Pick(LevelUpBonusState state, int count)
        {
            var result = new List<LevelUpBonusSkillDataTableRow>(count);

            var table = DataTableManager.Instance?.LevelUpBonusSkillDataTable;
            if (table?.Rows == null)
            {
                Debug.LogError("[LevelUpBonus] 데이터 테이블이 로드되지 않았습니다.");
                return result;
            }

            // 아직 더 획득할 수 있는 후보만 모읍니다.
            var candidates = new List<LevelUpBonusSkillDataTableRow>();
            foreach (var row in table.Rows)
            {
                if (row.Weight <= 0)
                    continue;

                if (!state.CanAcquire(row.Id, row.MaxLevel))
                    continue;

                candidates.Add(row);
            }

            for (var i = 0; i < count && candidates.Count > 0; i++)
            {
                var picked = PickWeighted(candidates);
                if (picked == null)
                    break;

                result.Add(picked);
                candidates.Remove(picked);
            }

            return result;
        }

        private static LevelUpBonusSkillDataTableRow PickWeighted(List<LevelUpBonusSkillDataTableRow> candidates)
        {
            var total = 0;
            foreach (var row in candidates)
                total += row.Weight;

            if (total <= 0)
                return null;

            var pick = Random.Range(0, total);
            var acc = 0;

            foreach (var row in candidates)
            {
                acc += row.Weight;

                if (pick < acc)
                    return row;
            }

            return candidates[candidates.Count - 1];
        }

        // 수치 강조 색 (TMP 리치 텍스트)
        private const string HighlightColorHex = "#4CD964";

        // 설명문의 {0}에 효과 값을 채웁니다.
        // Mul 타입은 비율이므로 퍼센트(0.1 -> 10)로 바꿔서 표시하고, 수치는 초록색으로 강조합니다.
        public static string BuildDescription(LevelUpBonusSkillDataTableRow row)
        {
            if (row == null)
                return string.Empty;

            var value = row.ValueType == EEffectValueType.Mul
                ? Mathf.RoundToInt(row.EffectValue * 100f).ToString()
                : FormatNumber(row.EffectValue);

            var highlighted = $"<color={HighlightColorHex}>{value}</color>";

            return string.Format(row.Desc, highlighted);
        }

        private static string FormatNumber(float value)
        {
            return Mathf.Approximately(value, Mathf.Round(value))
                ? Mathf.RoundToInt(value).ToString()
                : value.ToString("0.##");
        }
    }
}

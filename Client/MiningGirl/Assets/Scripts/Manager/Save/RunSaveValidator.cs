using System.Collections.Generic;
using Data;
using UnityEngine;

namespace Manager.Save
{
    // 저장을 복원해도 되는지 판정합니다.
    //
    // 하나라도 어긋나면 복원하지 않습니다. 반쯤 복원한 판은 어디서 터질지
    // 그때그때 다릅니다 - 이 프로젝트의 실패는 전부 에러가 안 나는 것들이었습니다.
    public static class RunSaveValidator
    {
        public enum EResult
        {
            Ok,

            // 저장 구조가 이 코드와 다릅니다.
            VersionMismatch,

            // 테이블에 없는 id를 가리킵니다.
            MissingId,

            // 스케줄 길이가 달라졌습니다. 그 웨이브 행이 바뀐 것입니다.
            ScheduleChanged,

            // 스테이지를 못 찾습니다. 이 경우만 홈으로 보냅니다.
            StageMissing,
        }

        public readonly struct Verdict
        {
            public readonly EResult Result;
            public readonly string Reason;

            public bool IsOk => Result == EResult.Ok;

            public Verdict(EResult result, string reason)
            {
                Result = result;
                Reason = reason;
            }
        }

        // id를 전수로 대조합니다. 표본으로 하면 없는 id 하나가 통과해
        // GetRow가 null을 내고, 그 null이 어디서 터질지는 그때그때 다릅니다.
        //
        // Weight 0은 실패가 아닙니다. 미사용 처리된 스킬을 들고 있던 판도
        // 행이 남아 있으면 정상 복원됩니다.
        public static Verdict Validate(RunSaveData data, DataTableManager tables)
        {
            if (data == null)
                return new Verdict(EResult.MissingId, "저장이 비어 있습니다");

            if (data.SchemaVersion != RunSaveStore.SchemaVersion)
                return new Verdict(EResult.VersionMismatch,
                    $"저장 구조 {data.SchemaVersion}, 코드 {RunSaveStore.SchemaVersion}");

            if (tables?.StageDataTable?.GetRow(data.StageId) == null)
                return new Verdict(EResult.StageMissing, $"스테이지를 찾지 못했습니다: {data.StageId}");

            if (tables.CharacterDataTable?.GetRow(data.CharacterId) == null)
                return new Verdict(EResult.MissingId, $"캐릭터: {data.CharacterId}");

            var skills = tables.SkillDataTable;
            var upgrades = tables.SkillUpgradeDataTable;
            var masteries = tables.SkillMasteryDataTable;

            for (var i = 0; i < data.Skills.Count; i++)
            {
                var save = data.Skills[i];

                if (skills?.GetRow(save.SkillId) == null)
                    return new Verdict(EResult.MissingId, $"스킬: {save.SkillId}");

                if (!string.IsNullOrEmpty(save.MasteryId) && FindMastery(masteries, save.MasteryId) == null)
                    return new Verdict(EResult.MissingId, $"강화스킬: {save.MasteryId}");

                for (var k = 0; k < save.UpgradeCounts.Count; k++)
                {
                    var type = save.UpgradeCounts[k].Type;

                    if (!System.Enum.IsDefined(typeof(ESkillUpgradeType), type ?? string.Empty))
                        return new Verdict(EResult.MissingId, $"강화 종류: {type}");

                    // 그 스킬에 그 종류의 강화 행이 남아 있어야 다시 적용할 수 있습니다.
                    if (FindUpgrade(upgrades, save.SkillId, type) == null)
                        return new Verdict(EResult.MissingId, $"강화 행: {save.SkillId} / {type}");
                }
            }

            var monsters = tables.MonsterDataTable;

            for (var i = 0; i < data.Monsters.Count; i++)
            {
                var id = data.Monsters[i].MonsterId;

                if (monsters?.GetRow(id) == null)
                    return new Verdict(EResult.MissingId, $"몬스터: {id}");
            }

            return new Verdict(EResult.Ok, string.Empty);
        }

        // 스케줄 길이 대조는 웨이브를 다시 만든 뒤에야 할 수 있어 따로 둡니다.
        public static Verdict ValidateSchedule(int savedCount, int rebuiltCount)
        {
            return savedCount == rebuiltCount
                ? new Verdict(EResult.Ok, string.Empty)
                : new Verdict(EResult.ScheduleChanged, $"스케줄 {savedCount} -> {rebuiltCount}");
        }

        private static SkillMasteryDataTableRow FindMastery(SkillMasteryDataTable table, string id)
        {
            if (table?.Rows == null)
                return null;

            for (var i = 0; i < table.Rows.Count; i++)
            {
                if (table.Rows[i] != null && table.Rows[i].Id == id)
                    return table.Rows[i];
            }

            return null;
        }

        public static SkillUpgradeDataTableRow FindUpgrade(SkillUpgradeDataTable table, string skillId, string type)
        {
            if (table?.Rows == null)
                return null;

            for (var i = 0; i < table.Rows.Count; i++)
            {
                var row = table.Rows[i];

                if (row != null && row.SkillId == skillId && row.UpgradeType.ToString() == type)
                    return row;
            }

            return null;
        }

        public static void LogFailure(Verdict verdict)
        {
            Debug.LogWarning($"[Save] 복원 실패 - {verdict.Reason}");
        }
    }
}

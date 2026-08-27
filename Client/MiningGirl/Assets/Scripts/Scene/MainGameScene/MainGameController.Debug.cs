#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using Scene.MainGameScene.Progress;
using Scene.MainGameScene.ViewModel;
using UnityEngine;

namespace Scene.MainGameScene
{
    // 측정용 기록. 에디터에서만 돕니다.
    //
    // 판이 왜 갈렸는지가 지금은 "몇 초에 끝났다"밖에 안 남습니다. 같은 스테이지가
    // 어떤 판은 클리어하고 어떤 판은 90초에 무너지는데, 그 사이에 무엇이 달랐는지를
    // 볼 수 있어야 원인을 좁힙니다.
    //
    // 남기는 것은 셋입니다.
    //   레벨업마다   뜬 카드 세 장과 고른 것, 그 시점의 위력·발사체·쿨
    //   웨이브마다   살아남은 수, 타워에 닿은 수, 남은 타워 체력
    //   판 끝        결과와 남은 리롤
    public partial class MainGameController
    {
        // 지금 3택에 떠 있는 선택지. 기록에 무엇이 떴는지 적을 때 씁니다.
        private readonly List<LevelUpChoice> _openChoices = new List<LevelUpChoice>();

        // 자동 플레이가 정책대로 고르려면 카드의 종류를 알아야 합니다.
        // 화면의 버튼만 보면 무작위 말고는 못 고릅니다.
        public LevelUpChoiceViewModel DebugChoiceViewModel => _choiceViewModel;

        public IReadOnlyList<LevelUpChoice> DebugOpenChoices => _openChoices;

        public IReadOnlyList<SkillState> DebugSkills => _inventory?.Skills;

        public int DebugRerollsLeft => _rerollsLeft;

        public bool DebugIsFinished => _isFinished;

        // 자동 측정이 이 판을 버릴지 정할 수 있게 열어 둡니다.
        // 복원된 판은 처음부터 돈 판이 아닙니다.
        public bool DebugIsRestored => _restoredFromSave;

        private void LogChoiceShown()
        {
            if (_openChoices.Count == 0)
                return;

            var sb = new StringBuilder();

            sb.Append("[측정] Lv").Append(_levelSystem.Level).Append(" 3택:");

            for (var i = 0; i < _openChoices.Count; i++)
            {
                if (i > 0)
                    sb.Append(" /");

                sb.Append(' ').Append(DescribeChoice(_openChoices[i]));
            }

            sb.Append("  | 리롤 남음 ").Append(_rerollsLeft);

            Debug.Log(sb.ToString());
        }

        private void LogChoicePicked(LevelUpChoice choice)
        {
            var sb = new StringBuilder();

            sb.Append("[측정] Lv").Append(_levelSystem.Level)
                .Append(" 선택: ").Append(DescribeChoice(choice))
                .Append("  ->");

            var skills = _inventory.Skills;

            for (var i = 0; i < skills.Count; i++)
            {
                var state = skills[i];

                sb.Append(' ').Append(state.Row.Id)
                    .Append(" 위력 ").Append(state.Damage.ToString("0.0"))
                    .Append(" 발사체 ").Append(state.ProjectileCount)
                    .Append(" 쿨 ").Append(state.Cooldown.ToString("0.0"));
            }

            Debug.Log(sb.ToString());
        }

        // 웨이브가 바뀌는 순간에 직전 웨이브의 결과를 적습니다.
        //
        // 어느 웨이브에서 밀리기 시작했는지가 이 한 줄로 보입니다. 타워에 닿은
        // 누적 수가 어디서 튀는지가 판이 무너진 지점입니다.
        private void LogWaveSnapshot(int startedWaveNo)
        {
            if (startedWaveNo <= 1)
                return;

            Debug.Log($"[측정] W{startedWaveNo - 1} 끝: 생존 {_field.AliveCount}"
                + $" 도달누적 {_field.ReachedTowerCount}"
                + $" 최대동시 {_field.PeakAliveCount}"
                + $" 타워 {(tower != null ? tower.CurrentHealth.ToString("0") : "?")}");
        }

        private void LogRunEnd(bool cleared)
        {
            Debug.Log($"[측정] 판 끝: 클리어={cleared}"
                + $" 경과 {_elapsed:0}초"
                + $" 레벨 {_levelSystem.Level}"
                + $" 처치 {_levelSystem.TotalKills}/{_stageMonsterCount}"
                + $" 경험치 {_levelSystem.TotalExp}"
                + $" 도달 {_field.ReachedTowerCount}"
                + $" 최대동시 {_field.PeakAliveCount}"
                + $" 리롤 남음 {_rerollsLeft}"
                + (_restoredFromSave ? "  [저장 복원된 판 - 측정에 쓰지 마십시오]" : string.Empty));
        }

        private static string DescribeChoice(LevelUpChoice choice)
        {
            if (choice == null)
                return "(없음)";

            switch (choice.Type)
            {
                case ELevelUpChoiceType.AcquireSkill:
                    return $"{choice.Skill.Name}(획득)";

                case ELevelUpChoiceType.Mastery:
                    return $"{choice.Mastery.Name}(강화스킬)";

                default:
                    return choice.Upgrade != null ? choice.Upgrade.Name : "(강화?)";
            }
        }
    }
}
#endif

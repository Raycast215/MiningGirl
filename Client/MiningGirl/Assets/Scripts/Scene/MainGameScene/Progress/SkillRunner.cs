using System.Collections.Generic;
using Scene.MainGameScene.Battle;
using UnityEngine;

namespace Scene.MainGameScene.Progress
{
    // 보유 스킬의 쿨다운을 각자 돌리고, 차면 알아서 쏩니다.
    //
    // 인게임에 터치 조작이 없으므로 발동 판단이 전부 여기 모입니다.
    public class SkillRunner
    {
        // 발사체가 여러 발일 때 출발점을 좌우로 벌리는 간격.
        // 같은 지점에서 동시에 나가면 초반 구간이 겹쳐 한 발처럼 보입니다.
        private const float MuzzleSpacing = 0.4f;

        private readonly SkillInventory _inventory;
        private readonly MonsterField _field;
        private readonly ProjectileLauncher _launcher;
        private readonly Transform _muzzle;

        // 스킬 Id별 남은 쿨다운.
        private readonly Dictionary<string, float> _cooldowns = new Dictionary<string, float>();

        private readonly List<MonsterUnit> _targetBuffer = new List<MonsterUnit>();

#if UNITY_EDITOR
        // 쿨이 찼는데 쏘지 못하고 기다린 시간입니다. 스킬별로 합산합니다.
        //
        // "조준할 적이 없으면 대기"가 화면에서 멈춰 선 그림으로 보일지가 쟁점인데,
        // 자동 플레이로는 눈으로 볼 수 없어 숫자로 대신 봅니다. 화면이 빈 대기는
        // 정상이고, 적이 보이는데 대기하는 시간만 문제가 됩니다.
        public static float DebugHoldEmptyTime;
        public static float DebugHoldAllReservedTime;

        // 한 번에 가장 오래 이어진 대기.
        //
        // 합계만으로는 판단이 안 됩니다. 0.3초짜리 대기가 백 번이면 눈에 안 보이지만
        // 3초가 한 번이면 멈춰 선 것으로 보입니다. 쟁점은 합이 아니라 최장 구간입니다.
        public static float DebugHoldLongestStreak;

        // 스킬별로 지금 이어지고 있는 대기.
        private readonly Dictionary<string, float> _debugHoldStreaks = new Dictionary<string, float>();

        public static void DebugResetHoldCounters()
        {
            DebugHoldEmptyTime = 0f;
            DebugHoldAllReservedTime = 0f;
            DebugHoldLongestStreak = 0f;
        }
#endif

        public SkillRunner(SkillInventory inventory, MonsterField field, ProjectileLauncher launcher, Transform muzzle)
        {
            _inventory = inventory;
            _field = field;
            _launcher = launcher;
            _muzzle = muzzle;
        }

        // 슬롯 UI가 원형 게이지를 그리는 데 씁니다.
        public float GetCooldownRatio(SkillState skill)
        {
            if (skill == null || !_cooldowns.TryGetValue(skill.Row.Id, out var remaining))
                return 0f;

            var total = skill.Cooldown;

            return total <= 0f ? 0f : Mathf.Clamp01(remaining / total);
        }

        public void Tick(float deltaTime)
        {
            var skills = _inventory.Skills;

            for (var i = 0; i < skills.Count; i++)
            {
                var skill = skills[i];

                _cooldowns.TryGetValue(skill.Row.Id, out var remaining);

                if (remaining > 0f)
                {
                    remaining -= deltaTime;
                    _cooldowns[skill.Row.Id] = remaining;

                    if (remaining > 0f)
                        continue;
                }

                // 조준할 적이 없으면 쏘지 않고 쿨이 찬 채로 기다립니다.
                // 적이 들어오는 순간 바로 나가야 초반 웨이브가 답답하지 않습니다.
                //
                // 화면에 적이 보여도 전부 죽을 예정이면 쏘지 않습니다. 시체에 쏘고
                // 쿨을 돌리는 것보다 0.2~0.4초 기다렸다 쏘는 쪽이 실질 화력이 높습니다.
                if (Fire(skill))
                {
                    _cooldowns[skill.Row.Id] = skill.Cooldown;

#if UNITY_EDITOR
                    _debugHoldStreaks[skill.Row.Id] = 0f;
#endif

                    continue;
                }

                _cooldowns[skill.Row.Id] = 0f;

#if UNITY_EDITOR
                // 화면이 빈 대기와, 적이 보이는데 하는 대기를 갈라 둡니다.
                if (_field.AliveCount == 0)
                {
                    DebugHoldEmptyTime += deltaTime;
                    _debugHoldStreaks[skill.Row.Id] = 0f;
                }
                else
                {
                    DebugHoldAllReservedTime += deltaTime;

                    _debugHoldStreaks.TryGetValue(skill.Row.Id, out var streak);
                    streak += deltaTime;
                    _debugHoldStreaks[skill.Row.Id] = streak;

                    if (streak > DebugHoldLongestStreak)
                        DebugHoldLongestStreak = streak;
                }
#endif
            }
        }

        // 스킬을 새로 얻으면 첫 발이 바로 나가도록 쿨을 0으로 둡니다.
        public void ResetCooldown(SkillState skill)
        {
            if (skill != null)
                _cooldowns[skill.Row.Id] = 0f;
        }

        // 한 발이라도 나갔으면 true. 조준할 적이 없어 못 쐈으면 false입니다.
        private bool Fire(SkillState skill)
        {
            var count = skill.ProjectileCount;
            var origin = _muzzle.position;

            if (count <= 1)
            {
                var target = _field.FindNearestTargetable(origin);

                if (target == null)
                    return false;

                FireOne(skill, origin, target);

                return true;
            }

            // 발사체마다 서로 다른 적을 하나씩 맡습니다.
            //
            // 후보가 발사체 수보다 적으면 그만큼만 쏘고 남는 발은 버립니다. 같은 적에
            // 몰아 쏘면 앞 발이 죽인 뒤 나머지가 시체로 날아가기 때문입니다. 그래서
            // 다발형은 적이 많을 때 강하고 적을 때는 단발과 다르지 않습니다.
            _field.FillNearestTargetable(origin, count, _targetBuffer);

            var fired = _targetBuffer.Count;

            if (fired == 0)
                return false;

            for (var i = 0; i < fired; i++)
            {
                // 실제로 나가는 수를 기준으로 가운데 대칭이 되게 벌립니다.
                var offset = (i - (fired - 1) * 0.5f) * MuzzleSpacing;

                FireOne(skill, origin + new Vector3(offset, 0f, 0f), _targetBuffer[i]);
            }

            return true;
        }

        private void FireOne(SkillState skill, Vector3 origin, MonsterUnit target)
        {
            var spec = skill.BuildProjectileSpec();
            var aimPoint = PredictAimPoint(origin, target, spec.Speed);
            var toAim = aimPoint - origin;

            _launcher.Fire(spec, origin, toAim, toAim.magnitude, target);
        }

        // 대상이 도착할 지점을 조준합니다.
        //
        // 현재 위치를 조준하면 비행 시간 동안 대상이 내려간 만큼 빗나갑니다. 조준선이
        // 비스듬할수록, 대상이 빠를수록 크게 벌어져서 MoveSpeed가 표에 없는 회피 능력치처럼
        // 굴었습니다. 몬스터가 등속 직선 하강이라 근사가 아니라 해가 정확히 나옵니다.
        //
        //   대상은 t초 뒤 P + (0, -v·t)에 있고, 발사체는 그때까지 s·t만큼 날아갑니다.
        //   |P + (0, -v·t) - M| = s·t 를 t에 대해 풀면 2차방정식이 됩니다.
        //
        //   (s² - v²)·t² + 2·Dy·v·t - |D|² = 0        (D = P - M)
        //
        // s > v 이면 상수항이 음수라 양의 해가 하나만 나옵니다.
        private static Vector3 PredictAimPoint(Vector3 origin, MonsterUnit target, float projectileSpeed)
        {
            var position = target.Position;
            var moveSpeed = target.MoveSpeed;

            if (moveSpeed <= 0f)
                return position;

            var a = projectileSpeed * projectileSpeed - moveSpeed * moveSpeed;

            // 발사체가 대상보다 느리면 따라잡지 못합니다. 그때는 현재 위치를 조준합니다.
            if (a <= 0.0001f)
                return position;

            var delta = position - origin;
            var b = 2f * delta.y * moveSpeed;
            var c = -delta.sqrMagnitude;

            var discriminant = b * b - 4f * a * c;

            if (discriminant < 0f)
                return position;

            var time = (-b + Mathf.Sqrt(discriminant)) / (2f * a);

            if (time <= 0f)
                return position;

            return position + new Vector3(0f, -moveSpeed * time, 0f);
        }
    }
}

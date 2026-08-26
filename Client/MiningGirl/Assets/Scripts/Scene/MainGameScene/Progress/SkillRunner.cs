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

        // 발사체 사이의 간격. 동시에 나가면 여러 발이 한 발처럼 보입니다.
        private const float FireDelay = 0.2f;

        // 한 번의 발사가 퍼지는 총 길이의 상한.
        private const float MaxFireSpread = 0.6f;

        private readonly SkillInventory _inventory;
        private readonly MonsterField _field;
        private readonly ProjectileLauncher _launcher;
        private readonly Transform _muzzle;

        // 스킬 Id별 남은 쿨다운.
        private readonly Dictionary<string, float> _cooldowns = new Dictionary<string, float>();

        private readonly List<MonsterUnit> _targetBuffer = new List<MonsterUnit>();

        // 아직 나가지 않은 발. 자기 차례가 되면 그때의 적을 조준합니다.
        private readonly List<PendingShot> _pending = new List<PendingShot>();

        private struct PendingShot
        {
            public readonly SkillState Skill;
            public readonly int Index;
            public readonly int Count;

            public float Remaining;

            public PendingShot(SkillState skill, float remaining, int index, int count)
            {
                Skill = skill;
                Remaining = remaining;
                Index = index;
                Count = count;
            }
        }

#if UNITY_EDITOR
        // 쿨이 찼는데 쏘지 못하고 기다린 시간입니다. 스킬별로 합산합니다.
        //
        // "조준할 적이 없으면 대기"가 화면에서 멈춰 선 그림으로 보일지가 쟁점인데,
        // 자동 플레이로는 눈으로 볼 수 없어 숫자로 대신 봅니다. 화면이 빈 대기는
        // 정상이고, 적이 보이는데 대기하는 시간만 문제가 됩니다.
        public static float DebugHoldEmptyTime;
        public static float DebugHoldAllReservedTime;

        // 위 두 값은 스킬별 합산이라 판 길이를 넘을 수 있습니다.
        //
        // 아래가 벽시계입니다 — "한 스킬이라도 적을 두고 기다린 프레임"의 시간 합.
        // 스킬 수로 나눌 필요가 없어 그대로 비율로 읽히고, 스킬이 늘어도 해석이
        // 흔들리지 않습니다. 밀도 판단에 쓰는 건 이쪽입니다.
        public static float DebugHoldWallClockTime;

        // 판이 시작한 뒤 흐른 시간. 위 값을 비율로 만들 때 분모입니다.
        public static float DebugTickedTime;

        // 한 번에 가장 오래 이어진 대기(벽시계).
        //
        // 합계만으로는 판단이 안 됩니다. 0.3초짜리 대기가 백 번이면 눈에 안 보이지만
        // 3초가 한 번이면 멈춰 선 것으로 보입니다. 쟁점은 합이 아니라 최장 구간입니다.
        public static float DebugHoldLongestStreak;

        // 지금 이어지고 있는 벽시계 대기.
        private static float _debugHoldStreak;

        public static void DebugResetHoldCounters()
        {
            DebugHoldEmptyTime = 0f;
            DebugHoldAllReservedTime = 0f;
            DebugHoldWallClockTime = 0f;
            DebugTickedTime = 0f;
            DebugHoldLongestStreak = 0f;
            _debugHoldStreak = 0f;
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

            TickPending(deltaTime);

#if UNITY_EDITOR
            DebugTickedTime += deltaTime;

            // 이번 프레임에 "적을 두고 기다린" 스킬이 하나라도 있었는지.
            // 벽시계 대기는 스킬 수와 무관하게 프레임 단위로 셉니다.
            var heldThisFrame = false;
#endif

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

                    continue;
                }

                _cooldowns[skill.Row.Id] = 0f;

#if UNITY_EDITOR
                // 화면이 빈 대기와, 적이 보이는데 하는 대기를 갈라 둡니다.
                if (_field.AliveCount == 0)
                {
                    DebugHoldEmptyTime += deltaTime;
                }
                else
                {
                    DebugHoldAllReservedTime += deltaTime;
                    heldThisFrame = true;
                }
#endif
            }

#if UNITY_EDITOR
            if (heldThisFrame)
            {
                DebugHoldWallClockTime += deltaTime;

                _debugHoldStreak += deltaTime;

                if (_debugHoldStreak > DebugHoldLongestStreak)
                    DebugHoldLongestStreak = _debugHoldStreak;
            }
            else
            {
                _debugHoldStreak = 0f;
            }
#endif
        }

        // 스킬을 새로 얻으면 첫 발이 바로 나가도록 쿨을 0으로 둡니다.
        public void ResetCooldown(SkillState skill)
        {
            if (skill != null)
                _cooldowns[skill.Row.Id] = 0f;
        }

        // 한 발이라도 나갔으면 true. 조준할 적이 없어 못 쐈으면 false입니다.
        //
        // 2발 이상이면 첫 발만 지금 나가고 나머지는 예약해 둡니다. 예약된 발은
        // 자기 차례가 왔을 때 그 시점의 적을 조준합니다 - 미리 조준해 두면 그 사이
        // 대상이 죽었을 때 그 발이 그대로 낭비됩니다.
        private bool Fire(SkillState skill)
        {
            var count = Mathf.Max(1, skill.ProjectileCount);
            var origin = _muzzle.position;

            var target = _field.FindNearestTargetable(origin);

            if (target == null)
                return false;

            FireOne(skill, MuzzleFor(origin, 0, count), target);

            if (count <= 1)
                return true;

            // 발사체가 늘어도 확산이 쿨다운을 잡아먹지 않게 총 길이를 묶습니다.
            // 0.2초 고정이면 5발에 0.8초가 걸려 연사가 아니라 점사로 보입니다.
            var spacing = Mathf.Min(FireDelay, MaxFireSpread / (count - 1));

            for (var i = 1; i < count; i++)
                _pending.Add(new PendingShot(skill, spacing * i, i, count));

            return true;
        }

        // 예약된 발을 시간이 되면 하나씩 내보냅니다.
        //
        // 자기 차례에 조준할 적이 없으면 그 발은 버립니다. 남는 발을 아무 데나
        // 보내면 다발형의 "여러 마리를 친다"가 무너집니다.
        private void TickPending(float deltaTime)
        {
            for (var i = _pending.Count - 1; i >= 0; i--)
            {
                var shot = _pending[i];
                shot.Remaining -= deltaTime;

                if (shot.Remaining > 0f)
                {
                    _pending[i] = shot;

                    continue;
                }

                _pending.RemoveAt(i);

                var origin = _muzzle.position;
                var target = _field.FindNearestTargetable(origin);

                if (target == null)
                    continue;

                FireOne(shot.Skill, MuzzleFor(origin, shot.Index, shot.Count), target);
            }
        }

        // 여러 발이 같은 점에서 나가면 초반 구간이 겹칩니다.
        // 시차가 생긴 뒤에도 출발점을 조금 벌려 두면 궤적이 더 갈라져 보입니다.
        private static Vector3 MuzzleFor(Vector3 origin, int index, int count)
        {
            if (count <= 1)
                return origin;

            return origin + new Vector3((index - (count - 1) * 0.5f) * MuzzleSpacing, 0f, 0f);
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

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

        // 부채꼴 발사체가 조준점 없이 날아갈 거리.
        // Sine 발사체의 진폭 수렴 기준이라 화면을 넘길 만큼이면 됩니다.
        private const float FanTargetDistance = 30f;

        // 조준할 적이 없어 그냥 나가는 예약 발사체의 벌어짐.
        //
        // 위쪽(90도)을 가운데로 둡니다 - 몬스터가 위에서 내려오므로 옆이나 아래로
        // 나가면 빗나간 게 아니라 엉뚱한 데 쏜 것으로 보입니다.
        //
        // 예전에는 120도 안에서 무작위였습니다. 조준 대상이 드물게 없을 때를
        // 가정한 값인데, 화력이 표적 수를 넘어서면서 이 경로가 기본이 됐습니다
        // (실측 무조준 비율 53~63%). 무작위 방향은 플레이어가 배울 수 없어서
        // 위력이 아니라 오작동으로 읽히고, 그 소음이 맞히는 순간의 피드백까지 깎습니다.
        //
        // 총 폭이 아니라 발당 간격으로 잡습니다. 총 폭을 고정하면 2발일 때
        // 부채가 아니라 갈라짐이 됩니다. 간격으로 두면 발수에 따라 자연히
        // 넓어지고 그게 "많이 쏜다"로 읽힙니다.
        // 각도가 아니라 화면 폭 비율로 잡습니다.
        //
        // 같은 각도가 기기마다 다른 그림이 됩니다 - 총구에서 본 화면 각폭이
        // 태블릿(3:4) 52.9도, 폰(9:16) 41.0도, 긴 폰(9:19.5) 34.1도입니다.
        // 22.5도가 태블릿에서는 화면의 42%, 긴 폰에서는 66%를 덮습니다.
        //
        // 부채가 읽히는 기준은 "몇 도인가"가 아니라 "화면에서 얼마나 벌어져
        // 보이는가"라서, 각도를 상수로 두면 기준이 기기마다 흔들립니다.
        private const float StrayFireStepRatio = 0.10f;

        // 그래도 화면 폭의 이만큼은 안 넘습니다.
        private const float StrayFireArcMaxRatio = 0.55f;

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

            // 이 발이 속한 볼리 번호. 예약된 발이 나중에 나가도 자기 볼리를 압니다.
            //
            // 진단용입니다. 화면에 동시에 뜬 볼리가 몇 개인지를 세는 데만 씁니다 -
            // 각도가 등차수열인 것끼리 묶어 추론할 수도 있지만, 부채가 겹치면
            // 어디까지가 한 볼리인지 정할 근거가 없고 세는 사람에 따라 값이 달라집니다.
            //
            // 게임 로직은 이 값을 안 읽습니다. 이 위에 기능을 만들지 마십시오.
            public readonly int DebugVolleyId;

            public float Remaining;

            public PendingShot(SkillState skill, float remaining, int index, int count, int volleyId)
            {
                Skill = skill;
                Remaining = remaining;
                Index = index;
                Count = count;
                DebugVolleyId = volleyId;
            }
        }

        // 볼리마다 번호를 하나씩 씁니다. 진단용이라 값 자체에 뜻은 없고,
        // 게임 로직은 이 값을 안 읽습니다.
        private int _volleySeq;

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

        // 저장이 남은 쿨다운을 꺼내고 되돌릴 때 씁니다.
        //
        // 쿨다운은 저장 대상입니다. 버리면 복원 직후 전 스킬이 동시에 나가서
        // "종료한 그 순간"이 아니게 됩니다 - 최대 3.6초라 눈에 띕니다.
        public float GetCooldownRemaining(SkillState skill)
        {
            float remaining;

            return skill != null && _cooldowns.TryGetValue(skill.Row.Id, out remaining) ? remaining : 0f;
        }

        public void SetCooldownRemaining(SkillState skill, float remaining)
        {
            if (skill != null)
                _cooldowns[skill.Row.Id] = Mathf.Max(0f, remaining);
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

            _volleySeq++;

#if UNITY_EDITOR
            // 부채꼴은 아래에서 먼저 return 하므로 여기서 넣어야 같이 받습니다.
            Battle.Projectile.DebugNextVolleyId = _volleySeq;
#endif

            // 부채꼴은 조준하지 않고 각도로 뿌립니다.
            //
            // 대상을 고르는 개념이 없으므로 조준 규칙 2(서로 다른 적)와 3(대기)이
            // 적용되지 않습니다. 예외를 위한 분기가 아니라, 대상을 안 고르니 그
            // 규칙들이 있는 코드를 아예 지나가지 않는 것입니다.
            if (skill.Mastery.HasValue && skill.Mastery.Type == EMasteryType.FanBurst)
                return FireFan(skill, origin, count);

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
                _pending.Add(new PendingShot(skill, spacing * i, i, count, _volleySeq));

            return true;
        }

        // 부채꼴로 한꺼번에 뿌립니다.
        //
        // 동시 발사라 발사 딜레이 0.2초의 예외입니다. 각도로 흩어지므로 겹쳐 보이지
        // 않고, 시차를 두면 오히려 부채꼴로 안 보입니다.
        //
        // 적이 없어도 쏩니다. 조준하지 않으니 "조준할 적이 없다"가 성립하지 않습니다.
        private bool FireFan(SkillState skill, Vector3 origin, int count)
        {
            var total = count + Mathf.Max(0, Mathf.RoundToInt(skill.Mastery.Value));

            if (total <= 0)
                return false;

            var spec = skill.BuildProjectileSpec();
            var arc = Mathf.Max(1f, skill.Mastery.Range);

            // 위쪽을 가운데로 두고 좌우로 벌립니다. 몬스터가 위에서 내려오기 때문입니다.
            var start = 90f + arc * 0.5f;
            var step = total <= 1 ? 0f : arc / (total - 1);

            for (var i = 0; i < total; i++)
            {
                var degree = start - step * i;
                var radian = degree * Mathf.Deg2Rad;
                var direction = new Vector3(Mathf.Cos(radian), Mathf.Sin(radian), 0f);

                // 조준 대상이 없으므로 예약도 걸지 않습니다.
                // 사거리는 화면을 넘기는 값이면 충분합니다.
                _launcher.Fire(spec, origin, direction, FanTargetDistance, null);
            }

            return true;
        }

        // 예약된 발을 시간이 되면 하나씩 내보냅니다.
        //
        // 자기 차례에 조준할 적이 없으면 위쪽 아무 데나 쏩니다. 버리지 않습니다 -
        // 쿨은 이미 돌아갔는데 발만 사라지면, 화면에는 "쏘다 만" 그림이 남습니다.
        // 빗나간 발이 뒤에 들어온 몬스터에 맞을 수도 있어 손해도 아닙니다.
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
                var muzzle = MuzzleFor(origin, shot.Index, shot.Count);

#if UNITY_EDITOR
                // 예약된 발은 자기 볼리 번호를 그대로 씁니다. 그 사이 다른 볼리가
                // 시작했어도 이 발은 앞 볼리 것입니다.
                Battle.Projectile.DebugNextVolleyId = shot.DebugVolleyId;
#endif

                if (target == null)
                {
                    FireStray(shot.Skill, muzzle, shot.Index, shot.Count);

                    continue;
                }

                FireOne(shot.Skill, muzzle, target);
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

        // 조준 대상이 없을 때 위쪽으로 내보냅니다.
        //
        // 각도를 발사체 번호로 정합니다. 무작위를 안 씁니다 - 같은 볼리는 매번
        // 같은 모양으로 나가고, 그러면 "조준 못 함"이 아니라 "위쪽을 훑는다"로
        // 읽힙니다. 발수도 낭비도 같은데 읽히는 것만 달라집니다.
        //
        // 판정 기준은 아트가 정했습니다 - 화면 중간 높이에서 이웃한 발이 겨우
        // 갈라져 보이면 됩니다. 총구 근처에서 겹쳐 나가다 올라가며 벌어지는 것이
        // 한 동작으로 읽히고, 처음부터 갈라져 나오면 여러 동작으로 읽힙니다.
        //
        // 번호를 그대로 쓰므로 볼리의 일부만 조준에 실패해도 남은 발이 제자리
        // 각도로 나갑니다. 실패한 발이 몇 번째냐에 따라 모양이 흔들리지 않습니다.
        //
        // 부채꼴과 같은 길로 나갑니다 - 조준점이 없으니 사거리는 화면을 넘기는
        // 값이면 되고, 타겟을 null로 넘겨 예측 조준을 건너뜁니다.
        private void FireStray(SkillState skill, Vector3 origin, int index, int count)
        {
            var degree = 90f;

            if (count > 1)
            {
                // 화면 위 끝에서 얼마나 벌어져 보일지를 먼저 정하고, 거기서 각도를 냅니다.
                //
                // 발사체가 거기까지 살아서 가므로 그 지점의 벌어짐이 곧 눈에 보이는
                // 폭입니다. 아트 판정 기준(화면 중간에서 이웃한 발이 겨우 갈라져
                // 보이는가)은 그 중간값이라 같이 만족합니다.
                var bounds = _field.Bounds;
                var ratio = Mathf.Min(StrayFireStepRatio * (count - 1), StrayFireArcMaxRatio);
                var spread = bounds.HalfWidth * 2f * ratio;
                var reach = Mathf.Max(0.01f, bounds.ScreenTopY - origin.y);
                var halfAngle = Mathf.Atan2(spread * 0.5f, reach) * Mathf.Rad2Deg;

                degree += (index - (count - 1) * 0.5f) * (halfAngle * 2f / (count - 1));
            }

            var radian = degree * Mathf.Deg2Rad;
            var direction = new Vector3(Mathf.Cos(radian), Mathf.Sin(radian), 0f);

            _launcher.Fire(skill.BuildProjectileSpec(), origin, direction, FanTargetDistance, null);
        }

        private void FireOne(SkillState skill, Vector3 origin, MonsterUnit target)
        {
            var spec = skill.BuildProjectileSpec();
            var aimPoint = SkillAiming.PredictAimPoint(origin, target, spec.Speed);
            var toAim = aimPoint - origin;

            _launcher.Fire(spec, origin, toAim, toAim.magnitude, target);
        }

    }
}

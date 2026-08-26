using System;
using System.Collections.Generic;
using UnityEngine;

namespace Scene.MainGameScene.Battle
{
    // 캐릭터에서 나가 날아가는 발사체.
    //
    // 유도하지 않습니다. 발사 순간의 방향으로만 나아가므로 적이 비켜서면 빗나갑니다.
    // 몬스터가 직선으로만 내려오니 그래도 대부분 맞습니다.
    public class Projectile : MonoBehaviour
    {
        // 어느 풀에서 나왔는지. ProjectileLauncher가 되돌릴 때 씁니다.
        public string PoolKey { get; set; }

        private readonly List<MonsterUnit> _hit = new List<MonsterUnit>();

        private MonsterField _field;
        private BattleBounds _bounds;
        private Action<Projectile> _onFinished;

        private Vector3 _origin;
        private Vector3 _direction;

        // 진행 방향에 수직인 축. 곡선 이동이 흔들리는 방향입니다.
        private Vector3 _perpendicular;

        private float _speed;
        private float _damage;
        private float _hitRange;

        private EProjectileMoveType _moveType;
        private float _waveAmplitude;
        private float _waveCycles;

        // 발사 시점의 타겟까지 거리. 여기에 가까워질수록 흔들림을 줄입니다.
        private float _targetDistance;

        private float _travelled;
        private float _elapsed;

        // 앞으로 더 맞힐 수 있는 수. 0이 되면 소멸합니다.
        private int _remainingHits;

        private bool _isActive;

#if UNITY_EDITOR
        // 명중 진단용 계측입니다. 에디터에서만 돌고 빌드에는 들어가지 않습니다.
        //
        // "안 맞는다"를 고치려면 먼저 갈라야 합니다.
        //  - 판정이 닿았는데 처리되지 않은 것    -> 코드 결함
        //  - 애초에 닿지 않은 것                 -> 유도하지 않는다는 사양대로의 결과
        // 눈으로 보면 둘이 똑같아 보이므로 숫자로만 구분됩니다.
        public static int DebugFired;
        public static int DebugHit;
        public static int DebugMissed;

        // 빗나간 발이 가장 가까이 스친 거리 ÷ 판정 반경. 1에 가까울수록 아슬아슬했습니다.
        public static readonly List<float> DebugMissClosest = new List<float>();

        // 빗나갔을 때 조준했던 대상이 이미 죽어 있었는지.
        public static int DebugMissTargetDead;

        // 몬스터 종류별 집계. 조준한 대상의 Id로 셉니다.
        //
        // 전체 비율 하나로는 원인을 못 가릅니다. 유도하지 않는 사양이라면 빠른 몬스터에
        // 빗나감이 몰려야 하고, 종류와 무관하게 고르게 퍼져 있다면 판정 쪽 문제입니다.
        public static readonly Dictionary<string, int> DebugFiredBy = new Dictionary<string, int>();

        // 조준한 그 몬스터를 맞힌 수.
        public static readonly Dictionary<string, int> DebugHitTargetBy = new Dictionary<string, int>();

        // 조준한 대상은 놓쳤지만 다른 몬스터를 맞힌 수.
        // 플레이어 눈에는 "옆에 있던 놈이 죽는다"로 보입니다.
        public static readonly Dictionary<string, int> DebugHitOtherBy = new Dictionary<string, int>();

        public static void DebugResetCounters()
        {
            DebugFired = 0;
            DebugHit = 0;
            DebugMissed = 0;
            DebugMissTargetDead = 0;
            DebugMissTargetGone = 0;
            DebugMissNear = 0;
            DebugMissWide = 0;
            DebugMissClosest.Clear();
            DebugFiredBy.Clear();
            DebugHitTargetBy.Clear();
            DebugHitOtherBy.Clear();
            DebugMissGoneBy.Clear();
            DebugMissNearBy.Clear();
            DebugMissWideBy.Clear();
        }

        private static void DebugCount(Dictionary<string, int> table, string id)
        {
            if (string.IsNullOrEmpty(id))
                return;

            table.TryGetValue(id, out var count);
            table[id] = count + 1;
        }

        // 조준했던 대상. 빗나간 이유를 가르는 데만 씁니다.
        public MonsterUnit DebugIntendedTarget { get; set; }

        // 조준 시점의 종류 Id. 대상이 풀로 돌아가 다른 종류로 재사용될 수 있어 따로 들고 있습니다.
        public string DebugIntendedId { get; set; }

        // 조준 시점의 대상 일련번호. 도착했을 때 번호가 다르면 그 사이에 죽고 새로 나온 개체입니다.
        public int DebugIntendedSerial { get; set; }

        // 빗나간 이유를 세 갈래로 나눈 집계입니다.
        public static int DebugMissTargetGone;   // 날아가는 사이에 대상이 죽었습니다
        public static int DebugMissNear;         // 대상은 살아 있고, 판정 반경 언저리를 스쳤습니다
        public static int DebugMissWide;         // 대상은 살아 있는데 한참 벗어났습니다

        // 같은 세 갈래를 몬스터 종류별로도 셉니다.
        // 종류별로 갈라야 "빠른 놈이라 놓친 것"과 "죽은 놈에게 쏜 것"이 구분됩니다.
        public static readonly Dictionary<string, int> DebugMissGoneBy = new Dictionary<string, int>();
        public static readonly Dictionary<string, int> DebugMissNearBy = new Dictionary<string, int>();
        public static readonly Dictionary<string, int> DebugMissWideBy = new Dictionary<string, int>();

        private float _debugClosest;
        private float _debugClosestToTarget;
        private bool _debugHitAny;
        private bool _debugHitTarget;
#endif

        public void Setup(
            MonsterField field,
            BattleBounds bounds,
            Vector3 origin,
            Vector3 direction,
            float targetDistance,
            ProjectileSpec spec,
            Action<Projectile> onFinished)
        {
            _field = field;
            _bounds = bounds;
            _onFinished = onFinished;

            _direction = direction.sqrMagnitude < 0.0001f ? Vector3.up : direction.normalized;
            _perpendicular = new Vector3(-_direction.y, _direction.x, 0f);

            _origin = origin;
            _speed = Mathf.Max(0.1f, spec.Speed);
            _damage = Mathf.Max(0f, spec.Damage);
            _hitRange = Mathf.Max(0f, spec.HitRange);

            _moveType = spec.MoveType;
            _waveAmplitude = Mathf.Max(0f, spec.WaveAmplitude);
            _waveCycles = Mathf.Max(0f, spec.WaveCycles);
            _targetDistance = Mathf.Max(0.01f, targetDistance);

            // PierceCount는 "첫 명중 뒤에 더 뚫는 수"라 실제로 맞힐 수 있는 건 +1마리입니다.
            _remainingHits = Mathf.Max(1, spec.PierceCount + 1);

            _hit.Clear();
            _travelled = 0f;
            _elapsed = 0f;

            transform.position = origin;
            transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg);

            _isActive = true;

#if UNITY_EDITOR
            DebugFired++;
            _debugClosest = float.MaxValue;
            _debugClosestToTarget = float.MaxValue;
            _debugHitAny = false;
            _debugHitTarget = false;
            DebugIntendedTarget = null;
            DebugIntendedId = null;
            DebugIntendedSerial = 0;
#endif
        }

        public void Tick(float deltaTime)
        {
            if (!_isActive)
                return;

            var previous = transform.position;

            _travelled += _speed * deltaTime;
            _elapsed += deltaTime;

            var position = CalculatePosition();
            transform.position = position;

            // 곡선일 때는 실제로 나아가는 방향을 봐야 그림이 맞습니다.
            var delta = position - previous;

            if (delta.sqrMagnitude > 0.000001f)
                transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

            if (position.y > _bounds.DespawnTopY || position.y < _bounds.DespawnBottomY
                || Mathf.Abs(position.x) > _bounds.HalfWidth + 2f)
            {
                Finish();

                return;
            }

            TryHit(previous, position);

#if UNITY_EDITOR
            DebugTrackTarget(previous, position);
#endif
        }

#if UNITY_EDITOR
        // 조준했던 그 대상에 얼마나 가까이 갔는지만 따로 잽니다.
        // TryHit은 맞은 순간 멈추므로, 놓친 발이 어디까지 갔는지는 여기서만 남습니다.
        private void DebugTrackTarget(Vector3 from, Vector3 to)
        {
            var target = DebugIntendedTarget;

            if (target == null || !target.IsAlive || target.DebugSerial != DebugIntendedSerial)
                return;

            var reach = _hitRange + target.BodyRadius;

            if (reach <= 0f)
                return;

            _debugClosestToTarget = Mathf.Min(
                _debugClosestToTarget,
                Mathf.Sqrt(SqrDistanceToSegment(target.Position, from, to)) / reach);
        }
#endif

        private Vector3 CalculatePosition()
        {
            var position = _origin + _direction * _travelled;

            if (_moveType != EProjectileMoveType.Sine || _waveAmplitude <= 0f || _waveCycles <= 0f)
                return position;

            // 타겟에 가까워질수록 진폭을 0으로 줄입니다.
            //
            // 줄이지 않으면 조준한 지점에서 진폭만큼 벗어난 채로 도착해, 곡선 스킬만
            // 눈에 띄게 잘 빗나갑니다. 위력이 같은 세 스킬 사이에서 그림만 다르게 하려는 것이므로
            // 명중률까지 달라지면 안 됩니다.
            var converge = Mathf.Clamp01(1f - _travelled / _targetDistance);
            var offset = _waveAmplitude * converge * Mathf.Sin(_elapsed * _waveCycles * Mathf.PI * 2f);

            return position + _perpendicular * offset;
        }

        // 이번 프레임에 지나온 구간 전체로 판정합니다.
        //
        // 도착 지점만 보면 프레임이 길어졌을 때 몬스터를 뚫고 지나갑니다.
        // 탄속 12에 프레임이 0.1초만 되어도 한 번에 1.2유닛을 건너뛰어, 몸통 반경과 맞먹습니다.
        // 저사양 기기나 순간적인 프레임 하락에서 발사체가 그냥 통과해 버립니다.
        private void TryHit(Vector3 from, Vector3 to)
        {
            var alive = _field.Alive;

            for (var i = 0; i < alive.Count; i++)
            {
                var unit = alive[i];

                if (!unit.IsAlive || _hit.Contains(unit))
                    continue;

                // HitRange는 몬스터 크기에 더해지는 추가 반경입니다.
                // 0.3만으로 중심을 맞히라고 하면 거의 스쳐 지나갑니다.
                // 반경은 종마다 다르고 프리팹 스케일도 반영되므로 개체에서 읽습니다.
                var reach = _hitRange + unit.BodyRadius;
                var sqrDistance = SqrDistanceToSegment(unit.Position, from, to);

#if UNITY_EDITOR
                // 맞히지 못한 발도 얼마나 가까이 스쳤는지 남겨 둡니다.
                if (reach > 0f)
                    _debugClosest = Mathf.Min(_debugClosest, Mathf.Sqrt(sqrDistance) / reach);
#endif

                if (sqrDistance > reach * reach)
                    continue;

                _hit.Add(unit);
                unit.TakeDamage(_damage);

#if UNITY_EDITOR
                _debugHitAny = true;

                if (unit == DebugIntendedTarget)
                    _debugHitTarget = true;
#endif

                _remainingHits--;

                if (_remainingHits > 0)
                    continue;

                Finish();

                return;
            }
        }

        private static float SqrDistanceToSegment(Vector3 point, Vector3 from, Vector3 to)
        {
            var segment = to - from;
            var lengthSqr = segment.sqrMagnitude;

            if (lengthSqr < 0.000001f)
                return (point - from).sqrMagnitude;

            var t = Mathf.Clamp01(Vector3.Dot(point - from, segment) / lengthSqr);

            return (point - (from + segment * t)).sqrMagnitude;
        }

        private void Finish()
        {
            if (!_isActive)
                return;

            _isActive = false;

#if UNITY_EDITOR
            DebugCount(DebugFiredBy, DebugIntendedId);

            if (_debugHitTarget)
            {
                DebugHit++;
                DebugCount(DebugHitTargetBy, DebugIntendedId);
            }
            else if (_debugHitAny)
            {
                DebugHit++;
                DebugCount(DebugHitOtherBy, DebugIntendedId);
            }
            else
            {
                DebugMissed++;

                if (_debugClosest < float.MaxValue)
                    DebugMissClosest.Add(_debugClosest);

                var target = DebugIntendedTarget;
                var targetGone = target == null
                    || !target.IsAlive
                    || target.DebugSerial != DebugIntendedSerial;

                if (targetGone)
                {
                    DebugMissTargetDead++;
                    DebugMissTargetGone++;
                    DebugCount(DebugMissGoneBy, DebugIntendedId);
                }
                else if (_debugClosestToTarget <= 1.5f)
                {
                    DebugMissNear++;
                    DebugCount(DebugMissNearBy, DebugIntendedId);
                }
                else
                {
                    DebugMissWide++;
                    DebugCount(DebugMissWideBy, DebugIntendedId);
                }
            }

            DebugIntendedTarget = null;
            DebugIntendedId = null;
#endif

            _onFinished?.Invoke(this);
        }
    }
}

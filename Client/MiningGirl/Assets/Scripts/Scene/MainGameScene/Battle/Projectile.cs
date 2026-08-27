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
        // 넉백으로 밀리는 거리(유닛). 몬스터의 KnockbackResist로 줄어듭니다.
        private const float KnockbackDistance = 1.5f;

        // 어느 풀에서 나왔는지. ProjectileLauncher가 되돌릴 때 씁니다.
        public string PoolKey { get; set; }

        // 연쇄로 내보낼 발의 스펙. 마스터리를 빼서 다시 연쇄하지 않게 합니다.
        public ProjectileSpec BuildChainSpec()
        {
            return new ProjectileSpec(
                PoolKey,
                _speed,
                _damage,
                0,
                _hitRange,
                _moveType,
                _waveAmplitude,
                _waveCycles);
        }

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

        // 조준한 적. 이 발의 피해량을 여기에 예약해 두었습니다.
        private MonsterUnit _target;

        // 예약을 아직 풀지 않았는지. 명중해도, 그냥 사라져도 한 번만 풀어야 합니다.
        private bool _hasReservation;

        // 이 발에 실린 강화스킬 효과.
        private MasterySpec _mastery;

        // 연쇄가 새 발사체를 만들 때 부릅니다. 발사체 스스로는 풀을 모릅니다.
        private Action<Projectile, MonsterUnit> _onChain;

        // 폭발 범위 안의 대상을 모으는 자리. 매번 새로 만들지 않습니다.
        private readonly List<MonsterUnit> _statusBuffer = new List<MonsterUnit>();

        private bool _isActive;

#if UNITY_EDITOR
        // 명중 진단용 계측입니다. 에디터에서만 돌고 빌드에는 들어가지 않습니다.
        //
        // "안 맞는다"를 고치려면 먼저 갈라야 합니다.
        //  - 판정이 닿았는데 처리되지 않은 것    -> 코드 결함
        //  - 애초에 닿지 않은 것                 -> 유도하지 않는다는 사양대로의 결과
        // 눈으로 보면 둘이 똑같아 보이므로 숫자로만 구분됩니다.
        // 아래 셋은 **조준해서 쏜 발만** 셉니다.
        //
        // 무조준 발사(부채꼴, 그리고 대기하다 위로 흘려보낸 예약분)를 분모에 섞으면
        // 명중률이 떨어지는데 그건 조준이 나빠져서가 아닙니다. 조준 품질과 몬스터
        // 밀도가 한 숫자에 섞이면 어느 쪽이 나빠졌는지 못 봅니다. (기획 규칙)
        public static int DebugFired;
        public static int DebugHit;
        public static int DebugMissed;

        // 무조준 발사. 부채꼴과 흘려보낸 예약분을 갈라 둡니다.
        //
        // 예약분 쪽이 "버리지 않고 쏘게 한" 변경의 실효 화력 상승분입니다.
        // 부채꼴과 합치면 강화스킬을 골랐는지에 따라 숫자가 흔들려 못 읽습니다.
        public static int DebugFiredFan;
        public static int DebugHitFan;
        public static int DebugFiredStray;
        public static int DebugHitStray;

        // 무조준 발이 살아 있던 시간의 합과 개수.
        //
        // 겹침의 직접 원인이 여기 있습니다. 조준 발은 맞으면 사라지는데(가까운 적까지
        // 8~12유닛이면 0.7~1.0초) 무조준은 맞을 게 없어 화면 위 경계까지 갑니다.
        // 그 차이가 쿨 한 바퀴의 일부를 먹으면 앞 볼리가 남은 채로 다음이 나갑니다.
        //
        // 비율만으로는 겹침을 예측 못 합니다 - 사라지는 게 하필 오래 사는 쪽이라
        // 비율이 내려가면 겹침은 그보다 더 크게 줄어듭니다.
        public static float DebugStrayLifeSum;
        public static int DebugStrayLifeCount;

        // 조준 발의 수명. 무조준 것과 나란히 놓아야 두 분포가 갈리는지 보입니다.
        //
        // 조준 발은 맞으면 사라지므로 가까운 적까지의 거리로 끝나고, 무조준은
        // 맞을 게 없어 화면 위 경계까지 갑니다. 그 차이가 겹침의 크기를 정합니다.
        public static float DebugAimedLifeSum;
        public static int DebugAimedLifeCount;

        // 이 발이 몇 번째 볼리에서 나왔는지.
        //
        // 화면에 동시에 뜬 볼리 수를 세는 데 씁니다. 각도가 등차수열인 것끼리
        // 묶어 추론할 수도 있는데, 부채 둘이 겹치면 어디까지가 한 볼리인지
        // 정할 근거가 없습니다. 번호를 달고 다니면 셀 필요가 없습니다.
        //
        // SkillRunner 가 볼리를 시작할 때 넣고, Setup 이 그때 값을 집습니다.
        //
        // 진단용입니다. 게임 로직은 이 값을 읽지 않습니다 - 조준·발사·판정 어디에도
        // 안 들어갑니다. 이 위에 기능을 만들지 마십시오. 빌드에도 안 들어갑니다.
#if UNITY_EDITOR
        public static int DebugNextVolleyId;

        private int _debugVolleyId;

        public int DebugVolleyId => _debugVolleyId;

        // 0=조준, 1=부채꼴, 2=흘려보낸 예약분. 측정에서 셋을 갈라야 합니다.
        public int DebugAimKind => _debugAimKind;
#endif

        // 이 발이 어느 쪽인지. 0=조준, 1=부채꼴, 2=흘려보낸 예약분.
        private int _debugAimKind;

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
            DebugFiredFan = 0;
            DebugHitFan = 0;
            DebugFiredStray = 0;
            DebugHitStray = 0;
            DebugStrayLifeSum = 0f;
            DebugStrayLifeCount = 0;
            DebugAimedLifeSum = 0f;
            DebugAimedLifeCount = 0;
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

        // 조준 시점의 종류 Id. 대상이 풀로 돌아가 다른 종류로 재사용될 수 있어 따로 들고 있습니다.
        private string _debugIntendedId;

        // 조준 시점의 대상 일련번호. 도착했을 때 번호가 다르면 그 사이에 죽고 새로 나온 개체입니다.
        private int _debugIntendedSerial;

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
            MonsterUnit target,
            Action<Projectile> onFinished,
            Action<Projectile, MonsterUnit> onChain = null)
        {
            _field = field;
            _bounds = bounds;
            _onFinished = onFinished;
            _onChain = onChain;
            _mastery = spec.Mastery;

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

            // 이 발의 피해량을 조준한 적에게 예약합니다.
            //
            // 관통이 있어도 첫 대상에만 겁니다. 뒤에 몇 마리가 걸릴지는 발사 시점에
            // 알 수 없어, 걸지 않은 몫을 예약하면 아무도 조준하지 않는 적이 생깁니다.
            _target = target;
            _hasReservation = false;

            if (_target != null && _damage > 0f)
            {
                _target.Reserve(_damage);
                _hasReservation = true;
            }

#if UNITY_EDITOR
            // 조준 대상이 없으면 무조준입니다. 부채꼴은 발사 시점부터 대상을 안 고르고,
            // 나머지는 자기 차례에 조준할 적이 없어 위로 흘려보낸 예약분입니다.
            _debugAimKind = target != null
                ? 0
                : (_mastery.HasValue && _mastery.Type == EMasteryType.FanBurst ? 1 : 2);

            if (_debugAimKind == 0)
                DebugFired++;
            else if (_debugAimKind == 1)
                DebugFiredFan++;
            else
                DebugFiredStray++;

            _debugClosest = float.MaxValue;
            _debugClosestToTarget = float.MaxValue;
            _debugHitAny = false;
            _debugHitTarget = false;
            _debugIntendedId = _target != null && _target.Row != null ? _target.Row.Id : null;
            _debugIntendedSerial = _target != null ? _target.DebugSerial : 0;
            _debugVolleyId = DebugNextVolleyId;
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
            var target = _target;

            if (target == null || !target.IsAlive || target.DebugSerial != _debugIntendedSerial)
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

                // 조준한 그 놈을 맞혔으면 예약을 먼저 풉니다.
                // 피해를 넣기 전에 풀어야 남은 체력과 예약이 어긋나지 않습니다.
                if (unit == _target)
                    ReleaseReservation();

                _field.ApplyDamage(unit, _damage);

                // 강화스킬 효과는 본체 피해가 들어간 뒤에 처리합니다.
                // 폭발이 먼저 들어가면 본체가 죽은 적을 때리게 됩니다.
                ApplyMasteryOnHit(unit);

#if UNITY_EDITOR
                _debugHitAny = true;

                if (unit == _target)
                    _debugHitTarget = true;
#endif

                _remainingHits--;

                if (_remainingHits > 0)
                    continue;

                Finish();

                return;
            }
        }

        // 명중 순간에 얹히는 강화스킬 효과.
        //
        // 연쇄만 발사체를 더 만들고 나머지는 이 자리에서 끝납니다. 연쇄로 나간 발은
        // 다시 연쇄하지 않으므로 스펙에서 마스터리를 빼고 보냅니다.
        private void ApplyMasteryOnHit(MonsterUnit unit)
        {
            if (!_mastery.HasValue)
                return;

            switch (_mastery.Type)
            {
                case EMasteryType.Explosion:
                {
                    var center = unit.Position;

                    // 피해보다 먼저 띄웁니다. 한 방에 죽는 대상이면 피해 처리 중에
                    // 몬스터가 사라져 터진 자리를 알 수 없게 됩니다.
                    _field.PlayEffect(_mastery.EffectAssetId, center);

                    // 본체에 맞은 대상은 폭발 피해를 중복으로 받지 않습니다.
                    _field.ApplyAreaDamage(center, _mastery.Range, _damage * _mastery.Value, unit);

                    // 상태이상과 넉백은 본체를 맞은 대상에게도 겁니다.
                    ApplyStatusInRadius(center, _mastery.Range);

                    break;
                }

                case EMasteryType.ChainOnHit:
                {
                    Chain(unit);

                    break;
                }

                default:
                {
                    // 부채꼴은 발사 시점에 처리되므로 명중 때는 상태이상만 겁니다.
                    ApplyStatusTo(unit);

                    break;
                }
            }
        }

        private void ApplyStatusInRadius(Vector3 center, float radius)
        {
            if (_mastery.StatusType == EStatusEffectType.None && !_mastery.Knockback)
                return;

            _field.FillInRadius(center, radius, _statusBuffer);

            for (var i = 0; i < _statusBuffer.Count; i++)
                ApplyStatusTo(_statusBuffer[i]);
        }

        private void ApplyStatusTo(MonsterUnit unit)
        {
            if (unit == null || !unit.IsAlive)
                return;

            if (_mastery.StatusType != EStatusEffectType.None)
            {
                // 화상 세기는 발동한 스킬의 현재 위력에 비례합니다.
                unit.ApplyStatus(_mastery.StatusType, _mastery.StatusDuration, _damage * _mastery.StatusValue);
            }

            if (_mastery.Knockback)
                unit.Knockback(KnockbackDistance);
        }

        // 명중 지점에서 다른 적으로 한 발 더 보냅니다.
        //
        // 방금 맞은 대상은 제외하고, 조준 규칙과 예측 사격을 그대로 적용합니다.
        // 유효 대상이 없으면 연쇄하지 않습니다.
        private void Chain(MonsterUnit from)
        {
            var count = Mathf.RoundToInt(_mastery.Value);

            if (count <= 0 || _onChain == null)
                return;

            for (var i = 0; i < count; i++)
                _onChain(this, from);
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
            // 흘려보낸 예약분만 셉니다. 부채꼴은 강화스킬을 골랐는지에 따라 섞입니다.
            if (_debugAimKind == 2)
            {
                DebugStrayLifeSum += _elapsed;
                DebugStrayLifeCount++;
            }
            else if (_debugAimKind == 0)
            {
                DebugAimedLifeSum += _elapsed;
                DebugAimedLifeCount++;
            }
#endif

#if UNITY_EDITOR
            // 무조준 발은 조준 통계에 넣지 않습니다. 맞았는지만 따로 셉니다.
            if (_debugAimKind != 0)
            {
                if (_debugHitAny)
                {
                    if (_debugAimKind == 1)
                        DebugHitFan++;
                    else
                        DebugHitStray++;
                }
            }
            else if (_debugHitTarget)
            {
                DebugHit++;
                DebugCount(DebugFiredBy, _debugIntendedId);
                DebugCount(DebugHitTargetBy, _debugIntendedId);
            }
            else if (_debugHitAny)
            {
                DebugHit++;
                DebugCount(DebugFiredBy, _debugIntendedId);
                DebugCount(DebugHitOtherBy, _debugIntendedId);
            }
            else
            {
                DebugMissed++;
                DebugCount(DebugFiredBy, _debugIntendedId);

                if (_debugClosest < float.MaxValue)
                    DebugMissClosest.Add(_debugClosest);

                var target = _target;
                var targetGone = target == null
                    || !target.IsAlive
                    || target.DebugSerial != _debugIntendedSerial;

                if (targetGone)
                {
                    DebugMissTargetDead++;
                    DebugMissTargetGone++;
                    DebugCount(DebugMissGoneBy, _debugIntendedId);
                }
                else if (_debugClosestToTarget <= 1.5f)
                {
                    DebugMissNear++;
                    DebugCount(DebugMissNearBy, _debugIntendedId);
                }
                else
                {
                    DebugMissWide++;
                    DebugCount(DebugMissWideBy, _debugIntendedId);
                }
            }

            _debugIntendedId = null;
#endif

            // 맞히지 못하고 사라진 발도 예약을 풀어야 합니다.
            // 남겨 두면 그 적은 죽을 예정으로 취급돼 아무도 다시 조준하지 않습니다.
            ReleaseReservation();

            _target = null;

            _onFinished?.Invoke(this);
        }

        private void ReleaseReservation()
        {
            if (!_hasReservation)
                return;

            _hasReservation = false;

            if (_target != null)
                _target.ReleaseReservation(_damage);
        }
    }
}

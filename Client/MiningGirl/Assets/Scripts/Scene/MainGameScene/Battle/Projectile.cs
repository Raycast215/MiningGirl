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
        }

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
                // 몸통 폭이 1.8유닛인데 0.3만으로 중심을 맞히라고 하면 거의 스쳐 지나갑니다.
                var reach = _hitRange + MonsterUnit.BodyRadius;

                if (SqrDistanceToSegment(unit.Position, from, to) > reach * reach)
                    continue;

                _hit.Add(unit);
                unit.TakeDamage(_damage);

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
            _onFinished?.Invoke(this);
        }
    }
}

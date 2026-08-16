using System;
using System.Collections.Generic;
using Scene.InGame.Entity.Interface;
using UnityEngine;

namespace MainGame.Card.Effects
{
    // 캐릭터 주위를 돌다가 최대 범위 안의 적에게 날아가 부딪히고 돌아오는 불덩이.
    //
    // 표적이 없을 때 가만히 붙어 있으면 죽은 오브젝트처럼 보여서,
    // 대기 중에는 캐릭터 주위를 천천히 자전하게 했습니다.
    public class FireBallObject : MonoBehaviour
    {
        // 게임이 멈춘 동안에는 불덩이도 멈춥니다.
        // 지속시간도 함께 멈춰야 정지 시간만큼 손해 보지 않습니다.
        private static bool _isPausedAll;

        // 스테이지 정리 때 한꺼번에 없애기 위한 목록
        private static readonly List<FireBallObject> Actives = new List<FireBallObject>();

        public static void SetPausedAll(bool paused)
        {
            _isPausedAll = paused;
        }

        // 스테이지 재시작 시 남아있는 불덩이를 모두 정리합니다.
        public static void ClearAll()
        {
            _isPausedAll = false;

            for (var i = Actives.Count - 1; i >= 0; i--)
            {
                if (Actives[i] != null)
                    Destroy(Actives[i].gameObject);
            }

            Actives.Clear();
        }

        private enum EState
        {
            Idle,       // 캐릭터 주위를 자전하며 대기
            Seek,       // 목표에게 날아가는 중
            Return,     // 캐릭터에게 돌아오는 중
        }

        private Func<Vector3> _getCenter;
        private Func<IReadOnlyList<IEntity>> _getMonsters;

        private float _maxRange;        // 적을 찾을 최대 거리(캐릭터 기준)
        private float _hitRadius;       // 부딪혔다고 볼 거리
        private float _moveSpeed;       // 날아가는 속도
        private float _idleDistance;    // 자전 반경
        private float _orbitSpeed;      // 자전 속도(초당 각도)
        private float _damage;
        private float _duration;

        private float _elapsed;
        private EState _state = EState.Idle;
        private IEntity _target;
        private float _orbitAngle;

        public void Init(
            Func<Vector3> getCenter,
            Func<IReadOnlyList<IEntity>> getMonsters,
            float maxRange,
            float damage,
            float duration,
            float moveSpeed,
            float hitRadius,
            float idleDistance,
            float orbitSpeed)
        {
            _getCenter = getCenter;
            _getMonsters = getMonsters;
            _maxRange = maxRange;
            _damage = damage;
            _duration = duration;
            _moveSpeed = moveSpeed;
            _hitRadius = hitRadius;
            _idleDistance = idleDistance;
            _orbitSpeed = orbitSpeed;
            _orbitAngle = UnityEngine.Random.Range(0f, 360f);

            transform.position = GetOrbitPosition();
        }

        private void OnEnable()
        {
            if (!Actives.Contains(this))
                Actives.Add(this);
        }

        private void OnDisable()
        {
            Actives.Remove(this);
        }

        private void Update()
        {
            // 정지 중에는 아무것도 하지 않습니다(지속시간도 흐르지 않음).
            if (_isPausedAll)
                return;

            var delta = Time.deltaTime;

            _elapsed += delta;

            if (_elapsed >= _duration)
            {
                Destroy(gameObject);
                return;
            }

            // 대기든 복귀든 궤도는 계속 돕니다(복귀 지점이 살아 움직이게).
            _orbitAngle += _orbitSpeed * delta;

            switch (_state)
            {
                case EState.Idle:
                    UpdateIdle();
                    break;

                case EState.Seek:
                    UpdateSeek(delta);
                    break;

                case EState.Return:
                    UpdateReturn(delta);
                    break;
            }
        }

        // 캐릭터 주위를 자전하며 사거리 안에 적이 들어오길 기다립니다.
        private void UpdateIdle()
        {
            transform.position = GetOrbitPosition();

            var target = FindTarget();
            if (target == null)
                return;

            _target = target;
            _state = EState.Seek;
        }

        // 목표에게 날아가 부딪히면 피해를 주고 돌아섭니다.
        private void UpdateSeek(float delta)
        {
            // 가는 도중 목표가 죽거나 사거리를 벗어나면 다른 적을 찾습니다.
            if (_target == null || !_target.GetActiveState() || !IsInRange(_target.GetPosition()))
            {
                _target = FindTarget();

                if (_target == null)
                {
                    _state = EState.Return;
                    return;
                }
            }

            var targetPosition = _target.GetPosition();

            MoveTowards(targetPosition, delta);

            if (Vector3.Distance(transform.position, targetPosition) > _hitRadius)
                return;

            _target.Hit(_damage, false);

            _target = null;
            _state = EState.Return;
        }

        // 궤도로 돌아오고, 도착하면 다시 자전하며 다음 목표를 찾습니다.
        private void UpdateReturn(float delta)
        {
            var orbit = GetOrbitPosition();

            MoveTowards(orbit, delta);

            if (Vector3.Distance(transform.position, orbit) > 0.1f)
                return;

            _state = EState.Idle;
        }

        private void MoveTowards(Vector3 destination, float delta)
        {
            transform.position = Vector3.MoveTowards(transform.position, destination, _moveSpeed * delta);
        }

        // 캐릭터 기준 자전 궤도 위의 현재 위치
        private Vector3 GetOrbitPosition()
        {
            var center = _getCenter?.Invoke() ?? transform.position;
            var rad = _orbitAngle * Mathf.Deg2Rad;

            return center + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * _idleDistance;
        }

        private bool IsInRange(Vector3 position)
        {
            var center = _getCenter?.Invoke() ?? transform.position;

            return Vector3.Distance(center, position) <= _maxRange;
        }

        // 캐릭터 기준 최대 범위 안에서 가장 가까운 적
        private IEntity FindTarget()
        {
            var monsters = _getMonsters?.Invoke();
            if (monsters == null)
                return null;

            var center = _getCenter?.Invoke() ?? transform.position;

            IEntity nearest = null;
            var nearestDistance = float.MaxValue;

            foreach (var monster in monsters)
            {
                if (monster == null || !monster.GetActiveState())
                    continue;

                var distance = Vector3.Distance(center, monster.GetPosition());

                if (distance > _maxRange || distance >= nearestDistance)
                    continue;

                nearest = monster;
                nearestDistance = distance;
            }

            return nearest;
        }
    }
}

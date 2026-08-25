using System;
using System.Collections.Generic;
using Scene.InGame.Entity.Interface;
using UnityEngine;

namespace MainGame.Card.Effects
{
    // 캐릭터 자리에서 카드를 놓은 방향으로 곧게 날아가는 얼음 화살.
    //
    // 파이어볼은 알아서 적을 찾아가지만 이쪽은 '방향'만 보고 날아갑니다.
    // 빗나갈 수 있다는 것이 이 카드의 값어치라, 날아가는 도중에 목표를 다시 찾지 않습니다.
    // 처음 닿은 적 하나에게 피해를 주고 그 자리에서 사라집니다.
    public class IceBoltObject : SkillEffectObjectBase
    {
        private Func<IReadOnlyList<IEntity>> _getMonsters;

        private Vector3 _direction;
        private float _damage;
        private float _moveSpeed;
        private float _hitRadius;
        private float _maxDistance;

        private float _traveled;

        // 명중 처리 뒤 Destroy가 실제로 반영되기까지 한 프레임이 남을 수 있어,
        // 그 사이에 두 번 때리지 않도록 잠급니다.
        private bool _isSpent;

        public void Init(
            Func<IReadOnlyList<IEntity>> getMonsters,
            Vector3 direction,
            float damage,
            float moveSpeed,
            float hitRadius,
            float maxDistance)
        {
            _getMonsters = getMonsters;
            _direction = direction.normalized;
            _damage = damage;
            _moveSpeed = moveSpeed;
            _hitRadius = hitRadius;
            _maxDistance = maxDistance;

            // 스프라이트가 오른쪽을 향해 그려져 있어 진행 방향으로 돌려줍니다.
            var angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;

            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void Update()
        {
            if (IsPausedAll || _isSpent)
                return;

            var step = _moveSpeed * Time.deltaTime;

            if (step <= 0f)
                return;

            // 목록을 프레임당 한 번만 받습니다.
            // (제공자가 매 호출 새 리스트를 만들어서, 아래 잘게 쪼갠 이동마다 부르면 낭비입니다.)
            var monsters = _getMonsters != null ? _getMonsters.Invoke() : null;

            // 한 프레임에 히트 반경보다 멀리 움직이면 적을 그냥 통과해 버립니다.
            // 이동을 반경 이하로 쪼개서 그 사이 지점도 검사합니다.
            var remain = step;
            var maxStep = Mathf.Max(0.01f, _hitRadius);

            while (remain > 0f)
            {
                var move = Mathf.Min(remain, maxStep);

                transform.position += _direction * move;
                _traveled += move;
                remain -= move;

                var target = FindHit(monsters);

                if (target != null)
                {
                    _isSpent = true;

                    target.Hit(_damage, false);

                    // 맞은 그 자리에서 이펙트를 없앱니다.
                    Destroy(gameObject);

                    return;
                }

                // 사거리를 다 쓰면 허공에서 사라집니다.
                if (_traveled < _maxDistance)
                    continue;

                Destroy(gameObject);

                return;
            }
        }

        private IEntity FindHit(IReadOnlyList<IEntity> monsters)
        {
            if (monsters == null)
                return null;

            var position = transform.position;
            var radiusSqr = _hitRadius * _hitRadius;

            for (var i = 0; i < monsters.Count; i++)
            {
                var monster = monsters[i];

                if (monster == null || !monster.GetActiveState())
                    continue;

                var offset = monster.GetPosition() - position;

                // 2D라 깊이는 보지 않습니다.
                offset.z = 0f;

                if (offset.sqrMagnitude > radiusSqr)
                    continue;

                return monster;
            }

            return null;
        }
    }
}

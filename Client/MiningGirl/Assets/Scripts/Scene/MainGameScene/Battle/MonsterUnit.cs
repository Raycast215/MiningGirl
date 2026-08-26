using System;
using Data;
using UnityEngine;

namespace Scene.MainGameScene.Battle
{
    // 몬스터 한 마리.
    //
    // x를 고정한 채 아래로만 내려오고, 타워 사거리에 닿으면 멈춰서 때립니다.
    // 방향 전환도 회피도 없으므로 공간 탐색 구조가 필요 없습니다.
    public class MonsterUnit : MonoBehaviour
    {
        // 명중 판정에 쓰는 몸통 반경.
        //
        // 스프라이트는 1.28유닛이지만 투명 여백을 빼면 실제 몸통은 0.88~1.28유닛입니다.
        // 종별로는 0.44~0.64로 갈리는데, 한 값으로 묶어 가장 흔한 크기에 맞췄습니다.
        // 여백까지 판정에 넣으면 스쳐 지나간 발사체가 맞은 것으로 보입니다.
        public const float BodyRadius = 0.5f;

        // 타워 앞에 나란히 설 때의 최소 간격. 스프라이트 폭(1.28)보다 조금 넓게 잡아야 안 겹칩니다.
        public const float BlockedSpacing = 1.3f;

        [SerializeField]
        private SpriteRenderer spriteRenderer;

        public MonsterDataTableRow Row { get; private set; }
        public bool IsAlive { get; private set; }

        // 타워 앞에 멈춰 선 상태. 겹침 정리는 멈춘 개체끼리만 합니다.
        public bool IsBlocked { get; private set; }

        public Vector3 Position => transform.position;

        private Tower _tower;
        private Action<MonsterUnit> _onDied;
        private Action<MonsterUnit> _onBlocked;

        private float _maxHealth;
        private float _currentHealth;
        private float _moveSpeed;
        private float _damage;
        private float _attackDelay;
        private float _stopY;

        private float _attackTimer;

        public void Setup(
            MonsterDataTableRow row,
            float statMultiplier,
            Tower tower,
            Vector3 spawnPosition,
            Action<MonsterUnit> onDied,
            Action<MonsterUnit> onBlocked)
        {
            Row = row;
            _tower = tower;
            _onDied = onDied;
            _onBlocked = onBlocked;

            // 스테이지 난이도 배율은 체력과 공격력 양쪽에 곱합니다.
            _maxHealth = Mathf.Max(1f, row.MaxHealth * statMultiplier);
            _currentHealth = _maxHealth;
            _damage = row.Damage * statMultiplier;

            _moveSpeed = Mathf.Max(0f, row.MoveSpeed);
            _attackDelay = Mathf.Max(0.05f, row.AttackDelay);

            // 사거리는 타워 윗면부터 잽니다. 몬스터 발끝이 타워에 닿아 보이는 높이입니다.
            _stopY = (tower != null ? tower.TopY : 0f) + row.AttackDistance;

            transform.position = spawnPosition;

            IsAlive = true;
            IsBlocked = false;
            _attackTimer = 0f;

            if (spriteRenderer != null)
                spriteRenderer.enabled = true;
        }

        // MainGameController가 매 프레임 돌려줍니다.
        // Update를 마리 수만큼 두지 않는 편이 호출 비용이 적습니다.
        public void Tick(float deltaTime)
        {
            if (!IsAlive)
                return;

            if (!IsBlocked)
            {
                var next = transform.position;
                next.y -= _moveSpeed * deltaTime;

                var arrived = next.y <= _stopY;

                if (arrived)
                {
                    next.y = _stopY;
                    IsBlocked = true;

                    // 도착하자마자 한 대 때리지 않도록 주기를 채우고 시작합니다.
                    _attackTimer = _attackDelay;
                }

                transform.position = next;

                // 겹침 정리는 자리를 잡은 뒤에 해야 합니다.
                if (arrived)
                    _onBlocked?.Invoke(this);

                return;
            }

            if (_tower == null || !_tower.IsAlive)
                return;

            _attackTimer -= deltaTime;

            if (_attackTimer > 0f)
                return;

            _attackTimer += _attackDelay;
            _tower.TakeDamage(_damage);
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive || amount <= 0f)
                return;

            _currentHealth -= amount;

            if (_currentHealth > 0f)
                return;

            IsAlive = false;

            // 풀로 돌아가는 건 다음 프레임이라, 그때까지 시체가 화면에 남지 않게 바로 끕니다.
            gameObject.SetActive(false);

            _onDied?.Invoke(this);
        }

        // 타워 앞에 멈춘 개체가 겹쳐 보이지 않도록 MonsterField가 x만 밀어 줍니다.
        public void NudgeX(float x)
        {
            var position = transform.position;
            position.x = x;
            transform.position = position;
        }
    }
}

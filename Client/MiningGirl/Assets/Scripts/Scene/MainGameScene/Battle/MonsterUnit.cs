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
        [SerializeField]
        [Tooltip("명중 판정에 쓰는 몸통 반경(스케일 적용 전). 스프라이트의 불투명 영역 폭 ÷ 2입니다.")]
        private float bodyRadius = 0.5f;

        // 실제 판정에 쓰는 반경. 프리팹 스케일이 걸린 정예·보스는 그만큼 커집니다.
        //
        // 스프라이트 전체(1.28유닛)가 아니라 불투명 영역을 씁니다.
        // 투명 여백까지 판정에 넣으면 스쳐 지나간 발사체가 맞은 것으로 보입니다.
        public float BodyRadius => bodyRadius * Mathf.Abs(transform.lossyScale.x);

        // 타워 앞에 나란히 설 때의 최소 간격. 그림 폭(1.28)보다 조금 넓게 잡아야 안 겹칩니다.
        public const float BlockedSpacing = 1.3f;

        // 보스에게 걸리는 빙결 지속시간 배율.
        //
        // 보스는 유일하게 시간이 위협인 상대입니다. 3초가 그대로 걸리면 보스전이
        // "묶고 때리기"가 되어 그 축이 통째로 지워집니다.
        public const float BossFreezeRate = 0.3f;

        [SerializeField]
        private SpriteRenderer spriteRenderer;

        public MonsterDataTableRow Row { get; private set; }
        public bool IsAlive { get; private set; }

        // 타워 앞에 멈춰 선 상태. 겹침 정리는 멈춘 개체끼리만 합니다.
        public bool IsBlocked { get; private set; }

        public Vector3 Position => transform.position;

        // 이 몬스터에게 이미 날아오고 있는 피해량의 합.
        //
        // 세지 않으면 쿨이 겹치는 순간 여러 발이 같은 놈에게 몰리고, 첫 발이 죽이면
        // 나머지는 시체로 날아갑니다. 위력이 체력에 맞춰져 있어 그 낭비가 100%입니다.
        public float ReservedDamage { get; private set; }

        // 조준해도 되는 대상인지. 이미 죽을 만큼 예약이 걸린 놈은 제외합니다.
        public bool IsTargetable => IsAlive && _currentHealth - ReservedDamage > 0f;

        // 예측 사격이 쓰는 현재 하강 속도.
        //
        // 타워 앞에 멈춰 선 뒤에는 움직이지 않으므로 0입니다. 빙결도 마찬가지입니다 -
        // 이 값이 틀리면 멈춰 있는 적을 앞질러 쏘게 됩니다.
        public float MoveSpeed => IsBlocked || IsFrozen ? 0f : _moveSpeed;

        // 빙결 중인지. 이동과 공격이 함께 멈춥니다.
        public bool IsFrozen => _freezeRemaining > 0f;

        // 화상 중인지. 표시용입니다.
        public bool IsBurning => _burnRemaining > 0f;

        public void Reserve(float amount)
        {
            if (amount > 0f)
                ReservedDamage += amount;
        }

        // 명중했거나 발사체가 사라졌을 때 풉니다.
        //
        // 대상이 먼저 죽으면 예약이 통째로 지워지므로, 뒤늦게 푸는 발이 있어도
        // 음수로 내려가지 않게 막습니다.
        public void ReleaseReservation(float amount)
        {
            ReservedDamage = Mathf.Max(0f, ReservedDamage - amount);
        }

#if UNITY_EDITOR
        // 스폰될 때마다 올라가는 일련번호입니다.
        //
        // 풀에서 재사용되므로 같은 오브젝트가 다른 몬스터로 되살아납니다. 발사체가
        // "조준한 그 놈이 아직 살아 있나"를 참조로만 보면, 죽고 다시 나온 개체를
        // 살아 있다고 착각합니다. 진단에서만 씁니다.
        private static int _debugSerialSeed;

        public int DebugSerial { get; private set; }
#endif

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

        // 남은 빙결 시간. 0보다 크면 멈춥니다.
        private float _freezeRemaining;

        // 남은 화상 시간과 초당 피해.
        private float _burnRemaining;
        private float _burnPerSecond;

        // 화상 피해를 넣어 줄 곳. 숫자 표시와 처치 처리를 거치려면 필드를 통해야 합니다.
        private Action<MonsterUnit, float> _onStatusDamage;

        public void Setup(
            MonsterDataTableRow row,
            float statMultiplier,
            Tower tower,
            Vector3 spawnPosition,
            Action<MonsterUnit> onDied,
            Action<MonsterUnit> onBlocked,
            Action<MonsterUnit, float> onStatusDamage)
        {
            Row = row;
            _tower = tower;
            _onDied = onDied;
            _onBlocked = onBlocked;
            _onStatusDamage = onStatusDamage;

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
            ReservedDamage = 0f;

            // 풀에서 재사용되므로 이전 개체의 상태가 남지 않게 지웁니다.
            _freezeRemaining = 0f;
            _burnRemaining = 0f;
            _burnPerSecond = 0f;

#if UNITY_EDITOR
            DebugSerial = ++_debugSerialSeed;
#endif

            if (spriteRenderer != null)
                spriteRenderer.enabled = true;
        }

        // MainGameController가 매 프레임 돌려줍니다.
        // Update를 마리 수만큼 두지 않는 편이 호출 비용이 적습니다.
        public void Tick(float deltaTime)
        {
            if (!IsAlive)
                return;

            TickStatus(deltaTime);

            // 화상으로 죽었으면 이번 프레임은 여기까지입니다.
            if (!IsAlive)
                return;

            // 빙결 중에는 내려오지도 때리지도 않습니다.
            if (IsFrozen)
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

        // 이 몬스터에게 실제로 적용된 피해를 돌려줍니다.
        //
        // 화면에 띄우는 숫자는 이 값이어야 합니다. 표기 위력이나 예약치를 띄우면
        // 나중에 방어력·저항이 붙는 순간 화면과 실제가 갈라집니다. 경감이 생기면
        // 여기서 빼고, 뺀 뒤의 값을 돌려주면 화면이 저절로 따라옵니다.
        //
        // 남은 체력으로 자르지는 않습니다. 체력 12에 위력 39가 들어가면 12가 아니라
        // 39를 띄웁니다 - 넘치는 건 경감이 아니라 초과 피해라 다른 이야기입니다.
        public float TakeDamage(float amount)
        {
            if (!IsAlive || amount <= 0f)
                return 0f;

            var applied = amount;

            _currentHealth -= applied;

            if (_currentHealth > 0f)
                return applied;

            IsAlive = false;

            // 죽으면 걸려 있던 예약은 의미가 없습니다. 남겨 두면 풀에서 다시 나올 때
            // 이미 예약이 찬 상태로 시작해 아무도 조준하지 않습니다.
            ReservedDamage = 0f;

            // 풀로 돌아가는 건 다음 프레임이라, 그때까지 시체가 화면에 남지 않게 바로 끕니다.
            gameObject.SetActive(false);

            _onDied?.Invoke(this);

            return applied;
        }

#region 상태이상

        // 빙결·화상을 거는 창구. MonsterField가 부릅니다.
        //
        // 같은 상태를 다시 걸면 지속시간을 갱신합니다(합산 아님). 강화스킬이 런당
        // 하나뿐이라 두 상태가 겹칠 일이 없어 중첩 규칙을 두지 않습니다.
        public void ApplyStatus(EStatusEffectType type, float duration, float perSecondDamage)
        {
            if (!IsAlive || duration <= 0f)
                return;

            switch (type)
            {
                case EStatusEffectType.Freeze:
                {
                    // 보스는 시간이 유일한 위협이라 묶이면 그 축이 지워집니다.
                    var scaled = Row != null && Row.MonsterType == EMonsterType.Boss
                        ? duration * BossFreezeRate
                        : duration;

                    _freezeRemaining = Mathf.Max(_freezeRemaining, scaled);

                    break;
                }

                case EStatusEffectType.Burn:
                {
                    _burnRemaining = Mathf.Max(_burnRemaining, duration);
                    _burnPerSecond = Mathf.Max(_burnPerSecond, perSecondDamage);

                    break;
                }
            }
        }

        // 밀어냅니다. 저항이 1이면 꿈쩍하지 않습니다.
        //
        // 몬스터는 x가 고정이고 아래로만 오므로 위로 밉니다. 타워 앞에 멈춰 있었다면
        // 그 상태를 풀어야 합니다 - 안 풀면 밀려난 자리에서 계속 타워를 때립니다.
        public void Knockback(float distance)
        {
            if (!IsAlive || distance <= 0f)
                return;

            var resist = Row != null ? Mathf.Clamp01(Row.KnockbackResist) : 0f;
            var applied = distance * (1f - resist);

            if (applied <= 0f)
                return;

            var position = transform.position;
            position.y += applied;
            transform.position = position;

            if (!IsBlocked)
                return;

            IsBlocked = false;
            _attackTimer = 0f;
        }

        private void TickStatus(float deltaTime)
        {
            if (_freezeRemaining > 0f)
                _freezeRemaining -= deltaTime;

            if (_burnRemaining <= 0f)
                return;

            _burnRemaining -= deltaTime;

            var tick = _burnPerSecond * deltaTime;

            if (tick <= 0f)
                return;

            // 화상은 발사체가 아니라 상태가 넣는 피해입니다. 숫자 표시와 처치 처리를
            // 발사체와 같은 자리에서 하려고 필드로 돌립니다.
            _onStatusDamage?.Invoke(this, tick);
        }

#endregion

        // 타워 앞에 멈춘 개체가 겹쳐 보이지 않도록 MonsterField가 x만 밀어 줍니다.
        public void NudgeX(float x)
        {
            var position = transform.position;
            position.x = x;
            transform.position = position;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using BehaviourTree;
using Cysharp.Threading.Tasks;
using Scene.InGame.Entity;
using Scene.InGame.Entity.Interface;
using UnityEngine;

namespace MainGame.Entity.Monster
{
    public class Monster : EntityBase
    {
        public event Action<Monster> OnDeath;

        private MonsterController _owner;
        private MonsterBaseStat _baseStat;
        private IStageMonsterModifier _stageModifier;
        private IRiskCardMonsterModifier _riskModifier;
        private int _stageIndex;

        private float _currentHp;
        private bool _isDead;

        // MonsterController.Spawn()에서 호출됩니다. 스폰 시점마다 스탯/타겟/보정을 다시 세팅합니다.
        public void Setup(
            MonsterController owner,
            MonsterBaseStat baseStat,
            IStageMonsterModifier stageModifier,
            IRiskCardMonsterModifier riskModifier,
            int stageIndex,
            IEntity target)
        {
            _owner = owner;
            _baseStat = baseStat;
            _stageModifier = stageModifier;
            _riskModifier = riskModifier;
            _stageIndex = stageIndex;

            _isDead = false;
            _currentHp = GetMaxHp();

            SetTarget(target);
        }

        public float GetMaxHp()
        {
            var stageMul = _stageModifier?.GetHpMultiplier(_stageIndex) ?? 1f;
            var riskMul = _riskModifier?.GetHpMultiplier() ?? 1f;
            return _baseStat.Hp * stageMul * riskMul;
        }

        public float GetCurrentHp() => _currentHp;

        public int GetGoldReward()
        {
            var riskGoldMul = _riskModifier?.GetGoldMultiplier() ?? 1f;
            return Mathf.RoundToInt(_baseStat.GoldReward * riskGoldMul);
        }

        // 화면 밖에 있을 때 렌더링을 꺼서 최적화하기 위해 MonsterController가 호출합니다.
        public void SetRendererVisible(bool visible)
        {
            if (spriteRenderer != null)
                spriteRenderer.enabled = visible;
        }

#region EntityBase

        public override async UniTaskVoid InitAsync()
        {
            base.InitAsync().Forget();

            // 몬스터는 항상 타겟(플레이어)을 향해 이동만 하면 되므로, 별도 타겟 탐색 없이
            // 이동 노드만 시퀀스로 연결합니다. 공격 노드는 필요해지면 이어서 추가합니다.
            // 몬스터끼리 너무 붙어 보이지 않도록 기본값보다 겹침 보정을 넓게 잡습니다.
            MoveNode.SetSeparationDistance(1.0f)
                .SetSeparationStrength(0.5f)
                .SetMaxSeparationOffset(0.8f);

            NodeRunner = new NodeRunner(new SequenceNode(new List<INode>
            {
                new ActionNode(MoveNode.ProcessNode),
            }));

            IsInitialized = true;
        }

        // IEnumerable<T>는 공변(covariant)이라 List<Monster>를 IEnumerable<IEntity>로 그대로 반환할 수 있습니다.
        // (이전에는 .Cast<IEntity>()로 매 프레임/몬스터마다 새 이터레이터를 할당하고 있었습니다 — GC 부담의 원인)
        public override IEnumerable<IEntity> GetNearCheckEntities()
        {
            return _owner?.ActivateList;
        }

        public override void Hit(float damage, bool isCritical)
        {
            if (_isDead)
                return;

            _currentHp -= damage;

            if (_currentHp <= 0f)
            {
                _isDead = true;
                OnDeath?.Invoke(this);
            }
        }

        public override float GetDamage()
        {
            var stageMul = _stageModifier?.GetDamageMultiplier(_stageIndex) ?? 1f;
            return _baseStat.Damage * stageMul;
        }

        public override float GetAttackDistance()
        {
            return _baseStat.AttackDistance;
        }

        public override float GetAttackDelay()
        {
            return _baseStat.AttackDelay;
        }

        public override float GetMoveSpeed()
        {
            var stageMul = _stageModifier?.GetMoveSpeedMultiplier(_stageIndex) ?? 1f;
            return _baseStat.MoveSpeed * stageMul;
        }

        // 몬스터는 아직 치명타/추가타 대상이 아니라 0으로 고정해둡니다. 필요해지면 확장합니다.
        public override float GetCriDamage() => 0f;
        public override float GetCriRate() => 0f;
        public override float GetExtraHitRate() => 0f;

#endregion
    }
}

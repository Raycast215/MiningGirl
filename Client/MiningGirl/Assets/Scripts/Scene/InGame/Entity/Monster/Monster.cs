using System;
using System.Collections.Generic;
using System.Linq;
using BehaviourTree;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using Scene.InGame.Entity;
using Scene.InGame.Entity.Interface;
using UnityEngine;

namespace MainGame.Entity.Monster
{
    public class Monster : EntityBase
    {
        private IMonsterDeathHandler _deathHandler;

        private MonsterController _owner;
        private MonsterBaseStat _baseStat;
        private IStageMonsterModifier _stageModifier;
        private IRiskCardMonsterModifier _riskModifier;
        private IFloatingDamagePresenter _damagePresenter;
        private int _stageIndex;

        private float _currentHp;
        private bool _isDead;

        private Tween _posTween;
        private Tween _colorTween;
        
        // MonsterController.Spawn()에서 호출됩니다. 스폰 시점마다 스탯/타겟/보정을 다시 세팅합니다.
        public void Setup(
            MonsterController owner,
            MonsterBaseStat baseStat,
            IStageMonsterModifier stageModifier,
            IRiskCardMonsterModifier riskModifier,
            int stageIndex,
            IEntity target,
            IFloatingDamagePresenter damagePresenter = null,
            IMonsterDeathHandler deathHandler = null)
        {
            _owner = owner;
            _deathHandler = deathHandler;
            _baseStat = baseStat;
            _stageModifier = stageModifier;
            _riskModifier = riskModifier;
            _damagePresenter = damagePresenter;
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
                .SetMaxSeparationOffset(0.8f)
                // 광물은 몬스터끼리보다 훨씬 넓고 강하게 비껴가도록 별도 파라미터를 적용합니다.
                .SetObstacleProvider(() => _owner?.GetObstacles())
                .SetObstacleAvoidance(3.5f, 1.2f, 1.5f);

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
            // 몬스터끼리의 겹침 보정 대상입니다. 광물은 여기 포함하지 않고,
            // MoveNode의 장애물 회피(더 넓은 반경/강한 힘)로 따로 처리합니다.
            return _owner?.ActivateList;
        }

        public override void Hit(float damage, bool isCritical)
        {
            if (_isDead)
                return;

            _currentHp -= damage;

            // 받은 데미지를 몬스터 위치에 플로팅 숫자로 표시합니다.
            _damagePresenter?.Show(Mathf.RoundToInt(damage), GetPosition(), isCritical);

            if (_currentHp <= 0f)
            {
                _isDead = true;

                // 죽으면 진행 중이던 넉백/색상 트윈을 정리하고 색을 원복한 뒤 풀로 반환합니다.
                // (반환된 오브젝트에 트윈이 남아 재스폰 시 위치/색이 튀는 것을 방지)
                if (_posTween != null) { _posTween.Kill(); _posTween = null; }
                if (_colorTween != null) { _colorTween.Kill(); _colorTween = null; }
                if (spriteRenderer != null) spriteRenderer.color = new Color(1f, 1f, 1f, 1f);

                _deathHandler?.OnMonsterDeath(this);
                return;
            }
            
            if (_posTween != null)
            {
                _posTween.Kill();
                _posTween = null;
            }

            var playerPos = Target.GetPosition();
            var myPos = transform.position;
            var vec = (playerPos - myPos).normalized;
            
            _posTween = transform.DOMove(myPos - vec, 0.2f);
            
            if (_colorTween != null)
            {
                _colorTween.Kill();
                _colorTween = null;
                spriteRenderer.color = new Color(1f, 1f, 1f, 1f);
            }
            
            _colorTween = spriteRenderer
                .DOColor(new Color(1f, 0f, 0f, 1f), 0.2f)
                .OnComplete(() => spriteRenderer.color = new Color(1f, 1f, 1f, 1f));
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

using Scene.InGame.Entity.Node;
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
        private AttackNode _attackNode;

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


        // 처치될 때 호출됩니다(골드 지급용). 스폰할 때 주입합니다.
        private System.Action<int> _onKilled;

        public void SetKilledHandler(System.Action<int> handler)
        {
            _onKilled = handler;
        }

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

            // 몬스터는 스폰 시 지정된 타겟(플레이어)을 향해 이동하다가 사거리에 들면 공격합니다.
            // 몬스터끼리 너무 붙어 보이지 않도록 기본값보다 겹침 보정을 넓게 잡습니다.
            MoveNode.SetSeparationDistance(1.0f)
                .SetSeparationStrength(0.5f)
                .SetMaxSeparationOffset(0.8f)
                // 광물은 몬스터끼리보다 훨씬 넓고 강하게 비껴가도록 별도 파라미터를 적용합니다.
                .SetObstacleProvider(() => _owner?.GetObstacles())
                .SetObstacleAvoidance(3.5f, 1.2f, 1.5f);

            _attackNode = new AttackNode(this);

            // 이동으로 사거리에 들어가면 공격합니다.
            NodeRunner = new NodeRunner(new SequenceNode(new List<INode>
            {
                new ActionNode(MoveNode.ProcessNode),
                new ActionNode(_attackNode.ProcessNode),
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

        // 피해 없이 밀려나기만 합니다(터치 밀치기용).
        // 위치를 즉시 대입하면 순간이동처럼 보이므로, 피격 넉백과 같은 DOMove 트윈을 씁니다.
        public void PushFrom(Vector3 origin, float distance, float duration = 0.2f)
        {
            if (_isDead)
                return;

            if (_posTween != null)
            {
                _posTween.Kill();
                _posTween = null;
            }

            var myPos = transform.position;
            var dir = myPos - origin;
            dir.z = 0f;

            // 정확히 겹쳐 있으면 방향이 없으므로 임의 방향으로 밀어냅니다.
            if (dir.sqrMagnitude < 0.0001f)
                dir = Vector3.up;

            // 비키네마틱 리지드바디라 transform을 직접 트윈하면 물리와 서로 덮어써서
            // 목표 거리에 못 미칩니다. 리지드바디의 MovePosition으로 옮깁니다.
            var body = GetComponent<Rigidbody>();
            var from = myPos;
            var to = myPos + dir.normalized * distance;
            var progress = 0f;

            _posTween = DOTween.To(() => progress, v =>
                {
                    progress = v;

                    var next = Vector3.Lerp(from, to, v);

                    if (body != null)
                        body.MovePosition(next);
                    else
                        transform.position = next;
                }, 1f, duration)
                .SetEase(Ease.OutQuad);
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

                // 처치 보상 지급(데이터의 GoldReward 기준).
                _onKilled?.Invoke(GetGoldReward());

                // 죽으면 진행 중이던 넉백/색상 트윈을 정리하고 색을 원복한 뒤 풀로 반환합니다.
                // (반환된 오브젝트에 트윈이 남아 재스폰 시 위치/색이 튀는 것을 방지)
                if (_posTween != null) { _posTween.Kill(); _posTween = null; }
                if (_colorTween != null) { _colorTween.Kill(); _colorTween = null; }
                if (spriteRenderer != null) spriteRenderer.color = new Color(1f, 1f, 1f, 1f);

                _attackNode?.Dispose();
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

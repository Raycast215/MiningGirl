
using System;
using System.Threading;
using BehaviourTree;
using Cysharp.Threading.Tasks;
using InGame.Player;
using Manager;
using Scene.InGame.Entity.Interface;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Scene.InGame.Entity.Node
{
    public class AttackNode : IDisposable
    {
        public bool IsPlaying { get; private set; }
        public bool IsAttackDone{ get; private set; }
        
        private readonly IEntity _entity;
        private CancellationTokenSource _cts;
        
        public AttackNode(IEntity iEntity)
        {
            _entity = iEntity;
        }
        
        public NodeState ProcessNode()
        {
            if (!_entity.GetActiveState())
            {
                _cts?.Cancel();
                IsAttackDone = false;
                return NodeState.Failure;
            }

            var target = _entity.GetTarget();

            if (target == null || !target.GetActiveState())
            {
                if (IsPlaying)
                    _cts?.Cancel();

                IsAttackDone = false;
                return NodeState.Failure;
            }

            if (IsPlaying)
                return NodeState.Running;

            if (IsAttackDone)
            {
                IsAttackDone = false;
                return NodeState.Success;
            }

            Attack(target).Forget();
            return NodeState.Running;
        }
        
        private async UniTaskVoid Attack(IEntity target)
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = new CancellationTokenSource();
            
            IsPlaying = true;
            IsAttackDone = false;
            
            try
            {
                if (!IsValidAttackTarget(target))
                    return;
                
                await UniTask.WaitForSeconds(_entity.GetAttackDelay(), cancellationToken: _cts.Token);
                
                if (!IsValidAttackTarget(target))
                    return;
                
                var damage = _entity.GetDamage();
                var isCritical = Random.Range(0, 100) < _entity.GetCriRate();
                var isExtraHit = Random.Range(0, 100) < _entity.GetExtraHitRate();

                if (isCritical)
                {
                    damage = _entity.GetDamage() * (1 + _entity.GetCriDamage());
                }

                target.Hit(damage, isCritical);
                SoundManager.Instance.PlaySfx("Hit1");
                
                if (isExtraHit)
                {
                    await UniTask.WaitForSeconds(0.2f, cancellationToken: _cts.Token);
                    
                    if (!IsValidAttackTarget(target))
                        return;
                    
                    target.Hit(damage, isCritical);
                    SoundManager.Instance.PlaySfx("Hit1");
                }
                
                IsAttackDone = true;
            }
            catch (OperationCanceledException)
            {
                // 공격 도중 취소 (타겟 사라짐 등) → 조용히 무시해도 됨
                // Debug.Log("Attack canceled");
            }
            catch (Exception e)
            {
                // Debug.LogException(e);
            }
            finally
            {
                IsPlaying = false;
            }
        }
        
        private bool IsValidAttackTarget(IEntity target)
        {
            return _entity != null
                   && _entity.GetActiveState()
                   && target != null
                   && target.GetActiveState();
        }
        
#region IDisposable

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

#endregion
    }
}
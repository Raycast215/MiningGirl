using System;
using System.Threading;
using BehaviourTree;
using Cysharp.Threading.Tasks;
using InGame.Player;
using Manager;
using Scene.InGame.Entity.Interface;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace Scene.InGame.Entity.Node
{
    public class AttackNode : IDisposable
    {
        public bool IsPlaying { get; private set; }
        public bool IsAttackDone{ get; private set; }
        
        private readonly IEntity _entity;

        private int _delay;
        private CancellationTokenSource _cts;
        
        public AttackNode(IEntity iEntity)
        {
            _entity = iEntity;
        }
        
        public AttackNode SetDelay(int delay)
        {
            _delay = delay;
            return this;
        }
        
        public NodeState ProcessNode()
        {
            var target = _entity.GetTarget();
            
            if (target == null || !target.GetActiveState())
            {
                // 이미 공격 중이면 취소
                if (IsPlaying)
                {
                    _cts?.Cancel();   // 아래 AttackNodeAsync 쪽에서 처리됨
                }
                
                return NodeState.Running;
            }
            
            // 공격 로직 진행 중이면 계속 Running
            if (IsPlaying)
                return NodeState.Running;
            
            // 한 사이클 끝났으면 한 번만 Success
            if (IsAttackDone)
            {
                IsAttackDone = false;
                return NodeState.Success;
            }
            
            // 새 공격 시작
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
                if (target == null || !target.GetActiveState())
                {
                    Debug.Log("======");
                    return;
                }
            
                // 방향 맞추기
                // var myPos = _entity.GetPosition();
                // var dir = (target.GetPosition() - myPos).normalized;
           
                // _playerMoveHandler.SetAnimation(EPlayerState.Attack, dir.y < 0 
                //     ? EPlayerDirection.Down 
                //     : EPlayerDirection.Up);
            
                await UniTask.DelayFrame(_delay, cancellationToken: _cts.Token);

                var damage = 1;
                // var random = Random.Range(0, 2) > 0 ;
            
                target.Damage(damage);
                // _handler.ShowDamageFloatingText(damage, target.GetPosition(), random);
                SoundManager.Instance.PlaySfx("Hit1");
            
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
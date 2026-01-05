using System;
using System.Threading;
using BehaviourTree;
using Cysharp.Threading.Tasks;
using Manager;
using UnityEngine;
using Random = UnityEngine.Random;

namespace InGame.Player
{
    public class PlayerAttackProcess : IDisposable
    {
        public bool IsPlaying { get; private set; }
        public bool IsAttackDone{ get; private set; }
        
        private IUnitInfoHandler _handler;
        private IPlayerMoveHandler _playerMoveHandler;
        private CancellationTokenSource _cts = new CancellationTokenSource();
        
        public PlayerAttackProcess(IUnitInfoHandler handler, IPlayerMoveHandler playerMoveHandler)
        {
            _handler = handler;
            _playerMoveHandler = playerMoveHandler;
        }
        
        public NodeState ProcessNode()
        {
            var target = _handler.GetPlayerTarget();
            
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
            AttackNodeAsync(target).Forget();
            return NodeState.Running;
        }

        private async UniTaskVoid AttackNodeAsync(IHit target)
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
                var dir = (target.GetPosition() - _handler.GetPlayerTransform().position).normalized;
           
                _playerMoveHandler.SetAnimation(EPlayerState.Attack, dir.y < 0 
                    ? EPlayerDirection.Down 
                    : EPlayerDirection.Up);
            
                await UniTask.DelayFrame(60, cancellationToken: _cts.Token);

                var damage = 1;
                var random = Random.Range(0, 2) > 0 ;
            
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
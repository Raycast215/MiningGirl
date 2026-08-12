using System;
using System.Threading;
using BehaviourTree;
using Cysharp.Threading.Tasks;
using Manager;
using Scene.InGame.Entity.Interface;
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
                // '공격 후 대기(후딜)' 방식입니다.
                // 사거리에 도달하면 곧바로 한 번 타격하고, 그 뒤에 다음 공격까지 대기합니다.
                // (이전의 '대기 후 공격(선딜)' 방식은 도착 직후 아무 반응이 없어 답답했고,
                //  타겟을 옮길 때마다 대기 시간을 새로 지불해야 했으며,
                //  대기 도중 판정이 어긋나면 타이머가 리셋되어 첫 타격이 지연되는 문제가 있었습니다.)
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

                // 타격이 끝난 뒤 다음 공격까지 대기합니다(쿨다운).
                await UniTask.WaitForSeconds(_entity.GetAttackDelay(), cancellationToken: _cts.Token);
                
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
            if (_entity == null || !_entity.GetActiveState())
                return false;

            if (target == null || !target.GetActiveState())
                return false;

            // 사거리 체크 — 대기(WaitForSeconds) 도중 타겟이 풀 재사용으로 다른 위치에 재활성화되거나
            // 플레이어가 멀어졌을 수 있으므로, 타격 직전에 실제로 사거리 안에 있는지 확인합니다.
            // (이 검증이 없으면 재시작 등으로 멀리 있는 대상이 '맞는' 현상이 생깁니다.)
            var sqrDist = (target.GetPosition() - _entity.GetPosition()).sqrMagnitude;
            // MoveNode가 멈추는 사거리와 정확히 같게 잡으면 경계에서 미세하게 어긋나 공격이 씹힐 수 있어
            // 여유를 둡니다. 광물은 피격 시 흔들림 트윈으로 위치가 변하므로 넉넉하게 잡습니다.
            var attackDist = _entity.GetAttackDistance() * 2.0f;

            return sqrDist <= attackDist * attackDist;
        }
        
#region IDisposable

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;

            // 진행 중이던 공격 상태를 초기화해서, 리셋 후 다음 공격이 정상적으로 다시 시작되게 합니다.
            IsPlaying = false;
            IsAttackDone = false;
        }

#endregion
    }
}
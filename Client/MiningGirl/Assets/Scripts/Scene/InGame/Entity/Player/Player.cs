using System;
using System.Collections.Generic;
using BehaviourTree;
using Cysharp.Threading.Tasks;
using Scene.InGame.Entity.Interface;
using Scene.InGame.Entity.Node;
using UnityEngine;

namespace Scene.InGame.Entity.Player
{
    public class Player : EntityBase
    {
        private event Action<Vector3> OnDirectionEvent;
        
        private AttackNode _attackNode;
        private SearchTargetNode _targetSearchNode;

        private Resource.IResourceProvider _resourceProvider;

        // 플레이어가 어떤 광물들을 대상으로 삼을지(가장 가까운 것 탐색) 공급자를 주입합니다.
        // InitAsync() 전에 호출되어야 행동 트리 구성 시점에 반영됩니다.
        public void SetResourceProvider(Resource.IResourceProvider resourceProvider)
        {
            _resourceProvider = resourceProvider;
        }

        public void InitDirectionEvent(Action<Vector3> onDirectionEvent)
        {
            OnDirectionEvent = null;
            OnDirectionEvent += onDirectionEvent;
        }

        // (테스트용) 현재 타겟 광물을 제외한 나머지 활성 광물 중 하나를 무작위로 골라 타겟으로 지정합니다.
        // SearchTargetNode는 타겟이 살아있으면 유지하므로, 여기서 강제로 바꾼 타겟 쪽으로 이동하게 됩니다.
        public void MoveToRandomResource()
        {
            var resources = _resourceProvider?.GetActiveResources();
            if (resources == null || resources.Count == 0)
                return;

            var current = GetTarget();

            // 현재 타겟을 제외한 활성 광물 후보를 모읍니다.
            var candidates = new List<IEntity>(resources.Count);
            foreach (var resource in resources)
            {
                if (resource == null || !resource.GetActiveState())
                    continue;
                if (resource == current)
                    continue;

                candidates.Add(resource);
            }

            if (candidates.Count == 0)
                return;

            var pick = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            SetTarget(pick);
        }
        
#region EntityBase

        public override async UniTaskVoid InitAsync()
        {
            base.InitAsync().Forget();
            
            // 가장 가까운 광물을 찾아 그 쪽으로 이동하는 행동 트리를 구성합니다.
            // 시퀀스: 타겟(가장 가까운 광물) 탐색 → 그 타겟을 향해 이동.
            // (채굴 공격 노드는 다음 단계에서 이어붙일 예정이라 지금은 이동까지만 연결합니다.)
            _targetSearchNode = new SearchTargetNode(this, _resourceProvider);

            NodeRunner = new NodeRunner(new SequenceNode(new List<INode>
            {
                new ActionNode(_targetSearchNode.ProcessNode),
                new ActionNode(MoveNode.ProcessNode),
            }));

            IsInitialized = true;
        }
        
        public override IEnumerable<IEntity> GetNearCheckEntities()
        {
            return null;
        }

        public override void Hit(float damage, bool isCritical)
        {
         
        }

        public override void SetDirection(Vector3 direction)
        {
            base.SetDirection(direction);
            OnDirectionEvent?.Invoke(direction);
        }
        
        public override float GetDamage()
        {
            return 0;
            //  return _handler.GetInGameData().GetStatData(EStatType.Damage).Value;
        }

        public override float GetAttackDistance()
        {
            // 타겟(광물)에 이 거리 이하로 가까워지면 이동을 멈춥니다(=채굴 사거리).
            return BaseData?.MoveToMinDistance ?? 0f;
        }

        public override float GetAttackDelay()
        {
            return 0;
            // return _handler.GetInGameData().GetStatData(EStatType.AttackDelay).Value;
        }

        public override float GetMoveSpeed()
        {
            return BaseData?.MoveSpeed ?? 0f;
        }

        public override float GetCriDamage()
        {
            return 0;
            // return _handler.GetInGameData().GetStatData(EStatType.CriDamage).Value;
        }

        public override float GetCriRate()
        {
            return 0;
            // return _handler.GetInGameData().GetStatData(EStatType.CriRate).Value;
        }

        public override float GetExtraHitRate()
        {
            return 0;
            // return _handler.GetInGameData().GetStatData(EStatType.ExtraHitRate).Value;
        }

#endregion
    }
}
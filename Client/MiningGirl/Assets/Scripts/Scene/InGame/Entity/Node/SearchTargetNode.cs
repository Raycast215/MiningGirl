using BehaviourTree;
using Scene.InGame.Entity.Interface;
using Scene.InGame.Entity.Resource;
using UnityEngine;

namespace Scene.InGame.Entity.Node
{
    // 현재 활성 광물들 중 가장 가까운 것을 찾아 엔티티의 타겟으로 지정하는 노드.
    // 광물 목록은 IResourceProvider를 통해 얻으므로, 이 노드는 구체 타입(Resource)에 의존하지 않습니다.
    public class SearchTargetNode
    {
        private readonly IEntity _iEntity;
        private readonly IResourceProvider _resourceProvider;

        public SearchTargetNode(IEntity iEntity, IResourceProvider resourceProvider)
        {
            _iEntity = iEntity;
            _resourceProvider = resourceProvider;
        }

        public NodeState ProcessNode()
        {
            // 현재 타겟이 아직 살아있으면(활성) 그대로 유지합니다. 매 프레임 타겟을 바꾸면
            // 여러 광물 사이를 오가며 떨리는 현상이 생기므로, 타겟이 사라졌을 때만 다시 찾습니다.
            var currentTarget = _iEntity.GetTarget();
            if (currentTarget != null && currentTarget.GetActiveState())
                return NodeState.Success;

            var resources = _resourceProvider?.GetActiveResources();
            if (resources == null || resources.Count == 0)
            {
                // 캘 광물이 하나도 없으면 타겟을 비워두고, 다음 프레임에 다시 시도합니다.
                _iEntity.SetTarget(null);
                return NodeState.Running;
            }

            var myPos = _iEntity.GetPosition();
            IEntity nearest = null;
            var nearestSqr = float.MaxValue;

            foreach (var resource in resources)
            {
                if (resource == null || !resource.GetActiveState())
                    continue;

                var sqr = (resource.GetPosition() - myPos).sqrMagnitude;
                if (sqr < nearestSqr)
                {
                    nearestSqr = sqr;
                    nearest = resource;
                }
            }

            if (nearest == null)
            {
                _iEntity.SetTarget(null);
                return NodeState.Running;
            }

            _iEntity.SetTarget(nearest);
            return NodeState.Success;
        }
    }
}

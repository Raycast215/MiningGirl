using System.Linq;
using BehaviourTree;
using Scene.InGame.Entity.Interface;

namespace Scene.InGame.Entity.Node
{
    public class SearchTargetNode
    {
        private readonly IEntityHandler _handler;
        private readonly IEntity _iEntity;
        
        public SearchTargetNode(IEntity iEntity, IEntityHandler handler)
        {
            _iEntity = iEntity;
            _handler = handler;
        }

        public NodeState ProcessNode()
        {
            var resourceList = _handler.GetResourceList();
            
            if (resourceList == null || resourceList.Count() == 0)
                return NodeState.Running;
            
            var myPos = _iEntity.GetPosition();
            var nearTarget = resourceList
                .Where(x => x.GetActiveState())
                .OrderBy(x => (x.GetPosition() - myPos).sqrMagnitude)
                .FirstOrDefault();

            if (nearTarget == null)
                return NodeState.Running;
            
            _iEntity.SetTarget(nearTarget);
            
            return NodeState.Success;
        }
    }
}

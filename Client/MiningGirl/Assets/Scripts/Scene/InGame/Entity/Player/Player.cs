using System.Collections.Generic;
using BehaviourTree;
using Cysharp.Threading.Tasks;
using InGame.Player;
using Scene.InGame.Entity.Interface;
using Scene.InGame.Entity.Node;

namespace Scene.InGame.Entity.Player
{
    public class Player : EntityBase
    {
        private IInGameHandler _handler;
        private AttackNode _attackNode;
        private SearchTargetNode _targetSearchNode;
        
        public void SetHandler(IInGameHandler handler)
        {
            _handler = handler;
        }
        
#region EntityBase

        public override async UniTaskVoid InitAsync()
        {
            base.InitAsync().Forget();
            
            _attackNode = new AttackNode(this).SetDelay(BaseData.AttackDelay);
            _targetSearchNode = new SearchTargetNode(this, _handler.GetEntityHandler());
            
            NodeRunner = new NodeRunner(new SequenceNode(new List<INode>()
            {
                new ActionNode(_targetSearchNode.ProcessNode),
                new ActionNode(MoveNode.ProcessNode),
                new ActionNode(_attackNode.ProcessNode),
            }));
            
            IsInitialized = true;
        }
        
        public override IEnumerable<IEntity> GetNearCheckEntities()
        {
            return null;
        }

        public override void Damage(float damage)
        {
         
        }

#endregion
    }
}
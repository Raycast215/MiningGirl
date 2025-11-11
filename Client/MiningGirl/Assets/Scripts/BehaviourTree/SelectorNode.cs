using System.Collections.Generic;

namespace BehaviourTree
{
    // Scripted by Raycast
    // 2025.01.24
    // BT에서 사용할 Selector Node입니다.
    public sealed class SelectorNode : INode
    {
        private readonly List<INode> _nodeList;

        public SelectorNode(List<INode> nodeList)
        {
            _nodeList = nodeList;
        }
        
        public NodeState Evaluate()
        {
            if (_nodeList == null || _nodeList.Count == 0)
                return NodeState.Failure;
            
            foreach (var node in _nodeList)
            {
                switch (node.Evaluate())
                {
                    case NodeState.Running:
                        return NodeState.Running;
                    
                    case NodeState.Success:
                        return NodeState.Success;
                }
            }
            
            return NodeState.Failure;
        }
    }
}
using System.Collections.Generic;

namespace BehaviourTree
{
    // Scripted by Raycast
    // 2025.01.24
    // BT에서 사용할 Sequence Node입니다.
    public sealed class SequenceNode : INode
    {
        private readonly List<INode> _nodeList;

        public SequenceNode(List<INode> nodeList)
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
                        continue;

                    case NodeState.Failure:
                        return NodeState.Failure;
                }
            }
            
            return NodeState.Success;
        }
    }
}
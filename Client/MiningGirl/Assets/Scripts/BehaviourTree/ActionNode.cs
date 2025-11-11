namespace BehaviourTree
{
    // Scripted by Raycast
    // 2025.01.24
    // BT에서 사용할 Action Node입니다.
    public sealed class ActionNode : INode
    {
        private event NodeStateCallback OnNodeStateReturned;

        public ActionNode(NodeStateCallback callback)
        {
            OnNodeStateReturned = callback;
        }

        public NodeState Evaluate()
        {
            return OnNodeStateReturned?.Invoke() ?? NodeState.Failure;
        }
    }
}
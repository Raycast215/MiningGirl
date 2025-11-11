namespace BehaviourTree
{
    // Scripted by Raycast
    // 2025.01.24
    // BT에서 사용할 Node를 실행하는 클래스입니다.
    public class NodeRunner
    {
        private readonly INode _node;

        public NodeRunner(INode node)
        {
            _node = node;
        }
        
        public void OperateNode()
        {
            _node?.Evaluate();
        }
    }
}
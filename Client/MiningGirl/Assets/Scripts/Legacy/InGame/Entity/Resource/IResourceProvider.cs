using System.Collections.Generic;
using Legacy.Scene.InGame.Entity.Interface;

namespace Legacy.Scene.InGame.Entity.Resource
{
    // 현재 채굴 가능한(활성) 광물 목록을 제공하는 대상에 대한 추상화.
    // 플레이어의 타겟 탐색 노드(SearchTargetNode)가 이 인터페이스를 통해 광물을 찾습니다.
    // ResourceController가 구현하며, 노드는 구체 타입(Resource)에 의존하지 않습니다.
    public interface IResourceProvider
    {
        IReadOnlyList<IEntity> GetActiveResources();
    }
}

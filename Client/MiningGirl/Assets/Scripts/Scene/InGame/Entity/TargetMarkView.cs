using UnityEngine;

namespace Scene.InGame.Entity
{
    // 카드로 조준된 대상 머리 위에 뜨는 표시 하나.
    //
    // 예전에는 몬스터·광물 프리팹이 이 오브젝트를 각자 자식으로 들고 있었습니다.
    // 그러면 같은 표시가 프리팹마다 복제되어 모양을 바꿀 때 전부 손봐야 하고,
    // 실제로 켜지는 건 한 번에 한두 개뿐인데 엔티티 수만큼 존재하게 됩니다.
    // 그래서 표시만 떼어내 TargetMarkController가 풀로 빌려주도록 했습니다.
    public class TargetMarkView : MonoBehaviour
    {
        // 대상 위치에서 height만큼 띄워 배치합니다.
        public void Follow(Vector3 targetPosition, float height)
        {
            targetPosition.y += height;

            transform.position = targetPosition;
        }
    }
}

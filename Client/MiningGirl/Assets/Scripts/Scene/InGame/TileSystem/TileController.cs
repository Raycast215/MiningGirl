using UnityEngine;

namespace Scene.InGame.TileSystem
{
    public class TileController : GameMonoInitializer
    {
        [SerializeField] 
        private GameObject prefab;
        [SerializeField]
        private float spacingX = 0.6f;
        [SerializeField]
        private float spacingY = 0.3f;
        
        public void Init()
        {
            const int count = 32;
            var half = (count - 1) * 0.5f;
            var centerPosition = Vector3.zero;
            
            for (var y = 0; y < count; y++)
            {
                for (var x = 0; x < count; x++)
                {
                    // 중심을 기준으로 하는 논리 좌표
                    var gridX = x - half;
                    var gridY = y - half;

                    // 아이소메트릭 좌표 변환
                    var position = centerPosition + new Vector3((gridX - gridY) * spacingX, (gridX + gridY) * spacingY, 1);

                    Instantiate(prefab, position, Quaternion.identity, transform
                    );
                }
            }
        }
    }
}
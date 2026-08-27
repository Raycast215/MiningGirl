using UnityEngine;

namespace Scene.MainGameScene.Battle
{
    // 전장 좌표를 한곳에 모읍니다.
    //
    // 가로 폭은 카메라에서 뽑습니다. 기기마다 화면비가 달라서 상수로 박으면
    // 좁은 기기에서 몬스터가 화면 밖에 스폰됩니다.
    public readonly struct BattleBounds
    {
        // 스폰 x가 이 범위를 벗어나면 몬스터가 화면 밖에 걸칩니다.
        public float HalfWidth { get; }

        // 몬스터가 나타나는 높이. 화면 바로 위라 스폰되는 순간은 보이지 않습니다.
        public float SpawnY { get; }

        // 화면 위 경계. 이 위에 있는 몬스터는 플레이어에게 안 보입니다.
        //
        // 조준이 이 선을 넘지 않습니다 - 안 보이는 적에게 화력이 쓰이면 그 한 발은
        // 플레이어에게 존재하지 않은 것이 되고, 보이는 적 하나가 그만큼 방치됩니다.
        public float ScreenTopY { get; }

        // 몬스터가 멈춰 서서 타워를 때리기 시작하는 기준선.
        public float TowerTopY { get; }

        // 발사체가 이 아래·위로 나가면 소멸합니다.
        public float DespawnTopY { get; }
        public float DespawnBottomY { get; }

        public BattleBounds(Camera camera, float towerTopY, float spawnMargin)
        {
            var halfHeight = camera != null && camera.orthographic ? camera.orthographicSize : 13f;
            var aspect = camera != null ? camera.aspect : 9f / 16f;

            HalfWidth = halfHeight * aspect;
            ScreenTopY = halfHeight;
            TowerTopY = towerTopY;
            SpawnY = halfHeight + spawnMargin;
            DespawnTopY = halfHeight + spawnMargin * 2f;
            DespawnBottomY = -halfHeight - spawnMargin;
        }

        // 몬스터 몸통이 화면 밖으로 삐져나오지 않는 x를 뽑습니다.
        public float RandomSpawnX(float bodyRadius)
        {
            var limit = Mathf.Max(0f, HalfWidth - bodyRadius);

            return Random.Range(-limit, limit);
        }

        public float ClampX(float x, float bodyRadius)
        {
            var limit = Mathf.Max(0f, HalfWidth - bodyRadius);

            return Mathf.Clamp(x, -limit, limit);
        }
    }
}

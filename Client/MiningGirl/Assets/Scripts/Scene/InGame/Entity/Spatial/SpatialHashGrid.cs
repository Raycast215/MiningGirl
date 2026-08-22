using System.Collections.Generic;
using Scene.InGame.Entity.Interface;
using UnityEngine;

namespace Scene.InGame.Entity.Spatial
{
    // 균등 격자 공간 해시.
    //
    // 프레임당 한 번 위치를 배열에 담아두고, 이후 근접 조회는 배열만 봅니다.
    // 근접 판정의 실제 비용은 계산량이 아니라 IEntity.GetPosition() 같은
    // 인터페이스/네이티브 호출이었습니다. (156마리 기준 29ms -> 1.4ms)
    //
    // 셀 크기를 조회 반경 이상으로 잡으면 3x3 셀만 봐도 반경 안의 이웃이 모두 들어옵니다.
    // MonoBehaviour가 아니라 씬 없이 테스트할 수 있습니다.
    public sealed class SpatialHashGrid
    {
        private readonly Dictionary<long, List<int>> _cells = new Dictionary<long, List<int>>();

        // 비운 셀 리스트를 재사용해 프레임마다 새로 할당하지 않습니다.
        private readonly List<List<int>> _bucketPool = new List<List<int>>();

        private readonly List<IEntity> _entities = new List<IEntity>();
        private readonly List<Vector3> _positions = new List<Vector3>();

        private float _cellSize = 1.6f;
        private float _invCellSize = 1f / 1.6f;

        public int Count
        {
            get { return _entities.Count; }
        }

        // 조회 반경보다 크게 잡아야 3x3 검사가 반경 전체를 덮습니다.
        public void SetCellSize(float size)
        {
            _cellSize = Mathf.Max(0.01f, size);
            _invCellSize = 1f / _cellSize;
        }

        public void Clear()
        {
            foreach (var pair in _cells)
            {
                pair.Value.Clear();
                _bucketPool.Add(pair.Value);
            }

            _cells.Clear();
            _entities.Clear();
            _positions.Clear();
        }

        // 위치는 부르는 쪽이 한 번만 읽어 넘깁니다.
        // (여기서 GetPosition을 부르면 인터페이스 호출을 줄이려는 목적이 사라집니다.)
        public void Add(IEntity entity, Vector3 position)
        {
            if (entity == null)
                return;

            position.z = 0f;

            var index = _entities.Count;

            _entities.Add(entity);
            _positions.Add(position);

            GetBucket(KeyOf(position)).Add(index);
        }

        public IEntity GetEntity(int index)
        {
            return _entities[index];
        }

        public Vector3 GetPosition(int index)
        {
            return _positions[index];
        }

        // center 주변 3x3 셀에 들어 있는 인덱스를 results에 채웁니다.
        public void Query(Vector3 center, List<int> results)
        {
            results.Clear();

            if (_entities.Count == 0)
                return;

            var cx = Mathf.FloorToInt(center.x * _invCellSize);
            var cy = Mathf.FloorToInt(center.y * _invCellSize);

            for (var dx = -1; dx <= 1; dx++)
            {
                for (var dy = -1; dy <= 1; dy++)
                {
                    List<int> bucket;

                    if (!_cells.TryGetValue(Key(cx + dx, cy + dy), out bucket))
                        continue;

                    for (var i = 0; i < bucket.Count; i++)
                        results.Add(bucket[i]);
                }
            }
        }

        private List<int> GetBucket(long key)
        {
            List<int> bucket;

            if (_cells.TryGetValue(key, out bucket))
                return bucket;

            if (_bucketPool.Count > 0)
            {
                bucket = _bucketPool[_bucketPool.Count - 1];
                _bucketPool.RemoveAt(_bucketPool.Count - 1);
            }
            else
            {
                bucket = new List<int>();
            }

            _cells[key] = bucket;

            return bucket;
        }

        private long KeyOf(Vector3 position)
        {
            return Key(Mathf.FloorToInt(position.x * _invCellSize),
                Mathf.FloorToInt(position.y * _invCellSize));
        }

        // 셀 좌표 두 개를 그대로 64비트에 담습니다(해시 충돌 없음).
        private static long Key(int cx, int cy)
        {
            return ((long)cx << 32) ^ (uint)cy;
        }
    }
}

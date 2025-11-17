using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace InGame.System.Loader
{
    public class TileLoader
    {
        public List<Vector3> GetPosList { get; private set; }
        public List<Tile.Tile> GetTileList { get; private set; }
        public bool IsInitialized { get; private set; }

        private Transform _parent;
        private GameObject _prefab;
        private Queue<Tile.Tile> _queue;

        public TileLoader(Transform parent)
        {
            _parent = parent;
            _queue = new Queue<Tile.Tile>();
            GetTileList = new List<Tile.Tile>();
            GetPosList = new List<Vector3>();
        }

        public async UniTaskVoid Initialize()
        {
            // To Do: 어드레서블로 변경
            _prefab = Resources.Load<GameObject>("InGame/Tile");

            for (var i = 0; i < 100; i++)
            {
                Crate();
            }

            IsInitialized = true;
        }

        public void Load()
        {
            var posList = GetDiamondGrid(12, 1.5f, 1.5f);

            foreach (var pos in posList)
            {
                var tile = Get();
                var delay = Random.Range(0.0f, 1.0f);

                tile.SetPosition(pos);
                tile.SetDelay(delay);
                tile.Drop().Forget();

                GetTileList.Add(tile);
            }
        }

        private Tile.Tile Get()
        {
            if (_queue == null || _queue.Count == 0)
            {
                Crate();
            }

            return _queue?.Dequeue();
        }

        private void Crate()
        {
            var ins = Object.Instantiate(_prefab, _parent);

            ins.gameObject.SetActive(false);

            _queue.Enqueue(ins.GetComponent<Tile.Tile>());
        }

        private List<Vector2> GetDiamondGrid(int size, float stepX, float stepY)
        {
            int total = size * size;
            var result = new List<Vector2>(total);

            int half = size / 2;

            for (int y = -half; y <= half; y++)
            {
                for (int x = -half; x <= half; x++)
                {
                    float px = x * stepX;
                    float py = y * stepY;
                    result.Add(new Vector2(px, py));
                    GetPosList.Add(new Vector2(px, py));
                }
            }
            
            return result;
        }
    }
}
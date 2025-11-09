using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using InGame.System.Enemy;
using UnityEngine;

namespace InGame.System.Loader
{
    public class EnemyLoader
    {
        public List<EnemyController> GetEnemyList { get; private set; }
        public bool IsInitialized { get; private set; }
        
        private Transform _parent;
        private GameObject _prefab;
        private Queue<EnemyController> _queue;
        
        public EnemyLoader(Transform parent)
        {
            _parent = parent;
            GetEnemyList = new List<EnemyController>();
            _queue = new Queue<EnemyController>();
        }

        public async UniTaskVoid Initialize()
        {
            // To Do: 어드레서블로 변경
            _prefab = Resources.Load<GameObject>("InGame/Enemy");

            for (var i = 0; i < 10; i++)
            {
                Crate();
            }
            
            IsInitialized = true;
        }

        public void Load()
        {
            var posList = GetUIPositionsInRing(Vector2.zero, 300, 800, 10, 300);

            foreach (var pos in posList)
            {
                var enemy = Get();
                
                enemy.SetPosition(pos);
                enemy.gameObject.SetActive(true);
                GetEnemyList.Add(enemy);
            }
        }
        
        private EnemyController Get()
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
            
            _queue.Enqueue(ins.GetComponent<EnemyController>());
        }
        
        private List<Vector2> GetUIPositionsInRing(Vector2 basePos, float minRange, float maxRange, int count, float minDistanceBetweenPoints, int maxTryPerPoint = 25)
        {
            var result = new List<Vector2>(count);

            // 안전장치: 최소가 최대보다 크면 안 됨
            if (minRange > maxRange)
            {
                float tmp = minRange;
                minRange = maxRange;
                maxRange = tmp;
            }

            for (int i = 0; i < count; i++)
            {
                bool found = false;

                for (int t = 0; t < maxTryPerPoint; t++)
                {
                    // 각도 랜덤
                    float angle = Random.Range(0f, Mathf.PI * 2f);

                    // 고리 안에서 랜덤 반지름
                    // 균등 분포로 뽑으려면 sqrt 써주는게 좋다
                    float r = Mathf.Sqrt(Random.Range(minRange * minRange, maxRange * maxRange));

                    Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
                    Vector2 candidate = basePos + offset;

                    // 이미 뽑힌 애들과 거리 체크
                    bool overlap = false;
                    for (int j = 0; j < result.Count; j++)
                    {
                        if (Vector2.Distance(candidate, result[j]) < minDistanceBetweenPoints)
                        {
                            overlap = true;
                            break;
                        }
                    }

                    if (!overlap)
                    {
                        result.Add(candidate);
                        found = true;
                        break;
                    }
                }

                // 못 찾으면 그냥 스킵 (영역이 좁거나 개수가 너무 많을 때)
                if (!found)
                {
                    // 여기서 minRange 줄이거나 maxRange 늘리는 로직을 넣어도 됨
                }
            }

            return result;
        }
    }
}
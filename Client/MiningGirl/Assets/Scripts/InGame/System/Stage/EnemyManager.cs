using System;
using System.Collections.Generic;
using InGame.System.Enemy;
using UnityEngine;
using Random = UnityEngine.Random;

namespace InGame.System.Stage
{
    public class EnemyManager : GameInitializer
    {
        [SerializeField]
        private EnemyController prefab;
        [SerializeField]
        private Transform parent;

        [SerializeField]
        private int initCount = 10;
        [SerializeField] 
        private float minRange = 100;
        [SerializeField] 
        private float maxRange = 100;
        [SerializeField] 
        private float minDistance = 10;
        [SerializeField] 
        private float minDistanceBetweenPoints = 100;

        private Dictionary<int, EnemyController> _dic;
        
        private void Start()
        {
            Initialize();
        }

        public void Initialize()
        {
            var posList = GetUIPositionsInRing(Vector2.zero, minRange, maxRange, initCount, minDistanceBetweenPoints);

            _dic = new Dictionary<int, EnemyController>();
            
            foreach (var pos in posList)
            {
                var ins = Instantiate(prefab, parent);
                
                ins.gameObject.SetActive(true);
                ins.SetPosition(pos);
                _dic.Add(ins.GetHashCode(), ins);
            }
        }
        
        public static List<Vector2> GetUIPositionsInRing(Vector2 basePos, float minRange, float maxRange, int count,
            float minDistanceBetweenPoints, int maxTryPerPoint = 25)
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
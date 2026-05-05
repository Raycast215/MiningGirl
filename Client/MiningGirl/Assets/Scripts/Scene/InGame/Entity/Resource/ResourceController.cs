using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Scene.InGame.Entity.Data;
using UnityEngine;

namespace Scene.InGame.Entity.Resource
{
    public class SpawnData
    {
        public int Count { get; set; }
        public float Interval { get; set; }
    }
    
    public class ResourceController : EntityControllerBase<Resource>
    {
        private IInGameHandler _handler;
        private SpawnData _spawnData;
        
        public async UniTaskVoid InitAsync(IInGameHandler handler)
        {
            if (IsInitialized)
                return;
            
            _handler = handler;
            InitAsync("Stone", 10).Forget();
            await UniTask.WaitUntil(() => IsInitialized);

            var posList = GetUIPositionsInRing(Vector3.zero, 2, 10, 30, 2);
            
            foreach (var pos in posList)
            {
                Spawn(pos);
            }
            
            _spawnData = new SpawnData
            {
                Count = 3,
                Interval = 10
            };
        }

        public async void ExecuteSpawn()
        {
            if (!IsInitialized)
                return;

            var isBossStage = false;
            
            if (isBossStage)
            {
                Spawn(new Vector3(0, 10, 0), isBossStage);
                return;
            }

            try
            {
                while (true)
                {
                    var targetPos = _handler.GetEntityHandler().GetPlayer().GetPosition();
                    var posList = GetUIPositionsInRing(targetPos, 2, 10, _spawnData.Count, 2);
                
                    foreach (var pos in posList)
                    {
                        Spawn(pos);
                    }
                
                    await UniTask.WaitForSeconds(_spawnData.Interval);
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        }

        private void Spawn(Vector3 pos, bool isBossStage = false)
        {
            var ins = Get();

            if (isBossStage)
            {
                ins.BaseData = new EntityData
                {
                    MaxHealth = 10000,
                    Health = 10000,
                    MoveSpeed = 0,
                    MoveToMinDistance = 0,
                    AttackDelay = 0
                };
                
                ins.transform.localScale = Vector3.one * 5;
            }
            else
            {
                ins.BaseData = new EntityData
                {
                    MaxHealth = 100,
                    Health = 100,
                    MoveSpeed = 0,
                    MoveToMinDistance = 0,
                    AttackDelay = 0
                };
            }
            
            
                
            ins.SetHandler(_handler, x => Return(x as Resource));
            ins.InitAsync().Forget();
            ins.SetPosition(pos);
            ins.gameObject.SetActive(true);
        }
        
        private List<Vector3> GetUIPositionsInRing(
            Vector3 center,
            float minRadius,
            float maxRadius,
            int count,
            float minDistanceBetweenPoints,
            int maxTryPerPoint = 25)
        {
            var positions = new List<Vector3>(Mathf.Max(0, count));

            if (count <= 0)
                return positions;

            maxTryPerPoint = Mathf.Max(1, maxTryPerPoint);
            minRadius = Mathf.Max(0f, minRadius);
            maxRadius = Mathf.Max(0f, maxRadius);
            minDistanceBetweenPoints = Mathf.Max(0f, minDistanceBetweenPoints);

            if (minRadius > maxRadius)
                (minRadius, maxRadius) = (maxRadius, minRadius);

            float minRadiusSqr = minRadius * minRadius;
            float maxRadiusSqr = maxRadius * maxRadius;
            float minDistanceSqr = minDistanceBetweenPoints * minDistanceBetweenPoints;

            for (var i = 0; i < count; i++)
            {
                var placed = false;

                for (int attempt = 0; attempt < maxTryPerPoint; attempt++)
                {
                    float angle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);

                    // 면적 기준 균등 분포
                    float radius = Mathf.Sqrt(UnityEngine.Random.Range(minRadiusSqr, maxRadiusSqr));

                    float x = center.x + Mathf.Cos(angle) * radius;
                    float y = center.y + Mathf.Sin(angle) * radius;

                    Vector3 candidate = new Vector3(x, y, 0f);

                    if (IsFarEnough(candidate, positions, minDistanceSqr))
                    {
                        positions.Add(candidate);
                        placed = true;
                        break;
                    }
                }

                // 실패 시 스킵 (필요하면 fallback 추가 가능)
                if (!placed)
                {
                    // ex:
                    // positions.Add(center); // 강제 추가 등
                }
            }

            return positions;
        }
        
        private bool IsFarEnough(Vector3 candidate, List<Vector3> existingPoints, float minDistanceSqr)
        {
            for (int i = 0; i < existingPoints.Count; i++)
            {
                var diff = candidate - existingPoints[i];
                
                diff.z = 0f;

                if (diff.sqrMagnitude < minDistanceSqr)
                    return false;
            }

            return true;
        }
    }
}

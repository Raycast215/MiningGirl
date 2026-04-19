using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Scene.InGame.Entity.Data;
using Scene.InGame.Entity.Interface;
using UnityEngine;

namespace Scene.InGame.Entity.Resource
{
    public class ResourceController : EntityControllerBase<Resource>
    {
        private IInGameHandler _handler;
        
        public async UniTaskVoid InitAsync(IInGameHandler handler)
        {
            _handler = handler;
            InitAsync("Rock", 10).Forget();
            await UniTask.WaitUntil(() => IsInitialized);

            var posList = GetUIPositionsInRing(Vector3.zero, 2, 10, 30, 2);

            foreach (var pos in posList)
            {
                var ins = Get();
            
                ins.BaseData = new EntityData
                {
                    MaxHealth = 3,
                    Health = 3,
                    MoveSpeed = 0,
                    MoveToMinDistance = 0,
                    AttackDelay = 0
                };
                
                ins.SetHandler(_handler, x => Return(x as Resource));
                ins.InitAsync().Forget();
                ins.SetPosition(pos);
                ins.gameObject.SetActive(true);
            }
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

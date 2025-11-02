// using System;
// using System.Collections.Generic;
// using Cysharp.Threading.Tasks;
// using UnityEngine;
//
// namespace InGame.System.FloatingDamage
// {
//     /// <summary>
//     /// 같은 프레임에 발생한 데미지를 수집 후 EndOfFrame에 균일 간격으로 배치하여 표시.
//     /// - 그룹 기준: 동일 프레임 + 근사 위치(버킷)
//     /// - 배치: 중앙 정렬 좌우 대칭 (짝수개면 가운데 간격 기준)
//     /// </summary>
//     public class FloatingDamageController : GameInitializer
//     {
//         [Header("Pooling")]
//         [SerializeField] private Floating prefab;
//         [SerializeField] private int poolCount = 10;
//
//         [Header("Layout")]
//         [Tooltip("같은 타이밍에 나타나는 데미지 간의 가로 간격")]
//         [SerializeField] private float horizontalSpacing = 0.4f;
//
//         [Tooltip("한 줄 최대 개수(넘치면 다음 줄로 내려감, 영수증 쌓임 느낌)")]
//         [SerializeField] private int maxPerRow = 6;
//
//         [Tooltip("줄(행) 간의 세로 간격(+위쪽으로 쌓임)")]
//         [SerializeField] private float rowSpacing = 0.35f;
//
//         [Tooltip("그룹핑 버킷 크기. 값이 작을수록 '같은 자리'로 판정이 엄격해짐")]
//         [SerializeField] private float positionBucketSize = 0.25f;
//
//         private readonly Queue<Floating> _pool = new();
//         private readonly List<Pending> _pending = new();
//         private readonly Dictionary<BurstKey, List<Pending>> _frameGroups = new();
//
//         private bool _loopStarted;
//
//         private struct Pending
//         {
//             public int damage;
//             public Vector2 pos;
//             public int frame;
//         }
//
//         private struct BurstKey : IEquatable<BurstKey>
//         {
//             public int frame;
//             public Vector2Int cell; // quantized position
//
//             public bool Equals(BurstKey other) => frame == other.frame && cell == other.cell;
//             public override bool Equals(object obj) => obj is BurstKey other && Equals(other);
//             public override int GetHashCode() => (frame * 397) ^ cell.GetHashCode();
//         }
//
//         public async UniTaskVoid InitAsync()
//         {
//             LoadDamageObject();
//             if (!_loopStarted)
//             {
//                 _loopStarted = true;
//                 _ = ProcessLoop(); // fire-and-forget
//             }
//             await UniTask.WaitUntil(() => IsInitialized);
//         }
//
//         /// <summary>
//         /// 데미지 요청. 같은 프레임 내의 요청은 모았다가 EndOfFrame에 일괄 배치.
//         /// </summary>
//         public void Damage(int damage, Vector2 pos)
//         {
//             _pending.Add(new Pending
//             {
//                 damage = damage,
//                 pos = pos,
//                 frame = Time.frameCount
//             });
//         }
//
//         /// <summary>
//         /// EndOfFrame마다 pending을 그룹핑 후 균일 간격으로 배치하여 Spawn.
//         /// </summary>
//         private async UniTaskVoid ProcessLoop()
//         {
//             // 게임 종료 전까지 루프
//             while (this != null && gameObject != null)
//             {
//                 // 프레임 끝까지 대기
//                 await UniTask.WaitForEndOfFrame();
//
//                 if (_pending.Count == 0)
//                     continue;
//
//                 // 그룹 초기화
//                 _frameGroups.Clear();
//
//                 // 1) 같은 프레임 + 근사 위치(버킷)로 그룹핑
//                 for (int i = 0; i < _pending.Count; i++)
//                 {
//                     var p = _pending[i];
//                     var cell = Quantize(p.pos, positionBucketSize);
//                     var key = new BurstKey { frame = p.frame, cell = cell };
//
//                     if (!_frameGroups.TryGetValue(key, out var list))
//                     {
//                         list = new List<Pending>(4);
//                         _frameGroups.Add(key, list);
//                     }
//                     list.Add(p);
//                 }
//
//                 // 2) 그룹별로 균일 간격 배치 후 Spawn
//                 foreach (var kv in _frameGroups)
//                 {
//                     var list = kv.Value;
//                     int n = list.Count;
//                     if (n == 0) continue;
//
//                     // 앵커 = 평균 위치(동일 버킷이라 거의 같지만 여러 소스일 수 있어 평균을 사용)
//                     Vector2 anchor = Vector2.zero;
//                     for (int i = 0; i < n; i++) anchor += list[i].pos;
//                     anchor /= n;
//
//                     // 행/열 배치 좌표 계산
//                     var positions = ComputeGridPositions(
//                         count: n,
//                         anchor: anchor,
//                         hSpacing: horizontalSpacing,
//                         rowSpacing: rowSpacing,
//                         maxPerRow: Mathf.Max(1, maxPerRow)
//                     );
//
//                     // 데미지값 정렬은 원본 순서를 유지(원하면 크리티컬 우선 등 커스텀 가능)
//                     for (int i = 0; i < n; i++)
//                     {
//                         var pending = list[i];
//                         var spawnPos = positions[i];
//
//                         var dmg = GetOrCreate();
//                         dmg.Init(pending.damage, spawnPos, PoolRelease);
//                     }
//                 }
//
//                 _pending.Clear();
//             }
//         }
//
//         // 중앙 정렬 그리드 배치(좌우 대칭, 위로 쌓임)
//         private static List<Vector2> ComputeGridPositions(int count, Vector2 anchor, float hSpacing, float rowSpacing, int maxPerRow)
//         {
//             var result = new List<Vector2>(count);
//
//             int rows = Mathf.CeilToInt(count / (float)maxPerRow);
//             int placed = 0;
//
//             for (int r = 0; r < rows; r++)
//             {
//                 int remaining = count - placed;
//                 int take = Mathf.Min(maxPerRow, remaining);
//
//                 // 중앙 정렬 가로 오프셋들 계산 (짝수개면 중앙 간격 기준)
//                 var xOffsets = ComputeCenteredOffsets(take, hSpacing);
//
//                 // r=0이 맨 아래 줄, 위로 올라갈수록 y가 + (영수증 처럼 위로 쌓이게)
//                 float y = anchor.y + (r * rowSpacing);
//
//                 for (int i = 0; i < take; i++)
//                 {
//                     float x = anchor.x + xOffsets[i];
//                     result.Add(new Vector2(x, y));
//                 }
//
//                 placed += take;
//             }
//
//             return result;
//         }
//
//         // 개수에 따라 중앙 기준 좌우 대칭 오프셋 생성
//         private static List<float> ComputeCenteredOffsets(int count, float spacing)
//         {
//             var list = new List<float>(count);
//
//             if (count <= 0) return list;
//
//             if (count % 2 == 1)
//             {
//                 // 홀수: 0, ±s, ±2s ...
//                 int half = count / 2;
//                 for (int i = -half; i <= half; i++)
//                     list.Add(i * spacing);
//             }
//             else
//             {
//                 // 짝수: ±(0.5s), ±(1.5s) ...
//                 int half = count / 2;
//                 for (int i = 0; i < count; i++)
//                 {
//                     // i: 0..count-1 -> offsets: (-(half-0.5) + i)*spacing
//                     float offset = (-(half - 0.5f) + i) * spacing;
//                     list.Add(offset);
//                 }
//             }
//
//             return list;
//         }
//
//         private static Vector2Int Quantize(Vector2 pos, float bucketSize)
//         {
//             // 버킷 단위로 좌표를 정수 셀로 변환하여 "같은 자리" 판정
//             int x = Mathf.RoundToInt(pos.x / bucketSize);
//             int y = Mathf.RoundToInt(pos.y / bucketSize);
//             return new Vector2Int(x, y);
//         }
//
//         private Floating GetOrCreate()
//         {
//             Floating dmg;
//             if (_pool.Count == 0)
//             {
//                 dmg = UnityEngine.Object.Instantiate(prefab, transform);
//                 dmg.gameObject.SetActive(false);
//             }
//             else
//             {
//                 dmg = _pool.Dequeue();
//             }
//             return dmg;
//         }
//
//         private void LoadDamageObject()
//         {
//             for (var i = 0; i < poolCount; i++)
//             {
//                 var ins = Instantiate(prefab, transform);
//                 ins.gameObject.SetActive(false);
//                 _pool.Enqueue(ins);
//             }
//             IsInitialized = true;
//         }
//
//         private void PoolRelease(Floating poolObject)
//         {
//             poolObject.gameObject.SetActive(false);
//             _pool.Enqueue(poolObject);
//         }
//     }
// }
//
//
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace InGame.System.FloatingDamage
{
    public class FloatingDamageController : GameInitializer
    {
        [SerializeField]
        private Floating prefab;
        [SerializeField] 
        private int poolCount = 10;

        private Queue<Floating> _queue;

        public async UniTaskVoid InitAsync()
        {
            LoadDamageObject();
            await UniTask.WaitUntil(() => IsInitialized);
        }
        
        public void Damage(int damage, Vector2 pos, bool isAdd = false)
        {
            Floating dmg;
      
            if (_queue == null || _queue.Count == 0)
            {
                dmg = Instantiate(prefab, transform);
                dmg.gameObject.SetActive(false);
            }
            else
            {
                dmg = _queue.Dequeue();
            }
      
            dmg.Init(damage, pos, PoolRelease, isAdd);
        }
        
        private void LoadDamageObject()
        {
            _queue ??= new Queue<Floating>();

            for (var i = 0; i < poolCount; i++)
            {
                var ins = Instantiate(prefab, transform);
         
                ins.gameObject.SetActive(false);
                _queue.Enqueue(ins);
            }
      
            IsInitialized = true;
        }
        
        private void PoolRelease(Floating poolObject)
        {
            poolObject.gameObject.SetActive(false);
            _queue.Enqueue(poolObject);
        }
    }
}
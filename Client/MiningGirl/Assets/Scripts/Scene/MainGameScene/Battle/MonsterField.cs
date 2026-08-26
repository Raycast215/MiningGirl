using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Data;
using Manager;
using Pool;
using UnityEngine;

namespace Scene.MainGameScene.Battle
{
    // 필드에 살아 있는 몬스터를 모아 두고, 스폰·정리·타겟 조회를 맡습니다.
    //
    // 발사체가 매번 FindObjectsOfType으로 적을 찾으면 발사체 수 x 몬스터 수만큼
    // 비용이 붙으므로, 살아 있는 목록을 한곳에서 들고 있습니다.
    public class MonsterField
    {
        // 타워 앞에 나란히 설 때의 최소 간격.
        // 판정 반경이 아니라 그림 폭 기준입니다. 반경으로 잡으면 여백만큼 겹쳐 보입니다.
        private const float BlockedSpacing = MonsterUnit.BlockedSpacing;

        public event Action<MonsterUnit> OnMonsterKilled;

        public IReadOnlyList<MonsterUnit> Alive => _alive;
        public int AliveCount => _alive.Count;

        // 밸런스를 볼 때 쓰는 기록입니다.
        //
        // 타워에 몇 마리나 닿았는지가 난이도의 실제 지표입니다. 도달 수가 0이면
        // 몬스터 공격력을 몇 배로 올려도 타워는 멀쩡합니다.
        public int ReachedTowerCount { get; private set; }

        // 한 번에 화면에 가장 많이 깔렸던 수. "밀리면 밀릴수록 쌓인다"가 실제로 일어났는지 봅니다.
        public int PeakAliveCount { get; private set; }

        private readonly List<MonsterUnit> _alive = new List<MonsterUnit>();
        private readonly List<MonsterUnit> _deadBuffer = new List<MonsterUnit>();
        private readonly Dictionary<string, Pooling<MonsterUnit>> _pools = new Dictionary<string, Pooling<MonsterUnit>>();

        private readonly Transform _layer;
        private readonly Tower _tower;
        private readonly BattleBounds _bounds;
        private readonly float _statMultiplier;

        public MonsterField(Transform layer, Tower tower, BattleBounds bounds, float statMultiplier)
        {
            _layer = layer;
            _tower = tower;
            _bounds = bounds;
            _statMultiplier = statMultiplier;
        }

        // 스테이지가 시작되기 전에 쓸 프리팹을 전부 불러 둡니다.
        // 웨이브 도중에 처음 등장하는 종류를 그때 로드하면 그 프레임이 튑니다.
        public async UniTask PreloadAsync(IEnumerable<string> monsterIds)
        {
            foreach (var id in monsterIds)
            {
                if (string.IsNullOrEmpty(id) || _pools.ContainsKey(id))
                    continue;

                var prefab = await AddressableManager.Instance.LoadAsset<GameObject>(id);

                if (prefab == null)
                {
                    Debug.LogError($"[MonsterField] 몬스터 프리팹을 찾지 못했습니다: {id}");

                    continue;
                }

                var unit = prefab.GetComponent<MonsterUnit>();

                if (unit == null)
                {
                    Debug.LogError($"[MonsterField] {id} 프리팹에 MonsterUnit이 없습니다.");

                    continue;
                }

                var pool = new Pooling<MonsterUnit>(unit, 0, _layer);
                pool.Pool();

                _pools.Add(id, pool);
            }
        }

        public void Spawn(MonsterDataTableRow row)
        {
            if (row == null || !_pools.TryGetValue(row.Id, out var pool))
                return;

            var unit = pool.Get();
            var position = new Vector3(_bounds.RandomSpawnX(MonsterUnit.BodyRadius), _bounds.SpawnY, 0f);

            unit.Setup(row, _statMultiplier, _tower, position, HandleDied, HandleBlocked);

            _alive.Add(unit);

            if (_alive.Count > PeakAliveCount)
                PeakAliveCount = _alive.Count;
        }

        public void Tick(float deltaTime)
        {
            // Tick 도중에 죽으면 목록이 바뀌므로, 죽은 개체는 모아 두었다가 뒤에서 치웁니다.
            for (var i = 0; i < _alive.Count; i++)
                _alive[i].Tick(deltaTime);

            if (_deadBuffer.Count == 0)
                return;

            foreach (var unit in _deadBuffer)
            {
                _alive.Remove(unit);

                if (_pools.TryGetValue(unit.Row.Id, out var pool))
                    pool.Return(unit);
                else
                    unit.gameObject.SetActive(false);
            }

            _deadBuffer.Clear();
        }

        // 발사체가 1발일 때. 가장 가까운 적 하나입니다.
        public MonsterUnit FindNearest(Vector3 from)
        {
            MonsterUnit nearest = null;
            var best = float.MaxValue;

            for (var i = 0; i < _alive.Count; i++)
            {
                var unit = _alive[i];

                if (!unit.IsAlive)
                    continue;

                var distance = (unit.Position - from).sqrMagnitude;

                if (distance >= best)
                    continue;

                best = distance;
                nearest = unit;
            }

            return nearest;
        }

        // 발사체가 2발 이상일 때 쓰는 후보 목록. 가까운 순으로 count마리를 담습니다.
        //
        // 효율이 목적이 아니라 그림이 목적입니다. 발사체가 늘었는데 전부 같은 지점으로
        // 날아가면 겹쳐서 한 발처럼 보이고, 강화를 고른 보람이 사라집니다.
        public void FillNearest(Vector3 from, int count, List<MonsterUnit> buffer)
        {
            buffer.Clear();

            if (count <= 0)
                return;

            for (var i = 0; i < _alive.Count; i++)
            {
                var unit = _alive[i];

                if (!unit.IsAlive)
                    continue;

                var distance = (unit.Position - from).sqrMagnitude;
                var index = buffer.Count;

                while (index > 0 && (buffer[index - 1].Position - from).sqrMagnitude > distance)
                    index--;

                if (index >= count)
                    continue;

                buffer.Insert(index, unit);

                if (buffer.Count > count)
                    buffer.RemoveAt(buffer.Count - 1);
            }
        }

        public void Clear()
        {
            foreach (var unit in _alive)
            {
                if (unit != null)
                    unit.gameObject.SetActive(false);
            }

            _alive.Clear();
            _deadBuffer.Clear();
        }

        private void HandleDied(MonsterUnit unit)
        {
            _deadBuffer.Add(unit);

            OnMonsterKilled?.Invoke(unit);
        }

        // 타워 앞에 멈춘 개체끼리만 x를 벌립니다.
        // 내려오는 도중에 밀면 직선 하강이라는 규칙이 깨져 보입니다.
        private void HandleBlocked(MonsterUnit unit)
        {
            ReachedTowerCount++;

            var origin = unit.Position.x;

            for (var attempt = 0; attempt < 24; attempt++)
            {
                // 원래 자리에서 좌우로 번갈아 한 칸씩 벌려 가며 빈자리를 찾습니다.
                var step = BlockedSpacing * (attempt / 2 + 1) * 0.5f;
                var x = attempt == 0
                    ? origin
                    : _bounds.ClampX(origin + (attempt % 2 == 1 ? step : -step), MonsterUnit.BodyRadius);

                if (!IsOccupied(x, unit))
                {
                    unit.NudgeX(x);

                    return;
                }
            }

            // 자리가 없으면 그냥 겹친 채로 둡니다. 화면이 가득 찼다는 뜻이라 그 자체가 정보입니다.
        }

        private bool IsOccupied(float x, MonsterUnit self)
        {
            for (var i = 0; i < _alive.Count; i++)
            {
                var other = _alive[i];

                if (other == self || !other.IsAlive || !other.IsBlocked)
                    continue;

                if (Mathf.Abs(other.Position.x - x) < BlockedSpacing)
                    return true;
            }

            return false;
        }
    }
}

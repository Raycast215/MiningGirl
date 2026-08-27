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

        // 스폰·정렬에서 화면 가장자리를 물리는 여유. 그림 반폭(1.28 ÷ 2)입니다.
        private const float SpawnMargin = BlockedSpacing * 0.5f;

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

        // 범위 피해를 넣는 동안 목록이 바뀌므로 대상을 먼저 모아 둡니다.
        private readonly List<MonsterUnit> _areaBuffer = new List<MonsterUnit>();
        private readonly Dictionary<string, Pooling<MonsterUnit>> _pools = new Dictionary<string, Pooling<MonsterUnit>>();

        private readonly Transform _layer;
        private readonly Tower _tower;
        private readonly BattleBounds _bounds;

        // 발사 각도를 화면 폭 비율로 잡는 쪽에서 씁니다.
        // 같은 각도가 화면비에 따라 다른 그림이 되므로 각도를 상수로 둘 수 없습니다.
        public BattleBounds Bounds => _bounds;
        private readonly float _statMultiplier;
        private readonly FloatingDamageSpawner _floatingDamage;
        private readonly EffectSpawner _effects;

        public MonsterField(
            Transform layer,
            Tower tower,
            BattleBounds bounds,
            float statMultiplier,
            FloatingDamageSpawner floatingDamage,
            EffectSpawner effects)
        {
            _layer = layer;
            _tower = tower;
            _bounds = bounds;
            _statMultiplier = statMultiplier;
            _floatingDamage = floatingDamage;
            _effects = effects;
        }

        // 특정 위치에서 한 번 터지는 이펙트를 재생합니다.
        //
        // 피해 숫자와 같은 이유로 여기 둡니다 - 발사체가 스포너를 직접 들면 발사체를
        // 만드는 자리마다 스포너를 넘겨야 합니다. 필드는 이미 발사체가 들고 있습니다.
        public void PlayEffect(string effectAssetId, Vector3 position)
        {
            _effects?.Play(effectAssetId, position);
        }

        // 피해를 넣는 자리를 한곳으로 모읍니다.
        //
        // 숫자를 띄우는 건 맞은 순간에만 해야 하는데, 발사체가 직접 TakeDamage를 부르면
        // 그 호출부마다 띄우는 걸 잊지 않아야 합니다. 여기로 모으면 한 번만 적으면 됩니다.
        public void ApplyDamage(MonsterUnit unit, float amount)
        {
            if (unit == null || !unit.IsAlive || amount <= 0f)
                return;

            // 띄우는 숫자는 표기 위력이 아니라 실제로 깎인 양입니다.
            var applied = unit.TakeDamage(amount);

            _floatingDamage?.Show(applied, unit.Position);
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

            // 스폰 x는 그림이 화면 밖으로 삐져나오지 않을 만큼만 안쪽으로 물립니다.
            // 판정 반경이 아니라 그림 반폭 기준입니다.
            var position = new Vector3(_bounds.RandomSpawnX(SpawnMargin), _bounds.SpawnY, 0f);

            unit.Setup(row, _statMultiplier, _tower, position, HandleDied, HandleBlocked, HandleStatusDamage);

            _alive.Add(unit);

            if (_alive.Count > PeakAliveCount)
                PeakAliveCount = _alive.Count;
        }

        // 저장에서 되살립니다. 자리를 무작위로 뽑지 않고 저장된 좌표에 둡니다.
        public MonsterUnit SpawnAt(MonsterDataTableRow row, Vector3 position)
        {
            if (row == null || !_pools.TryGetValue(row.Id, out var pool))
                return null;

            var unit = pool.Get();

            unit.Setup(row, _statMultiplier, _tower, position, HandleDied, HandleBlocked, HandleStatusDamage);

            _alive.Add(unit);

            if (_alive.Count > PeakAliveCount)
                PeakAliveCount = _alive.Count;

            return unit;
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

        // 조준해도 되는 대상인지.
        //
        // 살아 있고 예약 피해로 이미 죽을 놈이 아니어야 하며, 그 위에 화면 안이어야
        // 합니다. 몬스터는 화면 위 밖(SpawnY)에서 생겨 내려오는데, 그때부터 조준하면
        // 플레이어가 못 보는 곳에 화력이 쓰입니다. 그 한 발은 무엇을 했는지 알 수
        // 없고, 다발형에서는 보이는 적 하나가 그만큼 방치됩니다.
        //
        // 중심 기준입니다. 중심이 경계에 있으면 몸통 절반이 보입니다.
        // 아래쪽은 안 봅니다 - 타워 앞에서 멈추므로 화면 밖으로 안 나갑니다.
        private bool CanAim(MonsterUnit unit)
        {
            if (!unit.IsTargetable)
                return false;

#if UNITY_EDITOR
            // 대조판용 스위치. 화면 밖 제한이 무조준 비율에 얼마나 기여하는지를
            // 재려면 그것만 끈 판이 필요합니다. 지금 62.9%가 제한 때문인지
            // 체력표 때문인지 섞여 있습니다.
            if (DebugIgnoreScreenBound)
                return true;
#endif

            return unit.Position.y <= _bounds.ScreenTopY;
        }

#if UNITY_EDITOR
        // 켜면 화면 밖 몬스터도 조준합니다. 측정 전용이고 빌드에는 안 들어갑니다.
        public static bool DebugIgnoreScreenBound;
#endif

        // 발사체가 1발일 때. 조준해도 되는 적 중 가장 가까운 하나입니다.
        //
        // 이미 죽을 만큼 피해가 날아오고 있는 놈은 후보에서 빠집니다.
        // 없으면 null이고, 그때는 쏘지 않습니다.
        public MonsterUnit FindNearestTargetable(Vector3 from)
        {
            MonsterUnit nearest = null;
            var best = float.MaxValue;

            for (var i = 0; i < _alive.Count; i++)
            {
                var unit = _alive[i];

                if (!CanAim(unit))
                    continue;

                var distance = (unit.Position - from).sqrMagnitude;

                if (distance >= best)
                    continue;

                best = distance;
                nearest = unit;
            }

            return nearest;
        }

        // 발사체가 2발 이상일 때 쓰는 후보 목록. 조준해도 되는 적을 가까운 순으로 count마리 담습니다.
        //
        // 서로 다른 적을 하나씩 맡습니다. 같은 놈에 몰아 쏘면 앞 발이 죽인 뒤에
        // 나머지가 시체로 날아가므로, 후보가 모자라면 그 수만큼만 쏘고 남는 발은 버립니다.
        // 그래서 다발형은 적이 많을 때 강하고 적을 때는 단발과 같아집니다.
        public void FillNearestTargetable(Vector3 from, int count, List<MonsterUnit> buffer)
        {
            buffer.Clear();

            if (count <= 0)
                return;

            for (var i = 0; i < _alive.Count; i++)
            {
                var unit = _alive[i];

                if (!CanAim(unit))
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

            _floatingDamage?.Clear();
            _effects?.Clear();
        }

        // 화상처럼 발사체가 아닌 피해도 같은 자리를 지나가게 합니다.
        // 숫자 표시와 처치 처리를 두 군데서 하면 한쪽을 빼먹습니다.
        private void HandleStatusDamage(MonsterUnit unit, float amount)
        {
            ApplyDamage(unit, amount);
        }

        // 범위 피해. 폭발이 씁니다.
        //
        // 예약을 걸지 않습니다(기획 규칙) - 발사 시점에 누가 범위 안에 들어올지 알 수
        // 없어서, 예약하면 실제로 안 맞을 적까지 조준 후보에서 빠집니다.
        public void ApplyAreaDamage(Vector3 center, float radius, float damage, MonsterUnit exclude)
        {
            if (radius <= 0f || damage <= 0f)
                return;

            _areaBuffer.Clear();

            var sqrRadius = radius * radius;

            for (var i = 0; i < _alive.Count; i++)
            {
                var unit = _alive[i];

                if (unit == exclude || !unit.IsAlive)
                    continue;

                // 반경은 몬스터 몸통까지 포함해서 봅니다. 중심점만 보면 큰 놈이 빠집니다.
                var reach = radius + unit.BodyRadius;

                if ((unit.Position - center).sqrMagnitude > reach * reach)
                    continue;

                _areaBuffer.Add(unit);
            }

            // ApplyDamage가 목록을 건드리므로 복사본으로 돕니다.
            for (var i = 0; i < _areaBuffer.Count; i++)
                ApplyDamage(_areaBuffer[i], damage);
        }

        // 범위 안의 살아 있는 적을 모읍니다. 상태이상과 넉백이 씁니다.
        public void FillInRadius(Vector3 center, float radius, List<MonsterUnit> buffer)
        {
            buffer.Clear();

            if (radius <= 0f)
                return;

            for (var i = 0; i < _alive.Count; i++)
            {
                var unit = _alive[i];

                if (!unit.IsAlive)
                    continue;

                var reach = radius + unit.BodyRadius;

                if ((unit.Position - center).sqrMagnitude <= reach * reach)
                    buffer.Add(unit);
            }
        }

        // 연쇄가 쓰는 조준. 방금 맞은 대상은 뺍니다.
        public MonsterUnit FindNearestTargetableExcept(Vector3 from, MonsterUnit exclude)
        {
            MonsterUnit nearest = null;
            var best = float.MaxValue;

            for (var i = 0; i < _alive.Count; i++)
            {
                var unit = _alive[i];

                if (unit == exclude || !CanAim(unit))
                    continue;

                var distance = (unit.Position - from).sqrMagnitude;

                if (distance >= best)
                    continue;

                best = distance;
                nearest = unit;
            }

            return nearest;
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
            // 넉백으로 밀렸다가 다시 닿은 개체는 세지 않습니다.
            // 도달 수는 "몇 마리가 닿았나"이지 "몇 번 닿았나"가 아닙니다.
            //
            // 개체에 플래그를 둡니다. 여기서 목록으로 들고 있으면 풀에서 재사용될 때
            // 이전 개체의 기록이 새 몬스터에 그대로 붙습니다.
            if (!unit.HasReachedTower)
                ReachedTowerCount++;

            var origin = unit.Position.x;

            for (var attempt = 0; attempt < 24; attempt++)
            {
                // 원래 자리에서 좌우로 번갈아 한 칸씩 벌려 가며 빈자리를 찾습니다.
                var step = BlockedSpacing * (attempt / 2 + 1) * 0.5f;
                var x = attempt == 0
                    ? origin
                    : _bounds.ClampX(origin + (attempt % 2 == 1 ? step : -step), SpawnMargin);

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

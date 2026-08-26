using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Manager;
using Pool;
using UnityEngine;

namespace Scene.MainGameScene.Battle
{
    // 발사체 프리팹을 이펙트 종류별로 풀링하고, 날아가는 것들을 매 프레임 굴립니다.
    public class ProjectileLauncher
    {
        private readonly Dictionary<string, Pooling<Projectile>> _pools = new Dictionary<string, Pooling<Projectile>>();
        private readonly List<Projectile> _flying = new List<Projectile>();
        private readonly List<Projectile> _finishedBuffer = new List<Projectile>();

        private readonly Transform _layer;
        private readonly MonsterField _field;
        private readonly BattleBounds _bounds;

        public ProjectileLauncher(Transform layer, MonsterField field, BattleBounds bounds)
        {
            _layer = layer;
            _field = field;
            _bounds = bounds;
        }

        // 스킬을 얻는 순간 로드하면 첫 발이 늦게 나가므로 미리 불러 둡니다.
        public async UniTask PreloadAsync(IEnumerable<string> effectAssetIds)
        {
            foreach (var id in effectAssetIds)
            {
                if (string.IsNullOrEmpty(id) || _pools.ContainsKey(id))
                    continue;

                var prefab = await AddressableManager.Instance.LoadAsset<GameObject>(id);

                if (prefab == null)
                {
                    Debug.LogError($"[Projectile] 이펙트 프리팹을 찾지 못했습니다: {id}");

                    continue;
                }

                var projectile = prefab.GetComponent<Projectile>();

                if (projectile == null)
                {
                    Debug.LogError($"[Projectile] {id} 프리팹에 Projectile이 없습니다.");

                    continue;
                }

                var pool = new Pooling<Projectile>(projectile, 0, _layer);
                pool.Pool();

                _pools.Add(id, pool);
            }
        }

        public void Fire(ProjectileSpec spec, Vector3 origin, Vector3 direction, float targetDistance)
        {
            if (string.IsNullOrEmpty(spec.EffectAssetId) || !_pools.TryGetValue(spec.EffectAssetId, out var pool))
                return;

            var projectile = pool.Get();

            projectile.PoolKey = spec.EffectAssetId;
            projectile.Setup(_field, _bounds, origin, direction, targetDistance, spec, HandleFinished);

            _flying.Add(projectile);
        }

        public void Tick(float deltaTime)
        {
            for (var i = 0; i < _flying.Count; i++)
                _flying[i].Tick(deltaTime);

            if (_finishedBuffer.Count == 0)
                return;

            foreach (var projectile in _finishedBuffer)
            {
                _flying.Remove(projectile);

                if (_pools.TryGetValue(projectile.PoolKey ?? string.Empty, out var pool))
                    pool.Return(projectile);
                else
                    projectile.gameObject.SetActive(false);
            }

            _finishedBuffer.Clear();
        }

        public void Clear()
        {
            foreach (var projectile in _flying)
            {
                if (projectile != null)
                    projectile.gameObject.SetActive(false);
            }

            _flying.Clear();
            _finishedBuffer.Clear();
        }

        private void HandleFinished(Projectile projectile)
        {
            _finishedBuffer.Add(projectile);
        }
    }
}

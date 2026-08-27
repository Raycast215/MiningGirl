using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Manager;
using Pool;
using UnityEngine;

namespace Scene.MainGameScene.Battle
{
    // 제자리에서 한 번 터지는 이펙트를 종류별로 풀링하고 재생합니다.
    //
    // 발사체와 같은 어드레서블 주소 체계를 쓰지만 풀은 따로 둡니다. 날아가는 것과
    // 제자리에서 끝나는 것은 붙는 컴포넌트가 다르고, 한 풀에 섞으면 꺼낼 때마다
    // 무엇인지 되물어야 합니다.
    public class EffectSpawner
    {
        private readonly Dictionary<string, Pooling<OneShotEffect>> _pools =
            new Dictionary<string, Pooling<OneShotEffect>>();

        private readonly List<OneShotEffect> _playing = new List<OneShotEffect>();
        private readonly List<OneShotEffect> _finishedBuffer = new List<OneShotEffect>();

        private readonly Transform _layer;

        public EffectSpawner(Transform layer)
        {
            _layer = layer;
        }

        // 터지는 순간에 로드하면 그 프레임이 튀고, 무엇보다 이펙트가 한 박자 늦게
        // 나와 무엇 때문에 터졌는지 읽히지 않습니다.
        public async UniTask PreloadAsync(IEnumerable<string> effectAssetIds)
        {
            foreach (var id in effectAssetIds)
            {
                if (string.IsNullOrEmpty(id) || _pools.ContainsKey(id))
                    continue;

                var prefab = await AddressableManager.Instance.LoadAsset<GameObject>(id);

                if (prefab == null)
                {
                    Debug.LogError($"[Effect] 이펙트 프리팹을 찾지 못했습니다: {id}");

                    continue;
                }

                var effect = prefab.GetComponent<OneShotEffect>();

                if (effect == null)
                {
                    Debug.LogError($"[Effect] {id} 프리팹에 OneShotEffect가 없습니다.");

                    continue;
                }

                var pool = new Pooling<OneShotEffect>(effect, 0, _layer);
                pool.Pool();

                _pools.Add(id, pool);
            }
        }

        // 등록되지 않은 주소면 조용히 넘깁니다.
        //
        // 이펙트가 없다고 게임이 멈출 이유는 없고, 없다는 사실은 선로딩에서 이미
        // 에러로 남습니다. 여기서 매 명중마다 다시 찍으면 로그가 묻힙니다.
        public void Play(string effectAssetId, Vector3 position)
        {
            if (string.IsNullOrEmpty(effectAssetId) || !_pools.TryGetValue(effectAssetId, out var pool))
                return;

            var effect = pool.Get();

            effect.PoolKey = effectAssetId;
            effect.Play(position);

            _playing.Add(effect);
        }

        public void Tick(float deltaTime)
        {
            for (var i = 0; i < _playing.Count; i++)
            {
                if (_playing[i].Tick(deltaTime))
                    _finishedBuffer.Add(_playing[i]);
            }

            if (_finishedBuffer.Count == 0)
                return;

            foreach (var effect in _finishedBuffer)
            {
                _playing.Remove(effect);

                effect.Stop();

                if (_pools.TryGetValue(effect.PoolKey ?? string.Empty, out var pool))
                    pool.Return(effect);
            }

            _finishedBuffer.Clear();
        }

        public void Clear()
        {
            foreach (var effect in _playing)
            {
                if (effect != null)
                    effect.Stop();
            }

            _playing.Clear();
            _finishedBuffer.Clear();
        }
    }
}

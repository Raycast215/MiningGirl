using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Manager
{
    public class AddressableManager : SingletonBase<AddressableManager>
    {
        private readonly Dictionary<string, object> _assetCacheDic = new Dictionary<string, object>();

        // Image별로 가장 마지막에 요청한 주소.
        //
        // 씬에 고정된 칸(3택 카드, 스킬 슬롯)만 들어오므로 개수가 늘지 않습니다.
        private readonly Dictionary<UnityEngine.UI.Image, string> _spriteRequestDic =
            new Dictionary<UnityEngine.UI.Image, string>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        
        /// Asset을 Load하고 반환합니다.
        ///
        /// 등록되지 않은 주소를 넣으면 null을 돌려주고 경고만 남깁니다.
        /// 예전에는 Result가 채워지기를 기다렸는데, 없는 주소는 Result가 영영 null이라
        /// 여기서 멈춘 채 초기화가 끝나지 않았습니다.
        /// (아직 만들지 못한 스테이지 BGM처럼, 비어 있는 게 정상인 주소가 있습니다.)
        public async UniTask<T> LoadAsset<T>(string assetName, Action<T> onComplete = null) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(assetName))
                return null;

            if (_assetCacheDic.TryGetValue(assetName, out var asset))
            {
                var ret = asset as T;

                onComplete?.Invoke(ret);
                return ret;
            }

            AsyncOperationHandle<T> handle;

            try
            {
                handle = Addressables.LoadAssetAsync<T>(assetName);
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning($"[Addressable] 불러오지 못했습니다: {assetName}\n{e.Message}");

                return null;
            }

            // 성공이든 실패든 끝나기를 기다립니다.
            await UniTask.WaitUntil(() => handle.IsDone, cancellationToken: _cts.Token);

            if (_cts == null || _cts.IsCancellationRequested)
                return null;

            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null)
            {
                UnityEngine.Debug.LogWarning($"[Addressable] 불러오지 못했습니다: {assetName}");

                // 실패했을 때는 onComplete를 부르지 않습니다.
                // 받는 쪽이 null을 정상 값으로 받아 그대로 쓰다 터지는 게 더 나쁩니다.
                Addressables.Release(handle);

                return null;
            }

            _assetCacheDic.TryAdd(assetName, handle.Result);

            onComplete?.Invoke(handle.Result);

            return handle.Result;
        }

        /// Label로 Asset을 Load하고 List로 반환합니다.
        public async UniTask<List<T>> LoadAssetsLabel<T>(string labelName, Action<T> onComplete = null) where T : UnityEngine.Object
        {
            var assetList = new List<T>();
            var locationList = await Addressables.LoadResourceLocationsAsync(labelName);
            
            foreach (var location in locationList)
            {
                var obj = await LoadAsset(location.PrimaryKey, onComplete);
                
                assetList.Add(obj);
            }
            
            return assetList;
        }

        /// 스프라이트를 불러와 Image에 넣습니다.
        /// 캐시에 있으면 즉시, 없으면 불러온 뒤 넣습니다.
        /// 불러오는 동안에는 감춰 두어 이전 그림이 남지 않게 합니다.
        public void ApplySprite(string assetName, UnityEngine.UI.Image target)
        {
            if (target == null)
                return;

            // 이 Image가 지금 무엇을 그려야 하는지 적어 둡니다.
            // 늦게 끝난 로드가 이미 바뀐 그림을 덮어쓰지 않게 하는 기준입니다.
            _spriteRequestDic[target] = assetName;

            if (string.IsNullOrEmpty(assetName))
            {
                target.enabled = false;

                return;
            }

            if (_assetCacheDic.TryGetValue(assetName, out var cached) && cached is UnityEngine.Sprite sprite)
            {
                target.sprite = sprite;
                target.enabled = true;

                return;
            }

            target.enabled = false;

            ApplySpriteAsync(assetName, target).Forget();
        }

        private async UniTaskVoid ApplySpriteAsync(string assetName, UnityEngine.UI.Image target)
        {
            var sprite = await LoadAsset<UnityEngine.Sprite>(assetName);

            // 불러오는 사이에 다른 카드로 바뀌었을 수 있습니다.
            if (sprite == null || target == null)
                return;

            // 기다리는 동안 같은 Image가 다른 주소를 요청했으면 이 결과는 버립니다.
            //
            // 3택 다시 뽑기처럼 같은 칸을 연달아 갈아 끼우면, 먼저 시작한 로드가
            // 나중에 끝나 새 카드 위에 옛 아이콘을 덮어씁니다. 캐시된 그림은 즉시
            // 들어가고 처음 보는 그림은 늦게 들어오므로 순서가 쉽게 뒤집힙니다.
            if (!_spriteRequestDic.TryGetValue(target, out var latest) || latest != assetName)
                return;

            target.sprite = sprite;
            target.enabled = true;
        }

        /// Asset을 Unload합니다.
        public void Unload(string assetName)
        {
            if (_assetCacheDic.TryGetValue(assetName, out var asset) is false) 
                return;

            _assetCacheDic.Remove(assetName);
            Addressables.Release(asset);
        }

 #region Override Methods Implementation
        
        protected override void Initialized()
        {
            IsInitialized = true;
        }
        
        protected override void OnDestroy()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            
            base.OnDestroy();
        }
        
#endregion
    }
}
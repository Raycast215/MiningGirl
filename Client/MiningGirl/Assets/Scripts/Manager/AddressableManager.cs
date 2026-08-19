using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;

namespace Manager
{
    public class AddressableManager : SingletonBase<AddressableManager>
    {
        private readonly Dictionary<string, object> _assetCacheDic = new Dictionary<string, object>();
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        
        /// Asset을 Load하고 반환합니다.
        public async UniTask<T> LoadAsset<T>(string assetName, Action<T> onComplete = null) where T : UnityEngine.Object
        {
            if (_assetCacheDic.TryGetValue(assetName, out var asset))
            {
                var ret = asset as T;
                
                onComplete?.Invoke(ret);
                return ret;
            }
           
            var handle = Addressables.LoadAssetAsync<T>(assetName);

            handle.Completed += loadedAsset =>  _assetCacheDic.TryAdd(assetName, loadedAsset.Result);
            handle.Completed += loadedAsset =>  onComplete?.Invoke(loadedAsset.Result);
            
            await UniTask.WaitUntil(() => handle.Result != null, cancellationToken: _cts.Token);
            
            if (_cts == null || _cts.IsCancellationRequested)
                return null;
            
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
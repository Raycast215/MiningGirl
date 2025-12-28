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
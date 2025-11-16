using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Scene
{ 
    public abstract class SceneControllerBase : GameInitializer, IDisposable
    {
        private CancellationTokenSource _cts;

        private void Start()
        {
            SceneStartAsync().Forget();
        }

        public async UniTask SceneStartAsync()
        {
            // 이전 토큰 초기화.
            Dispose();
            _cts = new CancellationTokenSource();
            IsInitialized = false;
            
            try
            {
                var isUILoaded = await LoadPreData(_cts.Token);

                if (!isUILoaded)
                {
                    Debug.LogError("[Scene] 초기화 실패");
                    return;
                }
                
                IsInitialized = true;
            }
            catch (OperationCanceledException)
            {
                Debug.Log("[Scene] 초기화 취소");
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }
        
        private void OnDestroy()
        { 
            Dispose();
        }

#region IDisposable

        public void Dispose()
        {
            if (_cts == null)
                return;
            
            _cts.Cancel();
            _cts.Dispose();
            _cts = null;
        }

#endregion

        protected abstract UniTask<bool> LoadPreData(CancellationToken token);
    }
}
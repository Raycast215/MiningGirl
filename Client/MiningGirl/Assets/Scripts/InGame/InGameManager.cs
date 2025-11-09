using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using InGame.System.Loader;
using UnityEngine;

namespace InGame
{
    public class InGameManager : GameInitializer, IDisposable
    {
        [SerializeField]
        private Transform actorTransform;
        
        private PlayerLoader _playerLoader;
        private EnemyLoader _enemyLoader;
        private CancellationTokenSource _cts;

        private void Start()
        {
            Initialize().Forget();
        }

        private async UniTaskVoid Initialize()
        {
            _cts = new CancellationTokenSource();
            
            try
            {
                _playerLoader = new PlayerLoader(actorTransform);
                _playerLoader.Load();
                await UniTask.WaitUntil(() => _playerLoader.GetPlayer != null, cancellationToken: _cts.Token);

                _enemyLoader = new EnemyLoader(actorTransform);
                _enemyLoader.Initialize().Forget();
                await UniTask.WaitUntil(() => _enemyLoader.IsInitialized, cancellationToken: _cts.Token);
                _enemyLoader.Load();
            }
            catch (OperationCanceledException)
            {
                Dispose();
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                throw;
            }
            
            IsInitialized = true;
        }

#region IDisposable

        public void Dispose()
        {
            _cts?.Cancel();
            _cts?.Dispose();
            _cts = null;
        }

#endregion
    }
}
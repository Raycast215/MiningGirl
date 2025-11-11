using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using InGame.System.Enemy;
using InGame.System.FloatingDamage;
using InGame.System.Loader;
using NUnit.Framework;
using UnityEngine;

namespace InGame
{
    public interface IInGameHandler
    {
        public List<IHit> GetEnemyList();
    }
    
    public class InGameManager : GameInitializer, IDisposable, IInGameHandler
    {
        [SerializeField]
        private Transform actorTransform;
        [SerializeField] 
        private Camera cam;
        [SerializeField]
        private FloatingDamageController floatingDamageController;
        
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
                floatingDamageController.InitAsync().Forget();
                await UniTask.WaitUntil(() => floatingDamageController.IsInitialized);
                
                _enemyLoader = new EnemyLoader(actorTransform);
                _enemyLoader.Initialize().Forget();
                await UniTask.WaitUntil(() => _enemyLoader.IsInitialized, cancellationToken: _cts.Token);
                _enemyLoader.Load();
                
                _playerLoader = new PlayerLoader(actorTransform);
                _playerLoader.Load();
                await UniTask.WaitUntil(() => _playerLoader.GetPlayer != null, cancellationToken: _cts.Token);
                
                cam.transform.SetParent(_playerLoader.GetPlayer.transform);
                _playerLoader.GetPlayer.Init(this, floatingDamageController.Damage);
                _playerLoader.GetPlayer.ExecuteFindEnemy().Forget();
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

#region IInGameHandler

        public List<IHit> GetEnemyList()
        {
            return _enemyLoader.GetEnemyList;
        }

#endregion
    }
}
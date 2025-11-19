using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using InGame.System.FloatingDamage;
using InGame.System.Loader;
using InGame.System.Skill;
using UnityEngine;
using Timer = InGame.System.Timer;

namespace InGame
{
    public interface IInGameHandler
    {
        public List<IHit> GetEnemyList();
        public List<Vector3> GetTilePosList();
        public Transform GetTileTransform();
    }

    public interface ISkillHandlerParentHandler
    {
        public Canvas GetUICanvas();
    }
    
    public class InGameManager : GameInitializer, IDisposable, IInGameHandler, ISkillHandlerParentHandler
    {
        [SerializeField]
        private Transform actorTransform;
        [SerializeField] 
        private Camera cam;

        [Header("UI")]
        [SerializeField] 
        private Timer timer;
        [SerializeField]
        private Canvas uiCanvas;
        
        [SerializeField] 
        private SkillController skillController;
        [SerializeField] 
        private Transform tileTransform;
        [SerializeField]
        private FloatingDamageController floatingDamageController;
        
        private TileLoader _tileLoader;
        private PlayerLoader _playerLoader;
        private EnemyLoader _enemyLoader;
        private CancellationTokenSource _cts;

        private void Start()
        {
            Application.targetFrameRate = 120;
            Initialize().Forget();
        }

        private async UniTaskVoid Initialize()
        {
            _cts = new CancellationTokenSource();
            
            try
            {
                timer.Init(120, null);
                
                skillController.Init(this);
                await UniTask.WaitUntil(() => skillController.IsInitialized);
                
                floatingDamageController.InitAsync().Forget();
                await UniTask.WaitUntil(() => floatingDamageController.IsInitialized);

                await UniTask.WaitForSeconds(1.0f, cancellationToken: _cts.Token);
                
                _tileLoader = new TileLoader(tileTransform);
                _tileLoader.Initialize().Forget();
                await UniTask.WaitUntil(() => _tileLoader.IsInitialized, cancellationToken: _cts.Token);
                _tileLoader.Load();
                
                await UniTask.WaitForSeconds(2.0f, cancellationToken: _cts.Token);
                
                _enemyLoader = new EnemyLoader(actorTransform, this);
                _enemyLoader.Initialize().Forget();
                await UniTask.WaitUntil(() => _enemyLoader.IsInitialized, cancellationToken: _cts.Token);
                _enemyLoader.Load();
                
                timer.Appear();
                skillController.Appear();
                await UniTask.WaitForSeconds(1.0f, cancellationToken: _cts.Token);
                
                _playerLoader = new PlayerLoader(actorTransform);
                _playerLoader.Load();
                await UniTask.WaitUntil(() => _playerLoader.GetPlayer != null, cancellationToken: _cts.Token);
                
                // 카메라 부모 설정.
                cam.transform.SetParent(_playerLoader.GetPlayer.transform);
                
                // 플레이어 시작.
                _playerLoader.GetPlayer.Init(this, floatingDamageController.Damage);
                _playerLoader.GetPlayer.ExecuteFindEnemy().Forget();
                
                // 타이머 시작.
                timer.Execute().Forget();
                
                // 스킬 게이지 시작.
                skillController.ExecuteSkillPointGauge().Forget();
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

        public List<Vector3> GetTilePosList()
        {
            var tempList = new List<Vector3>()
            {
                new Vector3(-1, 1, 0) * 1.5f, new Vector3(0, 1, 0) * 1.5f, new Vector3(1, 1, 0) * 1.5f,
                new Vector3(-1, 0, 0) * 1.5f, new Vector3(0, 0, 0) * 1.5f, new Vector3(1, 0, 0) * 1.5f,
                new Vector3(-1, -1, 0) * 1.5f, new Vector3(0, -1, 0) * 1.5f, new Vector3(1, -1, 0) * 1.5f,
            };
            
            return _tileLoader.GetPosList
                .Where(x => !tempList.Contains(x))
                .ToList();
        }

        public Transform GetTileTransform()
        {
            return tileTransform;
        }

#endregion

#region ISkillHandlerParentHandler

        public Canvas GetUICanvas()
        {
            return uiCanvas;
        }

#endregion
    }
}
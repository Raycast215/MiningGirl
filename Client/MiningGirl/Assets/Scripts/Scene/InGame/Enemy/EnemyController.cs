using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Scene.InGame.Enemy
{
    public class EnemyController : GameMonoInitializer
    {
        private EnemyPool _pool;
        private EnemySpawnListController _spawnListController;
        private EnemySpawnDataController _spawnDataController;
        private EnemySpawnProcess _spawnProcess;
        
        public void Init()
        {
            if (IsInitialized)
                return;

            Debug.Log("Init");
            
            _pool = new EnemyPool(transform);
            _spawnListController = new EnemySpawnListController();
            _spawnDataController = new EnemySpawnDataController(null);
            _spawnProcess = new EnemySpawnProcess();
            
            _pool.Init(_spawnListController.Remove);
            _spawnListController.Init(_pool.Release);
            _spawnDataController.Init();
            _spawnProcess.Init(() => SpawnEnemy().Forget());
            
            IsInitialized = true;
        }
        
        public void Execute()
        {
            Debug.Log("Execute");
            
            SetTimeScale();
            
            _spawnProcess.Execute().Forget();
        }
        
        public void Pause()
        {
            Debug.Log("Pause");
            
            _spawnProcess.Pause();
        }

        public void Resume()
        {
            Debug.Log("Resume");
            
            _spawnProcess.Resume().Forget();
        }

        public void SetTimeScale(float scale = 1.0f)
        {
            Debug.Log($"SetTimeScale: {scale}");
            
            _spawnProcess.SetTimeScale(scale);
        }
        
        public void Clear()
        {
            _spawnListController.Clear();
            _spawnProcess.Clear();
        }

        private async UniTask SpawnEnemy()
        {
            var enemy = await _pool.Get("Enemy");
            
            _spawnListController.Add(enemy);
            _spawnDataController.Set(enemy);
        }
    }
}
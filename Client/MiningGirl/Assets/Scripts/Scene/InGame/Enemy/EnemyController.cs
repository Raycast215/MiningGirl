using Cysharp.Threading.Tasks;

namespace Scene.InGame.Enemy
{
    public class EnemyController : GameMonoInitializer
    {
        private EnemyPool _pool;
        private EnemySpawner _spawner;
        private EnemySpawnProcess _spawnProcess;
        
        public void Init()
        {
            if (IsInitialized)
                return;

            _pool = new EnemyPool();
            _spawner = new EnemySpawner();
            _spawnProcess = new EnemySpawnProcess();
            
            _pool.Init(transform, _spawner.Release);
            _spawner.Init(_pool.Release);
            _spawnProcess.Init(_pool.Get);
            
            IsInitialized = true;
        }
        
        public void Execute()
        {
            SetTimeScale();
            
            _spawnProcess.Execute().Forget();
        }
        
        public void Pause()
        {
            _spawnProcess.Pause();
        }

        public void Resume()
        {
            _spawnProcess.Resume().Forget();
        }

        public void SetTimeScale(float scale = 1.0f)
        {
            _spawnProcess.SetTimeScale(scale);
        }
        
        public void Clear()
        {
            _spawner.Clear();
            _spawnProcess.Clear();
        }
    }
}
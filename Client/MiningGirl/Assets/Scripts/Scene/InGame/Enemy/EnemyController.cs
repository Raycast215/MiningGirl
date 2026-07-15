namespace Scene.InGame.Enemy
{
    public class EnemyController : GameMonoInitializer
    {
        private EnemySpawner _spawner;

        public void Init()
        {
            if (IsInitialized)
                return;
            
            _spawner.Init(transform);
            
            IsInitialized = true;
        }
        
        public void Clear()
        {
            _spawner.Clear();
        }
    }
}
using Cysharp.Threading.Tasks;
using Scene.InGame.Entity.Data;
using Scene.InGame.Entity.Interface;
using Scene.InGame.Entity.Resource;
using UnityEngine;

namespace Scene.InGame.Entity.Enemy
{
    public class EnemyController : EntityControllerBase<Enemy>
    {
        [SerializeField]
        private float minPosX = -3.0f;
        [SerializeField]
        private float maxPosX = 3.0f;
        [SerializeField] 
        private float minPosY = 2.0f;
        [SerializeField] 
        private float maxPosY = 5.0f;
        
        private IInGameHandler _handler;
        private SpawnData _spawnData;
        
        public void Init(IInGameHandler handler)
        {
            if (IsInitialized)
                return;
            
            _handler = handler;
            InitAsync("Enemy", 10).Forget();
            
            _spawnData = new SpawnData
            {
                Count = 1,
                Interval = 10
            };
        }

        public async void ExecuteSpawn()
        {
            if (!IsInitialized)
                return;

            while (true)
            {
                Spawn(_spawnData.Count);
                
                await UniTask.WaitForSeconds(_spawnData.Interval);
            }
        }
        
        public void Spawn(int count)
        {
            for (var i = 0; i < count; i++)
            {
                var player = _handler.GetEntityHandler().GetPlayer();
                var playerPos = player.GetPosition();
                var posX = playerPos.x + Random.Range(minPosX, maxPosX);
                var posY = playerPos.y + Random.Range(minPosY, maxPosY);
                var pos = new Vector2(posX, posY);
                
                Spawn(pos, player);
            }
        }
        
        private void Spawn(Vector3 pos, IEntity entity)
        {
            var ins = Get();
                
            ins.BaseData = new EntityData
            {
                MaxHealth = 3,
                Health = 3,
                MoveSpeed = 1,
                MoveToMinDistance = 2,
                AttackDelay = 300
            };
            
            ins.SetHandler(_handler, x => Return(x as Enemy));
            ins.InitAsync().Forget();
            ins.SetPosition(pos);
            ins.SetTarget(entity);
            ins.gameObject.SetActive(true);
        }
    }
}
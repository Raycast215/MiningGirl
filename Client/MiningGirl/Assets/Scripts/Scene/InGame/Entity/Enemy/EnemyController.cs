using Scene.InGame.Entity.Data;
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
        
        public void Init(IInGameHandler handler)
        {
            _handler = handler;
            InitAsync("Enemy", 10).Forget();
        }

        public void RandomSpawn()
        {
            var player = _handler.GetEntityHandler().GetPlayer();
            var playerPos = player.GetPosition();
            var posX = playerPos.x + Random.Range(minPosX, maxPosX);
            var posY = playerPos.y + Random.Range(minPosY, maxPosY);
            var pos = new Vector2(posX, posY);
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
            ins.SetTarget(player);
            ins.gameObject.SetActive(true);
        }
    }
}
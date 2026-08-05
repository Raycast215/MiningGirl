using Scene.InGame.Entity.Interface;
using UnityEngine;

namespace Scene.InGame.Enemy
{
    public class EnemySpawnDataController : GameInitializer
    {
        private IEntity _target;
        
        public EnemySpawnDataController(IEntity target)
        {
            _target = target;
        }
        
        public void Init()
        {
            if (IsInitialized)
                return;
            
            IsInitialized = true;
        }
        
        public void Set(IEntityObject entity)
        {
            // var data = new EntityData
            // {
            //     MaxHealth = 3,
            //     Health = 3,
            //     MoveSpeed = 1,
            //     MoveToMinDistance = 2,
            //     AttackDelay = 300
            // };
          
            // entity.InitAsync().Forget();
            // entity.SetTarget(entity);
            
            entity.SetPosition(GetPos());
            entity.SetActiveObject(true);
        }

        private Vector3 GetPos()
        {
            const float max = 1.2f;
            const float min = -0.2f;
            const float half = 0.5f;
            
            var distance = -Camera.main!.transform.position.z;
            var x = Random.value < half 
                ? Random.Range(min, max) 
                : Random.value < half ? min : max;
            var y = Random.value < half 
                ? Random.value < half ? min : max 
                : Random.Range(min, max);

            return Camera.main.ViewportToWorldPoint(new Vector3(x, y, distance));
        }
    }
}
using System;
using System.Collections.Generic;
using Scene.InGame.Entity.Interface;

namespace Scene.InGame.Enemy
{
    public class EnemySpawner : GameInitializer
    {
        private event Action<IEntity> OnReleased;
        
        public List<IEntity> ActivateList { get; private set; }

        public EnemySpawner Init(Action<IEntity> onReleased)
        {
            if (IsInitialized)
                return this;
              
            OnReleased += onReleased;
            
            ActivateList = new List<IEntity>();
            
            IsInitialized = true;
            return this;
        }

        public void Release(IEntity entity)
        {
            ActivateList.Remove(entity);
        }
        
        public void Clear()
        {
            if (ActivateList == null || ActivateList.Count == 0)
                return;
            
            ActivateList.ForEach(x => OnReleased?.Invoke(x));
            ActivateList.Clear();
        }
    }
}
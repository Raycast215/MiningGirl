using System;
using System.Collections.Generic;
using Scene.InGame.Entity.Interface;

namespace Scene.InGame.Enemy
{
    public class EnemySpawnListController : GameInitializer
    {
        private event Action<IEntityObject> OnReleased;
        
        public List<IEntityObject> ActivateList { get; private set; }

        public void Init(Action<IEntityObject> onReleased)
        {
            if (IsInitialized)
                return;
              
            OnReleased += onReleased;
            
            ActivateList = new List<IEntityObject>();
            
            IsInitialized = true;
        }

        public void Add(IEntityObject entity)
        {
            ActivateList.Add(entity);
        }
        
        public void Remove(IEntityObject entity)
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
using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Manager;
using Scene.InGame.Entity.Interface;
using UnityEngine;

namespace Scene.InGame.Enemy
{
    public class EnemyPool : GameInitializer
    {
        private event Action<IEntityObject> OnReleased;
        
        private readonly Dictionary<string, Queue<IEntityObject>> _dic;
        private readonly Transform _parent;

        public EnemyPool(Transform parent)
        {
            _parent = parent;
            _dic = new Dictionary<string, Queue<IEntityObject>>();
        }
        
        public void Init(Action<IEntityObject> onReleased)
        {
            if (IsInitialized)
                return;
            
            OnReleased += onReleased;
            
            IsInitialized = true;
        }

        public async UniTask<IEntityObject> Get(string entityId)
        {
            // 어떤 프리팹이 올지 몰라 미리 생성해두지 않음.(Lazy Pooling)
            // 첫 생성이면, 기본 수량만큼만 생성.
            
            if (!IsInitialized)
                return null;

            if (string.IsNullOrEmpty(entityId))
                return null;

            if (!_dic.ContainsKey(entityId) || _dic[entityId].Count == 0)
            {
                await Create(entityId);
            }
            
            return _dic[entityId].Dequeue();
        }
        
        public void Release(IEntityObject entity)
        {
            _dic[entity.GetId()].Enqueue(entity);
            entity.SetActiveObject(false);
        }

        private async UniTask Create(string entityId)
        {
            var prefab = await AddressableManager.Instance.LoadAsset<GameObject>(entityId);

            if (!_dic.ContainsKey(entityId))
            {
                _dic.Add(entityId, new Queue<IEntityObject>());

                const int count = 10;
                
                for (var i = 0; i < count; i++)
                {
                    CreateEntity();
                }
            }
            else
            {
                CreateEntity();
            }
            
            return;
            
            void CreateEntity()
            {
                var ins = UnityEngine.Object.Instantiate(prefab, _parent);
                var entity = ins.GetComponent<IEntityObject>();
                
                entity.SetActiveObject(false);
                entity.SetReleaseCallback(Callback);
                entity.SetId(entityId);
                
                _dic[entityId].Enqueue(entity);
            }
            
            void Callback(IEntityObject x)
            {
                OnReleased?.Invoke(x);
                Release(x);
            }
        }
    }
}
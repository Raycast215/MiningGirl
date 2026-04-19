using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Manager;
using Scene.InGame.Entity.Interface;
using UnityEngine;

namespace Scene.InGame.Entity
{
    public abstract class EntityControllerBase<T> : GameInitializer where T : IEntity
    {
        public List<T> ActivateList { get; private set; }
        
        private Queue<T> _queue;
        private string _prefabName;
        private int _count;

        public void Clear()
        {
            if (_queue == null || _queue.Count == 0)
                return;

            foreach (var entity in ActivateList)
            {
                _queue.Enqueue(entity);
                entity.GetTransform().gameObject.SetActive(false);
            }
            
            ActivateList.Clear();
            _queue.Clear();
        }

        public void UpdateEntity()
        {
            if (ActivateList == null || ActivateList.Count == 0)
                return;
            
            ActivateList.ForEach(x=> x.UpdateNode());
        }

        public void Return(T entity)
        {
            _queue.Enqueue(entity);
            ActivateList.Remove(entity);
            entity.GetTransform().gameObject.SetActive(false);
        }
        
        protected async UniTaskVoid InitAsync(string prefabName, int count)
        {
            _prefabName = prefabName;
            _count = count;
            _queue = new Queue<T>();
            ActivateList = new List<T>();
            
            for (var i = 0; i < _count; i++)
            {
                Create().Forget();
            }
            
            await UniTask.WaitUntil(() => _queue.Count == _count);
            
            IsInitialized = true;
        }
        
        protected T Get()
        {
            if (_queue == null || _queue.Count == 0)
            {
                Create().Forget();
            }

            var entity = _queue!.Dequeue();
            
            ActivateList.Add(entity);
            return entity;
        }
        
        private async UniTaskVoid Create()
        {
            try
            {
                var prefab = await AddressableManager.Instance.LoadAsset<GameObject>(_prefabName);
                var ins = Instantiate(prefab, transform);
            
                ins.gameObject.SetActive(false);
            
                _queue.Enqueue(ins.GetComponent<T>());
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
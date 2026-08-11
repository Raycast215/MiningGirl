using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Manager;
using Scene.InGame.Entity.Interface;
using UnityEngine;

namespace Scene.InGame.Entity
{
    public abstract class EntityControllerBase<T> : GameMonoInitializer where T : IEntity
    {
        public List<T> ActivateList { get; private set; }
        
        private Queue<T> _queue;
        private string _prefabName;
        private int _count;

        public void Clear()
        {
            // if (_queue == null || _queue.Count == 0 || ActivateList.Count == 0)
            //     return;

            foreach (var entity in ActivateList)
            {
                _queue.Enqueue(entity);
                entity.GetTransform().gameObject.SetActive(false);
            }
            
            ActivateList.Clear();
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
            _queue ??= new Queue<T>();
            ActivateList ??= new List<T>();
            
            for (var i = 0; i < _count; i++)
            {
                Create().Forget();
            }
            
            await UniTask.WaitUntil(() => _queue.Count == _count);
            
            IsInitialized = true;
        }
        
// 큐가 비어 있으면 새 인스턴스가 만들어질 때까지 실제로 기다린 뒤 꺼내줍니다.
        // (기존에는 Create()를 fire-and-forget으로 던지고 바로 Dequeue를 시도해서
        //  로드가 끝나기 전이면 InvalidOperationException이 발생했습니다.)
        protected async UniTask<T> Get()
        {
            if (_queue == null || _queue.Count == 0)
            {
                await Create();
            }

            var entity = _queue!.Dequeue();
            
            ActivateList.Add(entity);
            return entity;
        }
        
private async UniTask Create()
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
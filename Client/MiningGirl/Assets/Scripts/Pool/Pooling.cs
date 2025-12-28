using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Pool
{
    public class Pooling<T> : IDisposable where T : Component
    {
        private readonly T _prefab;
        private readonly int _capacity;
        private readonly Transform _layer;
        
        private Queue<T> _queue;

        private bool InstanceExist => _queue != null && _queue.Any();
        
        /// 풀링을 초기화합니다.
        public Pooling(T prefab, int capacity, Transform layer)
        {
            _prefab = prefab;
            _capacity = capacity;
            _layer = layer;
        }
        
        /// 풀링을 실행합니다.
        public void Pool()
        {
            _queue = new Queue<T>();

            for (var i = 0; i < _capacity; i++)
            {
                Return(Create());
            }
        }

        /// 인스턴스를 반환합니다.
        public T Get()
        {
            var instance = InstanceExist
                ? _queue.Dequeue()
                : Create();

            instance.gameObject.SetActive(true);
            
            return instance;
        }

        /// 오브젝트를 반환합니다.
        public void Return(T toTarget)
        {
            toTarget.gameObject.SetActive(false);

            _queue.Enqueue(toTarget);
        }

        /// 오브젝트를 제거합니다.
        public void Dispose()
        {
            while (InstanceExist)
            {
                UnityEngine.Object.Destroy(_queue.Dequeue());
            }
            
            _queue.Clear();
        }
        
        /// 오브젝트를 생성합니다.
        private T Create()
        {
            var createObject = UnityEngine.Object.Instantiate(_prefab, _layer);
            
            return createObject;
        }
    }
}
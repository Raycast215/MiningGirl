using System.Threading;
using UnityEngine;

namespace Pool
{
    public abstract class PoolBase<T> : MonoBehaviour where T : Component
    {
        public T Controller { get; protected set; }
        public string ID { get; private set; }
        
        protected CancellationTokenSource Cts = new CancellationTokenSource();
        
        public void SetController(T controller)
        {
            // Controller 저장.
            Controller = controller;
            // Controller를 Pool 오브젝트 하위로 이동.
            SetControllerParent(transform);
        }
        
        public void SetControllerParent(Transform parent, bool isActive = true)
        {
            // 오브젝트를 받아온 부모 하위로 이동.
            Controller.transform.SetParent(parent, false);
            // Controller 활성화 상태 변경.
            Controller.gameObject.SetActive(isActive);
        }

        public void SetID(string id)
        {
            ID = id;
        }

        private void OnDestroy()
        {
            Cts?.Cancel();
            Cts?.Dispose();
            Cts = null;
        }
        
        protected abstract void Release();
    }
}
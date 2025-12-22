using UnityEngine;

namespace Manager
{
    public abstract class SingletonBase<T> : MonoBehaviour where T : Component
    {
        public static T Instance
        {
            get
            {
                // 게임이 실행중이 아닐 때 인스턴스 참조로 싱글턴 오브젝트가 생성되는것을 방지
#if UNITY_EDITOR
                if (Application.isPlaying is false)
                    return null;
#endif
                
                // 생성된 인스턴스가 있으면 반환.
                if (_instance is not null)
                    return _instance;
                
                _instance = FindObjectOfType<T>();

                // 생성된 인스턴스가 있으면 반환.
                if (_instance is not null)
                    return _instance;

                // 생성된 인스턴스가 없다면 인스턴스 생성 후 반환.
                var typeName = typeof(T).Name;
                var obj = new GameObject(typeof(T).Name)
                {
                    name = typeName
                };

                _instance = obj.AddComponent<T>();
                
                return _instance;
            }
        }
        
        /// 인스턴스가 존재하는지 반환.
        public static bool HasInstance => _instance != null;
        public bool IsInitialized { get; protected set; }
        private static T _instance;
        
        protected virtual void Awake()
        {
            Initialized();
        }
        
        protected virtual void Initialized()
        {
            // 앱 미실행이면 리턴.
            if (Application.isPlaying is false)
                return;

            if (_instance == null)
            {
                // 최초 생성인 경우.
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                // 중복된 인스턴스이면 삭제 처리.
                if (this != _instance)
                    Destroy(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
            
            Destroy(gameObject);
        }
    }
}
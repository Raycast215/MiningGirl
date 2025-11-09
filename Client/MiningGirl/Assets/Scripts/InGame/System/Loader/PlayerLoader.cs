using UnityEngine;

namespace InGame.System.Loader
{
    public class PlayerLoader
    {
        public TestPlayer GetPlayer { get; private set; }
        
        private Transform _parent;
        
        public PlayerLoader(Transform parent)
        {
            _parent = parent;
        }

        public void Load()
        {
            // To Do: 어드레서블로 변경
            var prefab = Resources.Load<TestPlayer>("InGame/Player");
            var ins = Object.Instantiate(prefab, _parent);
            
            ins.transform.localPosition = Vector3.zero;
            
            GetPlayer = ins.GetComponent<TestPlayer>();
        }
    }
}
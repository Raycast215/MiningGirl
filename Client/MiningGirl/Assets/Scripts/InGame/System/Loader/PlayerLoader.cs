using Manager;
using UnityEngine;

namespace InGame.System.Loader
{
    public class PlayerLoader
    {
        public PlayerController GetPlayer { get; private set; }
        
        private Transform _parent;
        
        public PlayerLoader(Transform parent)
        {
            _parent = parent;
        }

        public void StopProcess()
        {
            GetPlayer.Stop();
        }
        
        public async void Load()
        {
            // To Do: 어드레서블로 변경
            var prefab = await AddressableManager.Instance.LoadAsset<GameObject>("Player_001");
            
            GetPlayer = Object.Instantiate(prefab, _parent).GetComponent<PlayerController>();
            GetPlayer.transform.localPosition = Vector3.zero;
        }
    }
}
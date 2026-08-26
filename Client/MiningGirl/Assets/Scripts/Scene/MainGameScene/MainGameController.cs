using Cysharp.Threading.Tasks;
using Manager;
using UnityEngine;

namespace Scene.MainGameScene
{
    public class MainGameController : GameMonoInitializer
    {
        private void Start()
        {
            InitAsync().Forget();
        }

        private async UniTaskVoid InitAsync()
        {
            IsInitialized = true;
            
            await UniTask.WaitUntil(() => IsInitialized);
            
            CoverUIManager.Instance.CoverUI.Hide().Forget();
        }
    }
}
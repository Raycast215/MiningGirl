using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

// 네임스페이스가 Legacy.Scene.InGame으로 바뀌면서 바깥쪽 Scene 네임스페이스를 자동으로 찾지 못해 명시합니다.
using Scene;

namespace Legacy.Scene.InGame
{
    public class InGameSceneController : SceneControllerBase
    {
        [SerializeField]
        private InGameController inGameController;
        
        protected override async UniTask<bool> LoadPreData(CancellationToken token)
        {
            try
            {
                await inGameController.InitAsync();
                return true;
            }
            catch (OperationCanceledException)
            {
                return false;
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }
}
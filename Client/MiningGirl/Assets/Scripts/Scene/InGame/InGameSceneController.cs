using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Scene.InGame
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
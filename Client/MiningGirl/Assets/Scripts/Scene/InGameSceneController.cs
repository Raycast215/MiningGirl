using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Scene
{
    public class InGameSceneController : SceneControllerBase
    {
        protected override async UniTask<bool> LoadPreData(CancellationToken token)
        {
            try
            {
                // UI 로드.
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
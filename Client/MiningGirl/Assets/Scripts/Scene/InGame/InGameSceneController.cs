using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Scene
{
    public class InGameSceneController : SceneControllerBase
    {
        // ui로드하고
        // 데이터 로드하고
        // 스테이지 세팅,
        // 캐릭터 세팅,
        // 스테이지 시작
        
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
using System;
using Cysharp.Threading.Tasks;
using MainGame.UI;
using Manager;
using UnityEngine;

namespace MainGame
{
    public class MainGameUIController : GameMonoInitializer
    {
        private event Action OnNextGameExecuted;
        
        [SerializeField]
        private TimerUI timerUI;

        public async UniTask InitAsync(Action onNextGameExecuted)
        {
            OnNextGameExecuted = null;
            OnNextGameExecuted += onNextGameExecuted;
            
            timerUI.Init(30, GameFinish);
            
            IsInitialized = true;
        }

        public void SetTime()
        {
            timerUI.SetTime(30);
        }

        public void GameStart()
        {
            timerUI.Execute().Forget();
        }
        
        public void GameFinish()
        {
            OnNextGameExecuted?.Invoke();
            Debug.Log("게임 종료");
        }
    }
}
using Cysharp.Threading.Tasks;
using InGame.System;
using Scene.InGame.UI.Resource;
using UnityEngine;

namespace Scene.InGame.UI
{
    public interface IInGameUIHandler
    {
        public void AddGoldCount(int add);
        public void AddStoneCount(int add);
    }
    
    public class InGameUI : GameInitializer, IInGameUIHandler
    {
        [SerializeField] 
        private Timer timerUI;
        [SerializeField]
        private CountViewerUI goldCountViewerUI;
        [SerializeField]
        private CountViewerUI stoneCountViewerUI;

        public async UniTaskVoid InitAsync()
        {
            timerUI.Init(180, null);
            goldCountViewerUI.SetCount(0);
            stoneCountViewerUI.SetCount(0);
            
            IsInitialized = true;
        }

        public void GameReady()
        {
            timerUI.Appear();
        }

        public void GameStart()
        {
            timerUI.Execute().Forget();
        }

        private void OnDestroy()
        {
            timerUI.StopProcess();
        }

#region IInGameUIHandler

        public void AddGoldCount(int add)
        {
            goldCountViewerUI.AddCount(add);
        }
        
        public void AddStoneCount(int add)
        {
            stoneCountViewerUI.AddCount(add);
        }

#endregion
    }
}
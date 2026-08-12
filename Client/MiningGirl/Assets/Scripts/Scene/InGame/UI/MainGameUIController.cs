using System;
using Cysharp.Threading.Tasks;
using MainGame.Entity;
using MainGame.UI;
using Manager;
using UnityEngine;

namespace MainGame
{
    public class MainGameUIController : GameMonoInitializer, IExpRewardHandler
    {
        private event Action OnNextGameExecuted;
        
        [SerializeField]
        private TimerUI timerUI;
        [SerializeField]
        private CostUI costUI;
        [SerializeField]
        private LevelExpUI levelExpUI;

        public async UniTask InitAsync(Action onNextGameExecuted)
        {
            OnNextGameExecuted = null;
            OnNextGameExecuted += onNextGameExecuted;
            
            timerUI.Init(30, GameFinish);
            costUI.Init();
            levelExpUI.Init();
            
            IsInitialized = true;
        }

        public void SetTime()
        {
            timerUI.SetTime(30);
            costUI.SetCost(0);
            levelExpUI.Reset();
        }

        public void GameStart()
        {
            timerUI.Execute().Forget();
            costUI.Execute().Forget();
        }
        
        // 카드 사용 등 외부에서 코스트를 다룰 때 쓰는 진입점입니다.
        public bool CanAffordCost(int amount) => costUI.CanAfford(amount);
        public bool TrySpendCost(int amount) => costUI.TrySpend(amount);
        public void AddCost(int amount, bool allowOvercharge = false) => costUI.Add(amount, allowOvercharge);

        // IExpRewardHandler 구현 — 몬스터 처치 등에서 경험치를 지급받습니다.
        public void OnExpGained(int amount)
        {
            levelExpUI.AddExp(amount);
        }

        public void GameFinish()
        {
            costUI.StopProcess();
            OnNextGameExecuted?.Invoke();
            Debug.Log("게임 종료");
        }
    }
}
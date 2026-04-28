using Cysharp.Threading.Tasks;
using InGame.System;
using Scene.InGame.UI.Growth.Stat;
using Scene.InGame.UI.Level;
using Scene.InGame.UI.Level.Test;
using Scene.InGame.UI.Resource;
using Scene.InGame.UI.Test;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Scene.InGame.UI
{
    public class InGameLevelData
    {
        public int Level { get; set; }
        public float Exp { get; set; }
        public float MaxExp { get; set; }
    }
    
    public interface IInGameUIHandler
    {
        public void AddGoldCount(int add);
        public void AddStoneCount(int add);
        public void AddExpCount(float add);
    }
    
    public class InGameUI : GameInitializer, IInGameUIHandler
    {
        [Header("Top")]
        [SerializeField] 
        private Timer timerUI;
        [SerializeField]
        private CountViewerUI goldCountViewerUI;
        [SerializeField]
        private CountViewerUI stoneCountViewerUI;
        
        [Header("Bottom")]
        [SerializeField]
        private LevelGaugeUI levelGaugeUI;

        [Header("TestUI")]
        [SerializeField]
        private GameObject testUI;
        [SerializeField]
        private ExpTestUIController expTestUIController;
        [SerializeField]
        private StatGrowthInfoUIController statGrowthInfoUIController;
        
        private InGameLevelData _levelData;
        
        public async UniTaskVoid InitAsync(InGameData inGameData)
        {
            _levelData = new InGameLevelData
            {
                Level = 1,
                Exp = 0,
                MaxExp = 10
            };
            
            timerUI.Init(180, null);
            
            goldCountViewerUI.SetCount(0);
            stoneCountViewerUI.SetCount(0);
            
            levelGaugeUI.SetValue(0);
            levelGaugeUI.SetLevel(_levelData.Level);

#region Test

            expTestUIController.Init(this);
            statGrowthInfoUIController.Init(this, inGameData);

#endregion

            IsInitialized = true;
        }

        public void GameReady()
        {
            timerUI.Appear();
        }

        public void GameStart()
        {
            timerUI.Execute().Forget();
            LayoutRebuilder.ForceRebuildLayoutImmediate(transform as RectTransform);
        }

        private void OnDestroy()
        {
            timerUI.StopProcess();
        }

#region IInGameUIHandler

        public void AddGoldCount(int add)
        {
            goldCountViewerUI.AddCount(add);
            statGrowthInfoUIController.RefreshUI();
        }
        
        public void AddStoneCount(int add)
        {
            stoneCountViewerUI.AddCount(add);
        }

        public void AddExpCount(float add)
        {
            _levelData.Exp += add;

            var ratio = 0.0f;
            
            if (_levelData.Exp >= _levelData.MaxExp)
            {
                var curExp = _levelData.Exp - _levelData.MaxExp;
                var level = _levelData.Level + 1;
                
                _levelData.Level = level;
                _levelData.MaxExp = level * 10;
                _levelData.Exp = curExp;
                
                ratio = curExp / _levelData.MaxExp;
                
                levelGaugeUI.SetValue(1.0f, () =>
                {
                    levelGaugeUI.SetValue(0, () =>
                    {
                        levelGaugeUI.SetValue(ratio);
                        levelGaugeUI.SetLevel(_levelData.Level);
                    });
                });
            }
            else
            {
                ratio = _levelData.Exp / _levelData.MaxExp;
                
                levelGaugeUI.SetValue(ratio);
                levelGaugeUI.SetLevel(_levelData.Level);
            }
        }

#endregion
    }
}
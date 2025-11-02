using System;
using Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace InGame
{
    public class StatViewer : GameInitializer
    {
        public event Action<int> OnLevelUpdated;
        
        [SerializeField]
        private TMP_Text levelText;
        [SerializeField] 
        private TMP_Text strText;
        [SerializeField]
        private TMP_Text dexText;
        [SerializeField]
        private TMP_Text lukText;
        [SerializeField]
        private TMP_Text dmgText;
        [SerializeField]
        private TMP_Text spdText;

        [SerializeField]
        private Button levelUpButton;
        [SerializeField]
        private Button levelResetButton;
        
        private PlayerStatTable _row;
        private int _level;
        
        private void Awake()
        {
            levelUpButton.onClick.RemoveAllListeners();
            levelUpButton.onClick.AddListener(LevelUp);
            
            levelResetButton.onClick.RemoveAllListeners();
            levelResetButton.onClick.AddListener(LevelReset);
        }
        
        public void Set(PlayerStatTable row)
        {
            _row = row;
            _level = 1;
            
            var stat = new CalcPlayerStat(_level, _row);
            
            levelText.text = $"{_level}";
            strText.text = $"{stat.Str}";
            dexText.text = $"{stat.Dex}";
            lukText.text = $"{stat.Luk}";
            dmgText.text = $"{stat.Damage:F1}";
            spdText.text = $"{stat.Speed:F1}";
            
            OnLevelUpdated?.Invoke(_level);
        }

        private void LevelUp()
        {
            _level += 1;
            
            var stat = new CalcPlayerStat(_level, _row);
            
            levelText.text = $"{_level}";
            strText.text = $"{stat.Str}";
            dexText.text = $"{stat.Dex}";
            lukText.text = $"{stat.Luk}";
            dmgText.text = $"{stat.Damage:F1}";
            spdText.text = $"{stat.Speed:F1}";
            
            OnLevelUpdated?.Invoke(_level);
        }

        private void LevelReset()
        {
            _level = 1;
            
            var stat = new CalcPlayerStat(_level, _row);
            
            levelText.text = $"{_level}";
            strText.text = $"{stat.Str}";
            dexText.text = $"{stat.Dex}";
            lukText.text = $"{stat.Luk}";
            dmgText.text = $"{stat.Damage:F1}";
            spdText.text = $"{stat.Speed:F1}";
            
            OnLevelUpdated?.Invoke(_level);
        }
    }
}
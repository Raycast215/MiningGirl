using System;
using TMPro;
using UI.Common;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Scene.InGame.UI.Growth.Stat
{
    public class StatGrowthInfoUI : GameMonoInitializer
    {
        private event Action OnButtonTouched;

        [SerializeField] 
        private TMP_Text levelText;
        [SerializeField] 
        private TMP_Text nameText;
        [SerializeField] 
        private TMP_Text valueText;
        [SerializeField] 
        private TMP_Text costText;
        [SerializeField]
        private Transform buttonGroup;
        [SerializeField] 
        private ButtonTouch enhanceButton;

        private bool _isEnhanceable;

        public void Init(string statNameText, Action onButtonTouched)
        {
            enhanceButton.GetButton.onClick.RemoveAllListeners();
            enhanceButton.GetButton.onClick.AddListener(OnTouchButton);
            
            OnButtonTouched = null;
            OnButtonTouched += onButtonTouched;

            if (nameText)
                nameText.text = statNameText;
        }

        public void Set(float statValue, ETextType statType)
        {
            valueText.text = statType switch
            {
                ETextType.Int => $"{statValue:N0}",
                ETextType.Float => $"{statValue:0.##}",
                ETextType.Percent => $"{statValue:0.#}%",
                _ => $"{statValue}"
            };
        }

        public void SetCost(int cost)
        {
            costText.text = $"{cost}";
        }

        public void SetLevel(int level)
        {
            levelText.text = $"Lv.{level}";
        }

        public void SetEnhanceState(bool isEnhanceable)
        {
            _isEnhanceable = isEnhanceable;
            
            if (_isEnhanceable)
            {
                costText.color = Color.white;
                enhanceButton.SetColor(0);
                return;
            }
            
            costText.color = Color.red;
            enhanceButton.SetColor(1);
        }
        
        private void OnTouchButton()
        {
            if (!_isEnhanceable)
                return;
            
            OnButtonTouched?.Invoke();
        }
    }
}
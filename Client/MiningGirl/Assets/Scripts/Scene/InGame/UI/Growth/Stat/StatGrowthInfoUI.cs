using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scene.InGame.UI.Growth.Stat
{
    public class StatGrowthInfoUI : GameInitializer
    {
        private event Action OnButtonTouched;
        
        [SerializeField] 
        private TMP_Text nameText;
        [SerializeField] 
        private TMP_Text valueText;
        [SerializeField]
        private Transform buttonGroup;
        [SerializeField] 
        private Button button;
        
        private void Awake()
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnTouchButton);
        }

        public void Init(string statNameText, Action onButtonTouched)
        {
            OnButtonTouched = null;
            OnButtonTouched += onButtonTouched;
            
            nameText.text = statNameText;
        }

        public void Set(float statValue)
        {
            valueText.text = $"{statValue:F1}";
        }

        private void OnTouchButton()
        {
            OnButtonTouched?.Invoke();
        }
    }
}
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Common
{
    public class GameSpeedViewer : GameInitializer
    {
        [SerializeField]
        private TMP_Text speedText;
        [SerializeField] 
        private Button touchButton;

        private int _timeSpeed;
        
        private void Awake()
        {
            touchButton.onClick.RemoveAllListeners();
            touchButton.onClick.AddListener(OnTouchButton);
        }

        private void Start()
        {
            _timeSpeed = 1;
            SetSpeed(_timeSpeed);
        }

        private void SetSpeed(int speed)
        {
            speedText.text = $"x{speed}";
            Time.timeScale = speed;
        }

        private void OnTouchButton()
        {
            _timeSpeed = Utility.Util.ClampIndex(_timeSpeed + 1, 1, 3);
            SetSpeed(_timeSpeed);
        }
    }
}
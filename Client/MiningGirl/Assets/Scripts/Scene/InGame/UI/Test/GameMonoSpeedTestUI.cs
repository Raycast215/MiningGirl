using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Scene.InGame.UI.Speed.Test
{
    public class GameMonoSpeedTestUI : GameMonoInitializer
    {
        [SerializeField]
        private TMP_Text textUI;
        [SerializeField] 
        private Button button;
        
        private void Awake()
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnTouchButton);
        }

        private void Start()
        {
            textUI.text = $"SPEED<br>x{Time.timeScale}";
        }

        private void OnTouchButton()
        {
            Time.timeScale = Utility.Util.ClampIndex((int)Time.timeScale + 1, 0, 3);
            textUI.text = $"SPEED<br>x{Time.timeScale}";
        }
    }
}
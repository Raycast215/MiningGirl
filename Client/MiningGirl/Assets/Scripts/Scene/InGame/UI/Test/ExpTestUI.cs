using System;
using UnityEngine;
using UnityEngine.UI;

namespace Scene.InGame.UI.Level.Test
{
    public class ExpTestUI : GameMonoInitializer
    {
        [SerializeField] 
        private Button button;

        public void Init(Action callback)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => callback?.Invoke());
        }
    }
}
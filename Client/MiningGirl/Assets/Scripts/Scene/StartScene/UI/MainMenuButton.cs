using System;
using Scene.MainScene;
using UnityEngine;
using UnityEngine.UI;

namespace Scene.StartScene.UI
{
    public class MainMenuButton : GameMonoInitializer
    {
        [SerializeField]
        private Animator animator;
        [SerializeField]
        private Button button;
        [SerializeField]
        private Button homeButton;
        
        public void Initialize(IMainBottomMenuHandler handler, Action callback)
        {
            homeButton.onClick.RemoveAllListeners();
            homeButton.onClick.AddListener(handler.ShowHomeMenu);
            
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(callback.Invoke);
            button.onClick.AddListener(Select);

            Unselect();
            IsInitialized = true;
        }

        public void Unselect()
        {
            button.gameObject.SetActive(true);
            homeButton.gameObject.SetActive(false);
            animator.Play("Idle", 0, 0);
        }

        private void Select()
        {
            button.gameObject.SetActive(false);
            homeButton.gameObject.SetActive(true);
            animator.Play("Select", 0, 0);
        }
    }
}
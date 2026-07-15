using Manager;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Scene.InGame.UI.Menu
{
    public class MenuController : GameMonoInitializer
    {
        [SerializeField] 
        private Button titleButton;

        private void Awake()
        {
            titleButton.onClick.RemoveAllListeners();
            titleButton.onClick.AddListener(OnTouchTitleButton);
        }

        private void OnTouchTitleButton()
        {
            CoverUIManager.Instance.CoverUI.Show(() => SceneManager.LoadScene("StartScene")).Forget();
        }
    }
}
using System;
using Cysharp.Threading.Tasks;
using Manager;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Scene.StartScene
{
    public class StartSceneController : GameInitializer
    {
        [SerializeField] 
        private TMP_Text text;
        [SerializeField]
        private Button touchButton;

        private void Awake()
        {
            touchButton.onClick.RemoveAllListeners();
            touchButton.onClick.AddListener(StartGame);
        }

        private void Start()
        {
            Initialize().Forget();
        }

        private async UniTaskVoid Initialize()
        {
            Application.targetFrameRate = 120;
            
            touchButton.gameObject.SetActive(false);
            
            text.text = "초기화...";
            
            await UniTask.WaitUntil(() => DataTableManager.Instance.IsInitialized);
            await UniTask.WaitForSeconds(1.0f);
            
            text.text = "Touch to Screen";
            
            touchButton.gameObject.SetActive(true);
        }

        private void StartGame()
        {
            SceneManager.LoadScene("InGameScene");
        }
    }
}
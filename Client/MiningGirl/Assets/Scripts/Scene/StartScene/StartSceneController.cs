using System;
using Cysharp.Threading.Tasks;
using DG.Tweening;
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
        private Button touchButton;

        [Header("Slider")]
        [SerializeField]
        private Slider slider;
        [SerializeField] 
        private TMP_Text text;
        
        [Header("StartUI")]
        [SerializeField] 
        private GameObject startObject;
        [SerializeField]
        private TMP_Text startText;
        
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
            
            text.gameObject.SetActive(true);
            slider.gameObject.SetActive(true);
            startObject.SetActive(false);
            touchButton.gameObject.SetActive(false);
            
            text.text = "데이터 초기화...";
            slider.value = 0;
            await UniTask.Yield();
            
            DataTableManager.Instance.PreLoadData().Forget();
            
            await UniTask.WaitUntil(() => DataTableManager.Instance.IsInitialized);

            slider.DOValue(1.0f, 1.0f);
            await UniTask.WaitForSeconds(1.0f);
            
            text.text = "Complete";
            await UniTask.Yield();
            
            text.gameObject.SetActive(false);
            slider.gameObject.SetActive(false);
            startObject.SetActive(true);
            touchButton.gameObject.SetActive(true);
            
            startText.DOFade(0.1f, 1.0f)
                .SetLoops(-1, LoopType.Yoyo);
        }

        private void StartGame()
        {
            SceneManager.LoadScene("InGameScene");
        }
    }
}
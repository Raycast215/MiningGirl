using System;
using System.Collections.Generic;
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

        [Header("BG")]
        [SerializeField] 
        private Image bgImage;
        [SerializeField] 
        private Image logoImage;
        [SerializeField] 
        private List<Sprite> bgImageList;
        [SerializeField] 
        private List<Sprite> logoImageList;

        private int _index;
        
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

            // 메인씬 재진입 시 예외.
            if (CoverUIManager.Instance.CoverUI != null && CoverUIManager.Instance.CoverUI.gameObject.activeInHierarchy)
            {
                CoverUIManager.Instance.CoverUI.Hide().Forget();
                await UniTask.WaitUntil(() => !CoverUIManager.Instance.CoverUI.gameObject.activeInHierarchy);
            }
            
            text.gameObject.SetActive(true);
            slider.gameObject.SetActive(true);
            startObject.SetActive(false);
            touchButton.gameObject.SetActive(false);
            
            text.text = "데이터 초기화...";
            slider.value = 0;
            await UniTask.Yield();

            GameDataManager.Instance.PreLoadData().Forget();
            DataTableManager.Instance.PreLoadData().Forget();
            CoverUIManager.Instance.PreLoadData();
            SoundManager.Instance.PreLoadData();
            
            await UniTask.WaitUntil(() => DataTableManager.Instance.IsInitialized);
            await UniTask.WaitUntil(() => CoverUIManager.Instance.IsInitialized);
            await UniTask.WaitUntil(() => GameDataManager.Instance.IsInitialized);
            await UniTask.WaitUntil(() => SoundManager.Instance.IsInitialized);

            SoundManager.Instance.PlayBgm("Bgm_1", 1, true);
            
            slider.DOValue(1.0f, 1.0f);
            await UniTask.WaitForSeconds(1.0f);
            
            text.text = "초기화 완료...";
            await UniTask.WaitForSeconds(1.0f);
            
            text.gameObject.SetActive(false);
            slider.gameObject.SetActive(false);
            startObject.SetActive(true);
            touchButton.gameObject.SetActive(true);
            
            startText.DOFade(0.1f, 1.0f).SetLoops(-1, LoopType.Yoyo);
        }

        private void StartGame()
        {
            // CoverUIManager.Instance.CoverUI.Show(() => SceneManager.LoadScene("MainScene")).Forget();
            CoverUIManager.Instance.CoverUI.Show(() => SceneManager.LoadScene("InGameScene")).Forget();
        }

#region BGChange

        public void ChangeBg()
        {
            _index = Utility.Util.ClampIndex(_index + 1, 0, bgImageList.Count - 1);
            
            bgImage.sprite = bgImageList[_index];
            logoImage.sprite = logoImageList[_index];
        }

        public void MoveToTestScene()
        {
            SceneManager.LoadScene("InfoScene");
        }

#endregion
    }
}
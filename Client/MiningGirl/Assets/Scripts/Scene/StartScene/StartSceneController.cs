using Cysharp.Threading.Tasks;
using DG.Tweening;
using Manager;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Scene.StartScene
{
    public class StartSceneController : GameMonoInitializer
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
        
        private bool _hasStarted;

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
            
            DataTableManager.Instance.PreLoadData().Forget();
            CoverUIManager.Instance.PreLoadData();
            SoundManager.Instance.PreLoadData();
            
            await UniTask.WaitUntil(() => DataTableManager.Instance.IsInitialized);
            await UniTask.WaitUntil(() => CoverUIManager.Instance.IsInitialized);
            await UniTask.WaitUntil(() => SoundManager.Instance.IsInitialized);

            SoundManager.Instance.PlayBgm("Bgm_1");
            
            slider.DOValue(1.0f, 1.0f);
            await UniTask.WaitForSeconds(1.0f);
            
            text.text = "초기화 완료...";
            await UniTask.WaitForSeconds(1.0f);
            
            text.gameObject.SetActive(false);
            slider.gameObject.SetActive(false);
            startObject.SetActive(true);
            touchButton.gameObject.SetActive(true);
            
            startText.DOFade(0.1f, 1.0f).SetLoops(-1, LoopType.Yoyo);

            // 테스트 편의를 위해 터치하지 않아도 3초 뒤 자동으로 다음 씬으로 넘어갑니다.
            // 그 전에 터치하면 기존처럼 즉시 넘어갑니다.
            AutoStartAfterDelay(1.0f).Forget();
        }

        private async UniTaskVoid AutoStartAfterDelay(float delaySeconds)
        {
            await UniTask.WaitForSeconds(delaySeconds);
            StartGame();
        }
        
        private void StartGame()
        {
            if (_hasStarted)
                return;

            _hasStarted = true;
            
            CoverUIManager.Instance.CoverUI.Show(() => SceneManager.LoadScene("InGameScene")).Forget();
        }
    }
}
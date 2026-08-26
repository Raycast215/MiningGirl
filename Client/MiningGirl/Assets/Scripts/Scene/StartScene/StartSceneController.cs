using Cysharp.Threading.Tasks;
using Manager;
using Scene.StartScene.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scene.StartScene
{
    public class StartSceneController : GameMonoInitializer
    {
        [SerializeField]
        private LoadingProgressUI loadingProgressUI;
        [SerializeField]
        private StartPromptUI startPromptUI;

        private void Awake()
        {
            startPromptUI.Bind(StartGame);
        }

        private void Start()
        {
            Initialize().Forget();
        }

        private async UniTaskVoid Initialize()
        {
            // 고정 프레임 설정.
            Application.targetFrameRate = 120;

            // 플레이 중 화면이 꺼지지 않도록 합니다.
            Screen.sleepTimeout = SleepTimeout.NeverSleep;

            // 메인씬 재진입 시 예외.
            if (CoverUIManager.Instance.CoverUI != null && CoverUIManager.Instance.CoverUI.gameObject.activeInHierarchy)
            {
                CoverUIManager.Instance.CoverUI.Hide().Forget();
                await UniTask.WaitUntil(() => !CoverUIManager.Instance.CoverUI.gameObject.activeInHierarchy);
            }

            loadingProgressUI.Show();
            startPromptUI.Hide();

            loadingProgressUI.SetMessage("데이터 초기화...");
            loadingProgressUI.SetProgress(0f);
            await UniTask.Yield();

            DataTableManager.Instance.PreLoadData().Forget();
            CoverUIManager.Instance.PreLoadData();
            SoundManager.Instance.PreLoadData();

            await UniTask.WaitUntil(() => DataTableManager.Instance.IsInitialized);
            await UniTask.WaitUntil(() => CoverUIManager.Instance.IsInitialized);
            await UniTask.WaitUntil(() => SoundManager.Instance.IsInitialized);

            SoundManager.Instance.PlayBgm("Bgm_1");

            loadingProgressUI.AnimateProgress(1.0f, 1.0f);
            await UniTask.WaitForSeconds(1.0f);

            loadingProgressUI.SetMessage("초기화 완료...");
            await UniTask.WaitForSeconds(1.0f);

            loadingProgressUI.Hide();
            startPromptUI.Show();

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
            if (IsInitialized)
                return;

            IsInitialized = true;

            CoverUIManager.Instance.CoverUI.Show(() => SceneManager.LoadScene("MainGameScene")).Forget();
        }
    }
}

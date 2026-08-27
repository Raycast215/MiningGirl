using Cysharp.Threading.Tasks;
using Manager;
using Scene.StartScene.UI;
using Scene.StartScene.ViewModel;
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
            startPromptUI.Hide();

            ShowStageSelect();
        }

        // 스테이지 선택을 띄웁니다(임시).
        //
        // 전에는 1초 뒤 자동으로 인게임에 들어갔습니다. 스테이지가 다섯 개가 되어
        // 어디로 들어갈지 고를 데가 있어야 하고, 정식 화면은 다음 작업입니다.
        private void ShowStageSelect()
        {
            // 씬에서 아무 캔버스나 집으면 안 됩니다. SRDebugger가 DontDestroyOnLoad에
            // 물리 크기 캔버스를 하나 띄워 두고 있어서, FindObjectOfType으로는
            // 그쪽이 잡혀 화면이 손톱만 하게 그려집니다.
            var canvas = startPromptUI == null ? null : startPromptUI.GetComponentInParent<Canvas>();
            var viewModel = new StageSelectViewModel(DataTableManager.Instance.StageDataTable);

            if (canvas == null || viewModel.Items.Count == 0)
            {
                // 고를 게 없으면 예전처럼 바로 들어갑니다. 스테이지 선택이 없다고
                // 게임을 못 켜면 데이터 문제 하나가 진입 자체를 막습니다.
                Debug.LogWarning("[StartScene] 스테이지 목록을 만들지 못해 바로 들어갑니다.");

                startPromptUI.Show();
                StartGame();

                return;
            }

            viewModel.Selected += HandleStageSelected;

            StageSelectUI.Create(canvas, viewModel);
        }

        private void HandleStageSelected(string stageId)
        {
            StageEntry.Select(stageId);

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

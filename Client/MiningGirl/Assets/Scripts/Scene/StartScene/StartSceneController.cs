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
            // 이어할 판이 있으면 고르는 화면을 건너뜁니다.
            //
            // 안 건너뛰면 유저가 스테이지 3을 골라도 저장(스테이지 1)이 이겨서
            // 다른 스테이지가 열립니다. 고른 것과 열린 것이 다른데 화면에는
            // 아무 말도 안 나오는, 이 프로젝트가 오늘 두 번 겪은 그 모양입니다.
            //
            // 다른 스테이지를 하고 싶으면 들어가서 포기하면 됩니다 - 판이 끝나면
            // 진행 저장이 지워지고 다음 실행에서 선택 화면이 다시 뜹니다.
            if (HasResumableRun())
            {
                Debug.Log("[Save] 이어할 판이 있어 스테이지 선택을 건너뜁니다.");

                StartGame();

                return;
            }

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

        // 저장이 있고 지금 코드로 되돌릴 수 있는지. 판정은 인게임과 같은 자리를 씁니다.
        private bool HasResumableRun()
        {
            if (!Manager.Save.RunSaveStore.Exists())
                return false;

            var save = Manager.Save.RunSaveStore.Read();

            if (save == null)
                return false;

            var verdict = Manager.Save.RunSaveValidator.Validate(save, DataTableManager.Instance);

            if (verdict.IsOk)
                return true;

            // 못 쓰는 저장은 여기서 지우지 않습니다. 인게임이 같은 판정을 다시
            // 하면서 지우고 안내까지 합니다 - 두 곳에서 지우면 한쪽만 고쳤을 때
            // 다른 쪽이 조용히 남습니다.
            Manager.Save.RunSaveValidator.LogFailure(verdict);

            return false;
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

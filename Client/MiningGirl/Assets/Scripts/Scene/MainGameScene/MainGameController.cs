using System.Linq;
using Cysharp.Threading.Tasks;
using Data;
using Manager;
using Scene.MainGameScene.Battle;
using Scene.MainGameScene.Progress;
using Scene.MainGameScene.UI;
using Scene.MainGameScene.ViewModel;
using Scene.MainGameScene.Wave;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Scene.MainGameScene
{
    // 스테이지 하나가 여기서 돕니다.
    //
    // 웨이브 전환은 시간, 레벨업은 처치 수라 둘이 따로 굴러갑니다.
    // 그래서 진행을 한 군데서 묶지 않고 WaveRunner와 LevelSystem을 나란히 돌립니다.
    //
    // UI는 직접 건드리지 않습니다. ViewModel에 상태를 넣으면 View가 알아서 그립니다.
    public class MainGameController : GameMonoInitializer
    {
        [Header("Stage")]
        [SerializeField]
        [Tooltip("1차에서는 스테이지 선택이 없어 고정입니다.")]
        private string stageId = "Stage_01";

        [SerializeField]
        [Tooltip("1차에서는 캐릭터 선택이 없어 고정입니다. 시작 스킬은 이 행에서 읽습니다.")]
        private string characterId = "Character_001";

        [Header("Scene")]
        [SerializeField]
        private Camera battleCamera;

        [SerializeField]
        private Tower tower;

        [SerializeField]
        [Tooltip("캐릭터가 서는 자리이자 발사체가 나가는 지점입니다.")]
        private Transform characterAnchor;

        [SerializeField]
        private Transform monsterLayer;

        [SerializeField]
        private Transform projectileLayer;

        [SerializeField]
        private SpriteRenderer background;

        [SerializeField]
        [Tooltip("화면 위 끝에서 이만큼 더 위에서 스폰합니다. 나타나는 순간이 보이지 않게 합니다.")]
        private float spawnMargin = 1.5f;

        [Header("Layout")]
        [SerializeField]
        [Tooltip("타워 윗변이 하단 UI 띠 위로 솟는 높이(유닛). 이 값만큼은 어느 기기에서든 보입니다.")]
        private float towerExposedHeight = 1.54f;

        [SerializeField]
        [Tooltip("타워 윗변에서 캐릭터 기준점까지의 거리. 각목이 다리를 살짝 가리는 양입니다.")]
        private float characterFootOffset = 0.21f;

        [Header("UI")]
        [SerializeField]
        private InGameHudUI hud;

        [SerializeField]
        private LevelUpChoiceUI levelUpChoiceUI;

        [SerializeField]
        private StageResultUI resultUI;

        private StageDataTableRow _stage;
        private CharacterDataTableRow _character;

        private BattleBounds _bounds;
        private MonsterField _field;
        private ProjectileLauncher _launcher;
        private SkillInventory _inventory;
        private SkillRunner _skillRunner;
        private LevelSystem _levelSystem;
        private LevelUpChoiceBuilder _choiceBuilder;
        private WaveRunner _waveRunner;

        private InGameHudViewModel _hudViewModel;
        private LevelUpChoiceViewModel _choiceViewModel;
        private StageResultViewModel _resultViewModel;

        private float _elapsed;
        private bool _isRunning;
        private bool _isFinished;

        // 한 프레임에 두 레벨이 오를 수 있어 밀린 만큼 세어 둡니다.
        private int _pendingLevelUps;

        private void Start()
        {
            InitAsync().Forget();
        }

        private void OnDestroy()
        {
            // 3택 도중에 씬을 나가면 timeScale이 0인 채로 남습니다.
            Time.timeScale = 1f;
        }

        private void Update()
        {
            if (!_isRunning)
                return;

            // 3택 중에는 timeScale이 0이라 deltaTime도 0입니다.
            // 웨이브 타이머와 스폰, 발사체가 한꺼번에 멈춥니다.
            var deltaTime = Time.deltaTime;

            _elapsed += deltaTime;

            _waveRunner.Tick(deltaTime);
            _field.Tick(deltaTime);
            _skillRunner.Tick(deltaTime);
            _launcher.Tick(deltaTime);

            _hudViewModel.Tick(_elapsed);

            CheckStageClear();
        }

#region 초기화

        private async UniTaskVoid InitAsync()
        {
            await EnsureDataLoaded();

            if (!ResolveStageData())
                return;

            ApplyBattleLayout();

            _bounds = new BattleBounds(battleCamera != null ? battleCamera : Camera.main, tower.TopY, spawnMargin);

            tower.Setup(_character.TowerMaxHealth);
            tower.OnDestroyed += HandleTowerDestroyed;

            _field = new MonsterField(monsterLayer, tower, _bounds, _stage.MonsterStatMultiplier);
            _field.OnMonsterKilled += HandleMonsterKilled;

            _launcher = new ProjectileLauncher(projectileLayer, _field, _bounds);

            var constants = DataTableManager.Instance.GameConstantDataTable;

            _inventory = new SkillInventory(constants.GetInt(EGameConstantType.SkillSlotMax, 5));
            _skillRunner = new SkillRunner(_inventory, _field, _launcher, characterAnchor);

            _levelSystem = new LevelSystem(
                _stage.TotalMonsterCount,
                _stage.WaveCount,
                constants.GetValue(EGameConstantType.LevelUpCurveRate, 1.5f));
            _levelSystem.OnLevelUp += HandleLevelUp;

            _choiceBuilder = new LevelUpChoiceBuilder(
                DataTableManager.Instance.SkillDataTable,
                DataTableManager.Instance.SkillUpgradeDataTable,
                _inventory);

            _waveRunner = new WaveRunner(
                DataTableManager.Instance.WaveDataTable,
                DataTableManager.Instance.MonsterDataTable,
                _field,
                _stage.Id,
                constants.GetValue(EGameConstantType.WaveStartDelay, 2f),
                constants.GetValue(EGameConstantType.WaveClearDelay, 1.5f));

            await LoadSceneAssets();

            GrantStartSkill();

            BindViewModels();

            PlayStageBgm();

            IsInitialized = true;
            _isRunning = true;

            HideCover();
        }

        // 타워와 캐릭터 높이를 하단 UI 띠에서 역산합니다.
        //
        // 상수로 박으면 SafeArea가 다른 기기에서 타워가 UI 뒤로 묻혀 각목만 삐죽한 띠로 보입니다.
        // 뜻은 하나입니다 — "타워 윗변이 UI 위로 towerExposedHeight만큼 솟는다".
        private void ApplyBattleLayout()
        {
            if (hud == null || tower == null || !hud.TryGetBottomBandWorldTopY(out var uiTopY))
                return;

            var towerTopY = uiTopY + towerExposedHeight;

            var towerPosition = tower.transform.position;
            towerPosition.y = towerTopY - tower.HalfHeight;
            tower.transform.position = towerPosition;

            if (characterAnchor == null)
                return;

            var characterPosition = characterAnchor.position;
            characterPosition.y = towerTopY - characterFootOffset;
            characterAnchor.position = characterPosition;
        }

        // Model이 다 만들어진 뒤에 붙입니다. View는 여기서 처음 값을 받습니다.
        private void BindViewModels()
        {
            _hudViewModel = new InGameHudViewModel(
                _waveRunner, _levelSystem, tower, _inventory, _skillRunner, hud.SlotViewCount);

            _choiceViewModel = new LevelUpChoiceViewModel(_inventory);
            _choiceViewModel.Selected += HandleChoiceSelected;

            var constants = DataTableManager.Instance.GameConstantDataTable;

            _resultViewModel = new StageResultViewModel(
                constants.GetValue(EGameConstantType.Star3HealthRate, 1f),
                constants.GetValue(EGameConstantType.Star2HealthRate, 0.5f));
            _resultViewModel.RetryRequested += Retry;

            hud.Bind(_hudViewModel);
            levelUpChoiceUI.Bind(_choiceViewModel);
            resultUI.Bind(_resultViewModel);

            _hudViewModel.Tick(_elapsed);
        }

        // StartScene을 거치지 않고 이 씬만 바로 재생해도 굴러가게 합니다.
        // 인게임만 반복해서 확인할 때 매번 로딩 씬을 지나가지 않아도 됩니다.
        private async UniTask EnsureDataLoaded()
        {
            if (DataTableManager.Instance.IsInitialized)
                return;

            DataTableManager.Instance.PreLoadData().Forget();

            await UniTask.WaitUntil(() => DataTableManager.Instance.IsInitialized);
        }

        private bool ResolveStageData()
        {
            _stage = DataTableManager.Instance.StageDataTable?.GetRow(stageId);
            _character = DataTableManager.Instance.CharacterDataTable?.GetRow(characterId);

            if (_stage == null)
            {
                Debug.LogError($"[MainGame] 스테이지를 찾지 못했습니다: {stageId}");

                return false;
            }

            if (_character != null)
                return true;

            Debug.LogError($"[MainGame] 캐릭터를 찾지 못했습니다: {characterId}");

            return false;
        }

        private async UniTask LoadSceneAssets()
        {
            // 웨이브 도중에 처음 등장하는 종류를 그때 불러오면 그 프레임이 튑니다.
            await _field.PreloadAsync(_waveRunner.CollectMonsterIds());

            // 3택으로 아직 없는 스킬을 고를 수 있으니 이펙트는 전부 미리 불러 둡니다.
            var effectIds = DataTableManager.Instance.SkillDataTable?.Rows?
                .Where(row => row != null && !string.IsNullOrEmpty(row.EffectAssetId))
                .Select(row => row.EffectAssetId)
                .Distinct() ?? Enumerable.Empty<string>();

            await _launcher.PreloadAsync(effectIds);

            await LoadCharacter();
            await LoadBackground();
        }

        private async UniTask LoadCharacter()
        {
            if (characterAnchor == null || string.IsNullOrEmpty(_character.AssetId))
                return;

            var prefab = await AddressableManager.Instance.LoadAsset<GameObject>(_character.AssetId);

            if (prefab == null)
            {
                Debug.LogError($"[MainGame] 캐릭터 프리팹을 찾지 못했습니다: {_character.AssetId}");

                return;
            }

            var instance = Instantiate(prefab, characterAnchor);

            instance.transform.localPosition = Vector3.zero;
        }

        private async UniTask LoadBackground()
        {
            if (background == null || string.IsNullOrEmpty(_stage.BgAssetId))
                return;

            var sprite = await AddressableManager.Instance.LoadAsset<Sprite>(_stage.BgAssetId);

            if (sprite == null)
            {
                Debug.LogWarning($"[MainGame] 배경을 찾지 못했습니다: {_stage.BgAssetId}");

                return;
            }

            background.sprite = sprite;
        }

        // 시작 스킬은 상수로 두지 않습니다. 캐릭터 선택이 붙으면 그대로 쓰입니다.
        private void GrantStartSkill()
        {
            var startSkill = DataTableManager.Instance.SkillDataTable?.GetRow(_character.StartSkillId);

            if (startSkill == null)
            {
                Debug.LogError($"[MainGame] 시작 스킬을 찾지 못했습니다: {_character.StartSkillId}");

                return;
            }

            var state = _inventory.Add(startSkill);

            _skillRunner.ResetCooldown(state);
        }

        private void PlayStageBgm()
        {
            if (string.IsNullOrEmpty(_stage.BgmId) || !SoundManager.Instance.IsInitialized)
                return;

            SoundManager.Instance.PlayBgm(_stage.BgmId);
        }

        private static void HideCover()
        {
            var cover = CoverUIManager.Instance.CoverUI;

            if (cover != null && cover.gameObject.activeInHierarchy)
                cover.Hide().Forget();
        }

#endregion

#region 진행

        private void HandleMonsterKilled(MonsterUnit unit)
        {
            // 몬스터 1마리 처치 = 경험치 1입니다.
            _levelSystem.AddKill();
        }

        private void HandleLevelUp(int level)
        {
            _pendingLevelUps++;

            if (!_choiceViewModel.IsVisible.Value)
                ShowNextLevelUp();
        }

        private void ShowNextLevelUp()
        {
            if (_isFinished)
                return;

            while (_pendingLevelUps > 0)
            {
                var choices = _choiceBuilder.Draw(
                    DataTableManager.Instance.GameConstantDataTable.GetInt(EGameConstantType.LevelUpChoiceCount, 3));

                if (choices.Count > 0)
                {
                    SetPaused(true);
                    _choiceViewModel.Show(_levelSystem.Level, choices);

                    return;
                }

                // 고를 게 하나도 없는 경우입니다. 지금 구성에서는 나오지 않지만
                // 시트의 Weight를 전부 0으로 두면 여기로 옵니다.
                Debug.LogWarning("[MainGame] 3택 후보가 없어 레벨업을 건너뜁니다.");
                _pendingLevelUps--;
            }

            SetPaused(false);
        }

        private void HandleChoiceSelected(LevelUpChoice choice)
        {
            ApplyChoice(choice);

            _pendingLevelUps--;

            ShowNextLevelUp();
        }

        private void ApplyChoice(LevelUpChoice choice)
        {
            switch (choice.Type)
            {
                case ELevelUpChoiceType.AcquireSkill:
                {
                    var state = _inventory.Add(choice.Skill);

                    // 얻자마자 한 발 나가야 고른 게 눈에 보입니다.
                    _skillRunner.ResetCooldown(state);

                    return;
                }

                case ELevelUpChoiceType.LevelUpSkill:
                {
                    _inventory.Find(choice.Skill.Id)?.LevelUp();

                    return;
                }

                case ELevelUpChoiceType.UpgradeSkill:
                {
                    _inventory.Find(choice.Skill.Id)?.ApplyUpgrade(choice.Upgrade);

                    return;
                }
            }
        }

        private void SetPaused(bool paused)
        {
            Time.timeScale = paused ? 0f : 1f;

            _hudViewModel.IsPaused.Value = paused;
        }

#endregion

#region 승패

        private void CheckStageClear()
        {
            // 20웨이브가 끝나고 필드가 비면 클리어입니다.
            // 타워에 닿은 몬스터는 사라지지 않으므로, 이 조건은 곧 총량을 다 잡았다는 뜻입니다.
            if (!_waveRunner.IsFinished || _field.AliveCount > 0)
                return;

            Finish(true);
        }

        private void HandleTowerDestroyed()
        {
            Finish(false);
        }

        private void Finish(bool cleared)
        {
            if (_isFinished)
                return;

            _isFinished = true;
            _isRunning = false;
            _pendingLevelUps = 0;

            Time.timeScale = 1f;

            _choiceViewModel.Hide();
            _launcher.Clear();

            _resultViewModel.Show(
                cleared,
                _waveRunner.CurrentWaveNo,
                _waveRunner.TotalWaveCount,
                _elapsed,
                tower.CurrentHealth,
                tower.MaxHealth);
        }

        private static void Retry()
        {
            Time.timeScale = 1f;

            SceneManager.LoadScene("MainGameScene");
        }

#endregion
    }
}

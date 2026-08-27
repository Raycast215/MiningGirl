using System;
using System.Collections.Generic;
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
    public partial class MainGameController : GameMonoInitializer
    {
#if UNITY_EDITOR
        // 에디터의 스테이지 지정 진입이 고른 값을 넣어 두는 자리입니다.
        //
        // SessionState는 에디터 메모리에만 남습니다. 파일로 새지 않으니 저장 시스템도,
        // 씬 파일도 건드리지 않고, 에디터를 닫으면 알아서 사라집니다.
        public const string DebugStageIdKey = "MiningGirl.Debug.StageId";
#endif

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
        private Transform floatingDamageLayer;

        [SerializeField]
        [Tooltip("맞은 자리에 떠오르는 피해 숫자. 비워 두면 숫자를 띄우지 않습니다.")]
        private FloatingDamageText floatingDamagePrefab;

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
        [Tooltip("타워 윗변에서 캐릭터 발까지의 거리. 각목이 다리를 살짝 가리는 양입니다.")]
        private float characterFootOffset = 0.21f;

        [SerializeField]
        [Tooltip("캐릭터 스프라이트에서 피벗과 발 사이 거리(스케일 1 기준). 그림의 불투명 영역을 재서 나온 값입니다.")]
        private float characterFootFromPivot = 0.42f;

        [Header("UI")]
        [SerializeField]
        private InGameHudUI hud;

        [SerializeField]
        private LevelUpChoiceUI levelUpChoiceUI;

        [SerializeField]
        private StageResultUI resultUI;

        [SerializeField]
        private PauseMenuUI pauseMenuUI;

        [SerializeField]
        [Tooltip("배속 버튼을 누를 때마다 이 순서로 돕니다. 마지막 다음은 처음으로 돌아옵니다.")]
        private float[] speedSteps = { 1f, 2f };

        private StageDataTableRow _stage;
        private CharacterDataTableRow _character;

        private BattleBounds _bounds;
        private MonsterField _field;
        private ProjectileLauncher _launcher;
        private SkillInventory _inventory;
        private SkillRunner _skillRunner;
        private LevelSystem _levelSystem;

        private EffectSpawner _effects;

        // 이 런에서 남은 3택 다시 뽑기 횟수.
        //
        // 인벤토리와 같은 수명입니다 - 스테이지를 나가면 사라지고 다음 스테이지로
        // 이월되지 않습니다. 이월하면 초반 스테이지를 아껴 후반에 몰아 쓰는 게
        // 최적이 되어 스테이지별로 난이도를 설계할 수 없게 됩니다.
        private int _rerollsLeft;

        // 다시 뽑기 직전에 보여 주던 카드들. 같은 조합이 다시 나왔는지 볼 때 씁니다.
        private readonly List<string> _shownChoiceKeys = new List<string>();
        private LevelUpChoiceBuilder _choiceBuilder;
        private WaveRunner _waveRunner;

        private InGameHudViewModel _hudViewModel;
        private LevelUpChoiceViewModel _choiceViewModel;
        private StageResultViewModel _resultViewModel;
        private PauseMenuViewModel _pauseMenuViewModel;

        private float _elapsed;
        private bool _isRunning;
        private bool _isFinished;

        // 하단 UI 띠에서 역산한 타워 윗변. 캐릭터 배치도 여기서 잽니다.
        private float _towerTopY;

        // 첫 레벨업까지 걸린 시간. 진입 구간이 뚫렸는지 보는 지표라 밸런스를 잡을 때 씁니다.
        // 여기가 늦어지면 못 잡아서 레벨이 안 오르고, 안 올라서 더 못 잡는 되먹임이 걸립니다.
        public float FirstLevelUpElapsed { get; private set; } = -1f;

        // 한 프레임에 두 레벨이 오를 수 있어 밀린 만큼 세어 둡니다.
        private int _pendingLevelUps;

        // 3택이 떠서 멈춘 상태와 메뉴로 멈춘 상태. 둘은 따로 켜지고 따로 꺼집니다.
        private bool _isChoicePaused;
        private bool _isMenuPaused;

        // 배속. 정지가 풀렸을 때 돌아갈 속도입니다.
        private float _speedMultiplier = 1f;

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
            _effects.Tick(deltaTime);

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

            // 프리팹을 안 꽂아 두면 숫자만 안 뜨고 나머지는 그대로 돕니다.
            var floatingDamage = floatingDamagePrefab != null
                ? new FloatingDamageSpawner(floatingDamagePrefab, floatingDamageLayer != null ? floatingDamageLayer : projectileLayer)
                : null;

            _effects = new EffectSpawner(projectileLayer);

            _field = new MonsterField(monsterLayer, tower, _bounds, _stage.MonsterStatMultiplier, floatingDamage, _effects);
            _field.OnMonsterKilled += HandleMonsterKilled;

            _launcher = new ProjectileLauncher(projectileLayer, _field, _bounds);

            var constants = DataTableManager.Instance.GameConstantDataTable;

            _inventory = new SkillInventory(constants.GetInt(EGameConstantType.SkillSlotMax, 5));

            // 인벤토리와 같은 수명입니다. 스테이지를 나가면 사라지고 이월되지 않습니다.
            _rerollsLeft = Mathf.Max(0, constants.GetInt(EGameConstantType.LevelUpRerollCount, 10));
            _skillRunner = new SkillRunner(_inventory, _field, _launcher, characterAnchor);

            _levelSystem = new LevelSystem(
                _stage.TotalMonsterCount,
                _stage.WaveCount,
                constants.GetValue(EGameConstantType.LevelUpCurveRate, 1.5f));
            _levelSystem.OnLevelUp += HandleLevelUp;

            _choiceBuilder = new LevelUpChoiceBuilder(
                DataTableManager.Instance.SkillDataTable,
                DataTableManager.Instance.SkillUpgradeDataTable,
                DataTableManager.Instance.SkillMasteryDataTable,
                _inventory);

            _waveRunner = new WaveRunner(
                DataTableManager.Instance.WaveDataTable,
                DataTableManager.Instance.MonsterDataTable,
                _field,
                _stage.Id,
                constants.GetValue(EGameConstantType.WaveStartDelay, 2f),
                constants.GetValue(EGameConstantType.WaveClearDelay, 1.5f));

            // 프로세스가 예고 없이 죽었을 때의 바닥입니다. 일시정지 저장이
            // 성공하면 그쪽이 이깁니다.
            _waveRunner.OnWaveStarted += HandleWaveStartedForSave;

            await LoadSceneAssets();

            // 저장이 있으면 되돌리고, 없거나 되돌리지 못하면 새 판으로 시작합니다.
            var restore = _pendingRestore;
            var restored = TryRestore();

            if (!restored)
            {
                if (restore != null)
                {
                    // 반쯤 되돌린 판은 어디서 터질지 그때그때 다릅니다.
                    // 그 스테이지 처음부터 시작하고 저장은 지웁니다.
                    Debug.LogWarning("[Save] 이어하기를 불러오지 못해 스테이지를 처음부터 시작합니다");

                    ClearRunSave();
                }

                GrantStartSkill();
            }

            BindViewModels();

            if (restored)
                RestoreOpenChoice(restore);

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

            _towerTopY = uiTopY + towerExposedHeight;

            var towerPosition = tower.transform.position;
            towerPosition.y = _towerTopY - tower.HalfHeight;
            tower.transform.position = towerPosition;
        }

        // 캐릭터는 그림을 붙인 뒤에 놓습니다.
        //
        // 스프라이트 피벗이 가운데라 발이 그보다 0.42유닛(스케일 1 기준) 아래에 있습니다.
        // 피벗을 기준선에 맞추면 캐릭터가 그만큼 파묻힙니다. 스케일이 바뀌면 그 거리도 같이
        // 늘어나므로 실제 스케일을 곱합니다.
        private void PlaceCharacter(GameObject character)
        {
            if (characterAnchor == null)
                return;

            // 스케일은 루트가 아니라 그림이 붙은 렌더러에서 읽습니다.
            // 프리팹 구조상 크기 조정이 자식에 걸려 있어, 루트를 보면 항상 1이 나옵니다.
            var renderer = character != null ? character.GetComponentInChildren<SpriteRenderer>(true) : null;
            var scale = renderer != null ? Mathf.Abs(renderer.transform.lossyScale.y) : 1f;

            var position = characterAnchor.position;
            position.y = _towerTopY - characterFootOffset + characterFootFromPivot * scale;
            characterAnchor.position = position;
        }

        // Model이 다 만들어진 뒤에 붙입니다. View는 여기서 처음 값을 받습니다.
        private void BindViewModels()
        {
            _hudViewModel = new InGameHudViewModel(
                _waveRunner, _levelSystem, tower, _inventory, _skillRunner, hud.SlotViewCount);

            _choiceViewModel = new LevelUpChoiceViewModel(_inventory);
            _choiceViewModel.Selected += HandleChoiceSelected;
            _choiceViewModel.RerollRequested += HandleRerollRequested;

            var constants = DataTableManager.Instance.GameConstantDataTable;

            _resultViewModel = new StageResultViewModel(
                constants.GetValue(EGameConstantType.Star3HealthRate, 1f),
                constants.GetValue(EGameConstantType.Star2HealthRate, 0.5f));
            _resultViewModel.RetryRequested += Retry;

            _pauseMenuViewModel = new PauseMenuViewModel(speedSteps);
            _pauseMenuViewModel.PauseRequested += HandleMenuPause;
            _pauseMenuViewModel.SurrenderRequested += HandleSurrender;
            _pauseMenuViewModel.SpeedChanged += HandleSpeedChanged;

            hud.Bind(_hudViewModel);
            levelUpChoiceUI.Bind(_choiceViewModel);
            resultUI.Bind(_resultViewModel);

            if (pauseMenuUI != null)
                pauseMenuUI.Bind(_pauseMenuViewModel);

            _hudViewModel.StageText.Value = InGameHudViewModel.FormatStage(_stage.Id);
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

        // 실제로 들어갈 스테이지 Id.
        //
        // 우선순위와 진행 저장 처리는 MainGameController.Save.cs에 있습니다.
        // 씬 파일의 stageId는 어느 경로로도 손대지 않으므로, 지정 진입이나
        // 스테이지 선택을 몇 번 하든 저장소에는 변화가 남지 않습니다.
        private string ResolveStageId()
        {
            return ResolveStageIdWithSave();
        }

        private bool ResolveStageData()
        {
            var id = ResolveStageId();

            _stage = DataTableManager.Instance.StageDataTable?.GetRow(id);
            _character = DataTableManager.Instance.CharacterDataTable?.GetRow(characterId);

            if (_stage == null)
            {
                Debug.LogError($"[MainGame] 스테이지를 찾지 못했습니다: {id}");

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

            // 3택으로 아직 없는 스킬을 고를 수 있으니 이펙트를 미리 불러 둡니다.
            //
            // 다만 이 판에 나올 수 있는 것만 봅니다. Weight가 0인 스킬은 3택 후보에서
            // 빠지므로(LevelUpChoiceBuilder) 얻을 방법이 없고, 아직 이펙트가 없는
            // 준비 중인 스킬이 여기 들어가면 스테이지 진입마다 로드 실패가 쌓입니다.
            // 에러가 깔리면 진짜 에러가 묻힙니다.
            var effectIds = DataTableManager.Instance.SkillDataTable?.Rows?
                .Where(row => row != null && !string.IsNullOrEmpty(row.EffectAssetId))
                .Where(row => row.Weight > 0 || row.Id == _character.StartSkillId)
                .Select(row => row.EffectAssetId)
                .Distinct() ?? Enumerable.Empty<string>();

            await _launcher.PreloadAsync(effectIds);

            // 강화스킬 이펙트는 폭발만 미리 불러 둡니다.
            //
            // 연쇄와 부채꼴은 발사체를 새로 내보내므로 그 스킬의 이펙트를 그대로
            // 씁니다. 시트의 EffectAssetId도 그 발사체 프리팹을 가리키고 있어서,
            // 걸러내지 않으면 OneShotEffect가 없다는 에러가 스테이지마다 쌓입니다.
            var masteryEffectIds = DataTableManager.Instance.SkillMasteryDataTable?.Rows?
                .Where(row => row != null && row.MasteryType == EMasteryType.Explosion)
                .Where(row => !string.IsNullOrEmpty(row.EffectAssetId))
                .Select(row => row.EffectAssetId)
                .Distinct() ?? Enumerable.Empty<string>();

            await _effects.PreloadAsync(masteryEffectIds);

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

            PlaceCharacter(instance);
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
            if (FirstLevelUpElapsed < 0f)
                FirstLevelUpElapsed = _elapsed;

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
                var choices = _choiceBuilder.Draw(ChoiceCount);

                if (choices.Count > 0)
                {
                    RememberShown(choices);

                    SetPaused(true);
                    _choiceViewModel.Show(_levelSystem.Level, choices);
                    ApplyRerollState();

                    return;
                }

                // 고를 게 하나도 없는 경우입니다. 지금 구성에서는 나오지 않지만
                // 시트의 Weight를 전부 0으로 두면 여기로 옵니다.
                Debug.LogWarning("[MainGame] 3택 후보가 없어 레벨업을 건너뜁니다.");
                _pendingLevelUps--;
            }

            SetPaused(false);
        }

        private int ChoiceCount =>
            DataTableManager.Instance.GameConstantDataTable.GetInt(EGameConstantType.LevelUpChoiceCount, 3);

        // 3택 카드를 다시 뽑습니다.
        //
        // 카드 세 장을 통째로 갈아 끼웁니다. 한 장만 다시 뽑게 하면 10회가 실질
        // 3회가 되고, 3택에서 실제로 하는 판단도 "이 중에 쓸 게 없다"이지
        // "한 장만 아쉽다"가 아닙니다.
        private void HandleRerollRequested()
        {
            if (_isFinished || _rerollsLeft <= 0)
                return;

            var choices = _choiceBuilder.Draw(ChoiceCount);

            if (choices.Count == 0)
                return;

            // 직전 후보를 빼지 않으므로 같은 조합이 다시 나올 수 있습니다.
            // 그대로 보여주면 버튼이 안 먹은 것으로 읽히므로 한 번만 다시 뽑습니다.
            // 두 번째도 같으면 그대로 보여줍니다 - 후보가 좁으면 다른 결과가 없습니다.
            if (IsSameAsShown(choices))
                choices = _choiceBuilder.Draw(ChoiceCount);

            _rerollsLeft--;

            RememberShown(choices);

            _choiceViewModel.Replace(choices);
            ApplyRerollState();
        }

        // 남은 횟수와 버튼을 누를 수 있는지를 ViewModel에 넣어 줍니다.
        private void ApplyRerollState()
        {
            var reason = ERerollBlockReason.None;

            if (_rerollsLeft <= 0)
                reason = ERerollBlockReason.Exhausted;
            else if (_choiceBuilder.LastCandidateCount <= ChoiceCount)
                reason = ERerollBlockReason.NotEnoughPool;

            _choiceViewModel.SetRerollState(_rerollsLeft, reason);
        }

        private void RememberShown(IReadOnlyList<LevelUpChoice> choices)
        {
            _shownChoiceKeys.Clear();
            _openChoiceKeys.Clear();

            for (var i = 0; i < choices.Count; i++)
            {
                var key = ChoiceKey(choices[i]);

                _shownChoiceKeys.Add(key);

                // 비교용은 정렬하므로 표시 순서는 따로 남깁니다. 저장에서
                // 3택을 되살릴 때 카드가 놓인 자리가 달라지면 안 됩니다.
                _openChoiceKeys.Add(key);
            }

            _shownChoiceKeys.Sort(StringComparer.Ordinal);
        }

        // 순서가 달라도 같은 세 장이면 같은 조합으로 봅니다.
        private bool IsSameAsShown(IReadOnlyList<LevelUpChoice> choices)
        {
            if (_shownChoiceKeys.Count != choices.Count)
                return false;

            var keys = new List<string>(choices.Count);

            for (var i = 0; i < choices.Count; i++)
                keys.Add(ChoiceKey(choices[i]));

            keys.Sort(StringComparer.Ordinal);

            for (var i = 0; i < keys.Count; i++)
            {
                if (!string.Equals(keys[i], _shownChoiceKeys[i], StringComparison.Ordinal))
                    return false;
            }

            return true;
        }

        // 형식은 LevelUpChoiceBuilder에 한 벌만 둡니다. 중복 방지와 저장이
        // 각자 만들면 둘이 어긋날 때 복원한 카드가 "본 적 없는 것"이 됩니다.
        private static string ChoiceKey(LevelUpChoice choice)
        {
            return LevelUpChoiceBuilder.ToKey(choice);
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

                case ELevelUpChoiceType.UpgradeSkill:
                {
                    _inventory.Find(choice.Skill.Id)?.ApplyUpgrade(choice.Upgrade);

                    return;
                }

                case ELevelUpChoiceType.Mastery:
                {
                    // 강화스킬은 그 스킬에 붙고 런당 하나뿐입니다.
                    _inventory.Find(choice.Skill.Id)?.SetMastery(choice.Mastery);
                    _inventory.MarkMasteryTaken();

                    return;
                }
            }
        }

        private void HandleMenuPause(bool paused)
        {
            _isMenuPaused = paused;

            ApplyTimeScale();
        }

        private void HandleSpeedChanged(float speed)
        {
            _speedMultiplier = Mathf.Max(0.1f, speed);

            ApplyTimeScale();
        }

        // 3택이 떠 있는 동안의 정지입니다. 메뉴 정지와 겹칠 수 있어 따로 셉니다.
        private void SetPaused(bool paused)
        {
            _isChoicePaused = paused;

            ApplyTimeScale();
        }

        // 정지와 배속을 한 곳에서 계산합니다.
        //
        // 3택 정지와 메뉴 정지가 각자 timeScale을 건드리면, 메뉴를 닫는 순간
        // 3택이 떠 있는데도 시간이 흐르기 시작합니다. 둘 중 하나라도 켜져 있으면 멈춥니다.
        private void ApplyTimeScale()
        {
            var paused = _isChoicePaused || _isMenuPaused;

            Time.timeScale = paused ? 0f : _speedMultiplier;

            if (_hudViewModel != null)
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

            // 클리어든 타워 파괴든 포기든 같은 자리입니다. 남겨 두면 다음 실행에서
            // 이미 끝난 판이 되살아납니다.
            ClearRunSave();

            Time.timeScale = 1f;

            _choiceViewModel.Hide();
            _launcher.Clear();

            _pauseMenuViewModel?.Lock();

            _resultViewModel.Show(
                cleared,
                _stage.Id,
                _waveRunner.CurrentWaveNo,
                _waveRunner.TotalWaveCount,
                _elapsed,
                tower.CurrentHealth,
                tower.MaxHealth);
        }

        // 메뉴에서 포기했을 때. 타워가 부서진 것과 같은 실패 처리로 갑니다.
        //
        // 결과 화면으로 가는 것·별 0개·보상 미지급이 기획 확정 대기 중입니다.
        // 지금은 기존 실패 경로를 그대로 쓰므로, 확정이 오면 이 메서드만 고치면 됩니다.
        private void HandleSurrender()
        {
            Finish(false);
        }

        private static void Retry()
        {
            Time.timeScale = 1f;

            SceneManager.LoadScene("MainGameScene");
        }

#endregion
    }
}

#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Data;
using Newtonsoft.Json;
using Scene.MainGameScene;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 스테이지를 골라 인게임에 바로 들어갑니다.
///
/// 스테이지 2~10 밸런스를 재려면 그 스테이지에서 시작할 수 있어야 하는데,
/// 1번부터 차례로 뚫으면 측정 한 번에 스테이지 수만큼 5분이 곱해집니다.
///
/// 고른 Id는 <see cref="SessionState"/>에만 넣습니다. 그래서
///  - 씬 파일의 stageId를 고치지 않습니다. 지정 진입을 몇 번 하든 저장소에 변화가 남지 않습니다
///  - 저장 시스템을 건드리지 않습니다. 해금 상태를 흉내 내지 않고 그 스테이지에서 시작만 합니다
///  - 에디터를 닫으면 사라집니다. 다음에 켰을 때 이전 지정이 남아 있어 헛 측정할 일이 없습니다
///
/// 읽는 쪽은 <c>MainGameController.ResolveStageId()</c> 하나뿐이고 <c>#if UNITY_EDITOR</c> 안에 있어
/// 빌드에는 이 경로가 들어가지 않습니다.
/// </summary>
public class StageDebugEntryWindow : EditorWindow
{
    private const string JsonFolder = "Assets/Data/SheetsJson";
    private const string ScenePath = "Assets/Scenes/MainGameScene.unity";

    // 플레이 중에 다른 스테이지로 갈아탈 때 씁니다.
    // 플레이 모드를 끄면 도메인이 다시 올라오면서 이 창의 필드가 날아가므로 SessionState에 둡니다.
    private const string PendingEnterKey = "MiningGirl.Debug.PendingStageId";
    private const string PendingAutoPlayKey = "MiningGirl.Debug.PendingAutoPlaySpeed";

    private const string AutoPlayPrefKey = "MiningGirl.Debug.AutoPlayOnEnter";
    private const string AutoPlaySpeedPrefKey = "MiningGirl.Debug.AutoPlaySpeed";

    private readonly List<StageDataTableRow> _stages = new();

    // 스테이지별 웨이브 행 수. 행이 없는 스테이지로 들어가면 몬스터가 하나도 안 나옵니다.
    private readonly Dictionary<string, int> _waveRowCounts = new();

    private string _loadError;
    private Vector2 _scroll;

    private bool _autoPlayOnEnter;
    private float _autoPlaySpeed = 10f;

    [MenuItem("Tools/MainGame/Stage Debug Entry")]
    private static void Open()
    {
        var window = GetWindow<StageDebugEntryWindow>("Stage Debug Entry");
        window.minSize = new Vector2(460f, 320f);
        window.Show();
    }

    private void OnEnable()
    {
        _autoPlayOnEnter = EditorPrefs.GetBool(AutoPlayPrefKey, true);
        _autoPlaySpeed = EditorPrefs.GetFloat(AutoPlaySpeedPrefKey, 10f);

        Reload();
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            $"{JsonFolder} 의 StageDataTable.json을 직접 읽습니다.\n" +
            "고른 스테이지는 에디터 메모리에만 남습니다. 씬 파일과 저장 데이터는 건드리지 않습니다.",
            MessageType.Info);

        DrawCurrentOverride();

        EditorGUILayout.Space();

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUI.BeginChangeCheck();

            _autoPlayOnEnter = EditorGUILayout.ToggleLeft("진입 후 자동 플레이", _autoPlayOnEnter, GUILayout.Width(140f));

            using (new EditorGUI.DisabledScope(!_autoPlayOnEnter))
                _autoPlaySpeed = EditorGUILayout.Slider(_autoPlaySpeed, 1f, 20f);

            if (EditorGUI.EndChangeCheck())
            {
                EditorPrefs.SetBool(AutoPlayPrefKey, _autoPlayOnEnter);
                EditorPrefs.SetFloat(AutoPlaySpeedPrefKey, _autoPlaySpeed);
            }

            if (GUILayout.Button("새로 읽기", GUILayout.Width(80f)))
                Reload();
        }

        EditorGUILayout.Space();

        if (!string.IsNullOrEmpty(_loadError))
        {
            EditorGUILayout.HelpBox(_loadError, MessageType.Error);

            return;
        }

        if (_stages.Count == 0)
        {
            EditorGUILayout.HelpBox("스테이지 행이 없습니다.", MessageType.Warning);

            return;
        }

        using var scroll = new EditorGUILayout.ScrollViewScope(_scroll);
        _scroll = scroll.scrollPosition;

        foreach (var stage in _stages)
            DrawStageRow(stage);
    }

    private void DrawCurrentOverride()
    {
        var current = SessionState.GetString(MainGameController.DebugStageIdKey, string.Empty);

        if (string.IsNullOrEmpty(current))
            return;

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.HelpBox(
                $"지정 진입 중: {current}\n씬을 그냥 재생해도 이 스테이지로 들어갑니다.",
                MessageType.Warning);

            if (GUILayout.Button("해제", GUILayout.Width(60f), GUILayout.Height(38f)))
            {
                SessionState.EraseString(MainGameController.DebugStageIdKey);

                Debug.Log("[StageDebug] 지정 해제. 씬의 stageId를 그대로 씁니다.");
            }
        }
    }

    private void DrawStageRow(StageDataTableRow stage)
    {
        using var _ = new EditorGUILayout.VerticalScope(EditorStyles.helpBox);

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField($"{stage.Id}  {stage.Name}", EditorStyles.boldLabel);

            if (GUILayout.Button("진입", GUILayout.Width(80f)))
                Enter(stage.Id);
        }

        var waveRows = _waveRowCounts.TryGetValue(stage.Id, out var count) ? count : 0;

        EditorGUILayout.LabelField(
            $"웨이브 {stage.WaveCount}    몬스터 {stage.TotalMonsterCount}마리    스탯 배율 x{stage.MonsterStatMultiplier:0.##}",
            EditorStyles.miniLabel);

        // 웨이브 행이 모자라면 들어가도 측정이 안 됩니다.
        // 눌러 보고 나서 알면 5분을 버리므로 누르기 전에 보여 줍니다.
        if (waveRows == 0)
            EditorGUILayout.HelpBox("WaveDataTable에 이 스테이지의 행이 없습니다. 들어가도 몬스터가 나오지 않습니다.", MessageType.Error);
        else if (waveRows != stage.WaveCount)
            EditorGUILayout.HelpBox($"웨이브 행이 {waveRows}개인데 WaveCount는 {stage.WaveCount}입니다.", MessageType.Warning);
    }

    private void Reload()
    {
        _stages.Clear();
        _waveRowCounts.Clear();
        _loadError = null;

        if (!TryReadRows<StageDataTableRow>("StageDataTable", out var stages, out _loadError))
            return;

        _stages.AddRange(stages.Where(row => row != null && !string.IsNullOrEmpty(row.Id)).OrderBy(row => row.Id));

        if (!TryReadRows<WaveDataTableRow>("WaveDataTable", out var waves, out var waveError))
        {
            _loadError = waveError;

            return;
        }

        foreach (var wave in waves)
        {
            if (wave == null || string.IsNullOrEmpty(wave.StageId))
                continue;

            _waveRowCounts.TryGetValue(wave.StageId, out var count);
            _waveRowCounts[wave.StageId] = count + 1;
        }
    }

    private static bool TryReadRows<T>(string fileName, out List<T> rows, out string error)
    {
        rows = new List<T>();
        error = null;

        var path = Path.Combine(JsonFolder, $"{fileName}.json");

        if (!File.Exists(path))
        {
            error = $"{path} 를 찾지 못했습니다.";

            return false;
        }

        try
        {
            rows = JsonConvert.DeserializeObject<List<T>>(File.ReadAllText(path)) ?? new List<T>();

            return true;
        }
        catch (JsonException exception)
        {
            error = $"{fileName}.json 파싱에 실패했습니다: {exception.Message}";

            return false;
        }
    }

    private void Enter(string stageId)
    {
        SessionState.SetString(MainGameController.DebugStageIdKey, stageId);

        // 이미 플레이 중이면 한 번 끄고 다시 들어가야 스테이지가 새로 잡힙니다.
        // 도메인 리로드를 거치므로 무엇을 하려던 참이었는지는 SessionState에 맡깁니다.
        if (EditorApplication.isPlaying)
        {
            SessionState.SetString(PendingEnterKey, stageId);
            SessionState.SetFloat(PendingAutoPlayKey, _autoPlayOnEnter ? _autoPlaySpeed : 0f);

            EditorApplication.isPlaying = false;

            return;
        }

        if (!TryOpenScene())
            return;

        SessionState.SetFloat(PendingAutoPlayKey, _autoPlayOnEnter ? _autoPlaySpeed : 0f);

        EditorApplication.isPlaying = true;
    }

    internal static bool TryOpenScene()
    {
        var opened = UnityEngine.SceneManagement.SceneManager.GetSceneByPath(ScenePath);

        if (opened.IsValid() && opened.isLoaded)
            return true;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return false;

        EditorSceneManager.OpenScene(ScenePath);

        return true;
    }

    internal static string TakePendingStageId()
    {
        var pending = SessionState.GetString(PendingEnterKey, string.Empty);

        if (!string.IsNullOrEmpty(pending))
            SessionState.EraseString(PendingEnterKey);

        return pending;
    }

    internal static float TakePendingAutoPlaySpeed()
    {
        var speed = SessionState.GetFloat(PendingAutoPlayKey, 0f);

        SessionState.EraseFloat(PendingAutoPlayKey);

        return speed;
    }
}

/// <summary>
/// 플레이 모드 전환을 이어 받습니다. 창이 닫혀 있어도 동작해야 하고, 도메인 리로드를
/// 건너뛰므로 창 인스턴스에 기대지 않습니다.
/// </summary>
[InitializeOnLoad]
internal static class StageDebugEntryHook
{
    static StageDebugEntryHook()
    {
        EditorApplication.playModeStateChanged -= OnChanged;
        EditorApplication.playModeStateChanged += OnChanged;
    }

    private static void OnChanged(PlayModeStateChange change)
    {
        switch (change)
        {
            case PlayModeStateChange.EnteredEditMode:
                ResumePending();

                break;

            case PlayModeStateChange.EnteredPlayMode:
                StartAutoPlayIfRequested();

                break;
        }
    }

    // 플레이를 끄고 다른 스테이지로 다시 들어가려던 참이었다면 여기서 이어 갑니다.
    private static void ResumePending()
    {
        if (string.IsNullOrEmpty(StageDebugEntryWindow.TakePendingStageId()))
            return;

        // 이 콜백 안에서 바로 isPlaying을 켜면 전환이 겹칩니다. 한 틱 미룹니다.
        EditorApplication.delayCall += () =>
        {
            if (StageDebugEntryWindow.TryOpenScene())
                EditorApplication.isPlaying = true;
        };
    }

    private static void StartAutoPlayIfRequested()
    {
        var speed = StageDebugEntryWindow.TakePendingAutoPlaySpeed();

        if (speed <= 0f)
            return;

        // 데이터 로딩이 끝나기 전이어도 됩니다. 자동 플레이는 화면에 뜬 것만 보고 움직입니다.
        InGameAutoPlayTester.Begin(speed);
    }
}
#endif

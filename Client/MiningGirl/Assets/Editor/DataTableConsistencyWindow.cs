#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Data;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 시트에서 뽑은 JSON끼리 아귀가 맞는지 검사합니다. 값이 옳은지가 아니라 서로 어긋나지 않는지를 봅니다.
///
/// 두 층으로 나뉩니다.
///  - 모든 테이블 공통: 파일 존재, 파싱 성공, Id 중복·누락. 테이블이 늘어도 자동으로 포함됩니다.
///  - 테이블 고유 규칙: 컬럼 사이나 테이블 사이의 약속. 기획 규칙이라 코드에 적어야만 알 수 있습니다.
///
/// 런타임에는 관여하지 않습니다. 데이터 클래스와 DataTableManager를 건드리지 않으려고
/// 검사 로직을 전부 이쪽 에디터 전용 코드로 몰아 두었습니다.
/// </summary>
public class DataTableConsistencyWindow : EditorWindow
{
    private const string JsonFolder = "Assets/Data/SheetsJson";

    private enum Severity
    {
        Info,
        Warning,
        Error,
    }

    private struct Result
    {
        public Severity Severity;
        public string Message;
    }

    private readonly List<Result> _results = new();
    private Vector2 _scroll;
    private bool _hasRun;

    [MenuItem("Tools/DataTable/Consistency Check")]
    private static void Open()
    {
        var window = GetWindow<DataTableConsistencyWindow>("DataTable Consistency");
        window.minSize = new Vector2(520f, 420f);
        window.Show();
    }

    private void OnGUI()
    {
        EditorGUILayout.HelpBox(
            $"{JsonFolder} 의 JSON을 직접 읽어 검사합니다. 플레이 모드나 Addressables 빌드가 필요 없습니다.",
            MessageType.Info);

        EditorGUILayout.Space();

        if (GUILayout.Button("검사 실행", GUILayout.Height(32f)))
            Run();

        EditorGUILayout.Space();

        if (!_hasRun)
            return;

        var errors = _results.Count(x => x.Severity == Severity.Error);
        var warnings = _results.Count(x => x.Severity == Severity.Warning);

        if (errors == 0 && warnings == 0)
            EditorGUILayout.HelpBox("문제 없습니다.", MessageType.Info);
        else
            EditorGUILayout.HelpBox($"오류 {errors}건 / 경고 {warnings}건", errors > 0 ? MessageType.Error : MessageType.Warning);

        EditorGUILayout.Space();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        foreach (var result in _results)
            EditorGUILayout.HelpBox(result.Message, ToMessageType(result.Severity));

        EditorGUILayout.EndScrollView();
    }

    private void Run()
    {
        _results.Clear();
        _hasRun = true;

        // [DataFile]이 붙은 행 타입을 전부 찾아 읽습니다. 테이블이 늘어도 여기는 그대로입니다.
        var tables = LoadAll();

        // 모든 테이블에 공통으로 적용되는 검사.
        foreach (var pair in tables)
            CheckCommon(pair.Key, pair.Value);

        // 아래는 특정 테이블에만 있는 규칙입니다.
        // 새 테이블에 고유 규칙이 생겼을 때만 여기에 한 줄 추가하면 됩니다.
        CheckWaveListLengths(Rows<WaveDataTableRow>(tables));
        CheckStageMonsterTotals(Rows<StageDataTableRow>(tables), Rows<WaveDataTableRow>(tables));
    }

    /// <summary>
    /// DataTableRowBase를 상속하고 [DataFile]이 붙은 타입을 모두 찾아, 각자의 JSON을 읽습니다.
    /// </summary>
    private Dictionary<System.Type, IList> LoadAll()
    {
        var tables = new Dictionary<System.Type, IList>();

        var rowTypes = typeof(DataTableRowBase).Assembly
            .GetTypes()
            .Where(x => !x.IsAbstract && typeof(DataTableRowBase).IsAssignableFrom(x))
            .Where(x => x.IsDefined(typeof(DataFileAttribute), false))
            .OrderBy(x => x.Name);

        foreach (var rowType in rowTypes)
        {
            var fileName = ((DataFileAttribute)rowType.GetCustomAttributes(typeof(DataFileAttribute), false)[0]).BaseFileName;
            var rows = Load(rowType, fileName);

            if (rows != null)
                tables[rowType] = rows;
        }

        if (tables.Count == 0)
            Add(Severity.Error, "읽어들인 테이블이 없습니다.");

        return tables;
    }

    private static List<T> Rows<T>(Dictionary<System.Type, IList> tables) where T : DataTableRowBase
    {
        return tables.TryGetValue(typeof(T), out var rows) ? rows as List<T> : null;
    }

    // 행 수와 Id 상태는 테이블 종류와 무관하게 항상 확인할 수 있습니다.
    // GetRow(id)가 FirstOrDefault라서 Id가 겹치면 뒤엣것이 조용히 묻힙니다.
    private void CheckCommon(System.Type rowType, IList rows)
    {
        var ids = rows.Cast<DataTableRowBase>().Select(x => x.Id).ToList();
        var blank = ids.Count(string.IsNullOrWhiteSpace);

        if (blank > 0)
            Add(Severity.Error, $"[{rowType.Name}] Id가 비어 있는 행이 {blank}개 있습니다.");

        var duplicates = ids
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .GroupBy(x => x)
            .Where(x => x.Count() > 1)
            .ToList();

        foreach (var group in duplicates)
            Add(Severity.Error, $"[{rowType.Name}] Id '{group.Key}'가 {group.Count()}번 나옵니다. 먼저 나온 행만 쓰이고 나머지는 묻힙니다.");

        if (blank == 0 && duplicates.Count == 0)
            Add(Severity.Info, $"[{rowType.Name}] {rows.Count}행. Id 중복·누락 없음.");
    }

    // MonsterIds와 Counts는 순서로 짝을 이루는 병렬 배열이라 길이가 어긋나면 스폰이 밀립니다.
    private void CheckWaveListLengths(List<WaveDataTableRow> waves)
    {
        if (waves == null)
            return;

        foreach (var wave in waves)
        {
            var idCount = wave.MonsterIds?.Count ?? 0;
            var countCount = wave.Counts?.Count ?? 0;

            if (idCount == countCount)
                continue;

            Add(Severity.Error,
                $"[WaveDataTable] {wave.Id}: MonsterIds {idCount}개, Counts {countCount}개로 길이가 다릅니다. 순서대로 짝을 이뤄야 합니다.");
        }
    }

    // 총 몬스터 수가 곧 총 경험치라, 어긋나면 성장 곡선이 통째로 틀어집니다.
    private void CheckStageMonsterTotals(List<StageDataTableRow> stages, List<WaveDataTableRow> waves)
    {
        if (stages == null || waves == null)
            return;

        var stageIds = new HashSet<string>(stages.Select(x => x.Id));

        // 어느 스테이지에도 속하지 않는 웨이브는 합계에서 조용히 빠지므로 따로 짚어 줍니다.
        foreach (var wave in waves.Where(x => !stageIds.Contains(x.StageId)))
        {
            Add(Severity.Error,
                $"[WaveDataTable] {wave.Id}: StageId '{wave.StageId}'가 StageDataTable에 없습니다.");
        }

        foreach (var stage in stages)
        {
            var stageWaves = waves.Where(x => x.StageId == stage.Id).ToList();

            if (stageWaves.Count == 0)
            {
                Add(Severity.Warning, $"[StageDataTable] {stage.Id}: 웨이브가 한 줄도 없습니다.");

                continue;
            }

            var total = stageWaves.Sum(x => x.Counts?.Sum() ?? 0);

            if (total == stage.TotalMonsterCount)
            {
                Add(Severity.Info, $"[{stage.Id}] 웨이브 {stageWaves.Count}개, 몬스터 합계 {total}마리. TotalMonsterCount와 일치합니다.");

                continue;
            }

            Add(Severity.Error,
                $"[{stage.Id}] 웨이브 Counts 합계가 {total}마리인데 TotalMonsterCount는 {stage.TotalMonsterCount}입니다. " +
                $"{total - stage.TotalMonsterCount:+#;-#;0}만큼 어긋납니다.");
        }
    }

    private IList Load(System.Type rowType, string tableName)
    {
        var path = Path.Combine(JsonFolder, tableName + ".json");

        if (!File.Exists(path))
        {
            Add(Severity.Error, $"[{rowType.Name}] {path} 파일이 없습니다. 익스포터를 먼저 실행하세요.");

            return null;
        }

        try
        {
            var listType = typeof(List<>).MakeGenericType(rowType);

            return JsonConvert.DeserializeObject(File.ReadAllText(path), listType) as IList;
        }
        catch (System.Exception e)
        {
            Add(Severity.Error, $"[{rowType.Name}] {path} 파싱에 실패했습니다.\n{e.Message}");

            return null;
        }
    }

    private void Add(Severity severity, string message)
    {
        _results.Add(new Result { Severity = severity, Message = message });
    }

    private static MessageType ToMessageType(Severity severity)
    {
        switch (severity)
        {
            case Severity.Error: return MessageType.Error;
            case Severity.Warning: return MessageType.Warning;
            default: return MessageType.None;
        }
    }
}
#endif

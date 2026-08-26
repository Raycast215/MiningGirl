#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

/// <summary>
/// 프로젝트 단위 설정을 모아 두는 에디터 창입니다.
/// 스크린 설정 탭은 Player Settings의 화면 방향을, 빌드 탭은 안드로이드 빌드를 다룹니다.
/// </summary>
public class ProjectToolsWindow : EditorWindow
{
    private static readonly string[] TabLabels = { "스크린 설정", "빌드" };

    private int _tabIndex;
    private Vector2 _scroll;

    [MenuItem("Tools/Project/Project Tools")]
    private static void Open()
    {
        var window = GetWindow<ProjectToolsWindow>("Project Tools");
        window.minSize = new Vector2(420f, 520f);
        window.Show();
    }

    private void OnEnable()
    {
        LoadOrientationFromProjectSettings();
        LoadBuildPrefs();
    }

    private void OnFocus()
    {
        // 창 밖에서 Player Settings를 직접 고쳤을 수 있으니 다시 읽어옵니다.
        // 단, 아직 적용하지 않은 편집 내용이 있으면 덮어쓰지 않습니다.
        if (!HasUnappliedOrientationChanges())
            LoadOrientationFromProjectSettings();

        // 창 밖에서 빌드 폴더가 바뀌었을 수 있으므로 파일명을 다시 계산합니다.
        InvalidateFileNameCache();
    }

    private void OnGUI()
    {
        // 바로가기와 탭은 항상 맨 위에 두고, 스크롤하지 않습니다.
        DrawProjectSettingsButtons();
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("탭", EditorStyles.boldLabel);
        _tabIndex = GUILayout.Toolbar(_tabIndex, TabLabels, GUILayout.Height(24f));
        EditorGUILayout.Space();

        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        if (_tabIndex == 0)
            DrawScreenTab();
        else
            DrawBuildTab();

        EditorGUILayout.EndScrollView();
    }

    // ------------------------------------------------------------------
    // 스크린 설정 탭
    // ------------------------------------------------------------------

    // Player Settings의 UIOrientation을 그대로 쓰되, 표시용 이름만 따로 둡니다.
    private static readonly UIOrientation[] OrientationValues =
    {
        UIOrientation.Portrait,
        UIOrientation.PortraitUpsideDown,
        UIOrientation.LandscapeLeft,
        UIOrientation.LandscapeRight,
        UIOrientation.AutoRotation,
    };

    private static readonly string[] OrientationLabels =
    {
        "세로 (Portrait)",
        "세로 뒤집기 (Portrait Upside Down)",
        "가로 왼쪽 (Landscape Left)",
        "가로 오른쪽 (Landscape Right)",
        "자동 회전 (Auto Rotation)",
    };

    // 창에서 편집 중인 값. 적용 버튼을 누르기 전까지는 Player Settings에 반영되지 않습니다.
    private UIOrientation _defaultOrientation;
    private bool _allowPortrait;
    private bool _allowPortraitUpsideDown;
    private bool _allowLandscapeLeft;
    private bool _allowLandscapeRight;

    private void DrawScreenTab()
    {
        EditorGUILayout.HelpBox(
            "여기서 적용하면 Player Settings가 바뀝니다. 프로젝트 전체에 반영됩니다.",
            MessageType.Info);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("현재 프로젝트 설정", EditorStyles.boldLabel);

        var current =
            $"기본 방향: {ToOrientationLabel(PlayerSettings.defaultInterfaceOrientation)}\n" +
            $"허용 - 세로: {PlayerSettings.allowedAutorotateToPortrait}  /  " +
            $"세로 뒤집기: {PlayerSettings.allowedAutorotateToPortraitUpsideDown}\n" +
            $"허용 - 가로 왼쪽: {PlayerSettings.allowedAutorotateToLandscapeLeft}  /  " +
            $"가로 오른쪽: {PlayerSettings.allowedAutorotateToLandscapeRight}\n" +
            $"빌드 타겟: {EditorUserBuildSettings.activeBuildTarget}";

        EditorGUILayout.HelpBox(current, MessageType.None);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("빠른 프리셋", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("세로 고정"))
                SetOrientationPreset(UIOrientation.Portrait, true, false, false, false);

            if (GUILayout.Button("세로 회전 허용"))
                SetOrientationPreset(UIOrientation.AutoRotation, true, true, false, false);

            if (GUILayout.Button("가로 고정"))
                SetOrientationPreset(UIOrientation.LandscapeLeft, false, false, true, false);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("변경할 설정", EditorStyles.boldLabel);

        var index = Mathf.Max(0, Array.IndexOf(OrientationValues, _defaultOrientation));
        _defaultOrientation = OrientationValues[EditorGUILayout.Popup("기본 화면 방향", index, OrientationLabels)];

        // 허용 방향은 자동 회전일 때만 실제로 쓰입니다.
        var isAutoRotation = _defaultOrientation == UIOrientation.AutoRotation;

        using (new EditorGUI.DisabledScope(!isAutoRotation))
        {
            EditorGUILayout.LabelField("자동 회전 허용 방향");
            EditorGUI.indentLevel++;
            _allowPortrait = EditorGUILayout.Toggle("세로", _allowPortrait);
            _allowPortraitUpsideDown = EditorGUILayout.Toggle("세로 뒤집기", _allowPortraitUpsideDown);
            _allowLandscapeLeft = EditorGUILayout.Toggle("가로 왼쪽", _allowLandscapeLeft);
            _allowLandscapeRight = EditorGUILayout.Toggle("가로 오른쪽", _allowLandscapeRight);
            EditorGUI.indentLevel--;
        }

        var hasAnyAllowed = _allowPortrait || _allowPortraitUpsideDown || _allowLandscapeLeft || _allowLandscapeRight;

        if (isAutoRotation && !hasAnyAllowed)
        {
            EditorGUILayout.HelpBox(
                "자동 회전인데 허용된 방향이 하나도 없습니다. 최소 하나는 켜야 적용할 수 있습니다.",
                MessageType.Warning);
        }

        EditorGUILayout.Space();

        var canApply = hasAnyAllowed || !isAutoRotation;
        var dirty = HasUnappliedOrientationChanges();

        if (dirty)
            EditorGUILayout.HelpBox("아직 적용하지 않은 변경이 있습니다.", MessageType.Warning);

        using (new EditorGUI.DisabledScope(!canApply || !dirty))
        {
            if (GUILayout.Button("프로젝트 설정에 적용", GUILayout.Height(30)))
                ApplyOrientationToProjectSettings();
        }

        using (new EditorGUI.DisabledScope(!dirty))
        {
            if (GUILayout.Button("변경 취소 (현재 설정 다시 불러오기)"))
            {
                LoadOrientationFromProjectSettings();
                GUI.FocusControl(null);
            }
        }
    }

    private void SetOrientationPreset(UIOrientation orientation, bool portrait, bool portraitUpsideDown, bool landscapeLeft, bool landscapeRight)
    {
        _defaultOrientation = orientation;
        _allowPortrait = portrait;
        _allowPortraitUpsideDown = portraitUpsideDown;
        _allowLandscapeLeft = landscapeLeft;
        _allowLandscapeRight = landscapeRight;

        GUI.FocusControl(null);
        Repaint();
    }

    private void LoadOrientationFromProjectSettings()
    {
        _defaultOrientation = PlayerSettings.defaultInterfaceOrientation;
        _allowPortrait = PlayerSettings.allowedAutorotateToPortrait;
        _allowPortraitUpsideDown = PlayerSettings.allowedAutorotateToPortraitUpsideDown;
        _allowLandscapeLeft = PlayerSettings.allowedAutorotateToLandscapeLeft;
        _allowLandscapeRight = PlayerSettings.allowedAutorotateToLandscapeRight;

        Repaint();
    }

    private void ApplyOrientationToProjectSettings()
    {
        PlayerSettings.defaultInterfaceOrientation = _defaultOrientation;
        PlayerSettings.allowedAutorotateToPortrait = _allowPortrait;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = _allowPortraitUpsideDown;
        PlayerSettings.allowedAutorotateToLandscapeLeft = _allowLandscapeLeft;
        PlayerSettings.allowedAutorotateToLandscapeRight = _allowLandscapeRight;

        AssetDatabase.SaveAssets();

        Debug.Log($"[Project Tools] 화면 방향 적용 완료: {ToOrientationLabel(_defaultOrientation)}");
        Repaint();
    }

    private bool HasUnappliedOrientationChanges()
    {
        return _defaultOrientation != PlayerSettings.defaultInterfaceOrientation ||
               _allowPortrait != PlayerSettings.allowedAutorotateToPortrait ||
               _allowPortraitUpsideDown != PlayerSettings.allowedAutorotateToPortraitUpsideDown ||
               _allowLandscapeLeft != PlayerSettings.allowedAutorotateToLandscapeLeft ||
               _allowLandscapeRight != PlayerSettings.allowedAutorotateToLandscapeRight;
    }

    private static string ToOrientationLabel(UIOrientation orientation)
    {
        var index = Array.IndexOf(OrientationValues, orientation);
        return index >= 0 ? OrientationLabels[index] : orientation.ToString();
    }

    // ------------------------------------------------------------------
    // 빌드 탭
    // ------------------------------------------------------------------

    private const string PrefOutputFolder = "MiningGirl.Build.OutputFolder";
    private const string PrefFileBaseName = "MiningGirl.Build.FileBaseName";
    private const string PrefAutoSuffix = "MiningGirl.Build.AutoSuffix";
    private const string PrefDevelopment = "MiningGirl.Build.Development";

    // 암호는 EditorPrefs에 남기지 않습니다. SessionState는 유니티를 껐다 켜면 사라집니다.
    private const string SessionKeystorePass = "MiningGirl.Build.KeystorePass";
    private const string SessionAliasPass = "MiningGirl.Build.AliasPass";

    private string _outputFolder;
    private string _fileBaseName;
    private bool _autoSuffix;
    private bool _development;

    private string _keystorePath;
    private string _aliasName;
    private string _keystorePass;
    private string _aliasPass;

    // 파일명 계산에 폴더 스캔이 들어가서 결과를 캐시합니다. OnGUI는 초당 여러 번 불립니다.
    private string _cachedFileName;
    private string _fileNameCacheKey;

    // 같은 프레임에 버튼이 두 번 눌려 빌드가 중복으로 걸리는 것을 막습니다.
    private bool _isBuilding;

    private void LoadBuildPrefs()
    {
        _outputFolder = EditorPrefs.GetString(PrefOutputFolder, GetDefaultOutputFolder());
        _fileBaseName = EditorPrefs.GetString(PrefFileBaseName, "MiningGirl_Test");
        _autoSuffix = EditorPrefs.GetBool(PrefAutoSuffix, true);
        _development = EditorPrefs.GetBool(PrefDevelopment, EditorUserBuildSettings.development);

        _keystorePath = PlayerSettings.Android.keystoreName;
        _aliasName = PlayerSettings.Android.keyaliasName;

        _keystorePass = SessionState.GetString(SessionKeystorePass, string.Empty);
        _aliasPass = SessionState.GetString(SessionAliasPass, string.Empty);
    }

    private void DrawBuildTab()
    {
        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
        {
            EditorGUILayout.HelpBox(
                $"현재 빌드 타겟이 {EditorUserBuildSettings.activeBuildTarget}입니다. 안드로이드로 전환해야 빌드할 수 있습니다.",
                MessageType.Error);

            if (GUILayout.Button("안드로이드로 전환"))
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Android, BuildTarget.Android);

            EditorGUILayout.Space();
        }

        DrawKeystoreSection();
        EditorGUILayout.Space();

        DrawOutputSection();
        EditorGUILayout.Space();

        DrawBuildOptionSection();
        EditorGUILayout.Space();

        DrawBuildButton();
    }

    private void DrawKeystoreSection()
    {
        EditorGUILayout.LabelField("키스토어", EditorStyles.boldLabel);

        EditorGUILayout.HelpBox(
            "암호는 이 창과 현재 유니티 세션에만 유지됩니다. 파일에 저장하지 않으며 유니티를 껐다 켜면 다시 입력해야 합니다.",
            MessageType.Info);

        var useCustom = EditorGUILayout.Toggle("커스텀 키스토어 사용", PlayerSettings.Android.useCustomKeystore);
        if (useCustom != PlayerSettings.Android.useCustomKeystore)
            PlayerSettings.Android.useCustomKeystore = useCustom;

        using (new EditorGUI.DisabledScope(!useCustom))
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                _keystorePath = EditorGUILayout.TextField("키스토어 파일", _keystorePath);

                if (GUILayout.Button("찾아보기", GUILayout.Width(70f)))
                {
                    var picked = EditorUtility.OpenFilePanel("키스토어 선택", GetProjectRoot(), "keystore,jks");
                    if (!string.IsNullOrEmpty(picked))
                    {
                        _keystorePath = ToProjectRelativeIfPossible(picked);
                        GUI.FocusControl(null);
                    }
                }
            }

            _aliasName = EditorGUILayout.TextField("키 별칭 (alias)", _aliasName);

            var newKeystorePass = EditorGUILayout.PasswordField("키스토어 암호", _keystorePass);
            if (newKeystorePass != _keystorePass)
            {
                _keystorePass = newKeystorePass;
                SessionState.SetString(SessionKeystorePass, _keystorePass);
            }

            var newAliasPass = EditorGUILayout.PasswordField("별칭 암호", _aliasPass);
            if (newAliasPass != _aliasPass)
            {
                _aliasPass = newAliasPass;
                SessionState.SetString(SessionAliasPass, _aliasPass);
            }

            if (GUILayout.Button("입력한 암호 지우기"))
            {
                _keystorePass = string.Empty;
                _aliasPass = string.Empty;
                SessionState.EraseString(SessionKeystorePass);
                SessionState.EraseString(SessionAliasPass);
                GUI.FocusControl(null);
            }

            var resolved = ResolveKeystoreFullPath(_keystorePath);
            if (!string.IsNullOrEmpty(_keystorePath) && !File.Exists(resolved))
                EditorGUILayout.HelpBox($"키스토어 파일을 찾을 수 없습니다.\n{resolved}", MessageType.Error);
        }
    }

    private void DrawOutputSection()
    {
        EditorGUILayout.LabelField("출력", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            _outputFolder = EditorGUILayout.TextField("출력 폴더", _outputFolder);

            if (GUILayout.Button("찾아보기", GUILayout.Width(70f)))
            {
                var picked = EditorUtility.OpenFolderPanel("출력 폴더 선택", _outputFolder, string.Empty);
                if (!string.IsNullOrEmpty(picked))
                {
                    _outputFolder = picked;
                    EditorPrefs.SetString(PrefOutputFolder, _outputFolder);
                    GUI.FocusControl(null);
                }
            }
        }

        var newBaseName = EditorGUILayout.TextField("파일 이름", _fileBaseName);
        if (newBaseName != _fileBaseName)
        {
            _fileBaseName = newBaseName;
            EditorPrefs.SetString(PrefFileBaseName, _fileBaseName);
        }

        var newAutoSuffix = EditorGUILayout.Toggle("날짜 + 일련번호 자동 붙이기", _autoSuffix);
        if (newAutoSuffix != _autoSuffix)
        {
            _autoSuffix = newAutoSuffix;
            EditorPrefs.SetBool(PrefAutoSuffix, _autoSuffix);
        }

        EditorGUILayout.LabelField("실제 파일", BuildFileName());
    }

    private void DrawBuildOptionSection()
    {
        EditorGUILayout.LabelField("빌드 옵션", EditorStyles.boldLabel);

        var newDevelopment = EditorGUILayout.Toggle("Development Build", _development);
        if (newDevelopment != _development)
        {
            _development = newDevelopment;
            EditorPrefs.SetBool(PrefDevelopment, _development);
        }

        var appBundle = EditorGUILayout.Toggle("App Bundle (AAB)", EditorUserBuildSettings.buildAppBundle);
        if (appBundle != EditorUserBuildSettings.buildAppBundle)
            EditorUserBuildSettings.buildAppBundle = appBundle;

        var scenes = GetEnabledScenes();
        EditorGUILayout.LabelField($"포함 씬 ({scenes.Length}개)");
        EditorGUI.indentLevel++;
        foreach (var scene in scenes)
            EditorGUILayout.LabelField(Path.GetFileNameWithoutExtension(scene));
        EditorGUI.indentLevel--;

        if (scenes.Length == 0)
        {
            EditorGUILayout.HelpBox(
                "Build Settings에 활성화된 씬이 없습니다. 씬을 추가해야 빌드할 수 있습니다.",
                MessageType.Error);
        }
    }

    private void DrawBuildButton()
    {
        var problems = CollectBuildProblems();

        if (problems.Count > 0)
            EditorGUILayout.HelpBox("빌드할 수 없습니다:\n- " + string.Join("\n- ", problems), MessageType.Warning);

        using (new EditorGUI.DisabledScope(problems.Count > 0 || _isBuilding))
        {
            if (GUILayout.Button(_isBuilding ? "빌드 준비 중..." : "빌드 실행", GUILayout.Height(34f)))
            {
                // OnGUI 안에서 바로 빌드하면 레이아웃이 깨지므로 다음 프레임으로 넘깁니다.
                _isBuilding = true;
                EditorApplication.delayCall += RunBuild;
            }
        }

        if (Directory.Exists(_outputFolder) && GUILayout.Button("출력 폴더 열기"))
            OpenFolder(_outputFolder);
    }

    /// <summary>
    /// 폴더 자체를 엽니다.
    /// RevealInFinder는 폴더를 넘기면 상위 폴더에서 선택만 하기 때문에 플랫폼별로 따로 처리합니다.
    /// </summary>
    private static void OpenFolder(string folder)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            return;

        var fullPath = Path.GetFullPath(folder);

#if UNITY_EDITOR_WIN
        System.Diagnostics.Process.Start("explorer.exe", "\"" + fullPath.Replace('/', '\\') + "\"");
#elif UNITY_EDITOR_OSX
        System.Diagnostics.Process.Start("open", "\"" + fullPath + "\"");
#else
        EditorUtility.RevealInFinder(fullPath + "/");
#endif
    }

    /// <summary>
    /// 두 탭 아래쪽에 공통으로 붙는 프로젝트 설정 바로가기입니다.
    /// </summary>
    /// <summary>
    /// 탭 위에 공통으로 두는 프로젝트 설정 바로가기입니다.
    /// </summary>
    /// <summary>
    /// 탭 위에 공통으로 두는 프로젝트 설정 바로가기입니다.
    /// </summary>
    private static void DrawProjectSettingsButtons()
    {
        EditorGUILayout.LabelField("바로가기", EditorStyles.boldLabel);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Project Settings"))
                SettingsService.OpenProjectSettings("Project/Player");

            if (GUILayout.Button("Build Settings"))
                EditorApplication.ExecuteMenuItem("File/Build Settings...");
        }
    }



    private List<string> CollectBuildProblems()
    {
        var problems = new List<string>();

        if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
            problems.Add("빌드 타겟이 안드로이드가 아닙니다.");

        if (GetEnabledScenes().Length == 0)
            problems.Add("활성화된 씬이 없습니다.");

        if (string.IsNullOrWhiteSpace(_fileBaseName))
            problems.Add("파일 이름이 비어 있습니다.");

        if (string.IsNullOrWhiteSpace(_outputFolder))
            problems.Add("출력 폴더가 비어 있습니다.");

        if (PlayerSettings.Android.useCustomKeystore)
        {
            if (string.IsNullOrWhiteSpace(_keystorePath))
                problems.Add("키스토어 파일 경로가 비어 있습니다.");
            else if (!File.Exists(ResolveKeystoreFullPath(_keystorePath)))
                problems.Add("키스토어 파일이 존재하지 않습니다.");

            if (string.IsNullOrWhiteSpace(_aliasName))
                problems.Add("키 별칭이 비어 있습니다.");

            if (string.IsNullOrEmpty(_keystorePass))
                problems.Add("키스토어 암호가 비어 있습니다.");

            if (string.IsNullOrEmpty(_aliasPass))
                problems.Add("별칭 암호가 비어 있습니다.");
        }

        return problems;
    }

    private void RunBuild()
    {
        EditorApplication.delayCall -= RunBuild;

        var useCustomKeystore = PlayerSettings.Android.useCustomKeystore;

        try
        {
            // 미리보기에 띄우던 이름과 같은 값을 쓰도록 캐시된 결과를 그대로 가져옵니다.
            var fileName = BuildFileName();
            var fullPath = Path.Combine(_outputFolder, fileName);

            if (!Directory.Exists(_outputFolder))
                Directory.CreateDirectory(_outputFolder);

            if (File.Exists(fullPath) &&
                !EditorUtility.DisplayDialog("파일 덮어쓰기", $"이미 같은 이름의 파일이 있습니다.\n\n{fullPath}\n\n덮어쓸까요?", "덮어쓰기", "취소"))
            {
                return;
            }

            if (useCustomKeystore)
            {
                PlayerSettings.Android.keystoreName = _keystorePath;
                PlayerSettings.Android.keyaliasName = _aliasName;
                PlayerSettings.Android.keystorePass = _keystorePass;
                PlayerSettings.Android.keyaliasPass = _aliasPass;
            }

            EditorUserBuildSettings.development = _development;

            var options = new BuildPlayerOptions
            {
                scenes = GetEnabledScenes(),
                locationPathName = fullPath,
                target = BuildTarget.Android,
                targetGroup = BuildTargetGroup.Android,
                options = _development ? BuildOptions.Development : BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                var sizeMb = summary.totalSize / 1024f / 1024f;
                Debug.Log($"[Project Tools] 빌드 성공: {fullPath} ({sizeMb:F1} MB, {summary.totalTime.TotalSeconds:F0}초)");
                EditorUtility.RevealInFinder(fullPath);
            }
            else
            {
                Debug.LogError($"[Project Tools] 빌드 실패: {summary.result} (에러 {summary.totalErrors}건)");
            }
        }
        finally
        {
            // 암호가 ProjectSettings.asset에 남으면 그대로 커밋될 수 있어서 빌드 후 반드시 지웁니다.
            if (useCustomKeystore)
            {
                PlayerSettings.Android.keystorePass = string.Empty;
                PlayerSettings.Android.keyaliasPass = string.Empty;
            }

            // 예외나 취소로 중간에 빠져나가더라도 버튼이 다시 활성화되도록 여기서 풀어줍니다.
            _isBuilding = false;

            // 방금 만든 파일 때문에 다음 일련번호가 밀리므로 캐시를 비웁니다.
            InvalidateFileNameCache();
            Repaint();
        }
    }

    /// <summary>
    /// 빌드 결과물 파일명을 돌려줍니다.
    /// 이름에 영향을 주는 값이 바뀌거나 캐시를 비우기 전까지는 이전 결과를 그대로 씁니다.
    /// </summary>
    private string BuildFileName()
    {
        var extension = EditorUserBuildSettings.buildAppBundle ? ".aab" : ".apk";
        var baseName = string.IsNullOrWhiteSpace(_fileBaseName) ? "Build" : _fileBaseName.Trim();
        var date = DateTime.Now.ToString("yyyyMMdd");

        // 날짜를 키에 넣어두면 창을 켜둔 채로 자정을 넘겨도 알아서 다시 계산됩니다.
        var key = $"{_outputFolder}|{baseName}|{_autoSuffix}|{extension}|{date}";

        if (key == _fileNameCacheKey && !string.IsNullOrEmpty(_cachedFileName))
            return _cachedFileName;

        _fileNameCacheKey = key;
        _cachedFileName = _autoSuffix
            ? $"{baseName}_{date}_{GetNextSequence(baseName, date, extension):D3}{extension}"
            : baseName + extension;

        return _cachedFileName;
    }

    /// <summary>
    /// 다음에 파일명을 물어볼 때 폴더를 다시 스캔하도록 캐시를 비웁니다.
    /// </summary>
    private void InvalidateFileNameCache()
    {
        _fileNameCacheKey = null;
        _cachedFileName = null;
    }


    /// <summary>
    /// 출력 폴더에서 오늘 날짜로 만들어진 파일을 찾아 다음 일련번호를 돌려줍니다.
    /// </summary>
    private int GetNextSequence(string baseName, string date, string extension)
    {
        if (string.IsNullOrWhiteSpace(_outputFolder) || !Directory.Exists(_outputFolder))
            return 1;

        var pattern = new Regex(
            "^" + Regex.Escape($"{baseName}_{date}_") + @"(\d+)" + Regex.Escape(extension) + "$",
            RegexOptions.IgnoreCase);

        var maxSequence = 0;

        foreach (var file in Directory.GetFiles(_outputFolder))
        {
            var match = pattern.Match(Path.GetFileName(file));
            if (match.Success && int.TryParse(match.Groups[1].Value, out var value))
                maxSequence = Mathf.Max(maxSequence, value);
        }

        return maxSequence + 1;
    }

    private static string[] GetEnabledScenes()
    {
        return EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
    }

    private static string GetProjectRoot()
    {
        return Path.GetDirectoryName(Application.dataPath).Replace('\\', '/');
    }

    /// <summary>
    /// 기존 빌드가 쌓여 있던 &lt;저장소&gt;/Client/Build 를 기본값으로 씁니다.
    /// </summary>
    private static string GetDefaultOutputFolder()
    {
        var root = GetProjectRoot();
        var candidate = Path.GetFullPath(Path.Combine(root, "..", "Build")).Replace('\\', '/');
        return candidate;
    }

    private static string ToProjectRelativeIfPossible(string absolutePath)
    {
        var root = GetProjectRoot() + "/";
        var normalized = absolutePath.Replace('\\', '/');

        return normalized.StartsWith(root, StringComparison.OrdinalIgnoreCase)
            ? normalized.Substring(root.Length)
            : normalized;
    }

    private static string ResolveKeystoreFullPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        return Path.IsPathRooted(path)
            ? path.Replace('\\', '/')
            : Path.GetFullPath(Path.Combine(GetProjectRoot(), path)).Replace('\\', '/');
    }
}
#endif

#if UNITY_EDITOR
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class StatCurvePreviewWindow : EditorWindow
{
    // 현재 편집/미리보기용 프리셋 인스턴스(메모리 상)
    StatCurvePresetSO working;

    // 표시 옵션
    bool autoYMax = true;
    float manualYMax = 200f;
    Color bgColor = new Color(0.11f, 0.11f, 0.11f);
    float padding = 36f;

    string exportDir = "Assets/Generated";
    string fileBaseName = "StatCurves";
    Vector2 scroll;

    [MenuItem("Tools/Balancing/Stat Curve Preview (Advanced)")]
    public static void ShowWindow()
    {
        var win = GetWindow<StatCurvePreviewWindow>("Stat Curves+");
        win.minSize = new Vector2(860, 600);
    }

    void OnEnable()
    {
        if (working == null)
        {
            working = ScriptableObject.CreateInstance<StatCurvePresetSO>();
        }
    }

    void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope())
        {
            DrawLeftPanel();  // 프리셋/입력/커브/캐릭터 목록
            DrawRightGraph(); // 그래프/내보내기
        }
    }

    // ---------- LEFT ----------
    void DrawLeftPanel()
    {
        using (new EditorGUILayout.VerticalScope(GUILayout.Width(360)))
        {
            EditorGUILayout.LabelField("Preset", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("New"))
                    working = ScriptableObject.CreateInstance<StatCurvePresetSO>();

                if (GUILayout.Button("Save As..."))
                    SavePresetAs();

                if (GUILayout.Button("Load..."))
                    LoadPreset();
            }

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Range & Samples", EditorStyles.boldLabel);
            working.minLevel = EditorGUILayout.IntField("Min Level", working.minLevel);
            working.maxLevel = EditorGUILayout.IntField("Max Level", working.maxLevel);
            working.samples  = EditorGUILayout.IntField("Samples",  working.samples);
            working.minLevel = Mathf.Max(1, working.minLevel);
            working.maxLevel = Mathf.Max(working.minLevel, working.maxLevel);
            working.samples  = Mathf.Clamp(working.samples, 2, 2000);

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Visible Stats / Ranks", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    working.showSTR = EditorGUILayout.ToggleLeft("STR", working.showSTR);
                    working.showDEX = EditorGUILayout.ToggleLeft("DEX", working.showDEX);
                    working.showLUK = EditorGUILayout.ToggleLeft("LUK", working.showLUK);
                }
                using (new EditorGUILayout.VerticalScope("box"))
                {
                    working.showR   = EditorGUILayout.ToggleLeft("R",   working.showR);
                    working.showSR  = EditorGUILayout.ToggleLeft("SR",  working.showSR);
                    working.showSSR = EditorGUILayout.ToggleLeft("SSR", working.showSSR);
                    working.showUR  = EditorGUILayout.ToggleLeft("UR",  working.showUR);
                }
            }

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Characters (Compare)", EditorStyles.boldLabel);

            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(280));
            if (working.characters == null || working.characters.Length == 0)
            {
                if (GUILayout.Button("+ Add Character")) AddCharacter();
            }
            else
            {
                for (int i = 0; i < working.characters.Length; i++)
                {
                    DrawCharacterConfig(working.characters[i], i);
                }
                if (GUILayout.Button("+ Add Character")) AddCharacter();
            }
            EditorGUILayout.EndScrollView();

            GUILayout.Space(6);
            EditorGUILayout.LabelField("Graph Options / Export", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                autoYMax = EditorGUILayout.ToggleLeft("Auto Y Max", autoYMax, GUILayout.Width(110));
                using (new EditorGUI.DisabledScope(autoYMax))
                {
                    manualYMax = EditorGUILayout.FloatField("Manual", manualYMax);
                }
            }
            bgColor = EditorGUILayout.ColorField("BG", bgColor);
            exportDir = EditorGUILayout.TextField("Export Folder", exportDir);
            fileBaseName = EditorGUILayout.TextField("File Base", fileBaseName);
        }
    }

    void DrawCharacterConfig(StatConfig c, int idx)
    {
        EditorGUILayout.BeginVertical("box");
        using (new EditorGUILayout.HorizontalScope())
        {
            c.label = EditorGUILayout.TextField("Label", c.label);
            c.color = EditorGUILayout.ColorField(c.color, GUILayout.Width(80));
        }

        EditorGUILayout.LabelField("Base Stats", EditorStyles.miniBoldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            c.baseStr = EditorGUILayout.FloatField("STR", c.baseStr);
            c.baseDex = EditorGUILayout.FloatField("DEX", c.baseDex);
            c.baseLuk = EditorGUILayout.FloatField("LUK", c.baseLuk);
        }

        EditorGUILayout.LabelField("Growth Rates", EditorStyles.miniBoldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            c.growthStr = EditorGUILayout.FloatField("STR", c.growthStr);
            c.growthDex = EditorGUILayout.FloatField("DEX", c.growthDex);
            c.growthLuk = EditorGUILayout.FloatField("LUK", c.growthLuk);
        }

        EditorGUILayout.LabelField("Rank Multipliers", EditorStyles.miniBoldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            c.mulR   = EditorGUILayout.FloatField("R",   c.mulR);
            c.mulSR  = EditorGUILayout.FloatField("SR",  c.mulSR);
            c.mulSSR = EditorGUILayout.FloatField("SSR", c.mulSSR);
            c.mulUR  = EditorGUILayout.FloatField("UR",  c.mulUR);
        }

        EditorGUILayout.LabelField("Growth Curves (0..1 → -1..+1 정도 권장)", EditorStyles.miniBoldLabel);
        c.curveWeight = EditorGUILayout.Slider("Curve Weight", c.curveWeight, 0f, 1f);
        c.curveStr = EditorGUILayout.CurveField("STR Curve", c.curveStr);
        c.curveDex = EditorGUILayout.CurveField("DEX Curve", c.curveDex);
        c.curveLuk = EditorGUILayout.CurveField("LUK Curve", c.curveLuk);

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Duplicate"))
                DuplicateCharacter(idx);
            if (GUILayout.Button("Remove"))
                RemoveCharacter(idx);
        }
        EditorGUILayout.EndVertical();
    }

    void AddCharacter()
    {
        var list = working.characters?.ToList() ?? new List<StatConfig>();
        var sc = new StatConfig { label = $"Char {list.Count + 1}" };
        // 색상 자동 배치 약간
        sc.color = Color.HSVToRGB((0.6f + list.Count * 0.12f) % 1f, 0.6f, 0.95f);
        list.Add(sc);
        working.characters = list.ToArray();
    }
    void DuplicateCharacter(int i)
    {
        var list = working.characters.ToList();
        var clone = JsonUtility.FromJson<StatConfig>(JsonUtility.ToJson(list[i]));
        clone.label += " Copy";
        list.Insert(i + 1, clone);
        working.characters = list.ToArray();
    }
    void RemoveCharacter(int i)
    {
        var list = working.characters.ToList();
        list.RemoveAt(i);
        working.characters = list.ToArray();
    }

    void SavePresetAs()
    {
        var path = EditorUtility.SaveFilePanelInProject("Save Preset", "StatCurvePreset", "asset", "Select path");
        if (string.IsNullOrEmpty(path)) return;

        var asset = ScriptableObject.CreateInstance<StatCurvePresetSO>();
        // working 복제
        EditorUtility.CopySerialized(working, asset);
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        EditorGUIUtility.PingObject(asset);
        Debug.Log($"[StatPreset] Saved: {path}");
    }

    void LoadPreset()
    {
        var path = EditorUtility.OpenFilePanel("Load Preset", "Assets", "asset");
        if (string.IsNullOrEmpty(path)) return;
        if (path.StartsWith(Application.dataPath))
        {
            var rel = "Assets" + path.Substring(Application.dataPath.Length);
            var asset = AssetDatabase.LoadAssetAtPath<StatCurvePresetSO>(rel);
            if (asset != null)
            {
                // working에 복사
                EditorUtility.CopySerialized(asset, working);
                Repaint();
                Debug.Log($"[StatPreset] Loaded: {rel}");
            }
            else
            {
                Debug.LogWarning("선택한 파일이 StatCurvePresetSO 가 아닙니다.");
            }
        }
        else
        {
            Debug.LogWarning("프로젝트 안의 asset만 로드할 수 있습니다.");
        }
    }

    // ---------- RIGHT ----------
    struct LineSeries { public string name; public Color color; public float[] rawY; }

    void DrawRightGraph()
    {
        using (new EditorGUILayout.VerticalScope())
        {
            var rect = GUILayoutUtility.GetRect(position.width - 380, position.height - 120);
            rect = new Rect(rect.x, rect.y + 8, rect.width - 8, rect.height - 8);

            var series = BuildAllSeries();
            float yMax = autoYMax ? CalcAutoYMax(series) : Mathf.Max(1f, manualYMax);

            // 배경
            EditorGUI.DrawRect(rect, bgColor);
            var plot = new Rect(rect.x + padding, rect.y + padding * 0.4f, rect.width - padding * 1.4f, rect.height - padding * 1.6f);
            EditorGUI.DrawRect(plot, new Color(0.13f, 0.13f, 0.13f));

            // 그리드
            Handles.BeginGUI();
            DrawGrid(plot, yMax);
            // 라인
            foreach (var s in series)
                DrawSeriesLine(plot, s, yMax);
            Handles.EndGUI();

            // 범례
            DrawLegend(plot, series);

            // 내보내기
            GUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Export CSV")) ExportCsv(series);
                if (GUILayout.Button("Export PNG")) ExportPng();
            }
        }
    }

    List<LineSeries> BuildAllSeries()
    {
        var list = new List<LineSeries>();
        if (working.characters == null) return list;

        var ranks = new (string label, Func<StatConfig, float> mul, bool on)[]
        {
            ("R",   c => c.mulR,   working.showR),
            ("SR",  c => c.mulSR,  working.showSR),
            ("SSR", c => c.mulSSR, working.showSSR),
            ("UR",  c => c.mulUR,  working.showUR),
        };
        var stats = new (string key, bool on, Func<StatConfig,float> baseVal, Func<StatConfig,float> growth, Func<StatConfig,AnimationCurve> curve)[]
        {
            ("STR", working.showSTR, c => c.baseStr, c => c.growthStr, c => c.curveStr),
            ("DEX", working.showDEX, c => c.baseDex, c => c.growthDex, c => c.curveDex),
            ("LUK", working.showLUK, c => c.baseLuk, c => c.growthLuk, c => c.curveLuk),
        };

        foreach (var ch in working.characters)
        {
            foreach (var st in stats)
            {
                if (!st.on) continue;
                foreach (var rk in ranks)
                {
                    if (!rk.on) continue;

                    var ys = SampleWithCurve(
                        st.baseVal(ch), st.growth(ch), rk.mul(ch),
                        working.minLevel, working.maxLevel, working.samples,
                        st.curve(ch), ch.curveWeight
                    );
                    list.Add(new LineSeries
                    {
                        name = $"{ch.label}-{st.key}-{rk.label}",
                        color = ch.color,
                        rawY = ys
                    });
                }
            }
        }
        return list;
    }

    // 곡선 반영 정확도를 위해 level마다 누적 계산
    float[] SampleWithCurve(float baseStat, float growthRate, float rankMul,
                            int lvMin, int lvMax, int count,
                            AnimationCurve curve, float curveWeight)
    {
        var ys = new float[count];
        if (count <= 1) { ys[0] = EvalWithCurve(baseStat, growthRate, rankMul, lvMin, lvMin, lvMax, curve, curveWeight); return ys; }

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / (count - 1);
            int lv = Mathf.RoundToInt(Mathf.Lerp(lvMin, lvMax, t));
            lv = Mathf.Clamp(lv, lvMin, lvMax);
            ys[i] = EvalWithCurve(baseStat, growthRate, rankMul, lv, lvMin, lvMax, curve, curveWeight);
        }
        return ys;
    }

    // 레벨 2..L 까지 각 레벨구간의 커브값을 반영해 성장률을 누적
    float EvalWithCurve(float baseStat, float growthRate, float rankMul, int level,
                        int lvMin, int lvMax, AnimationCurve curve, float curveWeight)
    {
        level = Mathf.Max(1, level);
        float bonus = 1f;
        int span = Mathf.Max(1, lvMax - lvMin);
        for (int L = 2; L <= level; L++)
        {
            float t = (float)(L - lvMin) / span;                 // 0..1
            float curveFactor = 1f + curve.Evaluate(Mathf.Clamp01(t)) * curveWeight; // 1 + [-1..+1]*w
            float stepGrowth = growthRate * curveFactor * 0.05f; // 레벨 당 성장률
            bonus *= (1f + stepGrowth);
        }
        return baseStat * bonus * Mathf.Max(0.0001f, rankMul);
    }

    float CalcAutoYMax(List<LineSeries> series)
    {
        float max = 0f;
        foreach (var s in series)
            if (s.rawY != null && s.rawY.Length > 0)
                max = Mathf.Max(max, s.rawY.Max());
        return Mathf.Max(1f, max * 1.08f);
    }

    void DrawGrid(Rect plot, float yMax)
    {
        Handles.color = new Color(1,1,1,0.06f);
        int vLines = 10, hLines = 6;
        for (int i = 0; i <= vLines; i++)
        {
            float t = (float)i / vLines;
            float x = Mathf.Lerp(plot.x, plot.xMax, t);
            Handles.DrawLine(new Vector3(x, plot.y), new Vector3(x, plot.yMax));
        }
        for (int j = 0; j <= hLines; j++)
        {
            float t = (float)j / hLines;
            float y = Mathf.Lerp(plot.yMax, plot.y, t);
            Handles.DrawLine(new Vector3(plot.x, y), new Vector3(plot.xMax, y));

            float val = yMax * t;
            var lab = new Rect(plot.x - 58, y - 8, 54, 16);
            GUI.color = new Color(1,1,1,0.7f);
            GUI.Label(lab, val.ToString("0.0"), EditorStyles.miniLabel);
            GUI.color = Color.white;
        }
        GUI.Label(new Rect(plot.x - 8, plot.yMax - 18, 60, 16), working.minLevel.ToString(), EditorStyles.miniLabel);
        GUI.Label(new Rect(plot.xMax - 22, plot.yMax - 18, 60, 16), working.maxLevel.ToString(), EditorStyles.miniLabel);
    }

    void DrawSeriesLine(Rect plot, LineSeries s, float yMax)
    {
        if (s.rawY == null || s.rawY.Length < 2) return;
        var pts = new Vector3[s.rawY.Length];
        for (int i = 0; i < s.rawY.Length; i++)
        {
            float t = (float)i / (s.rawY.Length - 1);
            float x = Mathf.Lerp(plot.x, plot.xMax, t);
            float y = Mathf.Lerp(plot.yMax, plot.y, Mathf.Clamp01(s.rawY[i] / yMax));
            pts[i] = new Vector3(x, y, 0f);
        }
        Handles.color = s.color;
        Handles.DrawAAPolyLine(3.0f, pts);
    }

    void DrawLegend(Rect plot, List<LineSeries> series)
    {
        var uniq = series.Select(s => (s.name.Split('-')[0], s.color)) // label만 추출
                         .Distinct()
                         .Take(20)
                         .ToList();
        var legend = new Rect(plot.xMax - 180, plot.y + 6, 170, 20 + 18 * uniq.Count);
        GUI.Box(legend, GUIContent.none);
        var r = new Rect(legend.x + 8, legend.y + 6, legend.width - 16, 18);
        foreach (var (label, color) in uniq)
        {
            EditorGUI.DrawRect(new Rect(r.x, r.y + 3, 12, 12), color);
            GUI.Label(new Rect(r.x + 18, r.y, r.width - 18, r.height), label);
            r.y += 18;
        }
    }

    void ExportCsv(List<LineSeries> series)
    {
        TryEnsureDir(exportDir);
        var path = Path.Combine(exportDir, $"{fileBaseName}_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

        var sb = new StringBuilder();
        sb.Append("Level");
        foreach (var s in series) sb.Append(',').Append(s.name);
        sb.AppendLine();

        for (int i = 0; i < working.samples; i++)
        {
            float t = (float)i / (working.samples - 1);
            int lv = Mathf.RoundToInt(Mathf.Lerp(working.minLevel, working.maxLevel, t));
            sb.Append(lv);
            foreach (var s in series)
                sb.Append(',').Append(i < s.rawY.Length ? s.rawY[i].ToString("0.####") : "");
            sb.AppendLine();
        }
        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        AssetDatabase.Refresh();
        Debug.Log($"[StatCurve] CSV saved: {path}");
    }

    void ExportPng()
    {
        TryEnsureDir(exportDir);
        var path = Path.Combine(exportDir, $"{fileBaseName}_{DateTime.Now:yyyyMMdd_HHmmss}.png");

        var tex = new Texture2D((int)position.width, (int)position.height, TextureFormat.RGBA32, false);
        var rt = RenderTexture.GetTemporary((int)position.width, (int)position.height, 24);
        ScreenCapture.CaptureScreenshotIntoRenderTexture(rt);
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0,0, rt.width, rt.height), 0, 0);
        tex.Apply();
        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);

        var bytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, bytes);
        DestroyImmediate(tex);
        AssetDatabase.Refresh();
        Debug.Log($"[StatCurve] PNG saved: {path}");
    }

    void TryEnsureDir(string dir)
    {
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
    }
}
#endif

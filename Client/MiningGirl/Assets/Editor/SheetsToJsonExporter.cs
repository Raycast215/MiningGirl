#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;
using System.Net.Sockets;                // Loopback 수신 (고정 경로)
using System.Security.Cryptography;      // PKCE(SHA256), 난수

// ─────────────────────────────────────────────────────────────────────────────
// SheetsToJsonExporter
//  - Google Drive 폴더 내 스프레드시트를 찾아 각 "시트(탭)"을 CSV로 export
//  - 시트(탭)마다 별도의 JSON 파일 저장 (파일명 = 시트명.json)
//  - 변경 감지: 기존 파일 내용과 동일하면 저장 스킵
//  - 인증: OAuth 2.0 (Web application + client_secret, 고정 redirect URI)
//  - CSV 내보내기: docs.google.com + gid (탭별 정확히 분리)
//  - OAuth CLIENT_ID / CLIENT_SECRET은 하드코딩 금지!
//      로드 순서: 환경변수 → 로컬 JSON(ProjectSettings/sheets_oauth.local.json) → EditorPrefs
// ─────────────────────────────────────────────────────────────────────────────
public class SheetsToJsonExporter : EditorWindow
{
    private string driveFolderId = "1IFpiCYE6nhu_rk5xhJpJN6l0TolUi2tM";
    private string outputFolder = "Assets/Data/SheetsJson";
    private bool prettyPrint = true;

    [MenuItem("Tools/Sheets/Export All (Google Drive Folder)")]
    private static void Open()
    {
        var wnd = GetWindow<SheetsToJsonExporter>("Sheets → JSON Exporter");
        wnd.minSize = new Vector2(640, 460);
        wnd.Show();
    }

    void OnGUI()
    {
        GUILayout.Space(10);
        EditorGUILayout.LabelField("Google OAuth 상태", EditorStyles.boldLabel);

        bool authed = GoogleOAuth.HasToken();
        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField(authed ? "로그인됨" : "로그인 필요", GUILayout.Width(90));
            if (!authed)
            {
                if (GUILayout.Button("Google 로그인", GUILayout.Height(24))) _ = SignInFlow();
            }
            else
            {
                if (GUILayout.Button("로그아웃(토큰 삭제)", GUILayout.Height(24)))
                {
                    GoogleOAuth.ClearToken();
                    ShowNotification(new GUIContent("토큰 삭제 완료"));
                }
            }

            if (GUILayout.Button("OAuth 설정 열기", GUILayout.Height(24)))
            {
                SheetsOAuthSettingsWindow.Open();
            }
        }

        GUILayout.Space(10);
        EditorGUILayout.LabelField("Google Drive / Sheets 설정", EditorStyles.boldLabel);
        driveFolderId = EditorGUILayout.TextField(new GUIContent("Drive Folder ID", "예: https://drive.google.com/drive/folders/<이부분>"), driveFolderId);

        GUILayout.Space(10);
        EditorGUILayout.LabelField("출력 설정", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            outputFolder = EditorGUILayout.TextField(new GUIContent("Output Folder", "JSON이 저장될 Unity 프로젝트 상대 경로"), outputFolder);

            if (GUILayout.Button("폴더 선택", GUILayout.Width(90)))
            {
                var abs = EditorUtility.OpenFolderPanel("JSON 저장 폴더 선택", Application.dataPath, "");
                if (!string.IsNullOrEmpty(abs))
                {
                    var projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
                    if (abs.StartsWith(projectPath))
                        outputFolder = abs.Substring(projectPath.Length + 1).Replace("\\", "/");
                    else
                        EditorUtility.DisplayDialog("경고", "프로젝트 폴더 내부를 선택해주세요.", "OK");
                }
            }
        }

        prettyPrint = EditorGUILayout.Toggle(new GUIContent("Pretty Print", "JSON 예쁘게 출력"), prettyPrint);

        GUILayout.Space(14);
        if (GUILayout.Button("Export", GUILayout.Height(36)))
        {
            if (string.IsNullOrWhiteSpace(driveFolderId))
            {
                EditorUtility.DisplayDialog("오류", "Drive Folder ID를 입력하세요.", "OK");
                return;
            }
            if (!AssetDatabase.IsValidFolder(outputFolder))
            {
                Directory.CreateDirectory(Path.Combine(Application.dataPath, "..", outputFolder));
                AssetDatabase.Refresh();
            }
            _ = ExportAll();
        }

        GUILayout.Space(8);
        EditorGUILayout.HelpBox(
            "Web 클라이언트(client_secret 포함) OAuth. 브라우저 로그인 후 자동으로 돌아옵니다.\n" +
            "CSV는 docs.google.com/export?gid=... 경로로 탭별 정확히 가져옵니다.\n" +
            "비밀값은 코드에 넣지 말고 환경변수/로컬 JSON/EditorPrefs를 사용하세요.",
            MessageType.Info);
    }

    private async System.Threading.Tasks.Task SignInFlow()
    {
        try
        {
            await GoogleOAuth.GetAccessTokenAsync(forceInteractive: true);
            ShowNotification(new GUIContent("로그인 성공"));
        }
        catch (Exception ex)
        {
            Debug.LogError(ex);
            EditorUtility.DisplayDialog("로그인 실패", ex.Message, "OK");
        }
    }

    private async System.Threading.Tasks.Task ExportAll()
    {
        try
        {
            var spreadsheets = await ListSpreadsheetsInFolder(driveFolderId);
            if (spreadsheets.Count == 0)
            {
                EditorUtility.DisplayDialog("결과", "해당 폴더에서 스프레드시트를 찾지 못했습니다.", "OK");
                return;
            }

            int totalSaved = 0, skipped = 0;

            for (int idx = 0; idx < spreadsheets.Count; idx++)
            {
                var file = spreadsheets[idx];
                EditorUtility.DisplayProgressBar("Exporting", $"{file.name}", (float)idx / Mathf.Max(1, spreadsheets.Count));

                var sheets = await GetSpreadsheetSheets(file.id);
                foreach (var sh in sheets)
                {
                    if (sh.title.Contains("//")) continue;

                    var csv = await ExportSheetCsv(file.id, sh.sheetId);
                    if (string.IsNullOrEmpty(csv)) continue;

                    var rows = ParseCsv(csv);
                    if (rows.Count < 2) continue;

                    int headerRowIndex = 0;
                    if (rows[0].Count > 0 && rows[0][0].TrimStart().StartsWith("//"))
                        headerRowIndex = 1;
                    if (rows.Count <= headerRowIndex) continue;

                    var headers = rows[headerRowIndex].Select(h => h?.Trim() ?? "").ToList();
                    var dataStart = headerRowIndex + 1;

                    var jsonArray = new List<Dictionary<string, object>>();
                    for (int r = dataStart; r < rows.Count; r++)
                    {
                        var row = rows[r];
                        if (row.All(string.IsNullOrWhiteSpace)) continue;

                        var obj = new Dictionary<string, object>(StringComparer.Ordinal);
                        for (int c = 0; c < headers.Count; c++)
                        {
                            var key = headers[c];
                            if (string.IsNullOrWhiteSpace(key)) continue;

                            string raw = (c < row.Count) ? (row[c] ?? "") : "";
                            object val = ConvertCellValue(raw);
                            obj[key] = val;
                        }
                        jsonArray.Add(obj);
                    }

                    // 파일명 = 시트명.json (스프레드시트명은 넣지 않음)
                    string safeSheetName = MakeSafeFileName(sh.title);
                    string fileName = $"{safeSheetName}.json";
                    string absPath = Path.GetFullPath(Path.Combine(Application.dataPath, "..", outputFolder, fileName));

                    var json = SerializeToJsonStable(jsonArray, prettyPrint);
                    bool wrote = WriteFileIfChanged(absPath, json);
                    if (wrote)
                    {
                        totalSaved++;
                        Debug.Log($"Saved JSON: {fileName} ({jsonArray.Count} rows)");
                    }
                    else
                    {
                        skipped++;
                        Debug.Log($"Unchanged, skipped: {fileName}");
                    }
                }
            }

            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("완료",
                $"스프레드시트: {spreadsheets.Count}개\n저장: {totalSaved}개 / 스킵: {skipped}개",
                "OK");
        }
        catch (Exception ex)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError(ex);
            EditorUtility.DisplayDialog("에러", $"실패: {ex.Message}", "OK");
        }
    }

    // ─ 변경시에만 저장 ─
    private static bool WriteFileIfChanged(string path, string newContent)
    {
        try
        {
            string Normalize(string s) => (s ?? "")
                .Replace("\r\n", "\n").Replace("\r", "\n")
                .Trim();

            var newNorm = Normalize(newContent);

            if (File.Exists(path))
            {
                var existing = File.ReadAllText(path, new UTF8Encoding(false));
                var oldNorm = Normalize(existing);
                if (string.Equals(oldNorm, newNorm, StringComparison.Ordinal))
                    return false;
            }

            File.WriteAllText(path, newNorm, new UTF8Encoding(false)); // BOM 없는 UTF-8
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"파일 저장 실패: {path}\n{e}");
            throw;
        }
    }

    // ─ 안정 직렬화(JSON): 배열 내 객체의 key 정렬 ─
    private static string SerializeToJsonStable(List<Dictionary<string, object>> array, bool pretty)
    {
        var sb = new StringBuilder();
        int indent = 0;

        void W(string t) => sb.Append(t);
        void NL()
        {
            if (!pretty) return;
            sb.Append('\n').Append(new string(' ', indent * 2));
        }

        W("[");
        if (pretty && array.Count > 0) NL();

        for (int i = 0; i < array.Count; i++)
        {
            var obj = array[i];
            W("{");
            var keys = obj.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
            if (pretty && keys.Count > 0) { indent++; NL(); }

            for (int j = 0; j < keys.Count; j++)
            {
                var key = keys[j];
                W($"\"{EscapeJson(key)}\": ");
                WriteAnyStable(obj[key], ref sb, pretty, ref indent);
                if (j < keys.Count - 1) { W(","); if (pretty) NL(); }
            }

            if (pretty && keys.Count > 0) { indent--; NL(); }
            W("}");
            if (i < array.Count - 1) { W(","); if (pretty) NL(); }
        }

        if (pretty && array.Count > 0) NL();
        W("]");
        return sb.ToString();
    }

    private static void WriteAnyStable(object val, ref StringBuilder sb, bool pretty, ref int indent)
    {
        switch (val)
        {
            case null: sb.Append("null"); return;
            case string s: sb.Append($"\"{EscapeJson(s)}\""); return;
            case bool b: sb.Append(b ? "true" : "false"); return;
            case int i32: sb.Append(i32.ToString(CultureInfo.InvariantCulture)); return;
            case long i64: sb.Append(i64.ToString(CultureInfo.InvariantCulture)); return;
            case double dbl: sb.Append(dbl.ToString("R", CultureInfo.InvariantCulture)); return;

            case IList<object> list:
            {
                sb.Append("[");
                if (list.Count == 0) { sb.Append("]"); return; }
                if (pretty) { indent++; sb.Append('\n').Append(new string(' ', indent * 2)); }
                for (int i = 0; i < list.Count; i++)
                {
                    WriteAnyStable(list[i], ref sb, pretty, ref indent);
                    if (i < list.Count - 1) { sb.Append(","); if (pretty) sb.Append('\n').Append(new string(' ', indent * 2)); }
                }
                if (pretty) { indent--; sb.Append('\n').Append(new string(' ', indent * 2)); }
                sb.Append("]");
                return;
            }

            case IEnumerable<object> ien:
            {
                var tmp = ien.ToList();
                WriteAnyStable(tmp, ref sb, pretty, ref indent);
                return;
            }

            default:
                if (val is float f) { sb.Append(((double)f).ToString("R", CultureInfo.InvariantCulture)); return; }
                if (val is decimal dec) { sb.Append(dec.ToString(CultureInfo.InvariantCulture)); return; }
                sb.Append($"\"{EscapeJson(val.ToString())}\"");
                return;
        }
    }

    // ─ CSV 파서 ─
    private static List<List<string>> ParseCsv(string csv)
    {
        var rows = new List<List<string>>();
        var row = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < csv.Length; i++)
        {
            char ch = csv[i];

            if (inQuotes)
            {
                if (ch == '"')
                {
                    if (i + 1 < csv.Length && csv[i + 1] == '"') { sb.Append('"'); i++; }
                    else { inQuotes = false; }
                }
                else sb.Append(ch);
            }
            else
            {
                if (ch == '"') inQuotes = true;
                else if (ch == ',') { row.Add(sb.ToString()); sb.Length = 0; }
                else if (ch == '\r') { /* skip */ }
                else if (ch == '\n')
                {
                    row.Add(sb.ToString()); sb.Length = 0;
                    rows.Add(row); row = new List<string>();
                }
                else sb.Append(ch);
            }
        }
        row.Add(sb.ToString());
        rows.Add(row);
        return rows;
    }

    // ─ 셀 값 변환 ─
    private static object ConvertCellValue(string raw)
    {
        if (raw == null) return null;
        string s = raw.Trim();
        if (string.IsNullOrEmpty(s) || string.Equals(s, "null", StringComparison.OrdinalIgnoreCase))
            return null;

        // 쉼표로 나뉜 값은 배열 처리 (필요에 맞게 규칙 변경 가능)
        if (s.Contains(","))
        {
            var tokens = s.Split(new[] { ',' }, StringSplitOptions.None)
                          .Select(t => t.Trim()).ToList();
            var list = new List<object>();
            foreach (var t in tokens)
                list.Add(ConvertScalar(t));
            return list;
        }
        return ConvertScalar(s);
    }

    private static object ConvertScalar(string s)
    {
        if (bool.TryParse(s, out bool b)) return b;
        if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out long l))
            return (l >= int.MinValue && l <= int.MaxValue) ? (object)(int)l : l;
        if (double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)) return d;
        return s;
    }

    private static string EscapeJson(string s)
    {
        if (string.IsNullOrEmpty(s)) return s ?? "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"")
                .Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    }

    private static string MakeSafeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder();
        foreach (var ch in name) sb.Append(invalid.Contains(ch) ? '_' : ch);
        return sb.ToString();
    }

    // ─ Google Drive / Sheets 호출 ─
    private static void AddAuth(UnityWebRequest req, string accessToken)
    {
        req.SetRequestHeader("Authorization", $"Bearer {accessToken}");
    }

    private async System.Threading.Tasks.Task<List<DriveFile>> ListSpreadsheetsInFolder(string folderId)
    {
        var result = new List<DriveFile>();
        string pageToken = null;
        var access = await GoogleOAuth.GetAccessTokenAsync();

        do
        {
            string url =
                "https://www.googleapis.com/drive/v3/files" +
                $"?q='{UnityWebRequest.EscapeURL(folderId)}'+in+parents+and+mimeType='application/vnd.google-apps.spreadsheet'" +
                "&fields=nextPageToken,files(id,name)" +
                (string.IsNullOrEmpty(pageToken) ? "" : $"&pageToken={pageToken}");

            using (var req = UnityWebRequest.Get(url))
            {
                AddAuth(req, access);
                await req.SendWebRequest();
#if UNITY_2020_1_OR_NEWER
                if (req.result != UnityWebRequest.Result.Success)
#else
                if (req.isNetworkError || req.isHttpError)
#endif
                    throw new Exception($"Drive list error: {req.error}\n{req.downloadHandler.text}");

                var json = req.downloadHandler.text;
                var parsed = JsonUtilityWrapper.FromJson<DriveListResponse>(json);
                if (parsed.files != null) result.AddRange(parsed.files);
                pageToken = parsed.nextPageToken;
            }
        } while (!string.IsNullOrEmpty(pageToken));

        return result;
    }

    private async System.Threading.Tasks.Task<List<SheetInfo>> GetSpreadsheetSheets(string spreadsheetId)
    {
        var access = await GoogleOAuth.GetAccessTokenAsync();
        string url =
            $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}" + // ← 오타 주의: 변수명
            $"?fields=sheets(properties(sheetId,title))";

        using (var req = UnityWebRequest.Get(url))
        {
            AddAuth(req, access);
            await req.SendWebRequest();
#if UNITY_2020_1_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
                throw new Exception($"Sheets meta error: {req.error}\n{req.downloadHandler.text}");

            var json = req.downloadHandler.text;
            var parsed = JsonUtilityWrapper.FromJson<SpreadsheetMeta>(json);
            var list = new List<SheetInfo>();
            if (parsed.sheets != null)
            {
                foreach (var s in parsed.sheets)
                {
                    list.Add(new SheetInfo { sheetId = s.properties.sheetId, title = s.properties.title });
                }
            }
            return list;
        }
    }

    // ─ 핵심: docs.google.com + gid 로 탭별 CSV 정확히 가져오기 ─
    private async System.Threading.Tasks.Task<string> ExportSheetCsv(string spreadsheetId, int sheetGid)
    {
        var access = await GoogleOAuth.GetAccessTokenAsync();

        // docs.google.com은 Authorization 헤더를 무시/쿠키 기반으로 처리할 수 있어
        // 쿼리스트링 access_token 전달이 가장 안정적.
        string url =
            $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/export?format=csv&gid={sheetGid}&access_token={UnityWebRequest.EscapeURL(access)}";

        using (var req = UnityWebRequest.Get(url))
        {
            await req.SendWebRequest();
#if UNITY_2020_1_OR_NEWER
            if (req.result != UnityWebRequest.Result.Success)
#else
            if (req.isNetworkError || req.isHttpError)
#endif
            {
                Debug.LogWarning($"CSV export 실패 (id={spreadsheetId}, gid={sheetGid}): {req.error}\n{req.downloadHandler.text}");
                return null;
            }
            return req.downloadHandler.text;
        }
    }

    // ─ DTOs ─
    [Serializable] private class DriveListResponse { public string nextPageToken; public List<DriveFile> files; }
    [Serializable] private class DriveFile { public string id; public string name; }
    [Serializable] private class SpreadsheetMeta { public List<SheetMetaItem> sheets; }
    [Serializable] private class SheetMetaItem { public SheetProperties properties; }
    [Serializable] private class SheetProperties { public int sheetId; public string title; }
    private struct SheetInfo { public int sheetId; public string title; }

    private static class JsonUtilityWrapper
    {
        public static T FromJson<T>(string json) => JsonUtility.FromJson<T>(json);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// GoogleOAuth (Web Application + client_secret 사용, 고정 Redirect URI)
// ─────────────────────────────────────────────────────────────────────────────
[Serializable]
class OAuthToken
{
    public string access_token;
    public string refresh_token;
    public int expires_in;
    public long expires_at_utc; // unix epoch seconds
    public string token_type;
}

static class GoogleOAuth
{
    // Redirect (GCP Authorized redirect URIs에 정확히 등록 필요)
    private const string FIXED_REDIRECT = "http://localhost:8080/oauth2/callback";
    private const int    FIXED_PORT = 8080;
    private const string FIXED_PATH = "/oauth2/callback";

    private const string TOKEN_KEY = "SheetsJsonExporter_OAuthToken";

    // 스코프
    private static readonly string[] SCOPES = new[]{
        "https://www.googleapis.com/auth/drive.readonly",
        "https://www.googleapis.com/auth/spreadsheets.readonly"
    };

    // ─ 비밀 로드: 환경변수 → 로컬 JSON → EditorPrefs ─
    // 환경변수 이름
    private const string ENV_ID = "SHEETS_OAUTH_CLIENT_ID";
    private const string ENV_SECRET = "SHEETS_OAUTH_CLIENT_SECRET";
    // 로컬 JSON 경로(버전관리 제외 권장): ProjectSettings/sheets_oauth.local.json
    private static string LocalJsonPath =>
        Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, "ProjectSettings", "sheets_oauth.local.json");
    // EditorPrefs 키
    private const string PREF_ID = "SHEETS_OAUTH_CLIENT_ID";
    private const string PREF_SECRET = "SHEETS_OAUTH_CLIENT_SECRET";

    private static (string clientId, string clientSecret) LoadClientConfig()
    {
        // 1) 환경변수
        var id = Environment.GetEnvironmentVariable(ENV_ID);
        var secret = Environment.GetEnvironmentVariable(ENV_SECRET);
        if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(secret))
            return (id, secret);

        // 2) 로컬 JSON
        try
        {
            if (File.Exists(LocalJsonPath))
            {
                var txt = File.ReadAllText(LocalJsonPath, Encoding.UTF8);
                var obj = JsonUtility.FromJson<LocalOAuthJson>(txt);
                if (obj != null && !string.IsNullOrEmpty(obj.client_id) && !string.IsNullOrEmpty(obj.client_secret))
                    return (obj.client_id, obj.client_secret);
            }
        }
        catch (Exception e) { Debug.LogWarning($"로컬 OAuth JSON 로드 실패: {e.Message}"); }

        // 3) EditorPrefs
        id = EditorPrefs.GetString(PREF_ID, "");
        secret = EditorPrefs.GetString(PREF_SECRET, "");
        if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(secret))
            return (id, secret);

        return ("", "");
    }

    public static bool HasToken()
    {
        var json = EditorPrefs.GetString(TOKEN_KEY, "");
        if (string.IsNullOrEmpty(json)) return false;
        var t = JsonUtility.FromJson<OAuthToken>(json);
        return t != null && !IsExpired(t);
    }
    public static void ClearToken() => EditorPrefs.DeleteKey(TOKEN_KEY);

    public static async System.Threading.Tasks.Task<string> GetAccessTokenAsync(bool forceInteractive = false)
    {
        var (CLIENT_ID, CLIENT_SECRET) = LoadClientConfig();
        EnsureClientConfigured(CLIENT_ID, CLIENT_SECRET);

        var tok = LoadToken();
        if (!forceInteractive && tok != null && !IsExpired(tok))
            return tok.access_token;

        if (tok != null && !string.IsNullOrEmpty(tok.refresh_token))
        {
            var refreshed = await RefreshAsync(CLIENT_ID, CLIENT_SECRET, tok.refresh_token);
            if (!string.IsNullOrEmpty(refreshed.access_token))
            {
                SaveToken(refreshed);
                return refreshed.access_token;
            }
        }

        var newTok = await RunWebClientPkceAsync(CLIENT_ID, CLIENT_SECRET);
        SaveToken(newTok);
        return newTok.access_token;
    }

    private static void EnsureClientConfigured(string id, string secret)
    {
        if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(secret))
            throw new Exception(
                "OAuth CLIENT_ID/CLIENT_SECRET이 설정되지 않았습니다.\n" +
                "- 환경변수: SHEETS_OAUTH_CLIENT_ID / SHEETS_OAUTH_CLIENT_SECRET\n" +
                "- 또는 로컬파일(ProjectSettings/sheets_oauth.local.json): {\"client_id\":\"...\",\"client_secret\":\"...\"}\n" +
                "- 또는 Unity Edit > Preferences > Sheets OAuth에서 설정하세요."
            );
    }

    private static async System.Threading.Tasks.Task<OAuthToken> RunWebClientPkceAsync(string CLIENT_ID, string CLIENT_SECRET)
    {
        string codeVerifier = GenerateCodeVerifier();
        string codeChallenge = Base64Url(Sha256(codeVerifier));
        string codeChallengeMethod = "S256";

        string authUrl =
            "https://accounts.google.com/o/oauth2/v2/auth"
            + "?response_type=code"
            + $"&client_id={UnityWebRequest.EscapeURL(CLIENT_ID)}"
            + $"&redirect_uri={UnityWebRequest.EscapeURL(FIXED_REDIRECT)}"
            + $"&scope={UnityWebRequest.EscapeURL(string.Join(" ", SCOPES))}"
            + $"&code_challenge={UnityWebRequest.EscapeURL(codeChallenge)}"
            + $"&code_challenge_method={UnityWebRequest.EscapeURL(codeChallengeMethod)}"
            + "&access_type=offline"
            + "&prompt=consent";

        Application.OpenURL(authUrl);
        string code = await WaitAuthorizationCodeAsyncFixed(FIXED_PORT, FIXED_PATH);

        var token = await ExchangeCodeForTokenAsync_WebClient(CLIENT_ID, CLIENT_SECRET, code, FIXED_REDIRECT, codeVerifier);
        token.expires_at_utc = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + token.expires_in;
        return token;
    }

    private static async System.Threading.Tasks.Task<string> WaitAuthorizationCodeAsyncFixed(int port, string expectedPath)
    {
        var listener = new TcpListener(System.Net.IPAddress.Loopback, port);
        listener.Start();
        try
        {
            using (var client = await listener.AcceptTcpClientAsync())
            using (var stream = client.GetStream())
            using (var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, leaveOpen: true))
            using (var writer = new StreamWriter(stream, new UTF8Encoding(false), 1024, leaveOpen: true) { NewLine = "\r\n", AutoFlush = true })
            {
                string requestLine = await reader.ReadLineAsync(); // "GET /oauth2/callback?code=... HTTP/1.1"
                if (string.IsNullOrEmpty(requestLine)) throw new Exception("빈 요청");

                string line; while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync())) { /* skip */ }

                string code = null;
                int sp1 = requestLine.IndexOf(' ');
                int sp2 = requestLine.IndexOf(' ', sp1 + 1);
                if (sp1 > 0 && sp2 > sp1)
                {
                    var pathWithQuery = requestLine.Substring(sp1 + 1, sp2 - sp1 - 1);
                    var qm = pathWithQuery.IndexOf('?');
                    var pathOnly = qm >= 0 ? pathWithQuery.Substring(0, qm) : pathWithQuery;
                    if (!string.Equals(pathOnly, expectedPath, StringComparison.Ordinal))
                        throw new Exception($"리다이렉트 경로 불일치: {pathOnly} != {expectedPath}");

                    if (qm >= 0 && qm < pathWithQuery.Length - 1)
                    {
                        var query = pathWithQuery.Substring(qm + 1);
                        foreach (var kv in query.Split('&'))
                        {
                            var parts = kv.Split(new[] { '=' }, 2);
                            if (parts.Length == 2 && parts[0] == "code")
                            { code = Uri.UnescapeDataString(parts[1]); break; }
                        }
                    }
                }

                string html = "<html><body>인증이 완료되었습니다. Unity 에디터로 돌아가세요.</body></html>";
                string response =
                    "HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=UTF-8\r\n" +
                    $"Content-Length: {Encoding.UTF8.GetByteCount(html)}\r\nConnection: close\r\n\r\n{html}";
                await writer.WriteAsync(response);

                if (string.IsNullOrEmpty(code)) throw new Exception("Authorization code 없음");
                return code;
            }
        }
        finally { listener.Stop(); }
    }

    private static async System.Threading.Tasks.Task<OAuthToken> ExchangeCodeForTokenAsync_WebClient(
        string CLIENT_ID, string CLIENT_SECRET,
        string code, string redirectUri, string codeVerifier)
    {
        var url = "https://oauth2.googleapis.com/token";
        var form = new WWWForm();
        form.AddField("client_id", CLIENT_ID);
        form.AddField("client_secret", CLIENT_SECRET);
        form.AddField("grant_type", "authorization_code");
        form.AddField("code", code);
        form.AddField("redirect_uri", redirectUri);
        form.AddField("code_verifier", codeVerifier);

        using var req = UnityWebRequest.Post(url, form);
        await req.SendWebRequest();
#if UNITY_2020_1_OR_NEWER
        if (req.result != UnityWebRequest.Result.Success)
#else
        if (req.isNetworkError || req.isHttpError)
#endif
            throw new Exception($"토큰 교환 실패: {req.error}\n{req.downloadHandler.text}");

        var tok = JsonUtility.FromJson<OAuthToken>(req.downloadHandler.text);
        return tok;
    }

    private static OAuthToken LoadToken()
    {
        var json = EditorPrefs.GetString(TOKEN_KEY, "");
        return string.IsNullOrEmpty(json) ? null : JsonUtility.FromJson<OAuthToken>(json);
    }
    private static void SaveToken(OAuthToken t)
    {
        EditorPrefs.SetString(TOKEN_KEY, JsonUtility.ToJson(t));
    }
    private static bool IsExpired(OAuthToken t) =>
        (DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60) >= t.expires_at_utc;

    private static async System.Threading.Tasks.Task<OAuthToken> RefreshAsync(string CLIENT_ID, string CLIENT_SECRET, string refreshToken)
    {
        var url = "https://oauth2.googleapis.com/token";
        var form = new WWWForm();
        form.AddField("client_id", CLIENT_ID);
        form.AddField("client_secret", CLIENT_SECRET);
        form.AddField("grant_type", "refresh_token");
        form.AddField("refresh_token", refreshToken);

        using var req = UnityWebRequest.Post(url, form);
        await req.SendWebRequest();
#if UNITY_2020_1_OR_NEWER
        if (req.result != UnityWebRequest.Result.Success)
#else
        if (req.isNetworkError || req.isHttpError)
#endif
        {
            Debug.LogWarning($"토큰 갱신 실패: {req.error}\n{req.downloadHandler.text}");
            return new OAuthToken();
        }

        var txt = req.downloadHandler.text;
        var tok = JsonUtility.FromJson<OAuthToken>(txt);
        if (!string.IsNullOrEmpty(tok.access_token))
        {
            tok.refresh_token = string.IsNullOrEmpty(tok.refresh_token) ? refreshToken : tok.refresh_token;
            tok.expires_at_utc = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + tok.expires_in;
        }
        return tok;
    }

    // 유틸(PKCE/인코딩)
    private static string GenerateCodeVerifier()
    {
        var bytes = new byte[32];
        using (var rnd = RandomNumberGenerator.Create()) rnd.GetBytes(bytes);
        return Base64Url(bytes);
    }
    private static byte[] Sha256(string s)
    {
        using var sha = SHA256.Create();
        return sha.ComputeHash(Encoding.ASCII.GetBytes(s));
    }
    private static string Base64Url(byte[] input) =>
        Convert.ToBase64String(input).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    [Serializable]
    private class LocalOAuthJson { public string client_id; public string client_secret; }
}

// ─────────────────────────────────────────────────────────────────────────────
// 간단한 OAuth 설정 에디터 윈도우 (EditorPrefs & 로컬 JSON 저장)
// ─────────────────────────────────────────────────────────────────────────────
public class SheetsOAuthSettingsWindow : EditorWindow
{
    private string clientId = "";
    private string clientSecret = "";
    private bool saveToLocalJson = true;

    public static void Open()
    {
        var w = GetWindow<SheetsOAuthSettingsWindow>("Sheets OAuth Settings");
        w.minSize = new Vector2(520, 220);
        w.Show();
    }

    void OnEnable()
    {
        clientId = EditorPrefs.GetString("SHEETS_OAUTH_CLIENT_ID", "");
        clientSecret = EditorPrefs.GetString("SHEETS_OAUTH_CLIENT_SECRET", "");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("OAuth Client 설정 (코드에 하드코딩 금지!)", EditorStyles.boldLabel);
        clientId = EditorGUILayout.TextField("CLIENT_ID", clientId);
        clientSecret = EditorGUILayout.TextField("CLIENT_SECRET", clientSecret);

        GUILayout.Space(8);
        saveToLocalJson = EditorGUILayout.ToggleLeft(
            new GUIContent("ProjectSettings/sheets_oauth.local.json에도 저장(버전관리 제외 권장)"), saveToLocalJson);

        GUILayout.Space(12);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("저장", GUILayout.Height(28)))
            {
                EditorPrefs.SetString("SHEETS_OAUTH_CLIENT_ID", clientId);
                EditorPrefs.SetString("SHEETS_OAUTH_CLIENT_SECRET", clientSecret);

                if (saveToLocalJson)
                {
                    try
                    {
                        var root = Directory.GetParent(Application.dataPath)!.FullName;
                        var path = Path.Combine(root, "ProjectSettings", "sheets_oauth.local.json");
                        var json = $"{{\"client_id\":\"{Escape(clientId)}\",\"client_secret\":\"{Escape(clientSecret)}\"}}";
                        File.WriteAllText(path, json, new UTF8Encoding(false));
                        Debug.Log($"Saved local OAuth JSON: {path}");
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"로컬 JSON 저장 실패: {e}");
                    }
                }

                Close();
            }

            if (GUILayout.Button("닫기", GUILayout.Height(28)))
                Close();
        }

        EditorGUILayout.HelpBox(
            "로드 우선순위: 환경변수 → 로컬 JSON(ProjectSettings/sheets_oauth.local.json) → EditorPrefs\n" +
            "이 파일(ProjectSettings/sheets_oauth.local.json)은 .gitignore에 추가하세요.",
            MessageType.Info);
    }

    private static string Escape(string s) =>
        (s ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
}
#endif

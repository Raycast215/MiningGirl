#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Scene.MainGameScene;
using Scene.MainGameScene.Battle;
using UnityEditor;
using UnityEngine;

// 아트가 요청한 화면을 판이 도는 동안 자동으로 잡습니다.
//
// "판 보다가 이런 화면이 나오면 알려 주십시오"는 운에 맡기는 것입니다. 조건을
// 코드로 걸고 맞는 프레임에서 찍으면 최악 화면이 잡히고, 안 잡히면 그것도 답입니다
// - 조건을 걸고 전 프레임을 훑어 0회면 표본이 아니라 전수입니다.
//
// 즉석으로 짜지 않고 파일로 둔 이유가 둘입니다.
//   조건을 매번 다시 쓰다가 틀렸습니다. 무조준만 보려는데 "생존 1마리 이하"로
//   걸어서 그 한 마리를 조준한 발이 섞였고, 아트가 각도를 재서 잡아냈습니다.
//   그리고 execute_code로 건 콜백은 도메인 리로드에 사라져 판 중간에 끊깁니다.
public static class CaptureConditions
{
    private const string EnabledKey = "MiningGirl.Capture.Enabled";
    private const string CountKeyPrefix = "MiningGirl.Capture.Count.";

    // 조건이 몇 판 동안 살아 있었는지.
    //
    // 0회를 근거로 쓰려면 이 값이 있어야 합니다. "안 나왔다"와 "안 봤다"는
    // 조건이 도중에 끊기면 구분이 안 됩니다 - 광재 열두 판 0회를 근거로 쓴 적이
    // 있는데, 그때 조건이 내내 살아 있었는지는 확인 안 했습니다. 운이 좋았습니다.
    private const string RunsKey = "MiningGirl.Capture.Runs";

    // 판이 바뀐 것을 알아보는 데 씁니다. 다시하기가 씬을 다시 부르므로
    // 컨트롤러 인스턴스가 새로 생깁니다.
    private const string LastControllerKey = "MiningGirl.Capture.LastController";

    // 조건마다 몇 장까지 찍을지. 같은 화면을 계속 찍어도 새로 아는 게 없습니다.
    private const int ShotsPerCondition = 3;

    private static readonly BindingFlags Hidden = BindingFlags.NonPublic | BindingFlags.Instance;

    private static string _folder;

    [MenuItem("Tools/MainGame/Capture Conditions 시작")]
    public static void Begin()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("[캡처조건] 플레이 모드에서만 씁니다.");

            return;
        }

        _folder = Path.Combine(Application.dataPath, "..", "..", "..", "Captures");
        Directory.CreateDirectory(_folder);

        SessionState.SetBool(EnabledKey, true);
        SessionState.SetInt(RunsKey, 0);
        SessionState.SetInt(LastControllerKey, 0);

        EditorApplication.update -= Tick;
        EditorApplication.update += Tick;

        Debug.Log($"[캡처조건] 시작. 저장 위치 {Path.GetFullPath(_folder)}");
    }

    [MenuItem("Tools/MainGame/Capture Conditions 정지")]
    public static void Stop()
    {
        SessionState.SetBool(EnabledKey, false);
        EditorApplication.update -= Tick;

        Report();
    }

    // 조건별로 몇 장 찍혔는지, 그리고 몇 판을 봤는지 한 번에 적습니다.
    //
    // 0장이 "안 나온다"의 근거가 되려면 분모가 같이 있어야 합니다.
    [MenuItem("Tools/MainGame/Capture Conditions 결과")]
    public static void Report()
    {
        var runs = SessionState.GetInt(RunsKey, 0);
        var keys = new[] { "stray_volley", "tower_health_overlap", "explosion", "slime_behind_stage", "slag_cluster" };

        var report = new System.Text.StringBuilder();

        report.AppendLine($"[캡처조건] 결과 - {runs}판 동안 조건이 살아 있었습니다");

        for (var i = 0; i < keys.Length; i++)
        {
            var shots = SessionState.GetInt(CountKeyPrefix + keys[i], 0);

            report.AppendLine($"  {keys[i],-22} {shots}장");
        }

        Debug.Log(report.ToString());
    }

    private static void Tick()
    {
        if (!Application.isPlaying || !SessionState.GetBool(EnabledKey, false))
        {
            EditorApplication.update -= Tick;

            return;
        }

        var controller = Object.FindObjectOfType<MainGameController>();

        if (controller == null)
            return;

        // 판이 바뀌면 셉니다. 0회를 근거로 쓸 때의 분모입니다.
        var id = controller.GetInstanceID();

        if (SessionState.GetInt(LastControllerKey, 0) != id)
        {
            SessionState.SetInt(LastControllerKey, id);
            SessionState.SetInt(RunsKey, SessionState.GetInt(RunsKey, 0) + 1);
        }

        // 복원된 판은 처음부터 돈 판이 아니라 화면도 대표성이 없습니다.
        //
        // 저장·복원이 밸런스 측정만 오염시키는 게 아닙니다 - 경과 223초짜리 거의
        // 끝난 판이 복원된 적이 있는데, 그 화면은 그 스테이지의 대표 화면이 아닙니다.
        if (controller.DebugIsRestored)
            return;

        var field = typeof(MainGameController).GetField("_field", Hidden)?.GetValue(controller);
        var bounds = typeof(MainGameController).GetField("_bounds", Hidden)?.GetValue(controller);

        if (field == null || bounds == null)
            return;

        var alive = (int)field.GetType().GetProperty("AliveCount").GetValue(field, null);
        var monsters = Object.FindObjectsOfType<MonsterUnit>();

        var stage = typeof(MainGameController).GetField("_stage", Hidden)?.GetValue(controller) as Data.StageDataTableRow;

        TryStrayVolley(alive);
        TryTowerHealthOverlap(monsters);
        TryExplosionOverVein(stage);
        TrySlimeBehindStageText(monsters);
        TrySlagCluster(monsters);
    }

    // 무조준 발사만 있는 프레임.
    //
    // 생존 0이어야 합니다. 한 마리라도 살아 있으면 그 한 마리를 조준한 발이 섞여
    // 부채 범위 밖의 각도가 나오고, 그 프레임으로 폭을 판정하면 틀립니다.
    //
    // 다만 생존 0이어도 한 프레임이 한 볼리는 아닙니다. 0.2초 간격으로 나가고
    // 발사체가 오래 살아서 앞 볼리가 남은 채로 다음 볼리가 나갑니다. 그래서 각도를
    // 같이 남깁니다 - 간격이 일정한 것끼리가 한 볼리입니다.
    private static void TryStrayVolley(int alive)
    {
        if (alive != 0)
            return;

        var projectiles = Object.FindObjectsOfType<Projectile>();

        if (projectiles.Length < 3)
            return;

        if (!Take("stray_volley", projectiles.Length))
            return;

        var field = typeof(Projectile).GetField("_direction", Hidden);
        var angles = new List<string>();

        foreach (var projectile in projectiles)
        {
            var direction = (Vector3)field.GetValue(projectile);

            angles.Add((Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg).ToString("0.0"));
        }

        Shoot("stray_volley", $"무조준 {projectiles.Length}발 생존0  수직에서 {string.Join(" / ", angles)}  [판정] 보류 - 무조준 비율이 내려간 뒤 \"어수선한가\"로 봅니다. 간격이 일정한 것끼리가 한 볼리입니다");
    }

    // 몬스터가 타워 체력바에 겹친 프레임.
    //
    // 경험치 막대보다 오래 겹칩니다 - 거기는 지나가는데 여기는 멈춰 섭니다.
    // 그리고 몰릴 때 겹치는데, 그때가 그 숫자를 제일 봐야 할 때입니다.
    private static void TryTowerHealthOverlap(MonsterUnit[] monsters)
    {
        var track = FindGaugeTrack("TowerHealth");

        if (track == null)
            return;

        var canvas = track.GetComponentInParent<Canvas>();
        var corners = new Vector3[4];
        track.rectTransform.GetWorldCorners(corners);

        var camera = Camera.main;
        var bottom = camera.ScreenToWorldPoint(
            new Vector3(0f, RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[0]).y, 10f)).y;
        var top = camera.ScreenToWorldPoint(
            new Vector3(0f, RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[1]).y, 10f)).y;

        var overlapping = 0;

        for (var i = 0; i < monsters.Length; i++)
        {
            var y = monsters[i].transform.position.y;

            if (y >= bottom && y <= top)
                overlapping++;
        }

        if (overlapping == 0 || !Take("tower_health_overlap", overlapping))
            return;

        Shoot("tower_health_overlap", $"타워 체력바에 겹친 몬스터 {overlapping}마리 (월드 y {bottom:0.0}~{top:0.0})  [판정] 몬스터가 체력바에 깨끗하게 잘리는가. 안 잘리면 trackColor 알파 1.0");
    }

    // 폭발이 광맥 위에서 터지는 프레임.
    //
    // 스테이지 5에서만 찍습니다. 잉걸 광맥이 5번 배경이라, 다른 스테이지에서 찍으면
    // 파일은 쌓이는데 볼 게 없습니다 - 아트가 볼 것은 "폭발의 어두운 가장자리가
    // 잉걸 광맥과 갈라지는가"입니다.
    private static void TryExplosionOverVein(Data.StageDataTableRow stage)
    {
        if (stage == null || stage.Id != "Stage_05")
            return;

        var effects = Object.FindObjectsOfType<OneShotEffect>();

        if (effects.Length == 0 || !Take("explosion", effects.Length))
            return;

        Shoot("explosion", $"폭발 {effects.Length}개  [판정] 폭발의 어두운 가장자리(루마 71.7)가 잉걸 광맥(106)과 갈라지는가");
    }

    // 광석 슬라임이 STAGE 글자 뒤를 지나는 프레임.
    //
    // 글자가 화면 왼쪽 끝인데 스폰 x는 몸통이 화면 밖으로 안 나가게 제한되므로
    // 잘 안 생깁니다. 조건을 넓혀 억지로 만들지 않습니다 - 안 잡히면 "게임이
    // 그 화면을 거의 안 만든다"가 답입니다.
    private static void TrySlimeBehindStageText(MonsterUnit[] monsters)
    {
        var text = FindText("StageText");

        if (text == null)
            return;

        var canvas = text.GetComponentInParent<Canvas>();
        var corners = new Vector3[4];
        text.rectTransform.GetWorldCorners(corners);

        var camera = Camera.main;
        var screenLeft = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[0]);
        var screenRight = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, corners[2]);

        var left = camera.ScreenToWorldPoint(new Vector3(screenLeft.x, screenLeft.y, 10f));
        var right = camera.ScreenToWorldPoint(new Vector3(screenRight.x, screenRight.y, 10f));

        for (var i = 0; i < monsters.Length; i++)
        {
            if (monsters[i].Row.Id != "Monster_001")
                continue;

            var p = monsters[i].transform.position;

            if (p.x < left.x || p.x > right.x || p.y < left.y || p.y > right.y)
                continue;

            if (!Take("slime_behind_stage", 1))
                return;

            Shoot("slime_behind_stage", $"광석 슬라임이 STAGE 글자 뒤 ({p.x:0.0}, {p.y:0.0})  [판정] 폰트 36에서 외곽선 0.6px이 밝은 초록 뒤에서 버티는가. WAVE(폰트 56)는 통과했습니다");

            return;
        }
    }

    // 광재 둘이 몸 폭 안으로 붙은 프레임.
    //
    // 아트가 닫은 항목이지만 트리거는 남깁니다. 밸런스가 바뀌어 화면이 붐비면
    // 저절로 걸립니다.
    private static void TrySlagCluster(MonsterUnit[] monsters)
    {
        for (var i = 0; i < monsters.Length; i++)
        {
            if (monsters[i].Row.Id != "Monster_007")
                continue;

            for (var k = i + 1; k < monsters.Length; k++)
            {
                if (monsters[k].Row.Id != "Monster_007")
                    continue;

                var distance = Vector3.Distance(monsters[i].transform.position, monsters[k].transform.position);
                var bodyWidth = monsters[i].BodyRadius * 2f;

                if (distance > bodyWidth)
                    continue;

                if (!Take("slag_cluster", 1))
                    return;

                Shoot("slag_cluster", $"광재 둘 거리 {distance:0.00} (몸 폭 {bodyWidth:0.00})  [판정] 균열 무리 개수로 몇 마리인지 세어지는가");

                return;
            }
        }
    }

    // 조건별로 정해진 장수까지만, 그리고 이전보다 나은 화면일 때만 찍습니다.
    //
    // score를 두는 이유는 첫 프레임이 최악이 아니기 때문입니다. 발사체 3개짜리를
    // 찍고 끝내면 6개짜리를 못 봅니다.
    private static bool Take(string key, int score)
    {
        var shots = SessionState.GetInt(CountKeyPrefix + key, 0);

        if (shots >= ShotsPerCondition)
            return false;

        var best = SessionState.GetInt(CountKeyPrefix + key + ".best", 0);

        if (score <= best)
            return false;

        SessionState.SetInt(CountKeyPrefix + key + ".best", score);
        SessionState.SetInt(CountKeyPrefix + key, shots + 1);

        return true;
    }

    private static void Shoot(string key, string note)
    {
        var shots = SessionState.GetInt(CountKeyPrefix + key, 0);
        var path = Path.Combine(_folder, $"{key}_{shots}.png");

        ScreenCapture.CaptureScreenshot(path);

        Debug.Log($"[캡처조건] {key} {shots}장째 - {note}");
    }

    private static UnityEngine.UI.Image FindGaugeTrack(string gaugeName)
    {
        foreach (var image in Object.FindObjectsOfType<UnityEngine.UI.Image>(true))
        {
            if (image.name != "Track")
                continue;

            if (image.transform.parent != null && image.transform.parent.name == gaugeName)
                return image;
        }

        return null;
    }

    private static TMPro.TextMeshProUGUI FindText(string name)
    {
        foreach (var text in Object.FindObjectsOfType<TMPro.TextMeshProUGUI>(true))
        {
            if (text.name == name)
                return text;
        }

        return null;
    }
}
#endif

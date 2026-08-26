using Scene.MainGameScene.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// 인게임 한 판을 사람 손 없이 빠르게 돌려 보는 테스트 도구입니다.
//
// 한 스테이지가 4~5분이라 밸런스를 확인할 때마다 그만큼 앉아 있을 수 없습니다.
// 배속을 걸고 3택이 뜨면 자동으로 하나 골라 끝까지 진행시킵니다.
//
// 에디터 전용이고 게임 코드에는 손을 대지 않습니다.
public static class InGameAutoPlayTester
{
    private const string SpeedKey = "MiningGirl.AutoPlay.Speed";

    private static bool _running;
    private static float _speed = 10f;

    // 3택은 timeScale을 0으로 만들고, 고르면 1로 되돌립니다.
    // 그래서 배속은 매 프레임 다시 걸어 줘야 유지됩니다.
    public static bool IsRunning => _running;

    [MenuItem("Tools/MainGame/Auto Play x10")]
    public static void StartFast()
    {
        Begin(10f);
    }

    [MenuItem("Tools/MainGame/Auto Play x1")]
    public static void StartNormal()
    {
        Begin(1f);
    }

    [MenuItem("Tools/MainGame/Auto Play Stop")]
    public static void Stop()
    {
        _running = false;
        EditorApplication.update -= Tick;

        if (Application.isPlaying && Time.timeScale > 0f)
            Time.timeScale = 1f;

        Debug.Log("[AutoPlay] 정지");
    }

    public static void Begin(float speed)
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("[AutoPlay] 플레이 모드에서만 씁니다.");

            return;
        }

        _speed = Mathf.Clamp(speed, 0.1f, 20f);
        EditorPrefs.SetFloat(SpeedKey, _speed);

        if (_running)
            return;

        _running = true;
        EditorApplication.update += Tick;

        Debug.Log($"[AutoPlay] 시작 x{_speed}");
    }

    private static void Tick()
    {
        if (!Application.isPlaying)
        {
            Stop();

            return;
        }

        // 3택이 떠 있는지는 카드 버튼이 살아 있는지로 봅니다.
        // 오버레이가 꺼져 있으면 activeInHierarchy가 아니라 하나도 잡히지 않습니다.
        var choiceUI = Object.FindObjectOfType<LevelUpChoiceUI>(true);

        if (choiceUI != null && PickRandomChoice(choiceUI))
            return;

        // 결과 화면이 뜨면 더 진행할 게 없습니다.
        var result = Object.FindObjectOfType<StageResultUI>(true);

        if (result != null && IsResultShowing(result))
        {
            Stop();

            return;
        }

        if (Time.timeScale > 0f)
            Time.timeScale = _speed;
    }

    private static bool IsResultShowing(StageResultUI result)
    {
        var so = new SerializedObject(result);
        var root = so.FindProperty("root").objectReferenceValue as GameObject;

        return root != null && root.activeSelf;
    }

    private static bool PickRandomChoice(LevelUpChoiceUI choiceUI)
    {
        // GetComponentsInChildren(false)를 꺼져 있는 오브젝트에서 부르면
        // activeInHierarchy가 아니라 자식의 activeSelf로 걸러져 꺼진 카드까지 딸려 옵니다.
        // 그대로 누르면 숨어 있는 3택을 계속 고르게 되므로 직접 확인합니다.
        if (!choiceUI.gameObject.activeInHierarchy)
            return false;

        var buttons = choiceUI.GetComponentsInChildren<Button>(false);
        var active = new System.Collections.Generic.List<Button>();

        foreach (var button in buttons)
        {
            if (button.gameObject.activeInHierarchy)
                active.Add(button);
        }

        if (active.Count == 0)
            return false;

        active[Random.Range(0, active.Count)].onClick.Invoke();

        return true;
    }
}

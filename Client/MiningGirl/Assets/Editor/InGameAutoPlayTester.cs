using System.Collections.Generic;
using Scene.MainGameScene;
using Scene.MainGameScene.Progress;
using Scene.MainGameScene.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// 인게임 한 판을 사람 손 없이 빠르게 돌려 보는 테스트 도구입니다.
//
// 한 스테이지가 4~5분이라 밸런스를 확인할 때마다 그만큼 앉아 있을 수 없습니다.
// 배속을 걸고 3택이 뜨면 자동으로 하나 골라 끝까지 진행시킵니다.
//
// 고르는 방식이 둘입니다.
//   집중   한 스킬에 위력과 발사체를 몰아주고, 화력 카드가 안 뜨면 다시 뽑습니다
//   무작위 아무거나 고릅니다
//
// 둘을 나눠 둔 이유는 결과가 갈리기 때문입니다. 위력은 곱연산이고 발사체는 그
// 위에 곱해지므로, 한 스킬에 몰면 두 축이 곱으로 커지고 나눠 가지면 각각 선형입니다.
// 실측에서 같은 레벨 예산으로 화력이 4.6배 갈렸습니다.
//
// 에디터 전용이고 게임 코드에는 손을 대지 않습니다.
public static class InGameAutoPlayTester
{
    private const string SpeedKey = "MiningGirl.AutoPlay.Speed";

    // 위력을 이만큼 넣기 전에는 위력이 없는 3택을 다시 뽑습니다.
    //
    // 날벌레(체력 17)를 한 발에 죽이려면 위력 강화가 2회 필요합니다.
    // 12 x 1.2^2 = 17.28. 두 발이 필요한 구간이 길어지면 초반이 밀립니다.
    private const int DamageFloor = 2;

    private static bool _running;
    private static bool _focused = true;
    private static float _speed = 10f;

    public static bool IsRunning => _running;

    [MenuItem("Tools/MainGame/Auto Play x10 (집중)")]
    public static void StartFocusedFast()
    {
        Begin(10f, true);
    }

    [MenuItem("Tools/MainGame/Auto Play x1 (집중)")]
    public static void StartFocusedNormal()
    {
        Begin(1f, true);
    }

    [MenuItem("Tools/MainGame/Auto Play x10 (무작위)")]
    public static void StartRandomFast()
    {
        Begin(10f, false);
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

    public static void Begin(float speed, bool focused)
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("[AutoPlay] 플레이 모드에서만 씁니다.");

            return;
        }

        _speed = Mathf.Clamp(speed, 0.1f, 20f);
        _focused = focused;
        EditorPrefs.SetFloat(SpeedKey, _speed);

        if (_running)
            return;

        _running = true;
        EditorApplication.update += Tick;

        Debug.Log($"[AutoPlay] 시작 x{_speed} ({(focused ? "집중" : "무작위")})");
    }

    private static void Tick()
    {
        if (!Application.isPlaying)
        {
            Stop();

            return;
        }

        if (HandleChoice())
            return;

        // 결과 화면이 뜨면 더 진행할 게 없습니다.
        var result = Object.FindObjectOfType<StageResultUI>(true);

        if (result != null && IsResultShowing(result))
        {
            Stop();

            return;
        }

        // 3택은 timeScale을 0으로 만들고, 고르면 1로 되돌립니다.
        // 그래서 배속은 매 프레임 다시 걸어 줘야 유지됩니다.
        if (Time.timeScale > 0f)
            Time.timeScale = _speed;
    }

    private static bool HandleChoice()
    {
        var controller = Object.FindObjectOfType<MainGameController>();

        // 컨트롤러를 못 잡으면 예전처럼 버튼만 보고 무작위로 누릅니다.
        if (controller == null)
            return PickRandomByButton();

        var viewModel = controller.DebugChoiceViewModel;

        if (viewModel == null || !viewModel.IsVisible.Value)
            return false;

        var choices = controller.DebugOpenChoices;

        if (choices == null || choices.Count == 0)
            return false;

        if (!_focused)
        {
            viewModel.Select(Random.Range(0, choices.Count));

            return true;
        }

        // 화력 카드가 없으면 다시 뽑습니다. 리롤은 정확히 이 분산을 없애라고
        // 넣은 기능인데, 봇이 안 쓰면 측정에서 그 기능이 통째로 빠집니다.
        if (viewModel.CanReroll.Value && ShouldReroll(controller, choices))
        {
            viewModel.Reroll();

            return true;
        }

        viewModel.Select(BestIndex(choices));

        return true;
    }

    private static bool ShouldReroll(MainGameController controller, IReadOnlyList<LevelUpChoice> choices)
    {
        var hasDamage = false;
        var hasProjectile = false;

        for (var i = 0; i < choices.Count; i++)
        {
            var upgrade = choices[i].Upgrade;

            if (upgrade == null)
                continue;

            if (upgrade.UpgradeType == ESkillUpgradeType.Damage)
                hasDamage = true;
            else if (upgrade.UpgradeType == ESkillUpgradeType.ProjectileCount)
                hasProjectile = true;
        }

        // 위력이 아직 바닥에 못 미쳤으면 위력만 노립니다.
        if (!hasDamage && MaxDamageCount(controller) < DamageFloor)
            return true;

        return !hasDamage && !hasProjectile;
    }

    private static int MaxDamageCount(MainGameController controller)
    {
        var skills = controller.DebugSkills;

        if (skills == null)
            return 0;

        var max = 0;

        for (var i = 0; i < skills.Count; i++)
        {
            var count = skills[i].GetUpgradeCount(ESkillUpgradeType.Damage);

            if (count > max)
                max = count;
        }

        return max;
    }

    // 강화스킬 > 위력 > 발사체 > 나머지 강화 > 신규 획득.
    //
    // 신규 획득이 맨 아래인 이유는 나눠 가지면 지기 때문입니다. 실측에서
    // 갈라진 빌드가 초당 19.5, 몰아준 빌드가 89.5였습니다.
    private static int BestIndex(IReadOnlyList<LevelUpChoice> choices)
    {
        var best = 0;
        var bestScore = -1;

        for (var i = 0; i < choices.Count; i++)
        {
            var score = ScoreOf(choices[i]);

            if (score <= bestScore)
                continue;

            bestScore = score;
            best = i;
        }

        return best;
    }

    private static int ScoreOf(LevelUpChoice choice)
    {
        if (choice.Type == ELevelUpChoiceType.Mastery)
            return 5;

        if (choice.Type == ELevelUpChoiceType.AcquireSkill)
            return 0;

        if (choice.Upgrade == null)
            return 1;

        switch (choice.Upgrade.UpgradeType)
        {
            case ESkillUpgradeType.Damage: return 4;
            case ESkillUpgradeType.ProjectileCount: return 3;
            default: return 2;
        }
    }

    private static bool IsResultShowing(StageResultUI result)
    {
        var so = new SerializedObject(result);
        var root = so.FindProperty("root").objectReferenceValue as GameObject;

        return root != null && root.activeSelf;
    }

    // 컨트롤러를 못 잡을 때만 쓰는 예전 경로입니다.
    //
    // GetComponentsInChildren(false)를 꺼져 있는 오브젝트에서 부르면
    // activeInHierarchy가 아니라 자식의 activeSelf로 걸러져 꺼진 카드까지 딸려 옵니다.
    // 그대로 누르면 숨어 있는 3택을 계속 고르게 되므로 직접 확인합니다.
    private static bool PickRandomByButton()
    {
        var choiceUI = Object.FindObjectOfType<LevelUpChoiceUI>(true);

        if (choiceUI == null || !choiceUI.gameObject.activeInHierarchy)
            return false;

        var buttons = choiceUI.GetComponentsInChildren<Button>(false);
        var active = new List<Button>();

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

using System.IO;
using Scene.MainGameScene;
using Scene.MainGameScene.Battle;
using Scene.MainGameScene.UI;
using TMPro;
using UI.Common;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// MainGameScene의 인게임 구성을 코드로 만듭니다.
//
// 손으로 배치하지 않고 스크립트로 둔 이유는, 레이아웃 수치가 기획 문서의
// 픽셀 값(1080x1920 기준)에서 그대로 오기 때문입니다. 문서가 바뀌면 여기 숫자만 고치고
// 다시 돌리면 됩니다.
//
// 여러 벌 쓰이는 조각(스킬 슬롯 칸, 3택 카드, 별 아이콘)은 씬에 직접 만들지 않고
// 프리팹으로 저장한 뒤 꽂습니다. 모양을 고칠 때 한 곳만 고치면 됩니다.
public static class MainGameSceneBuilder
{
    // 기획 문서의 세로 배분(1080x1920 기준, px)
    private const float TopBarHeight = 100f;
    private const float SkillSlotAreaHeight = 200f;
    private const float TowerHealthBarHeight = 80f;

    // 타워 아트에서 각목 구간의 높이(유닛). 몬스터 사거리 기준선은 그 아래 몸통 윗선입니다.
    private const float TowerPlankHeight = 0.82f;

    // 배치 y는 상수로 박지 않습니다. MainGameController가 하단 UI 띠에서 역산합니다.
    // 여기 값은 에디터에서 보기 위한 초기 위치일 뿐입니다.
    private const float TowerInitialCenterY = -9.05f;
    private const float CharacterInitialY = -7.9f;

    private const string TowerSpritePath = "Assets/Sprites/InGame/Tower/Tower_01.png";
    private const string TowerDamagedSpritePath = "Assets/Sprites/InGame/Tower/Tower_01_Damaged.png";
    private const string TowerBrokenSpritePath = "Assets/Sprites/InGame/Tower/Tower_01_Broken.png";
    private const string StarSpritePath = "Assets/Sprites/UI/Star.png";
    private const string StarEmptySpritePath = "Assets/Sprites/UI/Star_Empty.png";

    private const string UiPrefabFolder = "Assets/Prefabs/MainGame/UI";
    private const string SkillSlotPrefabPath = UiPrefabFolder + "/SkillSlot.prefab";
    private const string ChoiceCardPrefabPath = UiPrefabFolder + "/LevelUpChoiceCard.prefab";
    private const string StarIconPrefabPath = UiPrefabFolder + "/StarIcon.prefab";

    [MenuItem("Tools/MainGame/Build In-Game Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.GetActiveScene();

        if (scene.name != "MainGameScene")
        {
            Debug.LogError("[Builder] MainGameScene을 열고 실행하세요.");

            return;
        }

        var controller = Object.FindObjectOfType<MainGameController>();

        if (controller == null)
        {
            Debug.LogError("[Builder] MainGameController를 찾지 못했습니다.");

            return;
        }

        EnsureUiPrefabs();

        var camera = FixCamera();

        BuildBattleRoot(out var tower, out var characterAnchor, out var monsterLayer,
            out var projectileLayer, out var background);

        var hud = BuildHud();
        var levelUpUI = BuildLevelUpChoiceUI();
        var resultUI = BuildResultUI();

        var so = new SerializedObject(controller);
        so.FindProperty("battleCamera").objectReferenceValue = camera;
        so.FindProperty("tower").objectReferenceValue = tower;
        so.FindProperty("characterAnchor").objectReferenceValue = characterAnchor;
        so.FindProperty("monsterLayer").objectReferenceValue = monsterLayer;
        so.FindProperty("projectileLayer").objectReferenceValue = projectileLayer;
        so.FindProperty("background").objectReferenceValue = background;
        so.FindProperty("hud").objectReferenceValue = hud;
        so.FindProperty("levelUpChoiceUI").objectReferenceValue = levelUpUI;
        so.FindProperty("resultUI").objectReferenceValue = resultUI;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        Debug.Log("[Builder] 완료");
    }

#region 프리팹

    private static void EnsureUiPrefabs()
    {
        if (!Directory.Exists(UiPrefabFolder))
            Directory.CreateDirectory(UiPrefabFolder);

        AssetDatabase.Refresh();

        SavePrefab(BuildSkillSlot(), SkillSlotPrefabPath);
        SavePrefab(BuildChoiceCard(), ChoiceCardPrefabPath);
        SavePrefab(BuildStarIcon(), StarIconPrefabPath);

        AssetDatabase.SaveAssets();
    }

    private static void SavePrefab(GameObject source, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(source, path);
        Object.DestroyImmediate(source);
    }

    private static T InstantiatePrefab<T>(string path, Transform parent) where T : Component
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

        if (prefab == null)
        {
            Debug.LogError($"[Builder] 프리팹을 찾지 못했습니다: {path}");

            return null;
        }

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);

        return instance.GetComponent<T>();
    }

    private static GameObject BuildSkillSlot()
    {
        var slot = CreateImage("SkillSlot", null, new Color(0.10f, 0.10f, 0.14f, 0.85f));
        slot.rectTransform.sizeDelta = new Vector2(160f, 160f);

        var view = slot.gameObject.AddComponent<SkillSlotView>();

        var icon = CreateImage("Icon", slot.transform, Color.white);
        Inset(icon.rectTransform, 10f);
        icon.preserveAspect = true;

        // 남은 쿨다운만큼 덮는 원형 게이지.
        var cover = CreateImage("CooldownCover", slot.transform, new Color(0f, 0f, 0f, 0.65f));
        Inset(cover.rectTransform, 10f);
        cover.type = Image.Type.Filled;
        cover.fillMethod = Image.FillMethod.Radial360;
        cover.fillOrigin = (int)Image.Origin360.Top;
        cover.fillClockwise = false;

        // 아이콘이 밝으면 흰 글자가 묻히므로 어두운 판을 깔아 둡니다.
        var levelPlate = CreateImage("LevelPlate", slot.transform, new Color(0f, 0f, 0f, 0.72f));
        var plateRect = levelPlate.rectTransform;
        plateRect.anchorMin = new Vector2(1f, 0f);
        plateRect.anchorMax = new Vector2(1f, 0f);
        plateRect.pivot = new Vector2(1f, 0f);
        plateRect.sizeDelta = new Vector2(80f, 40f);
        plateRect.anchoredPosition = new Vector2(-8f, 8f);

        var levelText = CreateText("Level", levelPlate.transform, "Lv.1", 30f, FontStyles.Bold);
        Stretch(levelText.rectTransform);

        var empty = CreateImage("Empty", slot.transform, new Color(1f, 1f, 1f, 0.10f));
        Inset(empty.rectTransform, 30f);

        var so = new SerializedObject(view);
        so.FindProperty("icon").objectReferenceValue = icon;
        so.FindProperty("cooldownCover").objectReferenceValue = cover;
        so.FindProperty("levelRoot").objectReferenceValue = levelPlate.gameObject;
        so.FindProperty("levelText").objectReferenceValue = levelText;
        so.FindProperty("emptyMark").objectReferenceValue = empty.gameObject;
        so.ApplyModifiedPropertiesWithoutUndo();

        return slot.gameObject;
    }

    private static GameObject BuildChoiceCard()
    {
        var card = CreateImage("LevelUpChoiceCard", null, new Color(0.14f, 0.15f, 0.20f, 1f));
        card.rectTransform.sizeDelta = new Vector2(940f, 300f);

        var item = card.gameObject.AddComponent<LevelUpChoiceItemUI>();
        var button = card.gameObject.AddComponent<Button>();
        button.targetGraphic = card;

        var icon = CreateImage("Icon", card.transform, Color.white);
        var iconRect = icon.rectTransform;
        iconRect.anchorMin = new Vector2(0f, 0.5f);
        iconRect.anchorMax = new Vector2(0f, 0.5f);
        iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.sizeDelta = new Vector2(180f, 180f);
        iconRect.anchoredPosition = new Vector2(30f, 0f);
        icon.preserveAspect = true;

        var title = CreateText("Title", card.transform, "스킬 이름", 48f, FontStyles.Bold);
        PlaceInCard(title.rectTransform, 74f);
        title.alignment = TextAlignmentOptions.Left;

        var subtitle = CreateText("Subtitle", card.transform, "Lv.1 → Lv.2", 38f, FontStyles.Normal);
        PlaceInCard(subtitle.rectTransform, 0f);
        subtitle.alignment = TextAlignmentOptions.Left;

        var detail = CreateText("Detail", card.transform, "위력 12 → 17", 34f, FontStyles.Normal);
        PlaceInCard(detail.rectTransform, -68f);
        detail.alignment = TextAlignmentOptions.Left;
        detail.color = new Color(0.75f, 0.82f, 0.95f, 1f);

        var badge = CreateImage("NewBadge", card.transform, new Color(0.90f, 0.60f, 0.15f, 1f));
        var badgeRect = badge.rectTransform;
        badgeRect.anchorMin = new Vector2(1f, 1f);
        badgeRect.anchorMax = new Vector2(1f, 1f);
        badgeRect.pivot = new Vector2(1f, 1f);
        badgeRect.sizeDelta = new Vector2(120f, 56f);
        badgeRect.anchoredPosition = new Vector2(-20f, -20f);

        var badgeText = CreateText("Label", badge.transform, "NEW", 30f, FontStyles.Bold);
        Stretch(badgeText.rectTransform);

        var so = new SerializedObject(item);
        so.FindProperty("button").objectReferenceValue = button;
        so.FindProperty("icon").objectReferenceValue = icon;
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("subtitleText").objectReferenceValue = subtitle;
        so.FindProperty("detailText").objectReferenceValue = detail;
        so.FindProperty("newBadge").objectReferenceValue = badge.gameObject;
        so.ApplyModifiedPropertiesWithoutUndo();

        return card.gameObject;
    }

    private static GameObject BuildStarIcon()
    {
        var root = CreateRect("StarIcon", null);
        root.GetComponent<RectTransform>().sizeDelta = new Vector2(160f, 160f);

        var view = root.AddComponent<StarIconView>();

        var filled = AssetDatabase.LoadAssetAtPath<Sprite>(StarSpritePath);
        var empty = AssetDatabase.LoadAssetAtPath<Sprite>(StarEmptySpritePath);

        var icon = CreateImage("Icon", root.transform, Color.white);
        Stretch(icon.rectTransform);
        icon.sprite = filled;
        icon.type = Image.Type.Simple;
        icon.preserveAspect = true;

        var so = new SerializedObject(view);
        so.FindProperty("icon").objectReferenceValue = icon;
        so.FindProperty("filledSprite").objectReferenceValue = filled;
        so.FindProperty("emptySprite").objectReferenceValue = empty;
        so.ApplyModifiedPropertiesWithoutUndo();

        return root;
    }

#endregion

#region 월드

    // 카메라는 고정입니다. 추적이 없으므로 Cinemachine을 걷어냅니다.
    // 브레인이 붙어 있으면 가상 카메라가 매 프레임 위치를 덮어써서
    // 씬에 잡아 둔 좌표가 무시됩니다.
    private static Camera FixCamera()
    {
        var camera = Camera.main;

        if (camera == null)
            return null;

        var brain = camera.GetComponent<Unity.Cinemachine.CinemachineBrain>();

        if (brain != null)
            Object.DestroyImmediate(brain);

        foreach (var vcam in Object.FindObjectsByType<Unity.Cinemachine.CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.DestroyImmediate(vcam.gameObject);

        camera.orthographic = true;
        camera.orthographicSize = 13f;
        camera.transform.position = new Vector3(0f, 0f, -10f);
        camera.transform.rotation = Quaternion.identity;

        return camera;
    }

    private static void BuildBattleRoot(
        out Tower tower,
        out Transform characterAnchor,
        out Transform monsterLayer,
        out Transform projectileLayer,
        out SpriteRenderer background)
    {
        var root = GameObject.Find("Battle");

        if (root != null)
            Object.DestroyImmediate(root);

        root = new GameObject("Battle");

        // 배경
        var bgObject = new GameObject("Background");
        bgObject.transform.SetParent(root.transform, false);
        bgObject.transform.position = new Vector3(0f, 0f, 5f);
        background = bgObject.AddComponent<SpriteRenderer>();
        background.sortingOrder = -100;

        // 타워
        var towerObject = new GameObject("Tower");
        towerObject.transform.SetParent(root.transform, false);
        towerObject.transform.position = new Vector3(0f, TowerInitialCenterY, 0f);

        var intact = AssetDatabase.LoadAssetAtPath<Sprite>(TowerSpritePath);

        var towerRenderer = towerObject.AddComponent<SpriteRenderer>();
        towerRenderer.sprite = intact != null ? intact : BuiltinSprite();

        // 캐릭터(2)·몬스터(0)보다 앞입니다. 각목이 캐릭터 다리를 가리는 그림이 의도입니다.
        // 몬스터 머리 위 체력바(20·21)보다는 뒤라 체력은 가려지지 않습니다.
        towerRenderer.sortingOrder = 5;

        if (intact == null)
        {
            towerRenderer.color = new Color(0.28f, 0.26f, 0.32f, 1f);

            var spriteSize = towerRenderer.sprite != null ? towerRenderer.sprite.bounds.size : Vector3.one;
            towerObject.transform.localScale = new Vector3(
                40f / Mathf.Max(0.01f, spriteSize.x),
                2.73f / Mathf.Max(0.01f, spriteSize.y),
                1f);
        }

        tower = towerObject.AddComponent<Tower>();

        // topOffset은 중심에서 몸통 윗선까지의 거리입니다. 아트 높이에서 각목만큼 뺀 값입니다.
        var halfHeight = towerRenderer.sprite != null ? towerRenderer.sprite.bounds.extents.y : 1f;

        var towerSo = new SerializedObject(tower);
        towerSo.FindProperty("topOffset").floatValue = halfHeight - TowerPlankHeight;
        towerSo.FindProperty("bodyRenderer").objectReferenceValue = towerRenderer;
        towerSo.FindProperty("intactSprite").objectReferenceValue = intact;
        towerSo.FindProperty("damagedSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(TowerDamagedSpritePath);
        towerSo.FindProperty("brokenSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(TowerBrokenSpritePath);
        towerSo.ApplyModifiedPropertiesWithoutUndo();

        // 캐릭터 자리 (발사 원점)
        var anchor = new GameObject("CharacterAnchor");
        anchor.transform.SetParent(root.transform, false);
        anchor.transform.position = new Vector3(0f, CharacterInitialY, 0f);
        characterAnchor = anchor.transform;

        var monsters = new GameObject("MonsterLayer");
        monsters.transform.SetParent(root.transform, false);
        monsterLayer = monsters.transform;

        var projectiles = new GameObject("ProjectileLayer");
        projectiles.transform.SetParent(root.transform, false);
        projectileLayer = projectiles.transform;
    }

    private static Sprite BuiltinSprite()
    {
        return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    }

#endregion

#region HUD

    private static InGameHudUI BuildHud()
    {
        var canvas = GameObject.Find("Canvas");

        // HUD는 항상 전장 위에 그립니다.
        // Screen Space - Camera 캔버스는 sortingOrder로 스프라이트와 순서를 다투는데,
        // 타워가 캐릭터보다 앞이어야 해서 정렬 번호가 높아졌습니다. 그대로 두면 체력 바가 가려집니다.
        canvas.GetComponent<Canvas>().sortingOrder = 100;

        var top = canvas.transform.Find("Top");
        var bottom = canvas.transform.Find("Bottom");

        DestroyChild(top, "HUD");
        DestroyChild(bottom, "HUD Bottom");

        var root = CreateRect("HUD", top);
        Stretch(root.GetComponent<RectTransform>());

        var hud = root.AddComponent<InGameHudUI>();

        // 상단 중앙 — 웨이브 번호
        var waveText = CreateText("WaveText", root.transform, "WAVE 1/20", 56f, FontStyles.Bold);
        AnchorTop(waveText.rectTransform, new Vector2(700f, TopBarHeight), -20f);

        // 그 아래 — 경험치 게이지. 웨이브 남은 시간이 아닙니다.
        //
        // 경고 색은 꺼 둡니다. 체력이면 낮을수록 빨개져야 하지만, 경험치는
        // 매 구간이 0에서 시작하므로 켜 두면 레벨업 직후마다 빨갛게 물듭니다.
        var expGauge = CreateGauge("ExpGauge", root.transform, new Color(0.35f, 0.72f, 0.95f, 1f), "{0} / {1}");
        AnchorTop(expGauge.GetComponent<RectTransform>(), new Vector2(660f, 40f), -132f);
        SetGaugeDangerRatio(expGauge, 0f);

        // 그 아래 — 진행 시간
        var elapsedText = CreateText("ElapsedText", root.transform, "00:00", 40f, FontStyles.Normal);
        AnchorTop(elapsedText.rectTransform, new Vector2(300f, 52f), -182f);

        // 상단 우측 — 메뉴 버튼 (1차에서는 자리만 잡습니다)
        var menu = CreateImage("MenuButton", root.transform, new Color(0f, 0f, 0f, 0.45f));
        var menuRect = menu.rectTransform;
        menuRect.anchorMin = new Vector2(1f, 1f);
        menuRect.anchorMax = new Vector2(1f, 1f);
        menuRect.pivot = new Vector2(1f, 1f);
        menuRect.sizeDelta = new Vector2(96f, 96f);
        menuRect.anchoredPosition = new Vector2(-30f, -20f);
        menu.gameObject.AddComponent<Button>();

        var menuLabel = CreateText("Label", menu.transform, "≡", 52f, FontStyles.Bold);
        Stretch(menuLabel.rectTransform);

        // 하단 띠 — 위에서부터 타워 체력바, 스킬 슬롯.
        // 타워(월드)는 이 띠 위에 서고, 배치는 MainGameController가 여기서 역산합니다.
        var bottomRoot = CreateRect("HUD Bottom", bottom);
        Stretch(bottomRoot.GetComponent<RectTransform>());

        var towerGauge = CreateGauge("TowerHealth", bottomRoot.transform, new Color(0.85f, 0.35f, 0.30f, 1f), "{0} / {1}");
        AnchorBottom(towerGauge.GetComponent<RectTransform>(), new Vector2(1080f, TowerHealthBarHeight), SkillSlotAreaHeight);

        var slots = CreateRect("SkillSlots", bottomRoot.transform);
        AnchorBottom(slots.GetComponent<RectTransform>(), new Vector2(940f, 168f), 20f);

        var layout = slots.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 16f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = false;
        layout.childControlHeight = false;

        var slotViews = new SkillSlotView[5];

        for (var i = 0; i < slotViews.Length; i++)
        {
            slotViews[i] = InstantiatePrefab<SkillSlotView>(SkillSlotPrefabPath, slots.transform);
            slotViews[i].gameObject.name = $"Slot{i + 1}";
        }

        var hudSo = new SerializedObject(hud);
        hudSo.FindProperty("waveText").objectReferenceValue = waveText;
        hudSo.FindProperty("expGauge").objectReferenceValue = expGauge;
        hudSo.FindProperty("elapsedText").objectReferenceValue = elapsedText;
        hudSo.FindProperty("towerHealthGauge").objectReferenceValue = towerGauge;
        FillArray(hudSo.FindProperty("skillSlots"), slotViews);
        hudSo.ApplyModifiedPropertiesWithoutUndo();

        return hud;
    }

#endregion

#region 오버레이

    private static LevelUpChoiceUI BuildLevelUpChoiceUI()
    {
        var popupCanvas = GameObject.Find("Canvas - Popup").transform;

        DestroyChild(popupCanvas, "LevelUpChoice");

        var root = CreateImage("LevelUpChoice", popupCanvas, new Color(0f, 0f, 0f, 0.78f));
        Stretch(root.rectTransform);

        var ui = root.gameObject.AddComponent<LevelUpChoiceUI>();

        var header = CreateText("Header", root.transform, "LEVEL 2", 64f, FontStyles.Bold);
        AnchorTop(header.rectTransform, new Vector2(900f, 90f), -260f);

        var guide = CreateText("Guide", root.transform, "스킬을 고르세요", 42f, FontStyles.Normal);
        AnchorTop(guide.rectTransform, new Vector2(900f, 60f), -356f);

        // 세로 3장입니다. 가로로 놓으면 장당 폭이 360px라 이름과 수치가 안 들어갑니다.
        var items = new LevelUpChoiceItemUI[3];

        for (var i = 0; i < items.Length; i++)
        {
            items[i] = InstantiatePrefab<LevelUpChoiceItemUI>(ChoiceCardPrefabPath, root.transform);
            items[i].gameObject.name = $"Choice{i + 1}";

            AnchorTop(items[i].GetComponent<RectTransform>(), new Vector2(940f, 300f), -(470f + i * 320f));
        }

        var so = new SerializedObject(ui);
        so.FindProperty("root").objectReferenceValue = root.gameObject;
        so.FindProperty("headerText").objectReferenceValue = header;
        FillArray(so.FindProperty("items"), items);
        so.ApplyModifiedPropertiesWithoutUndo();

        root.gameObject.SetActive(false);

        return ui;
    }

    private static void PlaceInCard(RectTransform rect, float y)
    {
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = new Vector2(660f, 60f);
        rect.anchoredPosition = new Vector2(240f, y);
    }

    private static StageResultUI BuildResultUI()
    {
        var popupCanvas = GameObject.Find("Canvas - Popup").transform;

        DestroyChild(popupCanvas, "StageResult");

        var root = CreateImage("StageResult", popupCanvas, new Color(0f, 0f, 0f, 0.85f));
        Stretch(root.rectTransform);

        var ui = root.gameObject.AddComponent<StageResultUI>();

        // 별이 가장 위에, 가장 크게 들어갑니다. 반복 플레이의 목표가 별이므로.
        var starRow = CreateRect("Stars", root.transform);
        AnchorTop(starRow.GetComponent<RectTransform>(), new Vector2(600f, 180f), -420f);

        var starLayout = starRow.AddComponent<HorizontalLayoutGroup>();
        starLayout.spacing = 24f;
        starLayout.childAlignment = TextAnchor.MiddleCenter;
        starLayout.childForceExpandWidth = false;
        starLayout.childForceExpandHeight = false;
        starLayout.childControlWidth = false;
        starLayout.childControlHeight = false;

        var stars = new StarIconView[3];

        for (var i = 0; i < stars.Length; i++)
        {
            stars[i] = InstantiatePrefab<StarIconView>(StarIconPrefabPath, starRow.transform);
            stars[i].gameObject.name = $"Star{i + 1}";
        }

        var title = CreateText("Title", root.transform, "STAGE CLEAR", 76f, FontStyles.Bold);
        AnchorTop(title.rectTransform, new Vector2(900f, 100f), -640f);

        var wave = CreateText("Wave", root.transform, "도달 웨이브    20 / 20", 40f, FontStyles.Normal);
        AnchorTop(wave.rectTransform, new Vector2(760f, 56f), -800f);

        var elapsed = CreateText("Elapsed", root.transform, "소요 시간    05:00", 40f, FontStyles.Normal);
        AnchorTop(elapsed.rectTransform, new Vector2(760f, 56f), -866f);

        var towerText = CreateText("Tower", root.transform, "남은 타워 체력    1000 / 1000", 40f, FontStyles.Normal);
        AnchorTop(towerText.rectTransform, new Vector2(760f, 56f), -932f);

        var retry = CreateImage("RetryButton", root.transform, new Color(0.22f, 0.44f, 0.72f, 1f));
        AnchorTop(retry.rectTransform, new Vector2(520f, 120f), -1090f);

        var retryButton = retry.gameObject.AddComponent<Button>();
        retryButton.targetGraphic = retry;

        var retryLabel = CreateText("Label", retry.transform, "다시하기", 46f, FontStyles.Bold);
        Stretch(retryLabel.rectTransform);

        var so = new SerializedObject(ui);
        so.FindProperty("root").objectReferenceValue = root.gameObject;
        FillArray(so.FindProperty("stars"), stars);
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("waveText").objectReferenceValue = wave;
        so.FindProperty("elapsedText").objectReferenceValue = elapsed;
        so.FindProperty("towerText").objectReferenceValue = towerText;
        so.FindProperty("retryButton").objectReferenceValue = retryButton;
        so.ApplyModifiedPropertiesWithoutUndo();

        root.gameObject.SetActive(false);

        return ui;
    }

#endregion

#region 위젯 만들기

    private static void DestroyChild(Transform parent, string name)
    {
        var found = parent.Find(name);

        if (found != null)
            Object.DestroyImmediate(found.gameObject);
    }

    private static void FillArray<T>(SerializedProperty property, T[] values) where T : Object
    {
        property.arraySize = values.Length;

        for (var i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    }

    private static GameObject CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));

        if (parent != null)
            go.transform.SetParent(parent, false);

        return go;
    }

    private static Image CreateImage(string name, Transform parent, Color color)
    {
        var go = CreateRect(name, parent);
        var image = go.AddComponent<Image>();

        image.sprite = BuiltinSprite();
        image.type = Image.Type.Sliced;
        image.color = color;

        return image;
    }

    private static TMP_Text CreateText(string name, Transform parent, string text, float size, FontStyles style)
    {
        var go = CreateRect(name, parent);
        var label = go.AddComponent<TextMeshProUGUI>();

        label.text = text;
        label.fontSize = size;
        label.fontStyle = style;
        label.alignment = TextAlignmentOptions.Center;
        label.color = Color.white;
        label.raycastTarget = false;

        return label;
    }

    private static GaugeBarView CreateGauge(string name, Transform parent, Color fillColor, string format)
    {
        var root = CreateRect(name, parent);
        var view = root.AddComponent<GaugeBarView>();

        var track = CreateImage("Track", root.transform, new Color(0.08f, 0.08f, 0.10f, 0.85f));
        Stretch(track.rectTransform);

        // 줄어든 만큼 잠깐 남겨 보여 주는 뒷바. 타워가 얼마나 맞았는지 눈에 띄게 합니다.
        var delayed = CreateImage("Delayed", root.transform, new Color(0.85f, 0.35f, 0.25f, 0.9f));
        Inset(delayed.rectTransform, 4f);
        delayed.type = Image.Type.Filled;
        delayed.fillMethod = Image.FillMethod.Horizontal;

        var fill = CreateImage("Fill", root.transform, fillColor);
        Inset(fill.rectTransform, 4f);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;

        var label = CreateText("Label", root.transform, "0 / 0", 28f, FontStyles.Bold);
        Stretch(label.rectTransform);

        var so = new SerializedObject(view);
        so.FindProperty("track").objectReferenceValue = track;
        so.FindProperty("fill").objectReferenceValue = fill;
        so.FindProperty("delayedFill").objectReferenceValue = delayed;
        so.FindProperty("label").objectReferenceValue = label;
        so.FindProperty("fillColor").colorValue = fillColor;
        so.FindProperty("format").stringValue = format;
        so.ApplyModifiedPropertiesWithoutUndo();

        return view;
    }

    private static void SetGaugeDangerRatio(GaugeBarView gauge, float ratio)
    {
        var so = new SerializedObject(gauge);
        so.FindProperty("dangerRatio").floatValue = ratio;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void Inset(RectTransform rect, float padding)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(padding, padding);
        rect.offsetMax = new Vector2(-padding, -padding);
    }

    private static void AnchorTop(RectTransform rect, Vector2 size, float y)
    {
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = new Vector2(0f, y);
    }

    private static void AnchorBottom(RectTransform rect, Vector2 size, float y)
    {
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = size;
        rect.anchoredPosition = new Vector2(0f, y);
    }

#endregion
}

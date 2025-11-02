#if UNITY_EDITOR
using UnityEngine;

[System.Serializable]
public class StatConfig
{
    public string label = "Character A";     // 범례/파일명 등에 표시
    public Color color = new Color(0.35f, 0.65f, 1f);

    [Header("Base Stats")]
    public float baseStr = 10f;
    public float baseDex = 10f;
    public float baseLuk = 10f;

    [Header("Growth Rates (per stat)")]
    public float growthStr = 1.0f;
    public float growthDex = 1.0f;
    public float growthLuk = 1.0f;

    [Header("Rank Multipliers")]
    public float mulR = 1.0f;
    public float mulSR = 1.3f;
    public float mulSSR = 1.6f;
    public float mulUR = 2.0f;

    [Header("Growth Curve (Level 1..Max normalized 0..1)")]
    public AnimationCurve curveStr = AnimationCurve.Linear(0, 0, 1, 0);
    public AnimationCurve curveDex = AnimationCurve.Linear(0, 0, 1, 0);
    public AnimationCurve curveLuk = AnimationCurve.Linear(0, 0, 1, 0);

    [Tooltip("커브 영향도. 0=커브 무시, 1=커브를 100% 반영")]
    public float curveWeight = 1.0f; // 0..1
}

[CreateAssetMenu(fileName = "StatCurvePreset", menuName = "Balancing/Stat Curve Preset")]
public class StatCurvePresetSO : ScriptableObject
{
    [Range(1, 300)] public int minLevel = 1;
    [Range(1, 300)] public int maxLevel = 100;
    [Range(2, 2000)] public int samples = 200;

    [Header("표시 토글")]
    public bool showSTR = true;
    public bool showDEX = false;
    public bool showLUK = false;
    public bool showR   = true;
    public bool showSR  = true;
    public bool showSSR = true;
    public bool showUR  = false;

    [Header("비교할 캐릭터들")]
    public StatConfig[] characters = new StatConfig[]
    {
        new StatConfig(){ label = "A", color = new Color(0.35f,0.65f,1f)},
        new StatConfig(){ label = "B", color = new Color(1.00f,0.55f,0.35f)},
    };
}
#endif
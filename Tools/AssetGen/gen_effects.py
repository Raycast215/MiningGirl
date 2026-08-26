# -*- coding: utf-8 -*-
"""스킬 이펙트 프리팹 생성기

구매 팩(Pixel Effects 1 / 4)의 스프라이트 시트를 인게임 래핑 구조에 얹는다.
그림을 새로 그리는 게 아니라 기존 3종(FireBolt / IceBolt / LightningBolt)과
같은 3종 세트를 만들어 스프라이트만 갈아끼우는 작업이다.

    Effect_{Id}.prefab
    SkillEffectAnimator_{Id}.overrideController
    SkillEffect_Idle_{Id}.anim

    python Tools/AssetGen/gen_effects.py            # 프로젝트에 생성
    python Tools/AssetGen/gen_effects.py --check    # 원본 스프라이트만 확인하고 안 씀

GUID는 파일 이름에서 결정론적으로 뽑되, 기존 .meta가 있으면 그 값을 보존한다.
"""
import hashlib, io, os, re, struct, sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
ASSETS = os.path.join(ROOT, "Client", "MiningGirl", "Assets")
OUT = os.path.join(ASSETS, "Prefabs", "InGame", "Effect")
P1 = os.path.join(ASSETS, "Download", "Pixel Effects 1 - Pixel Art", "Sprites")
P4 = os.path.join(ASSETS, "Download", "Pixel Effects 4 - Pixel Art", "Sprites")

# 기존 3종이 물고 있는 공용 에셋. 새 세트도 같은 것을 쓴다.
MATERIAL = "a97c105638bdf8b4a8650670310a4cd3"      # SpriteRenderer 머티리얼
BASE_CTRL = "554af2de6d279024e8c1f760278241bd"     # SkillEffectAnimator.controller
BASE_CLIP = "a2ec6c5c9665aff44a0faff869634327"     # SkillEffect_Idle.anim (오버라이드 대상)

# ---------------------------------------------------------------- 정의
# scale : 프리팹 루트 스케일. Projectile은 position/rotation만 건드리므로 그대로 남는다.
# frame : 프레임 간격(초).
#
# 원본은 전부 PPU 32이라 칸 크기 / 32 = 유닛이다. 기존 볼트가 2.0 x 0.5~1.0 유닛이고
# 그게 화면(23.3 x 26.2 유닛)에서 읽히는 기준 크기다.
EFFECTS = [
    # 단일 x 표준. 원본이 1.0x1.0으로 기존 볼트보다 이미 작다 - 그대로 둔다.
    dict(id="QuartzShot", src="SunBolt",         dir=P4, scale=1.00, frame=0.05),
    # 관통 x 연사. 원본 2.0x2.0으로 몬스터(1.2유닛)보다 크다. 연사라 화면에 여러 발이
    # 동시에 뜨므로 기존 볼트 높이(1.0유닛) 언저리까지 줄인다.
    dict(id="MineGust",   src="StaticLightning", dir=P1, scale=0.60, frame=1/30),
    # 관통 x 표준. 기존 볼트와 같은 규격이라 손대지 않는다.
    dict(id="ShaftArrow", src="PoisonBolt",      dir=P4, scale=1.00, frame=0.05),
    # 관통 x 일격. 가장 무거운 단발이라 기준 크기를 유지한다.
    dict(id="DrillWedge", src="ShadowBolt",      dir=P1, scale=1.00, frame=0.05),
    # 다발 x 표준. 한 번에 2발이 나가므로 줄인다.
    dict(id="OreShard",   src="DeathBolt",       dir=P4, scale=0.70, frame=0.05),
    # 다발 x 일격. 원본이 2.5x2.0으로 가장 크고 2발 동시라 가장 많이 줄인다.
    # 다만 광석 파편보다는 커야 '일격'이 읽힌다.
    dict(id="Cavein",     src="MeteorShower",    dir=P4, scale=0.55, frame=1/30),
]

FRAME_TBL = re.compile(r"- first:\s*\n\s*213:\s*(-?\d+)\s*\n\s*second:\s*(\S+)")
GUID_RE = re.compile(r"^guid:\s*([0-9a-f]{32})\s*$", re.M)


def guid_of(key):
    return hashlib.md5(("MiningGirl.Effect." + key).encode()).hexdigest()


def resolve_guid(meta_path, key):
    """기존 .meta가 있으면 GUID를 보존한다 - 다시 돌려도 참조가 안 끊긴다"""
    if os.path.exists(meta_path):
        m = GUID_RE.search(io.open(meta_path, encoding="utf-8", errors="replace").read())
        if m:
            return m.group(1)
    return guid_of(key)


def local_id(key):
    """프리팹 안에서 쓸 양수 int64 - 파일 안에서만 유일하면 된다"""
    return int(hashlib.md5(key.encode()).hexdigest()[:15], 16)


def read_source(folder, name):
    """원본 시트의 guid와 프레임 서브에셋 ID를 순서대로 읽는다"""
    meta = os.path.join(folder, name + ".png.meta")
    if not os.path.exists(meta):
        raise SystemExit("원본 .meta가 없습니다: " + meta)
    t = io.open(meta, encoding="utf-8", errors="replace").read()
    guid = GUID_RE.search(t).group(1)
    frames = FRAME_TBL.findall(t)
    if not frames:
        raise SystemExit(name + ": 잘린 프레임이 없습니다 (spriteMode가 Multiple인지 확인)")
    frames.sort(key=lambda p: int(p[1].rsplit("_", 1)[-1]))
    d = open(os.path.join(folder, name + ".png"), "rb").read(33)
    tw, th = struct.unpack(">II", d[16:24])
    return guid, [int(i) for i, _ in frames], tw, th


# ---------------------------------------------------------------- 템플릿
ANIM = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!74 &7400000
AnimationClip:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {name}
  serializedVersion: 7
  m_Legacy: 0
  m_Compressed: 0
  m_UseHighQualityCurve: 1
  m_RotationCurves: []
  m_CompressedRotationCurves: []
  m_EulerCurves: []
  m_PositionCurves: []
  m_ScaleCurves: []
  m_FloatCurves: []
  m_PPtrCurves:
  - serializedVersion: 2
    curve:
{keys}    attribute: m_Sprite
    path: 
    classID: 212
    script: {{fileID: 0}}
    flags: 2
  m_SampleRate: 60
  m_WrapMode: 0
  m_Bounds:
    m_Center: {{x: 0, y: 0, z: 0}}
    m_Extent: {{x: 0, y: 0, z: 0}}
  m_ClipBindingConstant:
    genericBindings:
    - serializedVersion: 2
      path: 0
      attribute: 0
      script: {{fileID: 0}}
      typeID: 212
      customType: 23
      isPPtrCurve: 1
      isIntCurve: 0
      isSerializeReferenceCurve: 0
    pptrCurveMapping:
{mapping}  m_AnimationClipSettings:
    serializedVersion: 2
    m_AdditiveReferencePoseClip: {{fileID: 0}}
    m_AdditiveReferencePoseTime: 0
    m_StartTime: 0
    m_StopTime: {stop}
    m_OrientationOffsetY: 0
    m_Level: 0
    m_CycleOffset: 0
    m_HasAdditiveReferencePose: 0
    m_LoopTime: 1
    m_LoopBlend: 0
    m_LoopBlendOrientation: 0
    m_LoopBlendPositionY: 0
    m_LoopBlendPositionXZ: 0
    m_KeepOriginalOrientation: 0
    m_KeepOriginalPositionY: 1
    m_KeepOriginalPositionXZ: 0
    m_HeightFromFeet: 0
    m_Mirror: 0
  m_EditorCurves: []
  m_EulerEditorCurves: []
  m_HasGenericRootTransform: 0
  m_HasMotionFloatCurves: 0
  m_Events: []
"""

OVERRIDE = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!221 &22100000
AnimatorOverrideController:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_Name: {name}
  m_Controller: {{fileID: 9100000, guid: {base_ctrl}, type: 2}}
  m_Clips:
  - m_OriginalClip: {{fileID: 7400000, guid: {base_clip}, type: 2}}
    m_OverrideClip: {{fileID: 7400000, guid: {clip}, type: 2}}
"""

PREFAB = """%YAML 1.1
%TAG !u! tag:unity3d.com,2011:
--- !u!1 &{root_go}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {root_tr}}}
  m_Layer: 0
  m_Name: {name}
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{root_tr}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {root_go}}}
  serializedVersion: 2
  m_LocalRotation: {{x: 0, y: 0, z: 0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 0}}
  m_LocalScale: {{x: {scale}, y: {scale}, z: {scale}}}
  m_ConstrainProportionsScale: 0
  m_Children:
  - {{fileID: {child_tr}}}
  m_Father: {{fileID: 0}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!1 &{child_go}
GameObject:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  serializedVersion: 6
  m_Component:
  - component: {{fileID: {child_tr}}}
  - component: {{fileID: {renderer}}}
  - component: {{fileID: {animator}}}
  m_Layer: 0
  m_Name: Sprite Renderer
  m_TagString: Untagged
  m_Icon: {{fileID: 0}}
  m_NavMeshLayer: 0
  m_StaticEditorFlags: 0
  m_IsActive: 1
--- !u!4 &{child_tr}
Transform:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {child_go}}}
  serializedVersion: 2
  m_LocalRotation: {{x: -0, y: -0, z: -0, w: 1}}
  m_LocalPosition: {{x: 0, y: 0, z: 1}}
  m_LocalScale: {{x: 1, y: 1, z: 1}}
  m_ConstrainProportionsScale: 0
  m_Children: []
  m_Father: {{fileID: {root_tr}}}
  m_LocalEulerAnglesHint: {{x: 0, y: 0, z: 0}}
--- !u!212 &{renderer}
SpriteRenderer:
  serializedVersion: 2
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {child_go}}}
  m_Enabled: 1
  m_CastShadows: 0
  m_ReceiveShadows: 0
  m_DynamicOccludee: 1
  m_StaticShadowCaster: 0
  m_MotionVectors: 1
  m_LightProbeUsage: 1
  m_ReflectionProbeUsage: 1
  m_RayTracingMode: 0
  m_RayTraceProcedural: 0
  m_RayTracingAccelStructBuildFlagsOverride: 0
  m_RayTracingAccelStructBuildFlags: 1
  m_SmallMeshCulling: 1
  m_ForceMeshLod: -1
  m_MeshLodSelectionBias: 0
  m_RenderingLayerMask: 1
  m_RendererPriority: 0
  m_Materials:
  - {{fileID: 2100000, guid: {material}, type: 2}}
  m_StaticBatchInfo:
    firstSubMesh: 0
    subMeshCount: 0
  m_StaticBatchRoot: {{fileID: 0}}
  m_ProbeAnchor: {{fileID: 0}}
  m_LightProbeVolumeOverride: {{fileID: 0}}
  m_ScaleInLightmap: 1
  m_ReceiveGI: 1
  m_PreserveUVs: 0
  m_IgnoreNormalsForChartDetection: 0
  m_ImportantGI: 0
  m_StitchLightmapSeams: 1
  m_SelectedEditorRenderState: 0
  m_MinimumChartSize: 4
  m_AutoUVMaxDistance: 0.5
  m_AutoUVMaxAngle: 89
  m_LightmapParameters: {{fileID: 0}}
  m_GlobalIlluminationMeshLod: 0
  m_SortingLayerID: 0
  m_SortingLayer: 0
  m_SortingOrder: 0
  m_MaskInteraction: 0
  m_Sprite: {{fileID: {first}, guid: {sheet}, type: 3}}
  m_Color: {{r: 1, g: 1, b: 1, a: 1}}
  m_FlipX: 0
  m_FlipY: 0
  m_DrawMode: 0
  m_Size: {{x: 2, y: 1}}
  m_AdaptiveModeThreshold: 0.5
  m_SpriteTileMode: 0
  m_WasSpriteAssigned: 1
  m_SpriteSortPoint: 0
--- !u!95 &{animator}
Animator:
  serializedVersion: 7
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {{fileID: 0}}
  m_PrefabInstance: {{fileID: 0}}
  m_PrefabAsset: {{fileID: 0}}
  m_GameObject: {{fileID: {child_go}}}
  m_Enabled: 1
  m_Avatar: {{fileID: 0}}
  m_Controller: {{fileID: 22100000, guid: {override}, type: 2}}
  m_CullingMode: 0
  m_UpdateMode: 0
  m_ApplyRootMotion: 0
  m_LinearVelocityBlending: 0
  m_StabilizeFeet: 0
  m_AnimatePhysics: 0
  m_WarningMessage: 
  m_HasTransformHierarchy: 1
  m_AllowConstantClipSamplingOptimization: 1
  m_KeepAnimatorStateOnDisable: 0
  m_WriteDefaultValuesOnDisable: 0
"""

NATIVE_META = """fileFormatVersion: 2
guid: {guid}
NativeFormatImporter:
  externalObjects: {{}}
  mainObjectFileID: {main_id}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""

PREFAB_META = """fileFormatVersion: 2
guid: {guid}
PrefabImporter:
  externalObjects: {{}}
  userData: 
  assetBundleName: 
  assetBundleVariant: 
"""


# ---------------------------------------------------------------- 생성
def write(path, text):
    with io.open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)


def build(e, check_only=False):
    sheet, frames, _, _ = read_source(e["dir"], e["src"])
    n = len(frames)
    dt = e["frame"]

    if check_only:
        print(f"  {e['id']:12s} <- {e['src']:16s} 프레임 {n:2d}  간격 {dt*1000:.0f}ms  "
              f"길이 {n*dt:.2f}s  스케일 {e['scale']}")
        return

    clip_name = "SkillEffect_Idle_" + e["id"]
    ctrl_name = "SkillEffectAnimator_" + e["id"]
    pref_name = "Effect_" + e["id"]

    clip_path = os.path.join(OUT, clip_name + ".anim")
    ctrl_path = os.path.join(OUT, ctrl_name + ".overrideController")
    pref_path = os.path.join(OUT, pref_name + ".prefab")

    clip_guid = resolve_guid(clip_path + ".meta", clip_name)
    ctrl_guid = resolve_guid(ctrl_path + ".meta", ctrl_name)
    pref_guid = resolve_guid(pref_path + ".meta", pref_name)

    keys = "".join(f"    - time: {round(i*dt, 6)}\n"
                   f"      value: {{fileID: {fid}, guid: {sheet}, type: 3}}\n"
                   for i, fid in enumerate(frames))
    mapping = "".join(f"    - {{fileID: {fid}, guid: {sheet}, type: 3}}\n" for fid in frames)
    write(clip_path, ANIM.format(name=clip_name, keys=keys, mapping=mapping,
                                 stop=round(n * dt, 6)))
    write(clip_path + ".meta", NATIVE_META.format(guid=clip_guid, main_id=7400000))

    write(ctrl_path, OVERRIDE.format(name=ctrl_name, base_ctrl=BASE_CTRL,
                                     base_clip=BASE_CLIP, clip=clip_guid))
    write(ctrl_path + ".meta", NATIVE_META.format(guid=ctrl_guid, main_id=22100000))

    write(pref_path, PREFAB.format(
        name=pref_name, scale=e["scale"], material=MATERIAL, sheet=sheet,
        first=frames[0], override=ctrl_guid,
        root_go=local_id(pref_name + ".rootGO"), root_tr=local_id(pref_name + ".rootTR"),
        child_go=local_id(pref_name + ".childGO"), child_tr=local_id(pref_name + ".childTR"),
        renderer=local_id(pref_name + ".renderer"), animator=local_id(pref_name + ".animator")))
    write(pref_path + ".meta", PREFAB_META.format(guid=pref_guid))

    print(f"  {pref_name:20s} <- {e['src']:16s} 프레임 {n:2d}  "
          f"길이 {n*dt:.2f}s  스케일 {e['scale']}  guid {pref_guid}")


def main():
    check = "--check" in sys.argv
    if not os.path.isdir(OUT):
        print("출력 폴더가 없습니다:", OUT)
        return 1
    print("원본 확인" if check else "생성")
    for e in EFFECTS:
        build(e, check)
    if not check:
        print("\n어드레서블 등록은 개발 영역입니다 - Effect_{Id}를 주소로 올려야 합니다.")
    return 0


if __name__ == "__main__":
    sys.exit(main())

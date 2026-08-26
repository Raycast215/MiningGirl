# -*- coding: utf-8 -*-
"""생성한 PNG용 Unity .meta 작성기

Unity가 자동으로 붙이는 기본 임포트 설정(PPU 100 / Bilinear / 압축)은 도트 아트에
맞지 않으므로, 에셋 종류별 프리셋으로 .meta를 직접 써 준다.
GUID는 기존 .meta가 있으면 그 값을 그대로 재사용하고, 없을 때만 파일 이름에서
결정론적으로 뽑는다. 어느 쪽이든 다시 돌려도 참조가 안 끊긴다.

    python Tools/AssetGen/gen_meta.py monster
    python Tools/AssetGen/gen_meta.py background
    python Tools/AssetGen/gen_meta.py tower
    python Tools/AssetGen/gen_meta.py tower --force     # 기존 .meta도 덮어씀

기본값은 '이미 있는 .meta는 건드리지 않음'이다. Unity가 임포트하면서 스프라이트
서브에셋 정보를 .meta에 덧붙이는 경우가 있어, 덮어쓰면 그 정보가 날아갈 수 있다.
--force로 덮어써도 GUID만은 기존 값을 읽어 보존하므로 에셋 참조 자체는 안 끊긴다.
"""
import hashlib, os, re, sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SPRITES = os.path.join(ROOT, "Client", "MiningGirl", "Assets", "Sprites")

# ---------------------------------------------------------------- 프리셋
# folder        : Assets/Sprites 아래 상대 경로
# ppu           : Pixels Per Unit
# max_size      : maxTextureSize (실제 픽셀 크기보다 작으면 Unity가 강제로 축소한다)
# wrap          : 0=Repeat, 1=Clamp  (가로, 세로)
# filter        : 0=Point, 1=Bilinear
#                 인게임 도트 아트는 Point. UI는 기기마다 크기가 달라 정수배로 안 떨어지므로
#                 Point로 두면 픽셀 폭이 들쭉날쭉해진다 - Bilinear가 맞다.
# alpha         : alphaIsTransparency. 알파가 있는 그림은 1, 불투명 배경은 0
PRESETS = {
    "monster":    dict(folder="InGame/Monster",    ppu=50,  max_size=512,  wrap=(1, 1), filter=0, alpha=1),
    "background": dict(folder="InGame/Background", ppu=88,  max_size=4096, wrap=(0, 0), filter=0, alpha=0),
    "tower":      dict(folder="InGame/Tower",      ppu=88,  max_size=2048, wrap=(0, 1), filter=0, alpha=1),
    "ui":         dict(folder="UI",                ppu=100, max_size=2048, wrap=(1, 1), filter=1, alpha=1),
}

TEX_META = """fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: {max_size}
  textureSettings:
    serializedVersion: 2
    filterMode: {filter}
    aniso: 1
    mipBias: 0
    wrapU: {wrap_u}
    wrapV: {wrap_v}
    wrapW: {wrap_v}
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 0
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: {ppu}
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: {alpha}
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 4
    buildTarget: DefaultTexturePlatform
    maxTextureSize: {max_size}
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 0
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    customData:
    physicsShape: []
    bones: []
    spriteID: 5e97eb03825dee720800000000000000
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    spriteCustomMetadata:
      entries: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""

FOLDER_META = """fileFormatVersion: 2
guid: {guid}
folderAsset: yes
DefaultImporter:
  externalObjects: {{}}
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def guid_of(key):
    """파일 이름에서 GUID를 결정론적으로 생성 - 다시 돌려도 참조가 유지된다"""
    return hashlib.md5(("MiningGirl.TempMonster." + key).encode()).hexdigest()


GUID_RE = re.compile(r"^guid:\s*([0-9a-fA-F]{32})\s*$", re.MULTILINE)


def resolve_guid(meta_path, fallback_key):
    """기존 .meta가 있으면 그 GUID를 그대로 쓴다

    --force로 덮어쓸 때 GUID가 바뀌면 그 에셋을 물고 있는 프리팹/씬/Addressables가
    전부 끊긴다. Unity가 자동 생성한 .meta는 GUID가 랜덤이라 이름 해시로는 복원되지
    않으므로, 파일에서 읽어 보존하는 쪽이 항상 안전하다.
    """
    if os.path.exists(meta_path):
        m = GUID_RE.search(open(meta_path, encoding="utf-8", errors="replace").read())
        if m:
            return m.group(1)
    return guid_of(fallback_key)


def write(path, text, force):
    if os.path.exists(path) and not force:
        print("skip (이미 있음):", os.path.basename(path))
        return
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)
    print("wrote:", os.path.basename(path))


def main():
    args = [a for a in sys.argv[1:] if not a.startswith("--")]
    force = "--force" in sys.argv
    if not args or args[0] not in PRESETS:
        print("usage: gen_meta.py {%s} [--force]" % "|".join(PRESETS))
        return 1

    preset = PRESETS[args[0]]
    folder = os.path.join(SPRITES, *preset["folder"].split("/"))
    if not os.path.isdir(folder):
        print("폴더가 없습니다:", folder)
        return 1

    d = SPRITES                                       # 상위 폴더들의 .meta까지 확인
    for part in [None] + preset["folder"].split("/"):
        if part:
            d = os.path.join(d, part)
        key = d.replace("\\", "/")
        write(d + ".meta", FOLDER_META.format(guid=resolve_guid(d + ".meta", key)), False)

    for name in sorted(os.listdir(folder)):
        if not name.endswith(".png"):
            continue
        meta_path = os.path.join(folder, name + ".meta")
        text = TEX_META.format(guid=resolve_guid(meta_path, name), ppu=preset["ppu"],
                               max_size=preset["max_size"], alpha=preset["alpha"],
                               wrap_u=preset["wrap"][0], wrap_v=preset["wrap"][1],
                               filter=preset["filter"])
        write(meta_path, text, force)
    return 0


if __name__ == "__main__":
    sys.exit(main())

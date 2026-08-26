# -*- coding: utf-8 -*-
"""MiningGirl 임시 타워(갱도 바리케이드) 생성기

448x60 도트 -> 4배 확대 1792x240 (PPU 88 기준 20.36 x 2.73 유닛)
- 체력 바 UI가 타워 '아래'에 붙으므로 윗변 실루엣(뾰족한 각목 끝)을 살림
- 하단은 UI에 가려지므로 디테일을 빼고 단순한 그림자 띠로 마감
- 좌우 심리스라 화면비가 달라 잘려도 무방. 손상 3단계 출력.
"""
import zlib, struct, os, sys

W, H, SCALE = 448, 60, 4
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SPRITES = os.path.join(ROOT, "Client", "MiningGirl", "Assets", "Sprites", "InGame")
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(SPRITES, "Tower")
PREVIEW = sys.argv[2] if len(sys.argv) > 2 else None

_seed = 20260827


def rnd():
    global _seed
    _seed = (_seed * 1103515245 + 12345) & 0x7FFFFFFF
    return _seed / 0x7FFFFFFF


def rint(a, b):
    return a + int(rnd() * (b - a + 1)) % (b - a + 1)


def reseed(s):
    global _seed
    _seed = s


# ---------------------------------------------------------------- 팔레트
WOOD = [(96, 64, 40), (112, 76, 46), (128, 88, 54), (142, 100, 62)]
WOOD_LIT = (162, 118, 74)
WOOD_DARK = (68, 44, 27)
SEAM = (46, 30, 19)
POST = (86, 56, 34)
POST_LIT = (124, 84, 52)
TIP_LIT = (178, 134, 88)      # 깎아낸 각목 끝 단면
STEEL = (92, 98, 108)
STEEL_LIT = (146, 154, 164)
STEEL_DARK = (52, 56, 64)
BOLT = (176, 182, 190)
GROUND = [(52, 44, 38), (40, 34, 30)]
EDGE = (28, 20, 14)
CHAR = (24, 16, 12)

grid = [[None] * W for _ in range(H)]

# ---------------------------------------------------------------- 세로 구획
CAP = (18, 22)        # 상단 철제 빔
BAND_A = (23, 34)
BAND_B = (36, 47)
BAND_C = (49, 57)
GROUND_ROWS = (58, 59)  # UI에 가려지는 구간 - 단순 마감


def put(x, y, c):
    if 0 <= y < H:
        grid[y][x % W] = c   # x를 감아서 좌우 심리스 유지


def rect(x0, y0, x1, y1, c):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            put(x, y, c)


# ---------------------------------------------------------------- 판자 띠
def plank_band(y0, y1, seg_min, seg_max):
    x = 0
    while x < W:
        seg = rint(seg_min, seg_max)
        rect(x, y0, x + seg, y1, WOOD[rint(0, 3)])
        rect(x, y0, x + seg, y0, WOOD_LIT)
        rect(x, y1, x + seg, y1, WOOD_DARK)
        for _ in range((seg * (y1 - y0)) // 26):        # 나뭇결
            gx, gy = x + rint(0, seg), rint(y0 + 1, y1 - 1)
            col = WOOD_DARK if rnd() < 0.6 else WOOD_LIT
            for i in range(rint(3, 10)):
                put(gx + i, gy, col)
        rect(x + seg, y0, x + seg + 1, y1, SEAM)
        x += seg + 2


# ---------------------------------------------------------------- 뾰족한 각목
STAKES = []      # (x, w, top) - 손상 단계에서 부러뜨리려고 기억해 둠


def stake(x, w, top, bottom, body, lit):
    """위쪽 끝을 뾰족하게 깎은 세로 각목"""
    taper = max(2, w // 2)
    for i in range(taper + 1):                          # 뾰족한 끝
        inset = taper - i
        if x + inset > x + w - 1 - inset:
            continue
        rect(x + inset, top + i, x + w - 1 - inset, top + i, body)
        put(x + inset, top + i, TIP_LIT)                # 깎인 단면 하이라이트
    rect(x, top + taper, x + w - 1, bottom, body)
    rect(x, top + taper, x + 1, bottom, lit)            # 왼쪽 하이라이트
    rect(x + w - 1, top + taper, x + w - 1, bottom, WOOD_DARK)
    for _ in range(3):                                  # 세로 결
        gy = rint(top + taper + 1, max(top + taper + 2, bottom - 4))
        for i in range(rint(3, 7)):
            put(x + rint(1, max(1, w - 2)), gy + i, WOOD_DARK)
    STAKES.append((x, w, top))


# ---------------------------------------------------------------- 조립
plank_band(*BAND_A, 54, 96)
rect(0, 35, W - 1, 35, SEAM)
plank_band(*BAND_B, 62, 110)
rect(0, 48, W - 1, 48, SEAM)
plank_band(*BAND_C, 58, 100)

# 하단 마감 (UI에 가려지는 구간)
for x in range(W):
    put(x, GROUND_ROWS[0], GROUND[0])
    for y in range(GROUND_ROWS[0] + 1, H):
        put(x, y, GROUND[1])

# 지지 기둥 - 위로 길게 뻗어 뾰족하게 깎인 통나무
POST_XS = list(range(20, W, 56))
for px in POST_XS:
    stake(px, 10, 1, H - 1, POST, POST_LIT)

# 기둥 사이 짧은 각목들 - 윗변을 들쭉날쭉하게
reseed(8080)
for px in POST_XS:
    x = px + 14
    while x < px + 50:
        w = rint(4, 9)
        stake(x, w, rint(1, 15), CAP[1], WOOD[rint(1, 3)], WOOD_LIT)
        x += w + rint(4, 10)

# 상단 철제 빔 (각목 사이를 가로로 묶어 줌)
rect(0, CAP[0], W - 1, CAP[1], STEEL)
rect(0, CAP[0], W - 1, CAP[0], STEEL_LIT)
rect(0, CAP[1], W - 1, CAP[1], STEEL_DARK)
for x in range(6, W, 16):
    rect(x, CAP[0] + 1, x + 1, CAP[1] - 1, BOLT)

# 기둥 철제 밴드 + 볼트
for px in POST_XS:
    for by in (BAND_A[1] - 2, BAND_C[0] + 2):
        rect(px - 2, by, px + 11, by + 4, STEEL)
        rect(px - 2, by, px + 11, by, STEEL_LIT)
        rect(px - 2, by + 4, px + 11, by + 4, STEEL_DARK)
        rect(px, by + 1, px + 1, by + 2, BOLT)
        rect(px + 7, by + 1, px + 8, by + 2, BOLT)

# 사선 철제 보강대 (한 칸 걸러)
for i, px in enumerate(POST_XS):
    if i % 2:
        continue
    x0, x1 = px + 12, px + 52
    for step in range(x1 - x0):
        t = step / max(1, x1 - x0 - 1)
        y = int(BAND_C[1] - 1 - t * (BAND_C[1] - BAND_A[1] - 2))
        for k in range(3):
            put(x0 + step, y + k, STEEL if k else STEEL_LIT)


# ---------------------------------------------------------------- 외곽선
def outline(g):
    src = [row[:] for row in g]
    for y in range(H):
        for x in range(W):
            if src[y][x] is not None:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = (x + dx) % W, y + dy
                if 0 <= ny < H and src[ny][nx] is not None:
                    g[y][x] = EDGE
                    break


outline(grid)
BASE = [row[:] for row in grid]


# ---------------------------------------------------------------- 손상 단계
def crack(g, x, y, length):
    for i in range(length):
        g[min(H - 1, y + i)][x % W] = CHAR
        if i % 3 == 0:
            x += 1 if rnd() < 0.5 else -1


def hole(g, cx, cy, rx, ry):
    for y in range(cy - ry, cy + ry + 1):
        if not (0 <= y < H):
            continue
        for x in range(cx - rx, cx + rx + 1):
            dx = (x - cx) / rx
            dy = (y - cy) / ry
            d = dx * dx + dy * dy
            if d <= 1.0 and (d < 0.75 or rnd() < 0.6):
                g[y][x % W] = None
            elif d <= 1.18 and rnd() < 0.35:
                g[y][x % W] = CHAR


def snap_stake(g, x, w, top, break_row):
    """각목을 부러뜨려 윗변 실루엣을 무너뜨린다"""
    for y in range(top, break_row):
        for ox in range(-1, w + 1):
            g[y][(x + ox) % W] = None
    for ox in range(w):                                  # 부러진 단면
        if rnd() < 0.75:
            g[break_row][(x + ox) % W] = CHAR
        if rnd() < 0.4:
            g[break_row - 1][(x + ox) % W] = None


def make_damaged(level):
    """level 1: 금감 / level 2: 부서짐 (1의 손상 포함)"""
    reseed(4242)
    g = [row[:] for row in BASE]
    for _ in range(14):
        crack(g, rint(0, W - 1), rint(24, 46), rint(6, 16))
    for _ in range(10):                                  # 판자 모서리 파임
        x, y = rint(0, W - 1), rint(23, 53)
        for oy in range(rint(2, 4)):
            for ox in range(rint(2, 5)):
                g[min(H - 1, y + oy)][(x + ox) % W] = None
    for _ in range(6):                                   # 그을음
        x, y = rint(0, W - 1), rint(24, 53)
        for oy in range(rint(2, 5)):
            for ox in range(rint(4, 12)):
                if rnd() < 0.6 and g[min(H - 1, y + oy)][(x + ox) % W] is not None:
                    g[min(H - 1, y + oy)][(x + ox) % W] = CHAR
    short = [s for s in STAKES if s[1] < 10]             # 짧은 각목만 후보
    if level == 1:
        reseed(5150)
        for _ in range(3):                               # 작은 관통 구멍
            hole(g, rint(0, W - 1), rint(28, 46), rint(4, 7), rint(3, 5))
        for i, (x, w, top) in enumerate(short):          # 각목 몇 개만 부러뜨림
            if i % 5 == 0:
                snap_stake(g, x, w, top, top + rint(5, 9))
    if level >= 2:
        reseed(9191)
        for _ in range(5):
            hole(g, rint(0, W - 1), rint(26, 50), rint(8, 18), rint(6, 11))
        for _ in range(24):
            crack(g, rint(0, W - 1), rint(23, 48), rint(8, 20))
        for i, (x, w, top) in enumerate(short):          # 짧은 각목은 대부분 부러짐
            if i % 3:
                snap_stake(g, x, w, top, rint(CAP[0] - 8, CAP[0] - 1))
        for i, (x, w, top) in enumerate(STAKES):         # 기둥도 일부 파손
            if w == 10 and i % 3 == 0:
                snap_stake(g, x, w, top, rint(4, 11))
        for _ in range(16):                              # 철제 빔 결손
            x = rint(0, W - 1)
            for ox in range(rint(3, 9)):
                for y in range(CAP[0], CAP[0] + rint(2, 4)):
                    g[y][(x + ox) % W] = None
    return g


STAGES = [("Tower_01", BASE), ("Tower_01_Damaged", make_damaged(1)),
          ("Tower_01_Broken", make_damaged(2))]


# ---------------------------------------------------------------- 출력
def chunk(tag, data):
    return (struct.pack(">I", len(data)) + tag + data
            + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF))


def save(path, rows, w, h, scale, bg=None):
    raw = bytearray()
    for y in range(h):
        raw.append(0)
        row = rows[y // scale]
        for x in range(w):
            c = row[x // scale]
            if c is None:
                raw += (bytes(bg) + b"\xff") if bg else b"\x00\x00\x00\x00"
            else:
                raw += bytes(c) + b"\xff"
    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n"
                + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0))
                + chunk(b"IDAT", zlib.compress(bytes(raw), 9))
                + chunk(b"IEND", b""))
    print("wrote", os.path.basename(path), w, "x", h)


os.makedirs(OUT, exist_ok=True)
for name, g in STAGES:
    save(os.path.join(OUT, name + ".png"), g, W * SCALE, H * SCALE, SCALE)

if PREVIEW:
    gap = 6
    rows = []
    for _, g in STAGES:
        rows += [r[:] for r in g] + [[None] * W for _ in range(gap)]
    save(PREVIEW, rows, W * 2, len(rows) * 2, 2, bg=(54, 45, 37))

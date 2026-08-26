# -*- coding: utf-8 -*-
"""MiningGirl 임시 배경 생성기 - 흙 바닥 + 돌 부스러기 (세로 512x576 도트 -> 4배 확대 2048x2304, 상하좌우 심리스)"""
import os, sys, struct, zlib

W, H, SCALE = 512, 576, 4
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SPRITES = os.path.join(ROOT, "Client", "MiningGirl", "Assets", "Sprites", "InGame")
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(SPRITES, "Background", "Bg_Mine_01.png")
PREVIEW = sys.argv[2] if len(sys.argv) > 2 else None

_seed = 20260827


def rnd():
    """결과 재현을 위한 고정 시드 LCG"""
    global _seed
    _seed = (_seed * 1103515245 + 12345) & 0x7FFFFFFF
    return _seed / 0x7FFFFFFF


def rint(a, b):
    return a + int(rnd() * (b - a + 1)) % (b - a + 1)


# ---------------------------------------------------------------- 팔레트 (흙 톤 5단계)
SOIL = [(40, 33, 28), (47, 39, 32), (54, 45, 37), (61, 51, 42), (68, 57, 47)]
PEBBLE = [(58, 52, 47), (72, 65, 58), (86, 78, 70)]     # 돌 부스러기
PEBBLE_D = (34, 29, 26)                                  # 돌 아래 그림자

grid = [[SOIL[2]] * W for _ in range(H)]


def put(x, y, c):
    grid[y % H][x % W] = c                               # 좌표를 감아서 심리스 유지


def noise(cw, ch):
    """격자 랜덤값을 감아서 보간한 밸류 노이즈"""
    gw, gh = W // cw, H // ch
    vals = [[rnd() for _ in range(gw)] for _ in range(gh)]
    out = [[0.0] * W for _ in range(H)]
    for y in range(H):
        fy = y / ch
        y0 = int(fy) % gh; y1 = (y0 + 1) % gh
        ty = fy - int(fy); ty = ty * ty * (3 - 2 * ty)
        for x in range(W):
            fx = x / cw
            x0 = int(fx) % gw; x1 = (x0 + 1) % gw
            tx = fx - int(fx); tx = tx * tx * (3 - 2 * tx)
            a = vals[y0][x0] * (1 - tx) + vals[y0][x1] * tx
            b = vals[y1][x0] * (1 - tx) + vals[y1][x1] * tx
            out[y][x] = a * (1 - ty) + b * ty
    return out


# ---------------------------------------------------------------- 1) 흙 바닥
n1 = noise(128, 96)     # 큰 얼룩
n2 = noise(32, 32)      # 중간 결
n3 = noise(8, 8)        # 잔입자
n4 = noise(4, 4)        # 해상도가 올라간 만큼 추가한 미세 결
for y in range(H):
    for x in range(W):
        t = n1[y][x] * 0.45 + n2[y][x] * 0.28 + n3[y][x] * 0.17 + n4[y][x] * 0.1
        t = 0.5 + (t - 0.5) * 0.8                        # 대비를 눌러 차분하게
        grid[y][x] = SOIL[max(0, min(len(SOIL) - 1, int(t * len(SOIL))))]

# ---------------------------------------------------------------- 2) 돌 부스러기
for _ in range(600):
    x, y = rint(0, W - 1), rint(0, H - 1)
    col = PEBBLE[rint(0, 2)]
    w, h = rint(2, 6), rint(2, 4)
    for oy in range(h + 1):
        for ox in range(w + 1):
            put(x + ox, y + oy, col)
    for ox in range(w + 1):
        put(x + ox, y + h + 1, PEBBLE_D)                 # 아래쪽 그림자 한 줄

for _ in range(880):                                     # 더 잘게 흩뿌린 모래알 (2x2 도트)
    x, y = rint(0, W - 1), rint(0, H - 1)
    col = PEBBLE[rint(0, 1)]
    for oy in range(2):
        for ox in range(2):
            put(x + ox, y + oy, col)


# ---------------------------------------------------------------- 출력
def chunk(tag, data):
    return (struct.pack(">I", len(data)) + tag + data
            + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF))


def save(path, scale):
    w, h = W * scale, H * scale
    raw = bytearray()
    for y in range(h):
        raw.append(0)
        row = grid[y // scale]
        for x in range(w):
            raw += bytes(row[x // scale]) + b"\xff"
    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n"
                + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0))
                + chunk(b"IDAT", zlib.compress(bytes(raw), 9))
                + chunk(b"IEND", b""))
    print("wrote", path, w, "x", h)


save(OUT, SCALE)
if PREVIEW:
    save(PREVIEW, 1)

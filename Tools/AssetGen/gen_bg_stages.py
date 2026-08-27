# -*- coding: utf-8 -*-
"""스테이지 2~5 배경 생성기 - palette_stages.py가 정한 축을 그림으로 옮긴다

`gen_bg.py`와 같은 파이프라인(512x576 도트 -> 4배 확대 2048x2304, 상하좌우 심리스)에
**광맥 한 겹**을 더한다. 1번은 이미 그려져 있으므로 여기서 다시 만들지 않는다.

색과 광맥 밀도는 여기서 정하지 않는다. `palette_stages.STAGES`가 유일한 출처이고
이 파일은 그 값을 큰 캔버스에 옮기기만 한다. 시안과 결과물이 갈라지지 않게 하려면
축을 두 군데 적어 두면 안 된다.

`STAGES`의 `vein_n`은 **이 타일 기준 개수**다. 시안 조각이 그 값을 환산해서 쓴다.
반대로 잡았다가 한 번 틀렸다 - 150x200 조각에서 적당해 보이던 밀도가 512x576
한 장에서는 광맥밭이 됐다. 작은 크롭으로는 밀도를 판단할 수 없다.

    python Tools/AssetGen/gen_bg_stages.py            # 2~5 전부
    python Tools/AssetGen/gen_bg_stages.py 3          # 3번만
    python Tools/AssetGen/gen_bg_stages.py "" <미리보기폴더>   # 전부 + 미리보기

생성 뒤 `python Tools/AssetGen/gen_meta.py background`으로 .meta를 붙인다.
"""
import os, struct, sys, zlib

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from palette_stages import STAGES, vein_field

W, H, SCALE = 512, 576, 4
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUTDIR = os.path.join(ROOT, "Client", "MiningGirl", "Assets",
                      "Sprites", "InGame", "Background")

_seed = 0


def rnd():
    """결과 재현을 위한 고정 시드 LCG"""
    global _seed
    _seed = (_seed * 1103515245 + 12345) & 0x7FFFFFFF
    return _seed / 0x7FFFFFFF


def rint(a, b):
    return a + int(rnd() * (b - a + 1)) % (b - a + 1)


def noise(cw, ch):
    """격자 랜덤값을 감아서 보간한 밸류 노이즈 - 감기 때문에 이음매가 안 생긴다"""
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


def build(st):
    global _seed
    _seed = 20260827 + st["id"] * 7919      # 칸마다 다른 배치, 다시 돌려도 같은 그림
    soil, peb, sh = st["soil"], st["pebble"], st["shadow"]
    grid = [[soil[2]] * W for _ in range(H)]

    def put(x, y, c):
        grid[y % H][x % W] = c                       # 좌표를 감아서 심리스 유지

    # 1) 바닥
    n1, n2 = noise(128, 96), noise(32, 32)
    n3, n4 = noise(8, 8), noise(4, 4)
    for y in range(H):
        row, r1, r2, r3, r4 = grid[y], n1[y], n2[y], n3[y], n4[y]
        for x in range(W):
            t = r1[x] * .45 + r2[x] * .28 + r3[x] * .17 + r4[x] * .1
            t = .5 + (t - .5) * .8                   # 대비를 눌러 차분하게
            row[x] = soil[max(0, min(4, int(t * 5)))]

    # 2) 돌 부스러기와 모래알 - gen_bg.py와 같은 개수
    for _ in range(600):
        x, y = rint(0, W - 1), rint(0, H - 1)
        col = peb[rint(0, 2)]
        bw, bh = rint(2, 6), rint(2, 4)
        for oy in range(bh + 1):
            for ox in range(bw + 1):
                put(x + ox, y + oy, col)
        for ox in range(bw + 1):
            put(x + ox, y + bh + 1, sh)               # 아래쪽 그림자 한 줄
    for _ in range(880):
        x, y = rint(0, W - 1), rint(0, H - 1)
        col = peb[rint(0, 1)]
        for oy in range(2):
            for ox in range(2):
                put(x + ox, y + oy, col)

    # 3) 광맥 - 비스듬히 흐르는 가는 줄. 깊어질수록 늘고 뜨거워진다.
    #    위아래로 번지는 한 줄(halo)은 광맥 색과 가장 어두운 바닥을 섞은 것이라
    #    바닥이 바뀌면 같이 따라간다. 고정색을 쓰면 칸마다 겉돈다.
    n = 0
    if st["vein"]:
        n = st["vein_n"]
        vein_field(put, st, rnd, W, H, n, 82)
    return grid, n


def chunk(tag, data):
    return (struct.pack(">I", len(data)) + tag + data
            + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF))


def save(grid, path, scale):
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
    print("  wrote", path, w, "x", h)


def main():
    only = int(sys.argv[1]) if len(sys.argv) > 1 and sys.argv[1] else None
    preview = sys.argv[2] if len(sys.argv) > 2 else None
    for st in STAGES:
        if st["id"] == 1 or (only and st["id"] != only):
            continue                                  # 1번은 이미 그려져 있다
        print("Bg_Mine_%02d  %s" % (st["id"], st["name"]))
        grid, n = build(st)
        print("  광맥 %d줄" % n)
        save(grid, os.path.join(OUTDIR, "Bg_Mine_%02d.png" % st["id"]), SCALE)
        if preview:
            save(grid, os.path.join(preview, "bg_%02d_preview.png" % st["id"]), 1)


if __name__ == "__main__":
    main()

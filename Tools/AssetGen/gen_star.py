# -*- coding: utf-8 -*-
"""MiningGirl 결과 화면 별 아이콘 생성기

Star.png       채운 별 - 원석 결정 느낌의 5각별
Star_Empty.png 빈 별   - 같은 실루엣을 파낸 자리. 어둡지만 테두리가 살아 있어
                        "다시 하면 채울 수 있는 칸"으로 읽히게 한다

256x256 출력. 4배 슈퍼샘플링해서 가장자리를 부드럽게 다듬는다(UI라 크기가
기기마다 달라 도트를 그대로 쓰면 계단이 진다).
"""
import math, os, struct, sys, zlib

SIZE = 256          # 출력 픽셀
SS = 4              # 슈퍼샘플 배율
N = SIZE * SS

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
UI = os.path.join(ROOT, "Client", "MiningGirl", "Assets", "Sprites", "UI")
OUT = sys.argv[1] if len(sys.argv) > 1 else UI
PREVIEW = sys.argv[2] if len(sys.argv) > 2 else None

# ---------------------------------------------------------------- 팔레트
# 폐광 컨셉이라 매끈한 금색보다 원석 느낌으로. 다만 별 형태는 그대로 둔다.
GEM_LIT = (255, 226, 138)
GEM_MID = (243, 186, 74)
GEM_DIM = (206, 142, 46)
GEM_DEEP = (158, 98, 32)
GEM_RIM = (94, 54, 20)
SPARK = (255, 250, 226)

HOLE_IN = (44, 38, 40)          # 파낸 안쪽
HOLE_IN_D = (30, 26, 28)
# 테두리 돌. 채운 별의 테두리(GEM_RIM)와 같은 갈색 계열로 맞춘다 - 세 칸이 한 줄로
# 붙는 UI라 테두리 색이 다르면 '같은 칸의 채움/비움'이 아니라 서로 다른 아이콘으로 읽힌다.
STONE = (126, 80, 36)
STONE_LIT = (168, 112, 54)
STONE_DIM = (84, 50, 22)
HOLE_RIM = (36, 31, 33)

CX = CY = N / 2.0
R_OUT = N * 0.47
R_IN = R_OUT * 0.46


def star_points(r_out, r_in, rot=-math.pi / 2):
    """꼭짓점 10개짜리 5각별"""
    pts = []
    for i in range(10):
        a = rot + i * math.pi / 5
        r = r_out if i % 2 == 0 else r_in
        pts.append((CX + math.cos(a) * r, CY + math.sin(a) * r))
    return pts


def inside(pts, x, y):
    """짝수-홀수 규칙 내부 판정"""
    hit = False
    j = len(pts) - 1
    for i in range(len(pts)):
        xi, yi = pts[i]
        xj, yj = pts[j]
        if (yi > y) != (yj > y) and x < (xj - xi) * (y - yi) / (yj - yi) + xi:
            hit = not hit
        j = i
    return hit


OUTER = star_points(R_OUT, R_IN)
INNER = star_points(R_OUT * 0.86, R_IN * 0.80)      # 테두리 안쪽 경계


ROT = -math.pi / 2                                   # 별 꼭짓점이 위를 향하도록
LIGHT = math.radians(-125)                           # 빛은 왼쪽 위에서

# 면 10장의 밝기. 별 꼭짓점 사이 구간이 그대로 한 면이 되도록 각도를 맞춘다.
FACET = []
for _i in range(10):
    _dir = ROT + (_i + 0.5) * (math.pi / 5)          # 그 면이 향하는 방향
    FACET.append(0.72 + 0.46 * max(0.0, math.cos(_dir - LIGHT)))


def facet_of(x, y):
    ang = (math.atan2(y - CY, x - CX) - ROT) % (2 * math.pi)
    return int(ang / (math.pi / 5)) % 10


def mix(c, f):
    return tuple(max(0, min(255, int(v * f))) for v in c)


def lerp(a, b, t):
    t = max(0.0, min(1.0, t))
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def sparkle(x, y, px, py, r):
    """네 갈래 반짝임 (애스트로이드) 내부 판정"""
    dx, dy = abs(x - px) / r, abs(y - py) / r
    if dx > 1 or dy > 1:
        return False
    return dx ** 0.667 + dy ** 0.667 <= 1.0


SP_X, SP_Y, SP_R = CX - R_OUT * 0.26, CY - R_OUT * 0.30, R_OUT * 0.17


def render(filled):
    """슈퍼샘플 버퍼 한 장을 그린다 -> [(r,g,b,a)] * N*N"""
    buf = [None] * (N * N)
    for y in range(N):
        row = y * N
        vy = (y - CY) / R_OUT
        for x in range(N):
            if not inside(OUTER, x, y):
                continue
            edge = not inside(INNER, x, y)
            d = math.hypot(x - CX, y - CY) / R_OUT

            if filled:
                if edge:
                    buf[row + x] = GEM_RIM
                    continue
                if sparkle(x, y, SP_X, SP_Y, SP_R):
                    buf[row + x] = SPARK
                    continue
                # 면마다 밝기가 다른 결정. 위아래로 완만한 기울기를 얹는다.
                f = FACET[facet_of(x, y)] * (1.0 - vy * 0.16)
                if d > 0.72:                          # 끝으로 갈수록 짙게
                    f *= 0.88
                # 밝기에 따라 짙은 색 -> 밝은 색으로 연속 보간 (경계선이 안 생기게)
                buf[row + x] = lerp(GEM_DIM, GEM_LIT, (f - 0.68) / 0.52)
            else:
                if edge:
                    # 위 테두리는 어둡고 아래 테두리는 밝게 - 파인 자리로 보이게
                    lit = math.sin(math.atan2(y - CY, x - CX)) * 0.5 + 0.5
                    if lit > 0.64:
                        buf[row + x] = STONE_LIT
                    elif lit < 0.36:
                        buf[row + x] = STONE_DIM
                    else:
                        buf[row + x] = STONE
                    continue
                # 안쪽은 파낸 자리. 위쪽에 그림자가 지고 아래로 갈수록 살짝 밝다.
                buf[row + x] = lerp(HOLE_RIM, HOLE_IN, (vy + 0.55) / 1.05)
    return buf


def downsample(buf):
    """SS배 버퍼를 평균내어 SIZE로 줄인다 (가장자리 안티에일리어싱)"""
    out = [[None] * SIZE for _ in range(SIZE)]
    for oy in range(SIZE):
        for ox in range(SIZE):
            r = g = b = a = 0
            for sy in range(SS):
                base = (oy * SS + sy) * N + ox * SS
                for sx in range(SS):
                    c = buf[base + sx]
                    if c is not None:
                        r += c[0]; g += c[1]; b += c[2]; a += 255
            n = SS * SS
            if a == 0:
                out[oy][ox] = (0, 0, 0, 0)
            else:
                cnt = a // 255
                out[oy][ox] = (r // cnt, g // cnt, b // cnt, a // n)
    return out


def chunk(tag, data):
    return (struct.pack(">I", len(data)) + tag + data
            + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF))


def save(path, rows, bg=None):
    h = len(rows); w = len(rows[0])
    raw = bytearray()
    for y in range(h):
        raw.append(0)
        for x in range(w):
            c = rows[y][x]
            if bg is not None:                        # 미리보기용 배경 합성
                a = c[3] / 255.0
                c = tuple(int(c[i] * a + bg[i] * (1 - a)) for i in range(3)) + (255,)
            raw += bytes(c)
    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n"
                + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0))
                + chunk(b"IDAT", zlib.compress(bytes(raw), 9))
                + chunk(b"IEND", b""))
    print("wrote", os.path.basename(path), w, "x", h)


os.makedirs(OUT, exist_ok=True)
filled = downsample(render(True))
empty = downsample(render(False))
save(os.path.join(OUT, "Star.png"), filled)
save(os.path.join(OUT, "Star_Empty.png"), empty)

if PREVIEW:
    # 결과 화면처럼 별 3개를 나란히: 채움2 + 빔1, 그리고 전부 빔(실패) 한 줄
    gap = 12
    w = SIZE * 3 + gap * 4
    h = SIZE * 2 + gap * 3
    BG = (38, 33, 36)
    rows = [[BG + (255,)] * w for _ in range(h)]
    for line, states in enumerate(((True, True, False), (False, False, False))):
        for i, st in enumerate(states):
            src = filled if st else empty
            ox = gap + i * (SIZE + gap)
            oy = gap + line * (SIZE + gap)
            for y in range(SIZE):
                for x in range(SIZE):
                    c = src[y][x]
                    a = c[3] / 255.0
                    rows[oy + y][ox + x] = tuple(
                        int(c[k] * a + BG[k] * (1 - a)) for k in range(3)) + (255,)
    save(PREVIEW, rows)

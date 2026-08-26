# -*- coding: utf-8 -*-
"""MiningGirl 임시 몬스터 스프라이트 생성기 (32x32 도트를 4배 확대해 128x128 PNG로 출력)"""
import zlib, struct, os, sys

S = 32          # 논리 도트 해상도
SCALE = 4       # 최종 128x128
ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SPRITES = os.path.join(ROOT, "Client", "MiningGirl", "Assets", "Sprites", "InGame")
OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(SPRITES, "Monster")


def blank():
    return [[None] * S for _ in range(S)]


def put(g, x, y, c):
    if 0 <= x < S and 0 <= y < S and c is not None:
        g[y][x] = c


def rect(g, x0, y0, x1, y1, c):
    for y in range(y0, y1 + 1):
        for x in range(x0, x1 + 1):
            put(g, x, y, c)


def ellipse(g, cx, cy, rx, ry, c, y0=0, y1=S - 1):
    for y in range(max(0, y0), min(S - 1, y1) + 1):
        for x in range(S):
            dx = (x - cx + 0.5) / (rx + 0.5)
            dy = (y - cy + 0.5) / (ry + 0.5)
            if dx * dx + dy * dy <= 1.0:
                put(g, x, y, c)


def line(g, x0, y0, x1, y1, c, w=1):
    steps = max(abs(x1 - x0), abs(y1 - y0)) * 2 + 1
    for i in range(steps + 1):
        t = i / steps
        x = round(x0 + (x1 - x0) * t)
        y = round(y0 + (y1 - y0) * t)
        for oy in range(w):
            for ox in range(w):
                put(g, x + ox, y + oy, c)


def mirror(g):
    """왼쪽 절반을 오른쪽에 대칭 복사"""
    for y in range(S):
        for x in range(S // 2):
            g[y][S - 1 - x] = g[y][x]


def outline(g, col):
    """실루엣 바깥 1픽셀에 아웃라인"""
    src = [row[:] for row in g]
    for y in range(S):
        for x in range(S):
            if src[y][x] is not None:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < S and 0 <= ny < S and src[ny][nx] is not None:
                    g[y][x] = col
                    break


def chunk(tag, data):
    return (struct.pack(">I", len(data)) + tag + data
            + struct.pack(">I", zlib.crc32(tag + data) & 0xFFFFFFFF))


def save_png(path, rows, w, h):
    raw = bytearray()
    for y in range(h):
        raw.append(0)
        row = rows[y // SCALE]
        for x in range(w):
            c = row[x // SCALE]
            raw += bytes(c if c else (0, 0, 0, 0))
    png = (b"\x89PNG\r\n\x1a\n"
           + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0))
           + chunk(b"IDAT", zlib.compress(bytes(raw), 9))
           + chunk(b"IEND", b""))
    with open(path, "wb") as f:
        f.write(png)


# ---------------------------------------------------------------- 몬스터 정의
def slime():
    """Monster_001 : 광석 슬라임 - 기본 잡몹"""
    g = blank()
    BODY = (86, 196, 118, 255); DARK = (52, 148, 90, 255); LIT = (150, 232, 160, 255)
    ORE = (120, 220, 240, 255); ORE_D = (58, 150, 190, 255); INK = (26, 60, 42, 255)
    ellipse(g, 15, 20, 11, 9, BODY, y0=11, y1=27)
    rect(g, 6, 26, 25, 27, BODY)
    ellipse(g, 15, 25, 11, 4, DARK, y0=24, y1=27)       # 아래쪽 음영
    ellipse(g, 10, 16, 4, 3, LIT)                       # 하이라이트
    put(g, 9, 13, LIT); put(g, 10, 13, LIT)
    for x, y in ((19, 21), (20, 22), (21, 22), (20, 23), (11, 23), (12, 23), (11, 24)):
        put(g, x, y, ORE)                               # 몸속 광석 조각
    put(g, 21, 23, ORE_D); put(g, 12, 24, ORE_D)
    rect(g, 11, 17, 12, 19, INK)                        # 눈
    rect(g, 19, 17, 20, 19, INK)
    put(g, 11, 17, (240, 255, 245, 255)); put(g, 19, 17, (240, 255, 245, 255))
    rect(g, 14, 22, 17, 22, INK)                        # 입
    put(g, 14, 21, INK); put(g, 17, 21, INK)
    outline(g, (22, 52, 38, 255))
    return g


def bat():
    """Monster_002 : 동굴 박쥐 - 빠른 유형"""
    g = blank()
    BODY = (146, 108, 196, 255); DARK = (96, 66, 146, 255)
    WING = (118, 84, 172, 255); WING_D = (78, 52, 124, 255); EYE = (255, 96, 96, 255)
    for i in range(9):                                   # 왼쪽 날개
        line(g, 12 - i, 12 + i // 2, 12 - i, 20 - i // 2, WING if i % 3 else WING_D)
    line(g, 3, 14, 12, 11, WING_D)
    ellipse(g, 15, 18, 5, 6, BODY, y0=12, y1=24)
    ellipse(g, 15, 22, 5, 3, DARK, y0=21, y1=24)
    line(g, 11, 8, 13, 13, BODY, w=2)                    # 귀
    put(g, 12, 7, DARK); put(g, 11, 9, DARK)
    mirror(g)
    rect(g, 12, 16, 13, 17, EYE); rect(g, 18, 16, 19, 17, EYE)
    put(g, 12, 16, (255, 220, 220, 255)); put(g, 18, 16, (255, 220, 220, 255))
    rect(g, 14, 20, 17, 20, (250, 250, 255, 255))        # 송곳니
    put(g, 14, 21, (250, 250, 255, 255)); put(g, 17, 21, (250, 250, 255, 255))
    outline(g, (44, 28, 72, 255))
    return g


def spider():
    """Monster_003 : 갱도 거미 - 다리 8개"""
    g = blank()
    BODY = (74, 78, 104, 255); DARK = (46, 48, 70, 255); LIT = (108, 112, 142, 255)
    LEG = (40, 42, 62, 255); EYE = (255, 176, 64, 255)
    for i, (ex, ey) in enumerate(((2, 10), (1, 16), (2, 23), (5, 27))):   # 왼쪽 다리 4개
        line(g, 11, 16 + i, ex + 2, ey - 3, LEG)
        line(g, ex + 2, ey - 3, ex, ey, LEG)
    mirror(g)
    ellipse(g, 15, 19, 8, 7, BODY, y0=13, y1=26)         # 배
    ellipse(g, 15, 23, 7, 4, DARK, y0=22, y1=26)
    ellipse(g, 13, 16, 3, 2, LIT)
    ellipse(g, 15, 11, 5, 4, DARK, y0=7, y1=14)          # 머리
    for x, y in ((12, 10), (18, 10), (14, 12), (17, 12)):
        put(g, x, y, EYE)
    put(g, 13, 10, EYE); put(g, 19, 10, EYE)
    rect(g, 14, 8, 17, 8, (34, 36, 54, 255))
    for x, y in ((12, 20), (18, 21), (15, 22), (13, 24), (17, 18)):
        put(g, x, y, DARK)                               # 등껍질 무늬
    outline(g, (24, 26, 40, 255))
    return g


def golem():
    """Monster_004 : 암석 골렘 - 탱커"""
    g = blank()
    ROCK = (140, 124, 106, 255); DARK = (96, 84, 72, 255); LIT = (176, 160, 140, 255)
    GEM = (255, 148, 56, 255)
    rect(g, 10, 10, 21, 23, ROCK)                         # 몸통
    rect(g, 11, 4, 20, 11, ROCK)                          # 머리
    rect(g, 4, 12, 8, 20, ROCK); rect(g, 23, 12, 27, 20, ROCK)   # 팔 (몸통과 한 칸 띄움)
    rect(g, 3, 19, 9, 25, ROCK); rect(g, 22, 19, 28, 25, ROCK)   # 주먹
    rect(g, 11, 24, 14, 29, ROCK); rect(g, 17, 24, 20, 29, ROCK)  # 다리
    for x0, y0, x1, y1 in ((10, 21, 21, 23), (3, 23, 9, 25), (22, 23, 28, 25),
                           (11, 27, 14, 29), (17, 27, 20, 29)):
        rect(g, x0, y0, x1, y1, DARK)                     # 아래쪽 음영
    rect(g, 11, 4, 20, 5, LIT); rect(g, 10, 10, 21, 11, LIT)
    rect(g, 4, 12, 8, 13, LIT); rect(g, 23, 12, 27, 13, LIT)
    rect(g, 12, 7, 14, 8, GEM); rect(g, 17, 7, 19, 8, GEM)       # 발광 눈
    put(g, 12, 7, (255, 236, 180, 255)); put(g, 17, 7, (255, 236, 180, 255))
    for x, y in ((14, 13), (15, 14), (16, 15), (15, 16), (14, 17), (17, 12),
                 (12, 15), (19, 18), (11, 19), (19, 14)):
        put(g, x, y, DARK)                                # 균열
    rect(g, 14, 17, 17, 19, GEM)                          # 코어
    rect(g, 15, 18, 16, 18, (255, 236, 180, 255))
    outline(g, (58, 48, 40, 255))
    return g


def wraith():
    """Monster_005 : 망령 광부 - 정예/보스급"""
    g = blank()
    BODY = (140, 214, 226, 210); DARK = (92, 168, 190, 210); LIT = (198, 244, 250, 220)
    HELM = (216, 176, 64, 255); HELM_D = (154, 118, 36, 255)
    LAMP = (255, 244, 176, 255); EYE = (86, 244, 255, 255)
    ellipse(g, 15, 14, 7, 7, BODY, y0=8, y1=20)           # 머리/상체
    for y in range(19, 26):                               # 아래로 퍼지는 영혼 자락
        w = 7 + (y - 19) // 3
        rect(g, 15 - w, y, 16 + w, y, BODY)
    for x0, x1, bottom in ((6, 10, 28), (13, 18, 30), (21, 25, 27)):
        for y in range(26, bottom + 1):                   # 아래로 갈수록 가늘어지는 자락 3갈래
            t = (y - 26) // 2
            if x0 + t <= x1 - t:
                rect(g, x0 + t, y, x1 - t, y, BODY)
    ellipse(g, 15, 23, 8, 4, DARK, y0=22, y1=25)
    ellipse(g, 12, 12, 3, 2, LIT)
    rect(g, 8, 6, 23, 9, HELM)                            # 광부 헬멧
    rect(g, 8, 9, 23, 9, HELM_D)
    ellipse(g, 15, 5, 7, 4, HELM, y0=2, y1=6)
    rect(g, 13, 2, 18, 4, LAMP)                           # 헬멧 램프
    rect(g, 14, 2, 17, 2, (255, 255, 236, 255))
    rect(g, 11, 12, 13, 14, EYE); rect(g, 18, 12, 20, 14, EYE)   # 눈
    put(g, 11, 12, (240, 255, 255, 255)); put(g, 18, 12, (240, 255, 255, 255))
    rect(g, 13, 17, 18, 18, DARK)                         # 벌린 입
    put(g, 13, 16, DARK); put(g, 18, 16, DARK)
    outline(g, (34, 74, 96, 255))
    return g


MONSTERS = [("Monster_001_Slime", slime), ("Monster_002_Bat", bat),
            ("Monster_003_Spider", spider), ("Monster_004_Golem", golem),
            ("Monster_005_Wraith", wraith)]

os.makedirs(OUT, exist_ok=True)
for name, fn in MONSTERS:
    save_png(os.path.join(OUT, name + ".png"), fn(), S * SCALE, S * SCALE)
    print("wrote", name + ".png")

# 확인용 컨택트 시트 (체커보드 배경 합성)
if len(sys.argv) > 2:
    grids = [fn() for _, fn in MONSTERS]
    w = S * 5
    sheet = [[None] * w for _ in range(S)]
    for i, g in enumerate(grids):
        for y in range(S):
            for x in range(S):
                c = g[y][x]
                v = 210 if ((x // 4 + y // 4) % 2 == 0) else 170
                if c is None:
                    c = (v, v, v, 255)
                elif c[3] < 255:
                    a = c[3] / 255.0
                    c = tuple(int(c[k] * a + v * (1 - a)) for k in range(3)) + (255,)
                sheet[y][i * S + x] = c
    save_png(sys.argv[2], sheet, w * SCALE, S * SCALE)
    print("wrote sheet", sys.argv[2])

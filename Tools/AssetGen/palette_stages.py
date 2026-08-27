# -*- coding: utf-8 -*-
"""스테이지 배경 팔레트 스트립 - 그리기 전에 축부터 배치한다

배경은 스테이지별 개성이 아니라 "깊어질수록"이라는 한 방향의 연속 하강이다.
한 장씩 그려 나가면 직전 한 장만 보고 "조금만 더"를 반복하게 되어 중간이 균일해지므로,
**칸을 한 축 위에 전부 배치하고 확정한 뒤에** 개별 이미지를 그린다. 이 파일이 그 축이다.

## 범위가 10칸에서 5칸으로 줄면서 달라진 것

원래 계획은 1~10이었고 "인접 한 칸은 미묘해도 되지만 두세 칸 건너뛰면 확실히 달라야
한다"가 기준이었다. **5칸에서는 그 기준을 못 쓴다.** 1과 5가 네 걸음밖에 안 떨어져
있어서 "세 칸 건너 비교"라는 검증 자체가 성립하지 않는다.

그래서 기준을 올린다 - **인접한 두 칸이 각자 읽혀야 한다.** 미묘함에 쓸 여유가 없다.
대신 한 걸음에 두 가지를 같이 움직여 걸음마다 신호를 두 개씩 준다.

    바닥 명도   내려간다        (밝기)
    바닥 색상   따뜻함 -> 차가움 (색)
    광맥 세기   올라간다        (없음 -> 강함)
    광맥 색상   회백 -> 청록 -> 청 -> 열

바닥이 어두워지고 식는 동안 광맥은 밝아지고 뜨거워진다. 두 축이 반대로 움직이므로
가장 깊은 칸에서 대비가 최대가 된다. 명도 하강은 주장이 아니라 계산해서 찍는다
(Rec.709 루마, 아래 출력).

스테이지 1은 이미 그려져 있다(`Bg_Mine_01.png`). 여기 1번 칸은 새로 정한 색이 아니라
`gen_bg.py`가 실제로 쓰는 값을 그대로 옮긴 것이다. 축의 출발점은 정하는 게 아니라
이미 화면에 있는 것을 재는 것이다.

    python Tools/AssetGen/palette_stages.py <출력폴더>

산출물은 배경 그림이 아니라 스트립 한 장이다. 이 단계에서 고치는 게 그림을 다시
그리는 것보다 싸다. 확정되면 이 팔레트로 2~5번 배경을 생성한다.
"""
import os, sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from mock_lib import write_png
from mock_reroll import rect, text, tw

PAGE = (16, 15, 18)
LABEL = (150, 144, 162)

# ---------------------------------------------------------------- 축
# soil    바닥 5단계 (어두운 쪽 -> 밝은 쪽)
# pebble  돌 부스러기 3단계 / shadow 돌 아래 그림자
# vein    광맥 색과 밀도(패치당 개수). 1번은 광맥이 없다.
STAGES = (
    dict(id=1, name="흙",
         soil=[(40, 33, 28), (47, 39, 32), (54, 45, 37), (61, 51, 42), (68, 57, 47)],
         pebble=[(58, 52, 47), (72, 65, 58), (86, 78, 70)], shadow=(34, 29, 26),
         vein=None, vein_n=0),
    dict(id=2, name="젖은 흙",
         soil=[(30, 31, 28), (36, 37, 33), (42, 43, 39), (48, 49, 45), (54, 55, 51)],
         pebble=[(52, 54, 50), (64, 66, 61), (76, 78, 73)], shadow=(26, 27, 24),
         vein=(118, 140, 132), vein_n=10),
    dict(id=3, name="암반",
         soil=[(24, 27, 28), (29, 32, 34), (34, 38, 40), (40, 44, 46), (45, 50, 52)],
         pebble=[(54, 60, 63), (66, 72, 76), (78, 85, 90)], shadow=(20, 23, 24),
         vein=(96, 168, 172), vein_n=16),
    dict(id=4, name="결정층",
         soil=[(23, 22, 36), (28, 27, 43), (33, 32, 50), (38, 37, 57), (44, 43, 65)],
         pebble=[(56, 54, 80), (68, 66, 96), (80, 78, 112)], shadow=(18, 17, 28),
         vein=(140, 140, 246), vein_n=22),
    dict(id=5, name="심부",
         soil=[(21, 20, 21), (25, 24, 25), (29, 28, 29), (34, 32, 34), (39, 37, 39)],
         pebble=[(48, 45, 47), (58, 55, 57), (68, 64, 66)], shadow=(14, 13, 14),
         vein=(238, 124, 44), vein_n=30),
)

_seed = 20260827


def rnd():
    global _seed
    _seed = (_seed * 1103515245 + 12345) & 0x7FFFFFFF
    return _seed / 0x7FFFFFFF


def rint(a, b):
    return a + int(rnd() * (b - a + 1)) % (b - a + 1)


def luma(c):
    return 0.2126 * c[0] + 0.7152 * c[1] + 0.0722 * c[2]


def noise(w, h, cw, ch):
    """gen_bg.py와 같은 방식의 감기는 밸류 노이즈"""
    gw, gh = max(1, w // cw), max(1, h // ch)
    vals = [[rnd() for _ in range(gw)] for _ in range(gh)]
    out = [[0.0] * w for _ in range(h)]
    for y in range(h):
        fy = y / ch
        y0 = int(fy) % gh; y1 = (y0 + 1) % gh
        ty = fy - int(fy); ty = ty * ty * (3 - 2 * ty)
        for x in range(w):
            fx = x / cw
            x0 = int(fx) % gw; x1 = (x0 + 1) % gw
            tx = fx - int(fx); tx = tx * tx * (3 - 2 * tx)
            a = vals[y0][x0] * (1 - tx) + vals[y0][x1] * tx
            b = vals[y1][x0] * (1 - tx) + vals[y1][x1] * tx
            out[y][x] = a * (1 - ty) + b * ty
    return out


def patch(st, w, h):
    """그 팔레트로 실제 바닥을 그린 조각. 색표가 아니라 그림으로 봐야 판단이 선다."""
    soil, peb, sh = st["soil"], st["pebble"], st["shadow"]
    g = [[soil[2]] * w for _ in range(h)]
    n1 = noise(w, h, 64, 48); n2 = noise(w, h, 16, 16)
    n3 = noise(w, h, 4, 4); n4 = noise(w, h, 2, 2)
    for y in range(h):
        for x in range(w):
            t = n1[y][x] * .45 + n2[y][x] * .28 + n3[y][x] * .17 + n4[y][x] * .1
            t = .5 + (t - .5) * .8
            g[y][x] = soil[max(0, min(4, int(t * 5)))]

    def put(x, y, c):
        g[y % h][x % w] = c

    for _ in range(w * h // 100):                     # 돌 부스러기
        x, y = rint(0, w - 1), rint(0, h - 1)
        col = peb[rint(0, 2)]
        bw, bh = rint(2, 5), rint(2, 3)
        for oy in range(bh + 1):
            for ox in range(bw + 1):
                put(x + ox, y + oy, col)
        for ox in range(bw + 1):
            put(x + ox, y + bh + 1, sh)

    # 광맥 - 비스듬히 흐르는 가는 줄. 깊어질수록 개수가 늘고 색이 뜨거워진다.
    if st["vein"]:
        v = st["vein"]
        halo = tuple(int(a * .45 + b * .55) for a, b in zip(v, soil[0]))
        for _ in range(st["vein_n"]):
            x, y = rint(0, w - 1), rint(0, h - 1)
            dx = 1 if rnd() > .5 else -1
            for _ in range(rint(10, 26)):
                put(x, y, v); put(x, y + 1, halo); put(x, y - 1, halo)
                x += dx
                if rnd() > .62:
                    y += 1 if rnd() > .5 else -1
    return g


def main():
    out_dir = sys.argv[1] if len(sys.argv) > 1 else "."
    PW, PH, SC = 150, 200, 2                          # 조각 150x200을 2배로 본다
    SW, SH = PW * SC, PH * SC
    GAP, PAD = 28, 32
    SWH, SWG = 34, 4                                  # 색표 한 칸
    ROW = 22                                          # 이름/번호 줄

    W = PAD * 2 + len(STAGES) * (SW + GAP) - GAP
    H = PAD * 2 + ROW + 10 + SH + 12 + SWH * 2 + SWG
    c = [[PAGE] * W for _ in range(H)]

    # 광맥이 "세진다"는 건 밝아진다는 뜻이 아니다. 주황은 Rec.709에서 루마가 낮게
    # 나오지만 검은 바닥 위에서는 가장 강하게 읽힌다. 그래서 재는 것은 두 가지 -
    # **제 바닥 대비 명도비**와 **채도**다. 둘 다 단조 증가해야 축이 성립한다.
    print("칸  이름     바닥 루마  광맥 대비  광맥 채도  개수")
    for i, st in enumerate(STAGES):
        ox = PAD + i * (SW + GAP)
        text(c, str(st["id"]), ox, PAD, 4, LABEL)     # 스테이지 번호
        oy = PAD + ROW + 10
        p = patch(st, PW, PH)
        for y in range(SH):                            # 확대해서 붙인다
            row = p[y // SC]
            for x in range(SW):
                c[oy + y][ox + x] = row[x // SC]

        sy = oy + SH + 12                              # 색표 - 바닥 5 + 돌 3 + 광맥
        for k, col in enumerate(st["soil"]):
            rect(c, ox + k * (SWH + SWG), sy, SWH, SWH, col)
        sy2 = sy + SWH + SWG
        for k, col in enumerate(st["pebble"] + [st["shadow"]]):
            rect(c, ox + k * (SWH + SWG), sy2, SWH, SWH, col)
        if st["vein"]:
            rect(c, ox + 4 * (SWH + SWG), sy2, SWH, SWH, st["vein"])

        if st["vein"]:
            v = st["vein"]
            vl = "%8.2f  %9d" % (luma(v) / luma(st["soil"][2]), max(v) - min(v))
        else:
            vl = "%8s  %9s" % ("-", "-")
        print(" %d  %-6s  %8.1f  %s  %6d"
              % (st["id"], st["name"], luma(st["soil"][2]), vl, st["vein_n"]))

    p = os.path.join(out_dir, "stage_palette_strip.png")
    print("\nwrote", p, write_png(p, c))
    print("위: 그 팔레트로 실제로 그린 바닥 조각 / 아래: 바닥 5단계, 돌 3단계+그림자, 광맥")
    print("1번은 Bg_Mine_01.png가 쓰는 값 그대로다. 2~5번이 이번에 정하는 칸이다.")


if __name__ == "__main__":
    main()

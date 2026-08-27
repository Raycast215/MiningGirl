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

**5번 광맥만 이 단조에서 빠진다(대비 4.41 -> 3.74).** 원래 주황 (238,124,44)이었는데
파이어볼트 폭발의 틴트(색상 26.0도)와 1.3도 차이라 같은 색이었다. 실제 화면 크기로
합성해 보니 폭발이 광맥 무늬의 두꺼운 부분처럼 보이고 파이어볼트 발사체가 광맥 위에서
사라졌다. 파이어볼트는 시작 스킬이라 가장 자주 보는 이펙트다.

밀도를 줄여서는 안 고쳐진다(가늘고 성기게 한 안을 합성해 봤고 여전히 이어져 보였다).
색상이 겹치는 문제라 색상으로만 풀린다. 짙은 잉걸 (226,76,48)로 옮겼다 - 색상 8.4도라
폭발과 18도 떨어지고, 채도 178은 4번(106)보다 여전히 높다.

**대비 열이 여기서 꺾이는 걸 감수한다.** 그 열은 "깊을수록 광맥이 세진다"를 재려고
고른 대리 지표이고, 그보다 상위 규칙이 있다 - **배경이 플레이 레이어와 같은 색이면
안 된다.** 게다가 Rec.709는 검은 바닥 위의 진한 빨강을 낮게 잡는다. 루마 대신
대비+채도로 옮겨 온 이유와 같은 편향이 이번엔 반대로 작용한 것이다.

스테이지 1은 이미 그려져 있다(`Bg_Mine_01.png`). 여기 1번 칸은 새로 정한 색이 아니라
`gen_bg.py`가 실제로 쓰는 값을 그대로 옮긴 것이다. 축의 출발점은 정하는 게 아니라
이미 화면에 있는 것을 재는 것이다.

    python Tools/AssetGen/palette_stages.py <출력폴더>

산출물은 배경 그림이 아니라 스트립 한 장이다. 이 단계에서 고치는 게 그림을 다시
그리는 것보다 싸다. 확정되면 이 팔레트로 2~5번 배경을 생성한다.
"""
import math, os, sys

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
         vein=None, vein_n=0, vein_w=0),
    dict(id=2, name="젖은 흙",
         soil=[(30, 31, 28), (36, 37, 33), (42, 43, 39), (48, 49, 45), (54, 55, 51)],
         pebble=[(52, 54, 50), (64, 66, 61), (76, 78, 73)], shadow=(26, 27, 24),
         vein=(118, 140, 132), vein_n=14, vein_w=1),
    dict(id=3, name="암반",
         soil=[(24, 27, 28), (29, 32, 34), (34, 38, 40), (40, 44, 46), (45, 50, 52)],
         pebble=[(54, 60, 63), (66, 72, 76), (78, 85, 90)], shadow=(20, 23, 24),
         vein=(96, 168, 172), vein_n=22, vein_w=1),
    dict(id=4, name="결정층",
         soil=[(23, 22, 36), (28, 27, 43), (33, 32, 50), (38, 37, 57), (44, 43, 65)],
         pebble=[(56, 54, 80), (68, 66, 96), (80, 78, 112)], shadow=(18, 17, 28),
         vein=(140, 140, 246), vein_n=30, vein_w=2),
    dict(id=5, name="심부",
         soil=[(21, 20, 21), (25, 24, 25), (29, 28, 29), (34, 32, 34), (39, 37, 39)],
         pebble=[(48, 45, 47), (58, 55, 57), (68, 64, 66)], shadow=(14, 13, 14),
         vein=(226, 76, 48), vein_n=40, vein_w=2),
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


def vein(put, st, rand, x, y, length, th):
    """광맥 한 줄. 시안과 실제 배경이 같은 이 함수를 쓴다.

    각도는 밖에서 받는다. 줄마다 따로 정하면 사방으로 흩어져 바위 속 광맥이 아니라
    위에 뿌린 입자처럼 보인다. 양 끝은 가늘어졌다가 사라진다 - 시작과 끝이 뭉툭하면
    벽에 그린 선이 된다.
    """
    v = st["vein"]
    halo = tuple(int(a * .45 + b * .55) for a, b in zip(v, st["soil"][0]))
    sx = 1 if rand() > .5 else -1
    fx, fy = float(x), float(y)
    for i in range(length):
        th += (rand() - .5) * .16                    # 조금씩 휘어 자연스럽게
        fx += math.cos(th) * sx
        fy += math.sin(th)
        ix, iy = int(fx), int(fy)
        e = min(i, length - 1 - i) / max(1.0, length * .3)
        put(ix, iy - 1, halo)
        if e < .4:                                   # 끝은 번짐만 남기고 사라진다
            continue
        w = st["vein_w"] if e >= 1 else 1
        for k in range(w):
            put(ix, iy + k, v)
        put(ix, iy + w, halo)


def vein_field(put, st, rand, w, h, count, base_len):
    """광맥 전체 배치.

    **한 칸 안의 광맥은 같은 층리를 따른다.** 갱도 벽은 지층이라 광맥이 사방으로
    뻗지 않는다. 줄마다 각도를 새로 뽑으면 유성우처럼 보이고, 완전히 수평이면
    도트를 4배로 키웠을 때 주사선이 된다. 그래서 수평에서 6~18도만 기울인
    층리 각을 칸마다 하나 정하고 줄마다 ±10도만 흔든다.

    자리도 고르게 뿌리지 않고 2~4줄씩 뭉친다. 균등 분포는 배경이 아니라 무늬로
    읽힌다. 길이도 0.35~1.65배로 크게 벌려 짧은 반짝임과 긴 줄이 섞이게 한다.
    """
    dip = math.radians(rint2(rand, 6, 18)) * (1 if rand() > .5 else -1)
    made = 0
    while made < count:
        cx, cy = rint2(rand, 0, w - 1), rint2(rand, 0, h - 1)
        for _ in range(min(count - made, rint2(rand, 2, 4))):
            vein(put, st, rand,
                 cx + rint2(rand, -w // 10, w // 10),
                 cy + rint2(rand, -h // 14, h // 14),
                 max(6, int(base_len * (.35 + rand() * 1.3))),
                 dip + (rand() - .5) * .35)
            made += 1


def rint2(rand, a, b):
    return a + int(rand() * (b - a + 1)) % (b - a + 1)


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

    # 흩뿌리는 밀도는 gen_bg.py와 같게 맞춘다. 조각이 실제보다 거칠면 색은 맞아도
    # 질감이 다른 그림을 보고 판단하게 된다 - 512x576에 돌 600, 모래 880이 기준이다.
    for _ in range(round(w * h / 491.5)):             # 돌 부스러기
        x, y = rint(0, w - 1), rint(0, h - 1)
        col = peb[rint(0, 2)]
        bw, bh = rint(2, 5), rint(2, 3)
        for oy in range(bh + 1):
            for ox in range(bw + 1):
                put(x + ox, y + oy, col)
        for ox in range(bw + 1):
            put(x + ox, y + bh + 1, sh)

    for _ in range(round(w * h / 335.1)):             # 잘게 흩뿌린 모래알
        x, y = rint(0, w - 1), rint(0, h - 1)
        col = peb[rint(0, 1)]
        for oy in range(2):
            for ox in range(2):
                put(x + ox, y + oy, col)

    # 광맥 - 비스듬히 흐르는 줄. 깊어질수록 개수가 늘고 굵어지고 뜨거워진다.
    if st["vein"]:
        # vein_n은 실제 타일(512x576) 기준이므로 조각 크기로 환산해서 쓴다.
        # 밀도의 원본은 배송되는 그림 쪽이어야 한다 - 시안이 원본이면 시안에서
        # 좋아 보이는 값이 큰 화면에서 어떻게 되는지 아무도 안 본 채로 나간다.
        vein_field(put, st, rnd, w, h,
                   max(1, round(st["vein_n"] / 2.88)), 24)
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
    print("칸  이름     바닥 루마  광맥 대비  광맥 채도  타일당  굵기")
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
        print(" %d  %-6s  %8.1f  %s  %6d  %4d"
              % (st["id"], st["name"], luma(st["soil"][2]), vl,
                 st["vein_n"], st["vein_w"]))

    p = os.path.join(out_dir, "stage_palette_strip.png")
    print("\nwrote", p, write_png(p, c))
    print("위: 그 팔레트로 실제로 그린 바닥 조각 / 아래: 바닥 5단계, 돌 3단계+그림자, 광맥")
    print("1번은 Bg_Mine_01.png가 쓰는 값 그대로다. 2~5번이 이번에 정하는 칸이다.")


if __name__ == "__main__":
    main()

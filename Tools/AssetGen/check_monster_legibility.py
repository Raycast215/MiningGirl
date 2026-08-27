# -*- coding: utf-8 -*-
"""몬스터가 각 배경에서 바닥에 묻히는 자리가 있는지 훑는다

**이 표는 통과 판정이 아니다.** 어디를 눈으로 봐야 하는지 고르는 데만 쓴다.

재는 것은 "몬스터 몸통 평균 루마 / 그 자리 바닥 평균 루마"이고, 1.0에 가까울수록
그 자리에서 몸이 바닥과 같은 밝기라는 뜻이다. 자리를 여러 곳 뽑아 **최악**을 남긴다.

## 왜 최악인가

처음에는 바닥을 팔레트 중간 단계 하나로 놓고 쟀다. 실제 바닥은 큰 얼룩 노이즈와
광맥 번짐 때문에 자리마다 루마가 두 배 넘게 벌어진다. 대표값으로는 그 안에 몬스터와
겹치는 밝기가 있다는 걸 볼 수 없다. 실제로 그래서 바위 거미가 다섯 배경 전부에서
1.00 언저리인 걸 못 보고 통과시켰고, 개발이 실판에서 잡아 줬다.

## 이 값이 답하지 못하는 것

**테두리나 자체 명암이 만드는 실루엣을 못 잰다.** 광재는 이 표에서 0.98인데 실판에서
잘 튄다 - 위쪽 밝은 테두리가 몸통 평균과 무관하게 윤곽을 만들기 때문이다.

**그래서 값이 낮은 칸은 "봐야 할 곳"이지 "실패"가 아니다.** 판정은 그 자리에 놓고
합성해서 눈으로 한다.

## 표본이 아니라 전수로 훑는다

처음에는 자리를 무작위로 200곳 뽑아 최악을 남겼다. 표본 수를 50/100/200/400으로
스윕해 보니 **400곳에서도 값이 계속 움직였다**(마지막 두 단계 차가 0.08~0.47).
최악은 드문 자리라 무작위로는 잘 안 걸린다 - 즉 그때까지 낸 숫자는 "존재하는 최악"이
아니라 "그 표본에서 걸린 최악"이었다.

그래서 **모든 자리를 훑는다.** 바닥 루마의 누적합 표(summed-area table)를 만들어
어느 자리든 O(1)로 평균을 얻는다. 512x576 전 좌표를 다 봐도 빠르고, **표본 수라는
변수가 아예 없어진다.**

**대신 바닥 평균을 스프라이트의 경계 상자로 잰다** - 실루엣 모양 그대로가 아니다.
몸 바깥의 바닥이 조금 섞이지만, 이 표가 "어디를 볼지 고르는" 용도라 그 오차보다
전수라는 성질이 값지다.

    python Tools/AssetGen/check_monster_legibility.py            # 표만 출력
    python Tools/AssetGen/check_monster_legibility.py --write    # Docs에 기록
"""
import os, sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from mock_lib import decode
from palette_stages import STAGES
import gen_bg_stages as GB

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
ASSETS = os.path.join(ROOT, "Client", "MiningGirl", "Assets")
MON = os.path.join(ASSETS, "Sprites", "InGame", "Monster")
BG1 = os.path.join(ASSETS, "Sprites", "InGame", "Background", "Bg_Mine_01.png")
OUT = os.path.join(ROOT, "Docs", "MonsterLegibility.md")

# 시트 표시명. 파일 이름은 자산 식별자라 다르다.
NAMES = (("Monster_001_Slime", "광석 슬라임"), ("Monster_002_Bat", "동굴 박쥐"),
         ("Monster_003_Spider", "바위 거미"), ("Monster_004_Golem", "갱도지키"),
         ("Monster_005_Wraith", "심층의 것"), ("Monster_006_Rat", "갱도 쥐"),
         ("Monster_007_Slag", "광재 덩이"), ("Monster_008_BrokenMiner", "무너진 갱부"))
def luma(c):
    return .2126 * c[0] + .7152 * c[1] + .0722 * c[2]


def dots(path, step):
    """4배 확대본을 도트 격자로 되돌린다"""
    w, h, px = decode(path)
    return [[tuple(px[((y * step) * w + x * step) * 4 + k] for k in range(4))
             for x in range(w // step)] for y in range(h // step)]


def body_of(name):
    """몸통 평균 루마와 경계 상자 크기"""
    g = dots(os.path.join(MON, name + ".png"), 4)
    n = len(g)
    pts = [(x, y) for y in range(n) for x in range(n) if g[y][x][3] > 128]
    xs = [p[0] for p in pts]; ys = [p[1] for p in pts]
    bw = max(xs) - min(xs) + 1
    bh = max(ys) - min(ys) + 1
    return bw, bh, sum(luma(g[y][x]) for x, y in pts) / len(pts)


def backgrounds():
    out = {}
    w, h, px = decode(BG1)
    out[1] = [[tuple(px[((y * 4) * w + x * 4) * 4 + k] for k in range(3))
               for x in range(w // 4)] for y in range(h // 4)]
    for sid in range(2, 6):
        out[sid] = GB.build(STAGES[sid - 1])[0]
    return out


def integral(grid):
    """가로세로로 감기는 배경이라 오른쪽/아래로 한 번 더 이어 붙여 놓고 누적합"""
    h, w = len(grid), len(grid[0])
    W, H = w * 2, h * 2
    sat = [[0.0] * (W + 1) for _ in range(H + 1)]
    for y in range(H):
        row = grid[y % h]; acc = 0.0
        for x in range(W):
            acc += luma(row[x % w])
            sat[y + 1][x + 1] = sat[y][x + 1] + acc
    return sat, w, h


def worst_all(sat, w, h, bw, bh, mon):
    """모든 자리를 훑어 1.0에 가장 가까운 비를 남긴다"""
    area = float(bw * bh)
    best = 99.0
    for oy in range(h):
        r0 = sat[oy]; r1 = sat[oy + bh]
        for ox in range(w):
            fl = (r1[ox + bw] - r1[ox] - r0[ox + bw] + r0[ox]) / area
            r = mon / fl
            if abs(r - 1.0) < abs(best - 1.0):
                best = r
    return best


def main():
    grids = backgrounds()
    sats = {sid: integral(grids[sid]) for sid in range(1, 6)}
    rows = []
    for fname, label in NAMES:
        bw, bh, mon = body_of(fname)
        cells = [worst_all(*sats[sid], bw, bh, mon) for sid in range(1, 6)]
        rows.append((label, cells))

    head = "| 몬스터 | " + " | ".join("S%d" % s for s in range(1, 6)) + " |"
    sep = "|---" * 6 + "|"
    print(head); print(sep)
    lines = [head, sep]
    for label, cells in rows:
        line = "| %s | %s |" % (label, " | ".join("%.2f" % c for c in cells))
        print(line); lines.append(line)

    if "--write" in sys.argv:
        with open(OUT, "w", encoding="utf-8") as f:
            f.write(DOC % chr(10).join(lines))
        print("\nwrote", OUT)


DOC = """# 몬스터 배경 가독성 표

`Tools/AssetGen/check_monster_legibility.py`가 생성합니다. 배경이나 몬스터가
늘면 다시 돌리십시오.

## 읽는 법

**이 표는 통과 판정이 아닙니다.** 어디를 눈으로 봐야 하는지 고르는 데만 씁니다.

값은 **몬스터 몸통 평균 루마 / 그 자리 바닥 평균 루마**이고, 배경 위 여러 자리 중
**가장 나쁜 자리**를 남긴 것입니다. **1.00에 가까울수록 그 자리에서 몸이 바닥과 같은
밝기**라는 뜻입니다.

**이 값이 못 재는 것:** 테두리나 자체 명암이 만드는 실루엣. 광재는 이 표에서 1.0
근처인데 실판에서 잘 보입니다 - 위쪽 밝은 테두리가 몸통 평균과 무관하게 윤곽을
만들기 때문입니다. **그래서 낮은 칸은 "봐야 할 곳"이지 "실패"가 아닙니다.**

판정은 그 자리에 몬스터를 놓고 실제 화면 크기로 합성해서 눈으로 합니다.

## 세로로도 읽으십시오

가로로 읽으면 **한 몬스터가 어느 배경에서 안 읽히는가**가 나오고, 세로로 읽으면
**어느 배경이 여덟 종 전부에게 나쁜가**가 나옵니다. **둘 다 필요합니다.**

세로로 읽어서 나온 것: **스테이지 4가 여덟 종 모두에서 가장 낮습니다.** 배경이 제일
어두운 5번이 아닙니다. 원인은 **밝은 광맥과 굵기 2가 겹친 것**이고, 어두운 바닥은
오히려 가독성에 유리합니다(몬스터가 다 바닥보다 밝으므로).

```
스테이지  바닥평균  상자평균 최대  밝은쪽 폭   광맥루마  굵기
   1       46.3       54.0        7.7        -       0
   2       43.7       59.7       16.0      134.7     1
   3       39.5       59.5       20.0      153.0     1
   4       37.5       67.2       29.7      147.7     2   <- 최악
   5       30.8       48.2       17.4      105.9     2
```

**새 배경을 그릴 때: 바닥을 어둡게 하는 건 안전하고, 광맥을 밝고 굵게 하는 게
위험합니다.** 넷(광맥 루마·굵기·개수·바닥 밝기)이 얽혀 있어 공식은 없습니다.
배경마다 이 도구를 돌리십시오.

## 통과가 무엇에 딸려 있는지

**낮은 칸인데 실판에서 읽히는 몬스터는 대개 그럴 이유가 따로 있습니다.**

```
광재 덩이(007)   위쪽 밝은 테두리   테두리가 없으면 5번 바닥에서 실루엣이 사라집니다
바위 거미(003)   위쪽 밝은 테두리   없으면 다섯 배경 전부에서 1.00 언저리였습니다
```

**이 둘의 통과는 테두리에 딸려 있습니다.** 스프라이트를 다시 그리거나 테두리를
걷어내면 통과도 같이 사라집니다. **표만 보고 "통과였으니 괜찮다"고 하면 안 됩니다.**

## 표본이 아니라 전수입니다

배경의 **모든 자리**를 훑은 값이라 표본 수라는 변수가 없습니다. 같은 배경과 같은
스프라이트면 몇 번을 돌려도 같은 숫자가 나옵니다.

처음에는 무작위로 200곳을 뽑았는데, 표본 수를 400까지 늘려도 값이 계속 움직였습니다
(마지막 두 단계 차가 0.08~0.47). **최악은 드문 자리라 무작위로는 잘 안 걸립니다.**

**바닥 평균은 스프라이트의 경계 상자로 잽니다** - 실루엣 모양 그대로가 아닙니다.
몸 바깥 바닥이 조금 섞이지만, 이 표가 "어디를 볼지 고르는" 용도라 그 오차보다
전수라는 성질이 값집니다.

%s
"""


if __name__ == "__main__":
    main()

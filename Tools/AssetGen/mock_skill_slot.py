# -*- coding: utf-8 -*-
"""스킬 슬롯의 레벨 표시 시안

기획 확정: 표시 레벨의 정의가 바뀐다. 이전에는 그 스킬 카드를 몇 번 뽑았나(1~5)였고
이제는 획득 1 + 그 스킬에 붙은 강화 횟수의 합이다. 종류를 안 가리고 다 센다.

그래서 두 가지가 달라진다.

1. **상한이 사라진다.** `Lv.3/5` 같은 분모를 쓸 수 없다.
2. **두 자리가 나온다.** 상한은 **17**이다 - 도달 레벨 20이라 레벨업이 19회,
   스킬 3종 획득에 3회를 쓰면 한 스킬에 몰 수 있는 강화가 최대 16회, 거기에
   획득 1을 더한다. 리롤은 카드를 다시 뽑을 뿐 고르는 횟수를 늘리지 않으므로
   여기 안 들어간다. 웨이브 20 고정이 도달 레벨을 고정하는 한 세 자리는 안 나온다.

   **처음엔 14로 잡았다.** 도달 레벨이 17이던 시절의 값이고, 경험치 구조가
   '잡은 마리 수'에서 'EXP'로 바뀌면서 웨이브당 정확히 1레벨이 되어 20으로
   올랐다(기획 확정, 2026-08-27). **시안 결론은 안 바뀐다** - 14도 17도 두
   자리라 판 크기와 폰트가 그대로다. 상한이 세 자리가 되면 그때 다시 본다.

현재 프리팹은 `LevelPlate` 80x40에 폰트 30이다. "Lv.5"는 들어가지만 "Lv.12"는 넘친다.

    python Tools/AssetGen/mock_skill_slot.py <출력폴더>

두 안을 비교했고 **B로 확정**됐다(기획 승인). A는 판이 슬롯 폭의 62%라 두 자리에서
아이콘 하단을 덮고, B는 39%로 모서리에만 앉는다. 접두어를 빼면 숫자가 커지는 것에 더해
`Lv.`가 주는 "상한 있는 눈금" 인상도 같이 빠진다. A는 근거로 남겨 둔다.
"""
import os, sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from gen_effects import ASSETS
from mock_lib import decode, box, write_png
from mock_reroll import rect, paste, load_icon, PAGE

SKILL = os.path.join(ASSETS, "Download", "150 Fantasy Skill Icons", "Sprites")

SLOT_BG = (26, 26, 36)              # 프리팹 0.1,0.1,0.14 a0.85
PLATE = (18, 16, 22)                # 프리팹 검정 a0.72
PLATE_EDGE = (78, 70, 92)
TXT = (255, 248, 232)
EMPTY = (40, 40, 48)
LABEL = (150, 144, 162)

G = {
    "0": ["111", "101", "101", "101", "111"],
    "1": ["010", "110", "010", "010", "111"],
    "2": ["111", "001", "111", "100", "111"],
    "3": ["111", "001", "111", "001", "111"],
    "4": ["101", "101", "111", "001", "001"],
    "5": ["111", "100", "111", "001", "111"],
    "6": ["111", "100", "111", "101", "111"],
    "7": ["111", "001", "010", "010", "010"],
    "8": ["111", "101", "111", "101", "111"],
    "9": ["111", "101", "111", "001", "111"],
    "L": ["100", "100", "100", "100", "111"],
    "v": ["000", "000", "101", "101", "010"],
    ".": ["000", "000", "000", "000", "110"],
}


def tw(s, sc):
    return sum((3 + 1) * sc for _ in s) - sc


def text(canvas, s, ox, oy, sc, col):
    x = ox
    for ch in s:
        for r, row in enumerate(G[ch]):
            for c, v in enumerate(row):
                if v == "1":
                    for dy in range(sc):
                        for dx in range(sc):
                            canvas[oy + r * sc + dy][x + c * sc + dx] = col
        x += 4 * sc


def draw_slot(canvas, ox, oy, n, icon, label, plate_w, sc):
    """슬롯 하나. label이 None이면 빈 슬롯."""
    rect(canvas, ox, oy, n, n, SLOT_BG, round_=6)
    if icon is None:
        m = int(n * 0.375)                        # 프리팹 Empty -60,-60
        rect(canvas, ox + m, oy + m, n - m * 2, n - m * 2, EMPTY, round_=3)
        return
    pad = int(n * 0.0625)                         # 프리팹 Icon -20,-20
    paste(canvas, icon, ox + pad, oy + pad, n - pad * 2, SLOT_BG)
    if label is None:
        return
    # LevelPlate - 프리팹은 80x40에 우하단(-8, 8)
    ph = int(n * 0.25)
    px = ox + n - plate_w - int(n * 0.05)
    py = oy + n - ph - int(n * 0.05)
    rect(canvas, px - 1, py - 1, plate_w + 2, ph + 2, PLATE_EDGE, round_=4)
    rect(canvas, px, py, plate_w, ph, PLATE, round_=4)
    text(canvas, label, px + (plate_w - tw(label, sc)) // 2,
         py + (ph - 5 * sc) // 2, sc, TXT)


def main():
    out_dir = sys.argv[1] if len(sys.argv) > 1 else "."
    files = ("116-Fire-Pillar", "42-Ice", "47-Lightning", "124-Pick")
    N = 160                                       # 프리팹 슬롯 크기 그대로
    GAP, PAD = 16, 28
    ROW_GAP = 54

    # A: 판을 넓혀 접두어 유지 / B: 접두어를 빼고 숫자만
    rows = (
        ("A", ["Lv.1", "Lv.6", "Lv.11", "Lv.17", None], 100, 4),
        ("B", ["1", "6", "11", "17", None], 62, 6),
    )
    cols = 5
    W = PAD * 2 + cols * (N + GAP) - GAP
    H = PAD * 2 + 2 * (N + ROW_GAP) - ROW_GAP
    c = [[PAGE] * W for _ in range(H)]

    icons = [load_icon(os.path.join(SKILL, f + ".png"), N - N // 8) for f in files]

    for r, (_, labels, plate_w, sc) in enumerate(rows):
        oy = PAD + r * (N + ROW_GAP)
        for i, lab in enumerate(labels):
            draw_slot(c, PAD + i * (N + GAP), oy, N,
                      icons[i] if i < len(icons) else None, lab, plate_w, sc)
        # 행 구분 표시
        rect(c, PAD, oy + N + 18, 90, 8, LABEL)

    p = os.path.join(out_dir, "skill_slot_level.png")
    print("wrote", p, write_png(p, c))
    print("위 A: 판 100px + Lv. 접두어 / 아래 B: 판 62px + 숫자만")
    print("두 안 모두 두 자리(상한 17)까지 안 깨진다. 마지막 칸은 빈 슬롯.")


if __name__ == "__main__":
    main()

# -*- coding: utf-8 -*-
"""레벨업 3택 화면의 다시 뽑기 버튼 시안

기획 사양: 런당 10회. 3장 통째 리롤. 남은 횟수 표시. 상태 세 가지(활성 / 소진 /
후보 부족)가 서로 구분되어야 하고, 0이 되어도 버튼을 숨기지 않는다. 카드가 주인공이라
리롤이 먼저 눈에 들어오면 안 되지만 남은 횟수는 고르기 전에 읽혀야 한다.

아트 판단 세 가지.

1. **카드 3장 아래에 둔다.** 위 두 요구는 충돌하는 것처럼 보이지만 시선 순서로 풀린다.
   세로 화면에서 시선은 위에서 아래로 가므로, 카드 아래에 두면 카드보다 먼저 보이지
   않으면서도 고르기 전에 반드시 지나간다.

2. **주사위 아이콘.** 후보는 dice / refresh / reload / redo / undo였다. 새로고침 계열은
   "같은 것을 다시 불러온다"는 뜻이라 리롤의 핵심인 "다른 것이 나온다"를 놓친다.

3. **버튼은 스킬 슬롯 바로 위.** 유저가 "스킬 리스트 밑에 하단에"라고 지정했는데,
   팝업 캔버스(sortingOrder 0)가 HUD 캔버스(100)보다 아래라 3택 중에도 하단 슬롯이
   그대로 보인다. 슬롯 아래는 20px뿐이라 실질적 최하단이 슬롯 위다. 확인 단계가
   없어 오조작을 되돌릴 수 없으니 카드에서 멀어지는 것도 이 배치가 낫다.

4. **두 비활성을 숫자의 생사로 가른다.** 소진은 숫자가 0이고 같이 죽고, 부족은 숫자가
   살아 있는데 버튼만 죽는다. 기획이 말한 "자기 선택의 결과 vs 상황"이 그대로 보인다.
   부족 쪽에는 이유 한 줄이 필요하다 - 숫자가 멀쩡한데 못 누르면 버그로 읽힌다.

    python Tools/AssetGen/mock_reroll.py <출력폴더>

화면 시안은 1/2 축척(실제 1080x1920), 상태 비교는 실제 크기다.
"""
import os, sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from gen_effects import ASSETS
from mock_lib import decode, box, write_png

SKILL = os.path.join(ASSETS, "Download", "150 Fantasy Skill Icons", "Sprites")
PICTO = os.path.join(ASSETS, "Download", "Layer Lab", "GUI Pro-FantasyRPG",
                     "ResourcesData", "Sprites", "Component",
                     "Icon_PictoIcons", "128")

# 기존 화면에서 그대로 가져온 값
PAGE = (20, 18, 23)                 # 팝업 뒤 어두운 바닥
CARD = (36, 38, 51)                 # LevelUpChoiceCard 배경 (프리팹 0.14,0.15,0.2)
CARD_EDGE = (72, 74, 92)
BADGE = (230, 153, 38)              # NewBadge (프리팹 0.9,0.6,0.15)
BODY = (92, 86, 100)                # 글자 자리 표시
HEAD = (150, 144, 162)

# 조건 카드 시안과 같은 팔레트를 쓴다 - 같은 화면이다
BTN_ON = (96, 84, 116)
BTN_OFF = (40, 36, 47)
TXT_ON = (255, 248, 232)
TXT_OFF = (128, 120, 138)
ACCENT = (232, 196, 120)
ALIVE = (238, 228, 252)             # 부족 상태에서 살아 있는 숫자

GLYPH = {
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
}


def tw(s, scale):
    return sum((len(GLYPH[c][0]) + 1) * scale for c in s) - scale


def text(canvas, s, ox, oy, scale, col):
    x = ox
    for ch in s:
        for r, row in enumerate(GLYPH[ch]):
            for c, v in enumerate(row):
                if v == "1":
                    for dy in range(scale):
                        for dx in range(scale):
                            canvas[oy + r * scale + dy][x + c * scale + dx] = col
        x += (len(GLYPH[ch][0]) + 1) * scale


def paste(canvas, img, ox, oy, n, bg, col=None):
    for y in range(n):
        for x in range(n):
            r, g, b, a = img[y][x]
            if col:
                r, g, b = col
            f = a / 255.0
            canvas[oy + y][ox + x] = tuple(
                int(v * f + bg[k] * (1 - f)) for k, v in enumerate((r, g, b)))


def rect(canvas, ox, oy, w, h, col, round_=0):
    for y in range(h):
        for x in range(w):
            if round_ and (x < round_ or x >= w - round_) and (y < round_ or y >= h - round_):
                continue
            canvas[oy + y][ox + x] = col


# 팝업이 열렸을 때 HUD 슬롯에 남는 밝기. 개발 실측값이다.
#
# LevelUpChoice 배경은 검정 alpha 0.78이지만 슬롯에 실제로 남는 건 51%다(배경은 39%).
# sRGB가 비선형이라 밝은 픽셀이 상대적으로 덜 어두워진다. alpha 값으로 계산하면
# 22%가 나오는데 그건 틀린 숫자다 - 실측이 아니라 산술이었다.
SLOT_DIM = 0.49                     # 51% 남음


def dim(col, a):
    """검정 오버레이 alpha a가 위에 깔렸을 때 남는 색"""
    return tuple(int(v * (1 - a)) for v in col)


def load_icon(path, n):
    w, h, px = decode(path)
    return box(w, h, px, n)


# ----------------------------------------------------------------- 리롤 버튼
def draw_button(canvas, ox, oy, w, h, state, left, dice, num_s):
    """state: on / spent / blocked

    on      밝은 배경 + 아래 강조선. 숫자 밝음
    spent   어두운 배경. 숫자 0이고 같이 죽음        -> 내가 다 썼다
    blocked 어두운 배경. 숫자만 살아 있음 + 이유 줄  -> 지금은 못 쓴다
    """
    bg = BTN_ON if state == "on" else BTN_OFF
    rect(canvas, ox, oy, w, h, bg, round_=max(2, h // 12))

    if state == "on":                                   # 눌리는 쪽만 강조선
        inset = int(w * 0.09)
        for x in range(inset, w - inset):
            for y in range(max(2, h // 28)):
                canvas[oy + h - int(h * 0.13) - y][ox + x] = ACCENT

    icon_n = int(h * 0.52) // 2 * 2
    label_w = int(w * 0.34)
    num = str(left)
    num_w = tw(num, num_s)
    gap = int(h * 0.20)

    inner = icon_n + gap + label_w + gap + num_w
    x = ox + (w - inner) // 2
    icon_col = TXT_ON if state == "on" else TXT_OFF
    paste(canvas, dice, x, oy + (h - icon_n) // 2, icon_n, bg, col=icon_col)
    x += icon_n + gap

    # "다시 뽑기" 자리 - 글자는 개발이 넣는다
    lab_h = max(3, h // 12)
    rect(canvas, x, oy + (h - lab_h) // 2, label_w, lab_h,
         TXT_ON if state == "on" else TXT_OFF)
    x += label_w + gap

    # 남은 횟수 - 여기가 두 비활성을 가르는 자리다
    if state == "on":
        num_col = TXT_ON
    elif state == "spent":
        num_col = TXT_OFF                               # 0과 함께 죽는다
    else:
        num_col = ALIVE                                 # 살아 있다
    text(canvas, num, x, oy + (h - 5 * num_s) // 2, num_s, num_col)


def draw_card(canvas, ox, oy, w, h, icon, badge=False):
    rect(canvas, ox, oy, w, h, CARD_EDGE)
    rect(canvas, ox + 1, oy + 1, w - 2, h - 2, CARD)
    n = int(h * 0.6) // 2 * 2
    paste(canvas, icon, ox + int(h * 0.13), oy + (h - n) // 2, n, CARD)
    tx = ox + int(h * 0.13) + n + int(h * 0.12)
    tw_ = w - (tx - ox) - int(h * 0.13)
    for i, (frac, hh) in enumerate(((0.62, 0.085), (0.44, 0.055), (0.80, 0.055))):
        yy = oy + int(h * (0.24 + i * 0.20))
        rect(canvas, tx, yy, int(tw_ * frac), max(3, int(h * hh)),
             HEAD if i == 0 else BODY)
    if badge:
        bw, bh = int(w * 0.128), int(h * 0.187)
        rect(canvas, ox + w - bw - int(h * 0.08), oy + int(h * 0.08), bw, bh, BADGE, round_=2)


def main():
    out_dir = sys.argv[1] if len(sys.argv) > 1 else "."
    # ---------------------------------------------------------- 1) 화면 전체
    # 실제 1080x1920의 1/2. 카드 940x300 -> 470x150
    W, H = 540, 960
    scr = [[PAGE] * W for _ in range(H)]
    CW, CH = 470, 150
    BW, BH = 240, 70

    cx = (W - CW) // 2

    # 유저 지정: "리롤버튼은 스킬 리스트 밑에 하단에 버튼이 들어가도록"
    #
    # 그런데 3택 팝업은 HUD를 덮지 못한다. LevelUpChoice의 배경은 검정 알파 0.78이지만
    # 팝업 캔버스의 sortingOrder가 0이고 HUD 캔버스가 100이라, 팝업이 떠 있는 동안에도
    # HUD Bottom의 SkillSlots(940x168, 화면 하단 y=20)가 그 위에 그대로 보인다.
    #
    # 그래서 "화면 하단"에 버튼을 놓으면 스킬 슬롯과 겹친다. 슬롯 아래로는 20px밖에
    # 없어 들어갈 자리가 없으므로, 실질적인 최하단은 슬롯 바로 위다.
    TOP = 60
    SLOT_H, SLOT_Y = 84, 10          # 실제 168 / 하단 20
    BTN_GAP = 15                     # 슬롯과 버튼 사이 (실제 30px)

    y = TOP
    # Header/Guide 컨테이너는 900x60~90이지만 글자는 그 안에 중앙 정렬된다.
    # 막대를 컨테이너 폭으로 그리면 제목이 아니라 띠 두 개로 읽혀 레이아웃 판단을 흐린다.
    rect(scr, cx + (CW - 190) // 2, y + 8, 190, 30, HEAD)       # Header - 짧은 제목
    y += 45 + 13
    rect(scr, cx + (CW - 310) // 2, y + 9, 310, 14, BODY)       # Guide - 한 줄 안내
    y += 30 + 30

    card_icon = int(CH * 0.6) // 2 * 2
    for i, f in enumerate(("116-Fire-Pillar", "42-Ice", "47-Lightning")):
        draw_card(scr, cx, y + i * (CH + 12), CW, CH,
                  load_icon(os.path.join(SKILL, f + ".png"), card_icon),
                  badge=(i == 1))
    card_bottom = y + 3 * CH + 2 * 12

    # HUD의 스킬 슬롯.
    #
    # 목표는 "덮되 가리지 않는다" - 조작 대상이 아니라는 것과 내 투자 내역이라는 것을
    # 동시에 말해야 한다. 그리고 그 상태가 이미 성립해 있다. 슬롯 51% / 배경 39%로
    # 슬롯이 어두워지면서도 배경보다는 밝다. 캔버스를 나눌 필요가 없었다.
    sy = H - SLOT_Y - SLOT_H
    rect(scr, cx, sy, CW, SLOT_H, dim((30, 32, 44), SLOT_DIM))
    for i in range(5):
        n = SLOT_H - 20
        rect(scr, cx + 14 + i * (n + 12), sy + 10, n, n,
             dim((52, 54, 70), SLOT_DIM), round_=3)
        rect(scr, cx + 14 + i * (n + 12) + n - 26, sy + 10 + n - 18, 22, 14,
             dim((255, 248, 232), SLOT_DIM))       # 레벨 숫자 자리

    by = sy - BTN_GAP - BH
    draw_button(scr, (W - BW) // 2, by, BW, BH, "on", 10,
                load_icon(os.path.join(PICTO, "function_icon_dice.png"),
                          int(BH * 0.52) // 2 * 2), 4)
    print("카드 끝 %d / 버튼 %d~%d / 슬롯 %d~%d  (사이 %dpx, 실제 %dpx)"
          % (card_bottom, by, by + BH, sy, sy + SLOT_H,
             by - card_bottom, (by - card_bottom) * 2))

    p1 = os.path.join(out_dir, "reroll_screen.png")
    print("wrote", p1, write_png(p1, scr))

    # ------------------------------------------------------- 2) 상태 3종 비교
    BW2, BH2 = 480, 140
    PAD, GAP = 30, 26
    LABEL = 32
    W2 = PAD * 2 + BW2
    H2 = PAD * 2 + 3 * (BH2 + LABEL + GAP) - GAP
    st = [[PAGE] * W2 for _ in range(H2)]
    dice_btn = load_icon(os.path.join(PICTO, "function_icon_dice.png"),
                         int(BH2 * 0.52) // 2 * 2)
    rows = (("on", 10), ("spent", 0), ("blocked", 7))
    for i, (state, left) in enumerate(rows):
        y = PAD + i * (BH2 + LABEL + GAP)
        draw_button(st, PAD, y, BW2, BH2, state, left, dice_btn, 7)
        if state == "blocked":
            # 이유 한 줄 - 숫자만 살아 있으면 버그로 읽힌다.
            # 기획 확정 문구 "지금은 다시 뽑아도 같습니다" 14자.
            # 28px 기준 392px = 버튼 폭 480의 82%. 버튼 안에 갇히지 않으면서 넘치지도 않는다.
            # 강조선은 버튼 '안', 이 문구는 버튼 '밖 아래'다. 둘은 절대 같은 자리에 오지 않는다.
            rect(st, PAD + (BW2 - 392) // 2, y + BH2 + 14, 392, 9, TXT_OFF)
    # --------------------------------------------------- 3) 슬롯 밝기 비교
    # 첫 줄이 현재 실측(51%)이고 아래 둘은 "더 밝혀야 한다"가 나올 때의 후보다.
    # 지금은 손댈 이유가 없다 - 화면에서 안 읽힌다는 근거가 나오면 그때 본다.
    ALPHAS = (SLOT_DIM, 0.35, 0.20)
    SN, SG = 84, 14
    W3 = PAD * 2 + 5 * (SN + SG) - SG
    H3 = PAD * 2 + len(ALPHAS) * (SN + 40) - 40
    ov = [[PAGE] * W3 for _ in range(H3)]
    slot_icons = [load_icon(os.path.join(SKILL, f + ".png"), SN - 12)
                  for f in ("116-Fire-Pillar", "42-Ice", "47-Lightning",
                            "124-Pick", "42-Ice")]
    for r, a in enumerate(ALPHAS):
        oy = PAD + r * (SN + 40)
        for i in range(5):
            ox = PAD + i * (SN + SG)
            rect(ov, ox, oy, SN, SN, dim((30, 32, 44), a), round_=4)
            ic = slot_icons[i]
            for yy in range(SN - 12):
                for xx in range(SN - 12):
                    rr, gg, bb, aa = ic[yy][xx]
                    f = aa / 255.0
                    base = dim((30, 32, 44), a)
                    src = dim((rr, gg, bb), a)
                    ov[oy + 6 + yy][ox + 6 + xx] = tuple(
                        int(src[k] * f + base[k] * (1 - f)) for k in range(3))
            rect(ov, ox + SN - 30, oy + SN - 22, 26, 16, dim((18, 16, 22), a), round_=3)
            # 표시 레벨 실제 범위 1~14. 5의 배수로 두면 20/25가 나와 안 나오는 값이 된다.
            text(ov, ("1", "4", "9", "14", "20")[i], ox + SN - 26, oy + SN - 19, 3,
                 dim((255, 248, 232), a))
        rect(ov, PAD, oy + SN + 12, 70, 7, dim((150, 144, 162), 0.2))
    p3 = os.path.join(out_dir, "overlay_alpha.png")
    print("wrote", p3, write_png(p3, ov))
    print("남는 밝기 위에서부터:", " / ".join("%d%%" % round((1 - a) * 100) for a in ALPHAS))
    print("첫 줄이 현재 실측값이다.")

    p2 = os.path.join(out_dir, "reroll_states.png")
    print("wrote", p2, write_png(p2, st))
    print("상태 위에서부터: 활성(10) / 소진(0) / 후보 부족(7, 아래 이유 줄)")


if __name__ == "__main__":
    main()

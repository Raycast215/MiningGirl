# -*- coding: utf-8 -*-
"""3택 카드 하단 조건 표시 시안

기획 사양: 두 조건을 모두 표시. 각각 아이콘 + 진행도. 이 카드가 올리는 쪽을 강조.
아트 판단: 숫자가 먼저 읽혀야 한다(아이콘은 카드 본문과 중복 정보). 아이콘은 구분만.
"""
import os, sys
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from gen_effects import ASSETS
from mock_lib import decode, box, write_png

SKILL = os.path.join(ASSETS, "Download", "150 Fantasy Skill Icons", "Sprites")
PICTO = os.path.join(ASSETS, "Download", "Layer Lab", "GUI Pro-FantasyRPG",
                     "ResourcesData", "Sprites", "Component",
                     "Icon_PictoIcons", "128")

PAGE = (20, 18, 23)
CARD = (48, 43, 55)
CARD_EDGE = (86, 78, 98)
BODY = (92, 86, 100)
PILL_ON = (96, 84, 116)         # 강조되는 쪽 배경
PILL_OFF = (40, 36, 47)
TXT_ON = (255, 248, 232)
TXT_OFF = (128, 120, 138)
DONE = (126, 214, 132)          # 3/3 채워진 조건

# 3x5 픽셀 숫자와 슬래시
GLYPH = {
    "0": ["111", "101", "101", "101", "111"],
    "1": ["010", "110", "010", "010", "111"],
    "2": ["111", "001", "111", "100", "111"],
    "3": ["111", "001", "111", "001", "111"],
    "/": ["001", "001", "010", "100", "100"],
}


def text(canvas, s, ox, oy, scale, col):
    x = ox
    for ch in s:
        g = GLYPH[ch]
        for r, row in enumerate(g):
            for c, v in enumerate(row):
                if v == "1":
                    for dy in range(scale):
                        for dx in range(scale):
                            canvas[oy + r * scale + dy][x + c * scale + dx] = col
        x += (len(g[0]) + 1) * scale
    return x - ox - scale


def tw(s, scale):
    return sum((len(GLYPH[c][0]) + 1) * scale for c in s) - scale


def paste(canvas, img, ox, oy, n, bg, col=None):
    for y in range(n):
        for x in range(n):
            r, g, b, a = img[y][x]
            if col:
                r, g, b = col
            f = a / 255.0
            canvas[oy + y][ox + x] = tuple(
                int(v * f + bg[k] * (1 - f)) for k, v in enumerate((r, g, b)))


CARD_W, CARD_H = 172, 244
ICON = 104
COND = 28
GAP, PAD = 18, 20
NUM_S = 3                                   # 숫자 배율

# (스킬 아이콘, 위력 n, 발사체 n, 강조하는 쪽) - None이면 조건 줄 없음
CARDS = [
    ("116-Fire-Pillar", 2, 1, "power"),
    ("42-Ice",          3, 1, "multi"),
    ("124-Pick",        None, None, None),
]
W = PAD * 2 + len(CARDS) * (CARD_W + GAP) - GAP
H = PAD * 2 + CARD_H
canvas = [[PAGE] * W for _ in range(H)]

skills = {}
for f, _, _, _ in CARDS:
    w, h, px = decode(os.path.join(SKILL, f + ".png"))
    skills[f] = box(w, h, px, ICON)
pic = {}
for n in ("strength", "copy"):
    w, h, px = decode(os.path.join(PICTO, "function_icon_" + n + ".png"))
    pic[n] = box(w, h, px, COND)

for i, (sk, p, m, hi) in enumerate(CARDS):
    ox, oy = PAD + i * (CARD_W + GAP), PAD
    for y in range(CARD_H):
        for x in range(CARD_W):
            edge = x < 2 or x >= CARD_W - 2 or y < 2 or y >= CARD_H - 2
            canvas[oy + y][ox + x] = CARD_EDGE if edge else CARD
    paste(canvas, skills[sk], ox + (CARD_W - ICON) // 2, oy + 18, ICON, CARD)
    for ly, lw in ((ICON + 36, 130), (ICON + 50, 96), (ICON + 64, 112)):
        for x in range(lw):
            for y in range(5):
                canvas[oy + ly + y][ox + (CARD_W - lw) // 2 + x] = BODY
    if p is None:
        continue
    # 조건 줄 - 알약 두 개
    pill_h = 40
    py = CARD_H - pill_h - 14
    items = [("strength", p, hi == "power"), ("copy", m, hi == "multi")]
    pw = (CARD_W - 30) // 2 - 4
    for j, (icon, val, on) in enumerate(items):
        px0 = ox + 15 + j * (pw + 8)
        bg = PILL_ON if on else PILL_OFF
        for y in range(pill_h):
            for x in range(pw):
                # 모서리 둥글게
                if (x < 3 or x >= pw - 3) and (y < 3 or y >= pill_h - 3):
                    continue
                canvas[oy + py + y][px0 + x] = bg
        if on:                                        # 강조 쪽 아래 강조선
            for x in range(3, pw - 3):
                for y in range(2):
                    canvas[oy + py + pill_h - 3 + y][px0 + x] = (232, 196, 120)
        col = TXT_ON if on else TXT_OFF
        if val >= 3:
            col = DONE
        s = "%d/3" % val
        num_w = tw(s, NUM_S)
        inner = COND + 6 + num_w
        sx = px0 + (pw - inner) // 2
        paste(canvas, pic[icon], sx, oy + py + (pill_h - COND) // 2, COND, bg,
              col=col if not on else TXT_ON)
        text(canvas, s, sx + COND + 6, oy + py + (pill_h - 5 * NUM_S) // 2, NUM_S, col)

OUT = sys.argv[1] if len(sys.argv) > 1 else "condition_card.png"
print("wrote", OUT, write_png(OUT, canvas))
print("카드 왼쪽부터: 위력 강화(위력 강조) / 발사체 추가(발사체 강조, 위력 완료) / 조건 없는 카드")

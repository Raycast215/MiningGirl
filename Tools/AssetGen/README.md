# AssetGen — 임시 아트 생성기

MiningGirl의 임시 스프라이트를 **파이썬으로 직접 그려서** PNG로 뽑는 스크립트 모음입니다.
외부 라이브러리 없이 표준 라이브러리만 씁니다 (`zlib` + `struct`로 PNG를 직접 인코딩).

> **왜 코드로 그리나**
> 유료 AI 생성(fal 종량제)은 쓰지 않기로 확정됐습니다. 즉 **이 스크립트가 아트를 고칠 유일한 경로**입니다.
> 색을 한 단계 어둡게 하거나 돌 밀도를 줄이는 것도 전부 여기서 합니다.

## 실행

리포지토리 루트(`E:\MiningGirl`)에서 인자 없이 실행하면 Unity 에셋 폴더에 바로 씁니다.

```bash
python Tools/AssetGen/gen_monsters.py
python Tools/AssetGen/gen_bg.py
python Tools/AssetGen/gen_tower.py
python Tools/AssetGen/gen_star.py
```

출력 경로를 인자로 주면 그쪽에 씁니다 (에셋을 건드리지 않고 시험해 볼 때).

```bash
python Tools/AssetGen/gen_tower.py C:\temp\out C:\temp\preview.png
```

실행 후 Unity에서 **Assets > Refresh** (또는 MCP `refresh_unity`)를 한 번 돌려야 반영됩니다.

## 무엇이 어디로 나가나

| 스크립트 | 출력 | 픽셀 크기 | PPU | 월드 크기 |
|---|---|---|---|---|
| `gen_monsters.py` | `Assets/Sprites/InGame/Monster/Monster_00N_*.png` (5장) | 128×128 | 50 | 2.56 × 2.56 |
| `gen_bg.py` | `Assets/Sprites/InGame/Background/Bg_Mine_01.png` | 2048×2304 | 88 | 23.27 × 26.18 |
| `gen_tower.py` | `Assets/Sprites/InGame/Tower/Tower_01{,_Damaged,_Broken}.png` (3장) | 1792×240 | 88 | 20.36 × 2.73 |
| `gen_star.py` | `Assets/Sprites/UI/Star.png`, `Star_Empty.png` (2장) | 256×256 | 100 | UI (월드 무관) |

인게임 스프라이트는 **32×32 같은 작은 도트 격자를 4배로 확대**하는 방식입니다. 즉 화면상 1도트 = 4픽셀이고,
PPU 88 기준으로 1도트 = `4 / 88` = **0.045454 유닛**입니다. 레이아웃 계산은 이 값에서 나옵니다.

## 지켜야 하는 제약

- **타워 가로 20.36유닛 / 좌우 심리스** — 기기별 화면비가 달라 좌우가 잘리므로, 잘려도 이음새가 안 보여야 합니다. `put()`이 x를 `% W`로 감아서 그리는 이유입니다.
- **타워 세로 3유닛 이내** — 지금 2.73. 넘기면 캐릭터·UI 배치와 충돌합니다.
- **타워 상단 실루엣은 낮추지 말 것** — 체력 바 UI가 타워 *아래*에 있어서 윗변이 다 보이고, 각목이 부러지는 게 유일한 피격 피드백입니다.
- **배경은 상하좌우 심리스** — 화면비가 바뀌거나 Tiled로 늘려도 이음새가 없어야 합니다.
- 타워 세로를 바꿔도 배치는 코드 쪽이 `towerSprite.bounds.extents.y`를 실측해 쓰므로 자동으로 따라옵니다.

## 조정할 수 있는 것

### `gen_monsters.py` — 몬스터 5종

각 몬스터가 함수 하나(`slime` / `bat` / `spider` / `golem` / `wraith`)입니다.
함수 안에서 색 상수(`BODY` `DARK` `LIT` …)를 바꾸면 톤이, `ellipse` / `rect` / `put` 좌표를 바꾸면 형태가 바뀝니다.
좌우 대칭인 몬스터는 왼쪽 절반만 그리고 `mirror(g)`로 뒤집습니다. `outline(g, 색)`이 실루엣 바깥에 1픽셀 외곽선을 칩니다.

### `gen_bg.py` — 갱도 흙 바닥

| 항목 | 위치 |
|---|---|
| 흙 색 5단계 | `SOIL` |
| 돌 부스러기 색 | `PEBBLE`, `PEBBLE_D` |
| 얼룩 크기 | `noise(128, 96)` / `noise(32, 32)` / `noise(8, 8)` / `noise(4, 4)` — 셀이 클수록 큰 얼룩 |
| 얼룩 대비 | `t = 0.5 + (t - 0.5) * 0.8` 의 `0.8` (작을수록 평평) |
| 돌 개수 | `for _ in range(600)` (돌), `range(880)` (모래알) |
| 배치 패턴 | `_seed = 20260827` — 시드만 바꾸면 같은 톤의 다른 배치가 나옵니다 (`Bg_Mine_02` 뽑을 때) |

크기를 바꾸려면 `W, H, SCALE`을 고치는데, 노이즈 셀 크기(`128, 96` 등)가 `W`, `H`를 **나누어떨어져야** 합니다.

### `gen_tower.py` — 바리케이드 3단계

| 항목 | 위치 |
|---|---|
| 목재 / 철재 색 | `WOOD`, `POST`, `STEEL` 계열 상수 |
| 세로 구획 | `CAP` `BAND_A` `BAND_B` `BAND_C` `GROUND_ROWS` (도트 행 번호) |
| 각목 높이 | `stake(x, w, rint(1, 15), CAP[1], ...)` 의 `rint(1, 15)` — 작을수록 높이 솟음 |
| 기둥 간격 | `POST_XS = list(range(20, W, 56))` |
| 손상 정도 | `make_damaged(level)` 안의 균열/구멍/각목 부러짐 개수 |

3장은 **같은 `BASE` 그리드에서 파생**되므로 픽셀 단위로 레이아웃이 일치합니다.
교체해도 위치가 안 튀는 이유이니, 손상 단계를 손볼 때도 이 구조를 유지하세요.

인게임에서는 체력 66% / 33%를 경계로 세 장을 교체합니다.

### `gen_star.py` — 결과 화면 별 아이콘

인게임 아트와 만드는 방식이 다릅니다. 도트를 확대하는 대신 **4배 크기로 그린 뒤 평균내어
줄여서**(슈퍼샘플링) 가장자리를 부드럽게 만듭니다. UI는 기기마다 배율이 정수로 안 떨어져서
도트를 그대로 쓰면 픽셀 폭이 들쭉날쭉해지기 때문입니다.

| 항목 | 위치 |
|---|---|
| 채운 별 색 | `GEM_LIT` `GEM_MID` `GEM_DIM` `GEM_DEEP` `GEM_RIM` `SPARK` |
| 빈 별 색 | `HOLE_IN` `HOLE_RIM` (안쪽), `STONE` 계열 (테두리) |
| 광원 방향 | `LIGHT` (기본 -125° = 왼쪽 위) |
| 면 대비 | `FACET.append(0.72 + 0.46 * ...)` — 앞이 하한, 뒤가 폭 |
| 반짝임 위치·크기 | `SP_X` `SP_Y` `SP_R` |
| 별 비율 | `R_OUT`(바깥 반지름), `R_IN`(안쪽 = 뾰족한 정도) |
| 테두리 두께 | `INNER = star_points(R_OUT * 0.86, R_IN * 0.80)` |

**두 별의 테두리 색은 같은 계열로 유지하세요.** 결과 화면에서 ★★☆처럼 세 칸이 한 줄로
붙는데, 테두리 색이 다르면 "같은 칸의 채움/비움"이 아니라 서로 다른 아이콘 두 종류로
읽힙니다. 구분은 **안쪽 채움으로만** 주는 게 안정적입니다.

빈 별은 어둡되 초라해 보이면 안 됩니다 — 실패하면 셋 다 빈 별로 뜨는데, 그게 "다시 하면
채울 수 있다"로 읽혀야 합니다. 그래서 반투명 실루엣이 아니라 **테두리가 살아 있는 파낸 홈**
으로 그렸습니다.

## `gen_meta.py` — 임포트 설정

Unity 기본값(PPU 100 / Bilinear / 압축)은 도트 아트에 맞지 않아서 `.meta`를 직접 씁니다.
**새 PNG를 추가했을 때만** 돌리면 됩니다. 이미 있는 `.meta`는 건드리지 않습니다.

```bash
python Tools/AssetGen/gen_meta.py monster
python Tools/AssetGen/gen_meta.py background
python Tools/AssetGen/gen_meta.py tower
```

| 프리셋 | PPU | maxTextureSize | Wrap (U, V) | alphaIsTransparency |
|---|---|---|---|---|
| `monster` | 50 | 512 | Clamp, Clamp | 1 |
| `background` | 88 | 4096 | Repeat, Repeat | 0 |
| `tower` | 88 | 2048 | Repeat, Clamp | 1 |
| `ui` | 100 | 2048 | Clamp, Clamp | 1 |

공통으로 **무압축 / 밉맵 없음 / 피벗 중앙**입니다. 필터는 인게임 프리셋이 **Point**,
`ui`만 **Bilinear**입니다(위의 `gen_star.py` 설명 참고).

- `maxTextureSize`가 실제 픽셀 크기보다 작으면 Unity가 **말없이 축소**합니다. 배경이 4096인 이유는 세로 2304px 때문입니다.
- **GUID는 기존 `.meta`가 있으면 그 값을 읽어 그대로 재사용**합니다. 없을 때만 파일 이름의 MD5에서 뽑습니다. Unity가 자동 생성한 랜덤 GUID도 보존되므로, `--force`로 덮어써도 프리팹·씬·Addressables 참조가 안 끊깁니다.
- 다만 `--force`는 Unity가 임포트하면서 `.meta`에 덧붙인 스프라이트 서브에셋 정보를 날릴 수 있습니다(GUID와 별개). 임포트 설정을 일괄로 바꿔야 할 때만 쓰고, 쓴 뒤에는 Unity에서 스프라이트 참조가 살아 있는지 확인하세요.

## 결과가 항상 같은가

같습니다. 난수는 고정 시드 LCG(`_seed`)이고 시간·환경에 의존하는 값을 쓰지 않습니다.
실제로 리포에서 다시 돌려 기존 출력물과 **바이트 단위로 동일**함을 확인했습니다.
그래서 "스크립트를 고쳤을 때만 PNG가 바뀐다"가 보장되고, git diff가 의미를 가집니다.

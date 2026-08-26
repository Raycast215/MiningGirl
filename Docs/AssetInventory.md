# 에셋 재고 — 만들기 전에 여기부터

**목적: "이런 게 필요한데"가 나왔을 때 새로 만들지 말고 먼저 여기서 찾는다.**

유료 생성(fal 등)이 금지돼 있어 신규 아트는 `Tools/AssetGen/`의 코드 드로잉으로만 만든다.
그런데 프로젝트에 이미 구매한 팩이 여럿 들어와 있고 대부분 미사용이다.
**기성 자산으로 되는 일에 생성 비용을 쓰지 않는 게 이 문서의 전부다.**

조사 시점 2026-08-27. 사용 여부는 `.meta`의 guid가 우리 쪽 씬·프리팹·어드레서블에서
참조되는지로 셌다.

---

## 한눈에

| 팩 | 내용물 | 에셋 | 사용 | 언제 쓰나 |
|---|---|---|---|---|
| **150 Fantasy Skill Icons** | 스킬 아이콘 150종 | 151 png | **150건 등록** | 스킬·강화 아이콘. 더 만들 필요 없음 |
| **Layer Lab / GUI Pro-FantasyRPG** | 판타지 RPG UI 킷 | 4419 png, 355 prefab | 25건 (0.5%) | **3차 홈·캐릭터 선택·보상 화면** |
| **Pixel Effects 1 & 4** | 도트 이펙트 16종 | 16 png + anim | 5건 | 스킬 이펙트. 미사용 볼트 4종 남음 |
| **Sprite Shaders Ultimate** | 스프라이트 셰이더 | 11 shader, 33 cs | 5건 | 색 변형·피격 점멸. **복제해 칠하지 말 것** |
| **2DScrollingBattleBG** | 가로 스크롤 배경 15테마 | 122 png, 16 prefab | 1건 | **인게임에는 못 씀 (아래 참조)** |
| **GoldenSkullStudios** | 아이소메트릭 타일 | 299 png | 2건 | 시점이 달라 현재 게임과 안 맞음 |
| **Cainos** | 픽셀 타일 | 311 에셋 | 3건 | — |
| **StompyRobot (SRDebugger)** | 디버그 도구 | 325 에셋 | **0건** | 아트 자산 아님 |

---

## 스킬 아이콘 — 150종, 전부 어드레서블에 있다

```
원본   Assets/Download/150 Fantasy Skill Icons/Sprites/{n}-{이름}.png
주소   Skill_Icon_1{n-1}       (파일 번호 - 1, 네 자리)
범위   Skill_Icon_1000 ~ Skill_Icon_1149   150건 연속, 빠진 번호 없음
```

**주소 150건이 전부 `Download/` 사본을 가리키는 것을 guid로 역추적해 확인했다.**
`Assets/Resource/UI/Skill/Icon/` 아래에도 같은 팩이 있지만 **어드레서블에 물린 쪽이 아니다.**
guid가 달라 헷갈리기 쉬우니 주소로만 접근할 것.

### 아이콘 고를 때 알아야 하는 것

**실루엣으로는 구분되지 않는다.** 150종 전부 모서리 둥근 정사각 타일이고
불투명 면적이 95.9%로 편차가 0.0%p다. 알파만 남기면 전부 같은 사각형이다.

**구분은 색과 내부 그림으로만 된다.** 한 런에 스킬 5개가 하단 슬롯에 나란히 붙으므로,
고른 조합의 **평균 색조가 30도 안에 몰리면 같은 덩어리로 읽힌다.**

이 팩은 공격형 아이콘이 **주황과 파랑에 심하게 쏠려 있다.** 투사체로 읽히는 18종 중
초록은 `57-Poison-Arrow` 하나뿐이고 노랑은 `112-Greed` 하나뿐이다. 여러 스킬을
동시에 고를 때는 이 두 개를 먼저 확보해야 색이 벌어진다.

```
검수 도구는 커밋하지 않았다. 필요하면 다시 만든다:
  아이콘 전체 색인 + 색조 통계 -> 슬롯 5칸 시안으로 겹침 확인
```

---

## 이펙트 — 미사용 볼트가 4종 남아 있다

```
Assets/Download/Pixel Effects 1 - Pixel Art/Sprites/
  Dust  Fireball  LightningStorm  LightningStrike  ShadowBolt  Shield  StaticLightning  lava
Assets/Download/Pixel Effects 4 - Pixel Art/Sprites/
  DeathBolt  IceBolt  ImpactExplosion  LightningBolt  MeteorShower  PoisonBolt  SunBolt  ZombieGrasp
```

현재 인게임이 쓰는 것은 **3종**뿐이다 — `Fireball` / `IceBolt` / `LightningBolt`.

| 남은 것 | 성격 |
|---|---|
| `ShadowBolt` `DeathBolt` `PoisonBolt` `SunBolt` | 단일 투사체. 기존 볼트와 같은 구조로 바로 얹힌다 |
| `LightningStorm` `LightningStrike` `StaticLightning` `MeteorShower` `ImpactExplosion` `lava` | 광역·장판 |
| `Dust` `Shield` `ZombieGrasp` | 그 밖 |

### 인게임 래핑 구조

새 스킬 이펙트는 **그림을 그리는 일이 아니라 프리팹을 복제해 스프라이트를 갈아끼우는 일이다.**

```
Assets/Prefabs/InGame/Effect/
  Effect_{Id}.prefab
  SkillEffectAnimator_{Id}.overrideController
  SkillEffect_Idle_{Id}.anim
```

현재 `FireBolt` / `IceBolt` / `LightningBolt` 세 세트가 이 형태로 들어 있다.

**색만 다른 변형은 스프라이트를 복제하지 말고 `Sprite Shaders Ultimate`로 처리한다.**
원본을 복제해 칠하면 같은 그림이 두 벌 늘어난다.

---

## UI — Layer Lab은 이미 쓰고 있다

```
Assets/Download/Layer Lab/GUI Pro-FantasyRPG/
  ResourcesData/Sprites/   4419장
  Prefabs/                  355개
```

**284MB짜리 완성 UI 킷인데 참조가 25건(0.5%)뿐이다.** 다만 **그 25건이 이미 인게임에서 돌고 있다**
— 로딩 스피너, 3택 카드 프레임, 역할·코인·적 아이콘, 슬라이더. "3차에서 쓸 예정"이 아니라 현재형이다.

**세부 조사는 [UiKitForPhase3.md](UiKitForPhase3.md)에 있다.** 홈·캐릭터 선택·보상 화면이
완성 프리팹으로 들어 있고, 부품 재고와 걸리는 점(가로 조판 / 톤 차이 / 팩 자체의 정보 구조)을 정리했다.

---

## 배경 팩 — 인게임에는 쓸 수 없다

`2DScrollingBattleBG`에 **15테마 × 6레이어 패럴랙스 배경 + 완제품 프리팹**이 있고
그중 `Cave`가 있어 광산 주제에 맞아 보인다. **확인해 봤고, 인게임 전투 화면에는 안 맞는다.**

| 항목 | 팩 | 현재 인게임 |
|---|---|---|
| 규격 | 4096×2560 (레이어는 4096×1024) | 2048×2304 |
| 시점 | **지평선이 있는 측면 스크롤** | 위에서 내려다보는 평면 바닥 |
| 화풍 | 벡터 채색·그라디언트 | 도트 |
| 명도 | 밝고 채도 높음 | 어둡고 좁은 명도 폭 |

**시점이 결정적이다.** 팩 배경은 지평선과 원근으로 물러나는 바닥면을 전제하는데
현재 게임은 바리케이드가 화면을 가로지르는 평면 바닥이다. 넣으면 게임플레이에 없는
측면 시점을 암시하게 된다. **명도도 문제다** — 현재 배경은 몬스터 실루엣이 뜨도록
일부러 어둡고 좁은 명도 폭으로 잡았는데, 팩 배경은 밝고 화려해서 몬스터를 삼킨다.

**따라서 스테이지별 배경은 여전히 `Tools/AssetGen/gen_bg.py`로 만든다.**
팔레트 상수 9개(`SOIL` 5 + `PEBBLE` 3 + 그림자 1)만 바꾸면 스테이지 변형은 싸다.
**단 시드도 같이 바꿔야 한다** — 결정론적 생성이라 시드가 같으면 돌 부스러기 위치까지
동일해서 "같은 그림에 색만 입힌 것"으로 읽힌다.

메뉴·로딩·스테이지 선택 같은 **비전투 화면 배경으로는 쓸 수 있다.** 그때 다시 볼 것.

---

## 손대면 안 되는 것

- **팩 파일을 옮기거나 지우지 않는다.** 어드레서블 주소와 guid가 물려 있다.
  정리가 필요해 보이면 PM에 올린다
- **`Assets/Resource/UI/Skill/Icon/`은 어드레서블에 등록된 사본이 아니다.**
  이쪽을 참조하는 코드를 새로 쓰지 말 것
- **스프라이트를 복제해 색만 바꾸지 않는다.** 셰이더로 처리한다

## 라이선스 — 저장소만으로는 판정할 수 없습니다

2026-08-27 조사. **각 팩에 동봉된 문서를 그대로 읽어 정리했습니다. 법적 판단은 하지 않았습니다.**

### 결론

**"상업적으로 써도 되는가"에 대한 답이 저장소 안에 없습니다.**

아홉 팩 중 **라이선스 조항을 실제로 동봉한 것은 하나뿐**입니다(StompyRobot, MIT). 나머지는
readme에 소개글만 있거나 아예 문서가 없습니다. Unity Asset Store 팩은 보통 조항을 동봉하지
않고 **Asset Store EULA**에 기대는데, 그 EULA는 어느 라이선스로 구입했는지에 따라 달라집니다.

**따라서 확인해야 할 것은 팩 안이 아니라 구입 기록입니다.**

```
각 팩을 어디서 받았는가 (Unity Asset Store / 다른 경로 / 무료 배포)
Asset Store라면 어느 라이선스인가 (Single Entity / Multi Entity)
계정에 구입 기록이 남아 있는가
```

**이건 저장소를 뒤져서 알 수 없고 유저만 확인할 수 있습니다.**

### 팩별 실태

| 팩 | 동봉 문서 | 명시된 조항 | 현재 사용 |
|---|---|---|---|
| **Pixel Effects 1** | **없음** | — | **사용 중** (4/38) |
| **Pixel Effects 4** | **없음** | — | **사용 중** (7/41) |
| **150 Fantasy Skill Icons** | readme.rtf | **없음** (소개글과 연락처뿐) | **사용 중** (150/158) |
| **Layer Lab / GUI Pro** | 사용 가이드 링크 | **팩 조항 없음.** 번들 폰트만 OFL | **사용 중** (25/4996) |
| 2DScrollingBattleBG | Readme txt/pdf | 없음 (사양 설명뿐) | 미사용 |
| GoldenSkullStudios | README.txt | 없음 (제작자 소개) | 미사용 |
| Sprite Shaders Ultimate | ASE/Readme.txt | 없음 (ASE 사용법) | 일부 (5/450) |
| Cainos | Changelog.txt | 없음 | 일부 (3/311) |
| StompyRobot (SRDebugger) | **SRF/LICENSE** | **MIT** — 저작권 고지 유지 의무 | 미사용 (0/325) |

### 지금 걸리는 것

**1. `Pixel Effects 1 / 4` — 문서가 한 장도 없는데 이미 아홉 개 이펙트가 여기 얹혀 있습니다.**
기존 3종(FireBolt / IceBolt / LightningBolt)과 신규 6종 전부입니다. **되돌리려면 스킬 아홉 개의
이펙트를 통째로 바꿔야 합니다.** 우선 확인 대상입니다.

**2. `Layer Lab`은 3차 이야기가 아니라 이미 인게임에 들어가 있습니다.**

```
로딩 스피너 12프레임    ResourcesData/Animatons/Loading_rotate_00~11
카드 프레임             CardFrame_02_BgGradient / LineFrame_02 / LineTextFrame_03
역할 아이콘 3종         IconSet_Role_Assassin / Gladiator / Priest
코인·적 아이콘          icon_coin / function_icon_enemy
슬라이더 프리팹         Slider_Border_Tapered_01_Yellow
```

3차에서 쓸 예정이라 미리 보자는 이야기였는데, **이미 쓰고 있습니다.** 3차까지 가면 UI 전체가
여기 물리므로 되돌릴 범위가 지금과 비교가 안 됩니다.

**3. 아이콘 150종은 readme에 라이선스 조항이 아예 없습니다.** 소개글과 제작자 이메일뿐입니다.
어드레서블에 150건 전부 등록돼 있어 의존도가 가장 높습니다.

### 명확한 것

**StompyRobot (SRDebugger / SRF) — MIT입니다.** 조항이 파일로 들어 있고, **저작권 고지를 유지하면
상업적 이용·수정·배포가 됩니다.** 다만 **현재 참조 0건**이라 지금은 쓰지 않고 있습니다.

**Layer Lab 번들 폰트 3종(Alata / Josefin Sans / Play) — SIL Open Font License 1.1입니다.**
Google Fonts에서 온 것이고 각 폰트 폴더에 OFL 전문이 들어 있습니다. **현재 참조되지 않습니다**
(프로젝트는 TextMesh Pro 기본 LiberationSans를 씁니다). 다만 **저장소에 파일이 들어 있는 이상
재배포 조건은 그대로 걸립니다** — OFL은 폰트를 단독 판매할 수 없고, 예약된 폰트 이름을 바꿔
쓸 수 없습니다.

### 애매해서 유저가 정해야 하는 것

- **구입 경로와 라이선스 등급.** 위에 적은 대로 저장소로는 알 수 없습니다
- **크레딧 표기 의무가 있는지.** 어느 팩도 "표기하라"고 쓰지 않았지만, **안 쓰여 있다는 것이
  면제를 뜻하지는 않습니다.** Asset Store EULA 쪽에 있을 수 있습니다
- **원본 시트를 그대로 참조하는 방식이 재배포에 해당하는지.** 신규 이펙트 6종은 스프라이트를
  복제하지 않고 팩 안의 PNG를 프리팹이 직접 참조합니다. 빌드에는 그 이미지가 포함됩니다
- **`2DScrollingBattleBG` readme의 "Hand-painted, Not By AI" 문구** — 제작자의 제작 방식 주장입니다.
  다른 팩에는 그런 언급이 없습니다

### 되돌리기 비용 (지금 기준)

| 팩 | 문제가 생기면 |
|---|---|
| Pixel Effects 1/4 | 스킬 이펙트 9종 교체. 대체 팩이 프로젝트에 없어 **코드 드로잉으로 새로 그려야 함** |
| 150 아이콘 | 스킬·강화 아이콘 전량 교체. 어드레서블 주소 150건 재구성 |
| Layer Lab | 로딩·카드 프레임·아이콘 교체. **3차 착수 후에는 UI 전체** |
| 나머지 | 미사용이라 폴더 삭제로 끝남 |

---

## 직접 만들어야 하는 것

기성 팩에 없어서 코드로 그린 것들이다. 사용법과 조정 상수는 `Tools/AssetGen/README.md`.

| 에셋 | 생성기 |
|---|---|
| 몬스터 5종 | `gen_monsters.py` |
| 인게임 배경 | `gen_bg.py` |
| 바리케이드 3단계 | `gen_tower.py` |
| 결과 화면 별 | `gen_star.py` |

전부 결정론적이라 다시 돌리면 바이트가 같다.

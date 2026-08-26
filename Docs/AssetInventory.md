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

## UI — Layer Lab이 통째로 놀고 있다

```
Assets/Download/Layer Lab/GUI Pro-FantasyRPG/
  ResourcesData/Sprites/   4419장
  Prefabs/                  355개
```

**284MB짜리 완성 UI 킷인데 참조가 25건(0.5%)뿐이다.**

3차 범위(홈 · 캐릭터 선택 · 보상 지급)에 필요한 프레임·버튼·패널·팝업이
여기 다 있을 가능성이 높다. **그 화면들을 잡기 전에 이 팩을 먼저 열 것.**
아직 카테고리별 세부 조사는 하지 않았다 — 3차 착수 시점에 필요한 부분만 판다.

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

## 라이선스

각 팩에 동봉된 문서를 그대로 둔다. 상업적 이용 조건은 확인하지 않았다 —
**출시 판단이 필요한 시점에 PM이 별도로 검토할 항목이다.**

```
150 Fantasy Skill Icons/readme.rtf
2DScrollingBattleBG/Readme_2DScrollingBattleBG.txt
Layer Lab/GUI Pro-FantasyRPG/+README+
Sprite Shaders Ultimate/ASE/Readme.txt
```

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

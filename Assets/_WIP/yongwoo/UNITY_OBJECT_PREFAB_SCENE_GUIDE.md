# Unity 오브젝트 / 프리팹 / 씬 사용 가이드

이 문서는 하이어라키 작업을 할 때 헷갈리는 개념을 정리하고, 이 프로젝트에서 어떤 기준으로 씬과 프리팹을 나눌지 정하는 문서다.

목표는 "하이어라키를 오래 만져도 머리가 덜 터지게 만드는 기준"이다.

---

## 한줄 결론

- **Scene**: 지금 스테이지에만 존재하는 배치판.
- **GameObject**: 씬이나 프리팹 안에 놓이는 실제 물건.
- **Component**: GameObject에 붙는 기능 조각.
- **Prefab**: 여러 씬에서 반복해서 쓸 수 있는 GameObject 저장본.
- **Prefab Instance**: 씬에 배치된 프리팹 복사본.
- **ScriptableObject**: 숫자, 설정, 대화, 아이템 정보처럼 "데이터"만 따로 저장하는 파일.

---

## 1. GameObject

### 무엇인가

Unity에서 씬 안에 존재하는 모든 것은 기본적으로 GameObject다.

플레이어, 적, 바닥, 카메라, UI 패널, 빈 부모 오브젝트, 트리거 영역까지 전부 GameObject다.

### 핵심

GameObject 자체는 빈 껍데기고, 실제 기능은 Component가 한다.

예시:

| GameObject | 붙는 Component | 의미 |
|---|---|---|
| `Player` | `Rigidbody2D`, `BoxCollider2D`, `SimplePlayerController` | 움직이는 플레이어 |
| `Enemy_Melee` | `Rigidbody2D`, `Collider2D`, `EnemyController` | 적 |
| `DialogueTrigger` | `BoxCollider2D`, `DialogueTrigger` | 대화 시작 영역 |
| `Visual` | `SpriteRenderer`, `Animator` | 보이는 부분 |

### 이 프로젝트 기준

GameObject 이름만 봐도 역할이 보여야 한다.

좋은 예:

```text
Player
├── Visual
├── BodyCollider
├── Sensors
│   └── GroundCheck
├── Hand
└── Debug
```

나쁜 예:

```text
Player
├── Cube
├── Empty
├── Object
└── child
```

### 사용 기준

- 기능의 중심이 되는 오브젝트는 루트 GameObject로 둔다.
- 보이기만 하는 것은 `Visual` 아래에 둔다.
- 판정만 하는 것은 `Collider`, `Hitbox`, `Sensor`, `Trigger` 같은 이름을 쓴다.
- 디버그 확인용은 `Debug` 아래에 몰아둔다.

---

## 2. Component

### 무엇인가

Component는 GameObject에 붙는 기능 조각이다.

GameObject는 "누구인가"이고, Component는 "무엇을 할 수 있는가"다.

예시:

- `Transform`: 위치, 회전, 크기
- `SpriteRenderer`: 화면에 스프라이트 표시
- `Rigidbody2D`: 물리 이동
- `Collider2D`: 충돌/감지 영역
- `Animator`: 애니메이션 재생
- C# Script: 직접 만든 행동

### 이 프로젝트 기준

하나의 스크립트는 하나의 역할만 맡긴다.

좋은 분리:

- `SimplePlayerController`: 이동
- `SimplePlayerCombat`: 공격 입력과 무기 사용
- `PlayerInteraction`: 체력, 피격, 사망, 조작 잠금
- `PlayerAnimationDriver`: 애니메이션 반영
- `PlayerHand`: 조준 방향

나쁜 분리:

- `PlayerEverything`: 이동, 공격, 체력, UI, 사운드, 대화까지 전부 처리

### 판단 기준

스크립트 이름만 보고 역할이 바로 떠오르면 유지한다.
이름이 `Manager`, `Controller`, `Handler`인데 내용이 계속 늘어나면 역할을 다시 나눈다.

---

## 3. Scene

### 무엇인가

Scene은 게임 월드의 한 판, 한 화면, 한 공간이다.

이 프로젝트에서는 대략 아래처럼 쓴다.

| Scene | 역할 |
---|---|
| `Title` | 타이틀 화면 |
| `Dev_yongwoo` | 개인 테스트용 씬 |
| `Stage_01` ~ `Stage_05` | 실제 전투 스테이지 |
| `Ending` | 엔딩 |

### Scene에 직접 둬도 되는 것

그 씬에서만 의미가 있는 배치물은 씬에 직접 둬도 된다.

예시:

- 스테이지 바닥/벽 배치
- 배경 레이어 배치
- 그 씬 전용 카메라 위치
- 그 씬 전용 적 배치
- 그 씬 전용 대화 트리거 위치
- 그 씬 전용 시작 위치

### Scene에 직접 만들면 위험한 것

여러 씬에서 반복되거나, 나중에 수정될 가능성이 높은 것은 씬에 직접 만들지 말고 프리팹으로 만든다.

예시:

- 플레이어
- 적
- 총알
- 아이템 픽업
- DialogueTrigger
- Checkpoint
- Door
- Trap
- UI 패널
- 플랫폼 기믹

### 원인 -> 영향 -> 해결

원인: 씬 파일은 여러 사람이 동시에 수정하면 충돌이 나기 쉽다.

영향: Git에서 씬 충돌이 나면 사람이 직접 합치기 어렵고, 한쪽 작업을 버려야 할 수 있다.

해결: 반복 오브젝트는 프리팹으로 만들고, 씬에는 "배치"만 한다.

---

## 4. Prefab

### 무엇인가

Prefab은 GameObject 구조를 파일로 저장한 것이다.

씬에 직접 만든 오브젝트는 그 씬 안에만 있지만, 프리팹으로 만들면 여러 씬에서 같은 구조를 재사용할 수 있다.

### 이 프로젝트에서 프리팹으로 만들 대상

반복해서 배치하거나, 여러 사람이 가져다 쓸 가능성이 있으면 프리팹으로 만든다.

| 대상 | 프리팹으로 만드는 이유 |
|---|---|
| `Player` | 모든 씬에서 같은 플레이어가 필요함 |
| `Enemy_Melee` | 여러 스테이지에 반복 배치 |
| `Enemy_Ranged` | 수치/비주얼/판정 일괄 수정 필요 |
| `ItemPickup` | 무기/아이템을 바닥에 놓는 공통 방식 |
| `DialogueTrigger` | 대화 시작 조건을 씬마다 배치 |
| `Trap_Spike` | 스테이지 기믹 반복 사용 |
| `Checkpoint` | 리스폰 지점 반복 사용 |
| `Projectile_Player` | 런타임에서 생성됨 |
| `Projectile_Enemy` | 런타임에서 생성됨 |

### 프리팹의 핵심 원칙

프리팹은 혼자 씬에 놓아도 에러 없이 존재할 수 있어야 한다.

좋은 프리팹:

- 필요한 컴포넌트가 프리팹 안에 붙어 있다.
- 기본 비주얼이 있다.
- 기본 Collider가 있다.
- 필수 참조가 비어 있지 않다.
- 씬에 놓고 실행하면 최소한 에러는 안 난다.

나쁜 프리팹:

- 씬 안의 특정 오브젝트를 반드시 직접 참조해야만 동작한다.
- 어디선가 런타임에 컴포넌트를 몰래 붙여줘야만 동작한다.
- 자식 이름이 `Empty`, `Object`, `New Sprite`로 남아 있다.

### 프리팹 수정 기준

공용 프리팹 원본을 수정하면 그 프리팹을 쓰는 모든 씬에 영향이 간다.

그래서:

- 개인 실험 중이면 `Assets/_WIP/yongwoo/Prefabs` 안에서 수정한다.
- 공용 `Assets/Prefabs` 아래 프리팹을 수정할 때는 팀에 선언한다.
- 씬에서 프리팹 인스턴스 값만 바꾸는 것도 씬 파일 변경이다.

---

## 5. Prefab Instance와 Override

### 무엇인가

Prefab Instance는 씬에 배치된 프리팹 복사본이다.

원본 프리팹과 연결되어 있지만, 씬 안에서 일부 값을 바꿀 수 있다. 이 바뀐 값이 Override다.

예시:

- `Enemy_Melee` 프리팹 원본 체력: 1
- `Stage_03`에 배치한 특정 적 체력만: 2
- 이때 `Stage_03`의 그 적은 체력 값 Override를 가진다.

### Override를 써도 되는 경우

그 씬의 배치 의미가 달라지는 값은 Override로 둬도 된다.

예시:

- 적 시작 위치
- 적 순찰 범위
- 대화 트리거의 대화 데이터
- 문이 연결할 다음 씬 이름
- 체크포인트 번호
- 특정 스테이지의 적 체력/속도 소폭 조정

### Override를 줄여야 하는 경우

모든 인스턴스에 똑같이 적용해야 하는 값은 원본 프리팹을 고친다.

예시:

- 적 기본 Collider 크기
- 적 기본 SpriteRenderer 정렬
- 플레이어 기본 컴포넌트 구조
- DialogueTrigger의 기본 감지 방식
- ItemPickup의 기본 획득 방식

### 판단 기준

질문: "이 값이 이 씬의 이 한 개체만 달라야 하나?"

- 그렇다 -> 인스턴스 Override
- 아니다 -> 프리팹 원본 수정

---

## 6. Parent / Child 구조

### 무엇인가

하이어라키에서 부모-자식 구조는 오브젝트를 역할별로 묶는 방법이다.

부모의 Transform을 움직이면 자식도 같이 움직인다.

### 이 프로젝트 기본 구조

캐릭터는 아래 구조를 기본으로 한다.

```text
CharacterRoot
├── Visual
├── BodyCollider
├── Sensors
├── Hitboxes
├── VFX
└── Debug
```

### 역할

| 이름 | 역할 |
|---|---|
| `Root` | 전체 위치, 핵심 스크립트, Rigidbody |
| `Visual` | SpriteRenderer, Animator |
| `BodyCollider` | 몸 충돌 |
| `Sensors` | GroundCheck, WallCheck, PlayerDetect 등 감지점 |
| `Hitboxes` | 공격 판정 |
| `VFX` | 먼지, 피격, 잔상 등 이펙트 |
| `Debug` | 디버그 표시 |

### 왜 나누는가

원인: 보이는 위치, 충돌 위치, 공격 판정 위치가 항상 같지 않다.

영향: 한 오브젝트에 전부 붙이면 스프라이트를 살짝 옮기거나 Collider를 고칠 때 전체가 꼬인다.

해결: 루트, 비주얼, 충돌체, 센서를 분리한다.

---

## 7. Empty GameObject

### 무엇인가

Empty GameObject는 보이는 것도 없고 기능도 없는 빈 오브젝트다.

하지만 Unity에서는 매우 중요하다.

### 언제 쓰는가

- 정리용 부모
- 기준점
- 스폰 위치
- 카메라 타겟
- GroundCheck 위치
- 총구 위치
- 트리거 영역 루트
- 배경 레이어 묶음

### 이름 기준

빈 오브젝트일수록 이름이 중요하다.

좋은 예:

- `Muzzle`
- `GroundCheck`
- `SpawnPoint_Player`
- `PatrolPoint_Left`
- `PatrolPoint_Right`
- `StageBounds`
- `BackgroundRoot`

나쁜 예:

- `Empty`
- `GameObject`
- `Point`
- `Transform`

---

## 8. ScriptableObject

### 무엇인가

ScriptableObject는 씬에 놓이는 물건이 아니라, 데이터 파일이다.

프리팹은 "물건"이고, ScriptableObject는 "설정표"에 가깝다.

### 이 프로젝트에서 쓰기 좋은 대상

| 데이터 | 예시 |
|---|---|
| 플레이어 설정 | 이동 속도, 점프력, 대시 시간 |
| 적 설정 | 체력, 이동 속도, 공격 쿨다운 |
| 아이템 데이터 | 무기 타입, 스프라이트, 사용 횟수 |
| 대화 데이터 | 이름, 대사, 초상화, 연출 |
| 스테이지 설정 | 제한 시간, 배경음, 클리어 조건 |

### 프리팹과 구분

| 질문 | 선택 |
|---|---|
| 씬에 놓이는 실제 물건인가? | Prefab |
| 숫자/텍스트/설정만 담는가? | ScriptableObject |
| 여러 오브젝트가 같은 설정을 공유해야 하나? | ScriptableObject |
| Collider, SpriteRenderer, Rigidbody가 필요한가? | Prefab |

### 주의

저장 기준은 하나만 둔다.

예를 들어 무기 데미지를:

- 프리팹에도 쓰고
- ScriptableObject에도 쓰고
- 스크립트 기본값에도 쓰면

나중에 어떤 값이 진짜인지 모르게 된다.

---

## 9. Manager

### 무엇인가

Manager는 여러 오브젝트를 한꺼번에 알아야 할 때만 둔다.

이 프로젝트 개인 규칙에서는 "시스템"이라는 별도 개념보다, 전부 객체로 본다.

### Manager가 필요한 경우

- UI 전체 열기/닫기
- 저장/로드
- 오디오 재생 관리
- 씬 전환
- 풀링
- 입력 모드 전환

예시:

- `UIManager`
- `AudioManager`
- `SaveManager`
- `SceneFlowManager`
- `PoolManager`
- `GameInput`

### Manager가 필요 없는 경우

한 오브젝트 안에서 끝나는 일은 그 오브젝트가 직접 처리한다.

예시:

- 적이 플레이어를 감지한다 -> 적 스크립트
- 총알이 맞으면 데미지를 준다 -> 총알 스크립트
- 문 트리거에 들어가면 씬 이동한다 -> 문/트리거 스크립트
- 아이템을 주우면 장착한다 -> 아이템/플레이어 상호작용

### 판단 기준

질문: "이 일이 한 오브젝트 안에서 끝나나?"

- 끝난다 -> 그 오브젝트에 스크립트
- 여러 오브젝트를 조율해야 한다 -> Manager

---

## 10. 이 프로젝트의 작업 흐름

### 1단계: 개인 Dev 씬에서 실험

먼저 `Dev_yongwoo` 같은 개인 씬에서 빠르게 만든다.

이 단계에서는 완벽한 구조보다 동작 확인이 우선이다.

### 2단계: 반복될 것만 프리팹으로 뺀다

한 번만 쓸 배치물은 씬에 둬도 된다.

하지만 아래 느낌이 들면 프리팹으로 뺀다.

- 이거 다른 씬에도 놓을 것 같다.
- 이거 팀원이 배치해야 할 것 같다.
- 이거 나중에 수치를 자주 바꿀 것 같다.
- 이거 복붙하면 관리 터질 것 같다.

### 3단계: 프리팹 구조를 정리한다

프리팹으로 뺀 뒤에는 이름과 자식 구조를 정리한다.

최소 기준:

```text
PrefabRoot
├── Visual
├── Collider 또는 Hitbox
└── Debug
```

### 4단계: 씬에는 배치만 남긴다

스테이지 씬에는 가능한 한 아래만 남긴다.

- 프리팹 인스턴스 위치
- 씬 전용 배경
- 씬 전용 타일/지형
- 트리거 위치
- 연결할 데이터 참조

### 5단계: 공용 폴더로 올릴 때 선언한다

`Assets/Prefabs`, `Assets/Scripts`, `Assets/_Scenes/Stage_XX` 같은 공용 파일을 건드릴 때는 팀에 먼저 말한다.

---

## 11. 선택 기준표

### 씬에 직접 둘까, 프리팹으로 만들까?

| 질문 | 답 |
|---|---|
| 이 씬에서만 쓰나? | 씬에 직접 둬도 됨 |
| 여러 씬에서 반복되나? | 프리팹 |
| 런타임에 Instantiate 할 건가? | 프리팹 |
| 팀원이 배치해서 쓸 건가? | 프리팹 |
| 원본 수정으로 전체 반영하고 싶은가? | 프리팹 |
| 위치만 중요한가? | 씬 배치 |

### 프리팹 원본을 고칠까, 인스턴스 Override로 둘까?

| 질문 | 답 |
|---|---|
| 모든 적에게 적용되어야 하나? | 프리팹 원본 |
| 이 스테이지의 이 적만 달라야 하나? | 인스턴스 Override |
| 기본 Collider/Sprite/컴포넌트 구조인가? | 프리팹 원본 |
| 시작 위치/순찰 범위/대화 데이터인가? | 인스턴스 Override |

### ScriptableObject로 뺄까?

| 질문 | 답 |
|---|---|
| 같은 데이터를 여러 오브젝트가 공유하나? | ScriptableObject |
| 기획자가 인스펙터에서 데이터만 바꿔야 하나? | ScriptableObject |
| 씬에 놓이는 실체가 필요한가? | Prefab |
| 데이터가 한 프리팹에만 종속되나? | 프리팹 필드로 충분 |

---

## 12. 추천 하이어라키 예시

### 플레이어

```text
Player
├── Visual
│   └── Sprite
├── BodyCollider
├── Sensors
│   ├── GroundCheck
│   └── WallCheck
├── Hand
│   ├── WeaponVisual
│   └── Muzzle
├── Hitboxes
│   └── SlashHitbox
├── VFX
└── Debug
```

### 적

```text
Enemy_Melee
├── Visual
├── BodyCollider
├── Sensors
│   ├── PlayerDetect
│   └── GroundCheck
├── Hitboxes
│   └── AttackHitbox
└── Debug
```

### 아이템 픽업

```text
ItemPickup
├── Visual
├── Trigger
└── Debug
```

### 대화 트리거

```text
DialogueTrigger
├── Trigger
└── Debug
```

### 스테이지 씬

```text
Stage_01
├── Runtime
│   ├── MainCamera
│   ├── GameInput
│   └── UI
├── PlayerSpawn
├── Stage
│   ├── Ground
│   ├── Platforms
│   ├── Background
│   └── Foreground
├── Actors
│   ├── Player
│   └── Enemies
├── Interactables
│   ├── DialogueTriggers
│   ├── Doors
│   └── Checkpoints
└── Debug
```

---

## 13. 하이어라키 작업할 때 체크리스트

- [ ] 이름만 보고 역할을 알 수 있는가?
- [ ] 반복 배치할 오브젝트를 씬에 복붙하고 있지 않은가?
- [ ] 보이는 것과 충돌/판정이 분리되어 있는가?
- [ ] 프리팹 원본에 들어갈 값과 씬 Override 값을 구분했는가?
- [ ] 공용 씬이나 공용 프리팹을 수정하기 전에 팀에 말했는가?
- [ ] `.meta`가 깨지지 않도록 Unity Editor 안에서 이동했는가?
- [ ] ScriptableObject, prefab, scene, script 기본값 중 저장 기준이 하나인가?

---

## 14. 지금 프로젝트에 적용할 운영 기준

### 개인 작업 중

- `Assets/_WIP/yongwoo` 아래에서 마음껏 실험한다.
- 개인 Dev 씬에서는 빠르게 만들고 지워도 된다.
- 단, 수동으로 맞춘 Transform, Collider, Animator, Inspector 값은 함부로 자동 덮어쓰지 않는다.

### 팀원이 쓸 단계

팀원이 씬에 배치해서 쓸 대상은 프리팹으로 만든다.

최소 요구:

- 이름 정리
- 기본 비주얼
- 기본 Collider/Trigger
- 필수 스크립트 연결
- 인스펙터에서 바꿔야 하는 값이 보임
- 씬에 놓으면 에러 없이 실행됨

### 공용 승격 단계

개인 WIP에서 검증된 뒤 공용으로 옮긴다.

예시:

```text
Assets/_WIP/yongwoo/Prefabs/Enemy_Melee.prefab
-> Assets/Prefabs/Enemy/Enemy_Melee.prefab
```

파일 이동은 반드시 Unity Editor 안에서 한다.

---

## 15. 가장 중요한 감각

하이어라키가 복잡해지는 이유는 대부분 "무엇이 원본이고, 무엇이 배치인지"가 섞이기 때문이다.

그래서 아래만 계속 확인하면 된다.

1. 이건 실제 물건인가? -> GameObject / Prefab
2. 이건 데이터인가? -> ScriptableObject
3. 이 씬에만 있는가? -> Scene
4. 여러 번 쓸 건가? -> Prefab
5. 이 한 개만 달라야 하나? -> Prefab Instance Override
6. 모두 바뀌어야 하나? -> Prefab 원본 수정


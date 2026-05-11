# 캡스톤 팀 개발 규칙

> 액션 플랫포머 (카타나제로 스타일 도트 2D / 미래 SF / 심상세계)
> Unity + Git / 3인 팀

---

## 0. 프로젝트 시작 전 반드시 할 것

프로젝트를 만들자마자 아래 3가지를 먼저 한다. **안 하면 Git 협업이 안 된다.**

### 1) Unity 버전 통일

전원 **같은 Unity 버전**을 사용한다. 버전이 다르면 pull 받을 때마다 파일이 변경되어 충돌이 난다.

> 사용 버전: **6000.3.10f1**

### 2) Unity 프로젝트 설정 2가지

| 설정 | 경로 | 값 | 왜? |
|------|------|-----|-----|
| Asset Serialization | `Edit > Project Settings > Editor` | **Force Text** | 이걸 안 하면 씬/프리팹 파일이 바이너리라 Git이 변경사항을 읽지 못한다 |
| Version Control | `Edit > Project Settings > Editor` | **Visible Meta Files** | .meta 파일이 있어야 에셋끼리의 연결(참조)이 유지된다 |

### 3) .gitignore 설정

프로젝트 루트(최상위 폴더)에 `.gitignore` 파일을 만들고 아래 내용을 붙여넣는다.
이 파일들은 각자 PC에서 자동 생성되는 것들이라 Git에 올리면 안 된다.

```gitignore
# Unity 자동 생성 폴더 (용량 크고, 각자 PC에서 알아서 만들어짐)
/[Ll]ibrary/
/[Tt]emp/
/[Oo]bj/
/[Bb]uild/
/[Bb]uilds/
/[Ll]ogs/
/[Uu]ser[Ss]ettings/

# IDE (코드 에디터가 만드는 파일들)
.idea/
.vs/
.vscode/
*.csproj
*.sln
*.suo
*.user
*.pidb
*.booproj

# OS (운영체제가 만드는 파일)
.DS_Store
Thumbs.db
desktop.ini

# 빌드 결과물
*.apk
*.aab
*.unitypackage
*.app

# Crashlytics
crashlytics-build.properties
```

### 체크리스트

- [ ] Unity 버전 통일 확인
- [x] Asset Serialization → Force Text
- [x] Version Control → Visible Meta Files
- [x] .gitignore 적용
- [x] 폴더 구조 생성 (아래 참고)
- [ ] 각자 개인 브랜치 생성
- [ ] 각자 Dev 씬 생성

---

## 1. 폴더 구조

```
Assets/
├── _Scenes/               # 씬은 여기 한 곳에 모아두고 이름으로 구분한다
│   ├── Stage_01.unity     # 실제 게임에 들어가는 씬
│   ├── Stage_02.unity
│   ├── Dev_yongwoo.unity  # 개인 테스트용 씬
│   ├── Dev_B.unity
│   └── Dev_C.unity
│
├── _WIP/                  # 🔧 각자 작업 공간 (여기서 모든 작업을 한다)
│   ├── yongwoo/
│   │   ├── Scripts/
│   │   ├── Art/
│   │   ├── Prefabs/
│   │   └── ...            # 자유롭게 구성
│   ├── B/
│   │   ├── Scripts/
│   │   ├── Art/
│   │   ├── Prefabs/
│   │   └── ...
│   └── C/
│       ├── Scripts/
│       ├── Art/
│       ├── Prefabs/
│       └── ...
│
├── Scripts/               # ✅ 완성된 것만 여기로 옮긴다
│   ├── Player/
│   ├── Enemy/
│   ├── NPC/
│   ├── Dialogue/
│   ├── Stage/
│   ├── UI/
│   ├── System/            # 게임 매니저, 씬 전환 등
│   └── Utils/             # 공용 유틸리티
│
├── Art/                   # ✅ 완성된 아트
│   ├── Sprites/
│   │   ├── Player/
│   │   ├── Enemy/
│   │   ├── NPC/
│   │   ├── Tilemap/
│   │   └── UI/
│   ├── Animations/
│   └── VFX/
│
├── Audio/                 # ✅ 완성된 사운드
│   ├── BGM/
│   └── SFX/
│
├── Prefabs/               # ✅ 완성된 프리팹
│   ├── Player/
│   ├── Enemy/
│   ├── NPC/
│   ├── Stage/
│   └── UI/
│
├── Data/                  # ScriptableObject, JSON 등
├── Fonts/
├── ThirdParty/            # 서드파티 에셋은 여기 아래 에셋별 폴더로 둔다
├── Plugins/               # 코드/셰이더/툴 성격의 플러그인만 둔다
├── Resources/             # 플러그인/에셋 설정 파일 (예: DOTweenSettings)
└── Settings/              # 렌더 파이프라인/프로젝트용 설정 에셋
```

### 폴더 규칙

- **작업 중**에는 `_WIP/내이름/` 안에서 스크립트, 아트, 프리팹 등 **전부** 작업한다.
  - 내 폴더 안에서는 구조를 자유롭게 만들어도 된다.
  - 각자 폴더가 완전히 분리되어 있으므로 작업 중 파일 충돌이 거의 없다.
- **완성되면** 해당하는 공용 폴더(Scripts/Enemy/, Art/Sprites/ 등)로 옮긴다.
- 현재 프로젝트는 **플러그인**과 **서드파티 에셋**을 구분해서 관리한다.
  - 플러그인성 자산은 `Assets/Plugins`에 둔다.
  - 에셋 팩 / VFX / 아트 리소스는 `Assets/ThirdParty/에셋이름/` 형태로 분리한다.
- 현재 프로젝트의 대표 예시는 `Assets/ThirdParty/Feel`, `Assets/ThirdParty/ToonFX`, `Assets/ThirdParty/Market_Asset` 이다.
- 새 서드파티 에셋을 들여올 때도 가능하면 `Assets/ThirdParty/에셋이름/` 형태로 둔다.
- 외부 에셋 원본 파일은 절대 직접 수정하지 않는다. 필요하면 복사해서 사용한다.
- 이미 임포트된 외부 에셋 폴더는 **팀이 정한 구조 없이 수시로** 옮기지 않는다.
  - 현재 프로젝트는 `Assets/ThirdParty/에셋이름` 구조를 기준으로 정리한다.
  - 특히 `Assets/Resources/DOTweenSettings.asset` 같은 파일은 `Resources` 규칙을 유지해야 한다.
- 폴더를 분리해 두면 에셋 출처와 용도를 바로 파악할 수 있고, 삭제/업데이트/라이선스 관리도 편하다.

### 외부 에셋 관련 주의사항

- 외부 에셋은 가능하면 `Assets/ThirdParty` 아래에서 **에셋별 폴더를 유지**한다.
- `Plugins`는 아무 외부 에셋이나 넣는 폴더가 아니라, 코드/셰이더/에디터 툴 같은 플러그인만 넣는다.
- `Resources`는 경로 규칙이 중요한 파일이 있을 수 있으므로, 필요한 경우만 유지하고 함부로 옮기지 않는다.
- 폴더 이동이 꼭 필요하면 **반드시 Unity Editor 안에서** 이동하고, 임포트 후 참조가 유지되는지 확인한다.
- 데모 씬, 샘플 에셋, 대용량 파일은 꼭 필요한 것만 유지한다. 필요 없으면 저장소에 올리기 전에 정리한다.
- 저장소가 **public**이면 유료 Asset Store 에셋 원본 업로드는 재배포 문제가 생길 수 있으니 주의한다.
  - public 저장소에 올릴지 애매하면 먼저 팀장/담당자와 확인하고, 가능하면 private 저장소에서 관리한다.

### ⚠️ 파일 이동 시 주의사항

> **파일을 옮길 때는 반드시 Unity Editor 안에서 드래그해서 이동한다.**
>
> 윈도우 탐색기에서 직접 옮기면 `.meta` 파일이 안 따라가거나 새로 생성되면서
> 프리팹 연결, 애니메이션 참조 등이 **전부 깨질 수 있다.**
>
> 그리고 이동할 때 커밋을 "이동"과 "수정"으로 나눠서 하면 나중에 추적이 편하다.
> ```
> [Move] EnemyDrone 스크립트를 _WIP에서 Enemy 폴더로 이동
> [Update] EnemyDrone 감지 범위 조정
> ```

---

## 2. 씬 & 프리팹 충돌 방지 (⭐ 가장 중요)

### 왜 중요한가?

Unity의 씬(.unity) 파일과 프리팹(.prefab) 파일은 내부적으로 수천 줄의 텍스트 데이터다.
두 사람이 같은 씬을 동시에 수정하면 Git에서 **충돌(conflict)**이 나는데,
일반 코드와 달리 **사람이 읽고 수동으로 합치는 게 사실상 불가능하다.**

충돌이 나면 둘 중 하나의 작업을 버려야 한다. → **충돌을 안 나게 막는 게 핵심이다.**

### 예방 규칙

1. **자기 `_WIP/` 폴더 안에서는 자유롭게 작업한다** — 선언 필요 없음.

2. **공용 폴더나 다른 사람 파일을 건드릴 때는 반드시 팀톡에 선언한다**
   - 메인 씬(스테이지 씬 등), 공용 폴더의 스크립트/프리팹, 다른 사람이 만든 파일 등
   - 작업 시작 선언 → 작업 → 완료 선언 & 푸시

3. **프리팹 기반으로 작업한다**
   - 적, NPC, 기믹 등은 프리팹으로 만들어서 씬에 배치한다.
   - 프리팹을 수정하면 씬 파일을 안 건드려도 자동 반영되므로 충돌 위험이 줄어든다.

### 선언이 필요한 경우 / 필요 없는 경우

| 상황 | 선언 |
|------|------|
| `_WIP/내이름/` 안에서 작업 | ❌ 필요 없음 |
| `_Scenes/Dev_내이름.unity` 작업 | ❌ 필요 없음 |
| `_Scenes/Stage_01.unity` 수정 | ✅ **선언 필수** |
| 공용 `Scripts/Player/` 안의 파일 수정 | ✅ **선언 필수** |
| 공용 `Prefabs/` 안의 프리팹 수정 | ✅ **선언 필수** |
| 다른 사람의 `_WIP/` 파일 수정 | ✅ **선언 필수** (본인에게 먼저 말하기) |

### 작업 선언 예시 (카톡/디스코드)

```
🔒 Stage_01.unity 작업합니다
🔒 Scripts/Player/PlayerController.cs 수정합니다
✅ Stage_01.unity 끝, 푸시 완료
```

한 마디면 되니까 귀찮아도 꼭 하자. 이거 안 하면 몇 시간 작업이 날아간다.

### 만약 충돌이 났을 때 (씬/프리팹)

코드 파일(.cs)은 VS Code에서 양쪽을 비교하며 합칠 수 있지만,
**씬/프리팹은 수동 머지가 불가능**하다. 둘 중 하나를 선택해야 한다:

```bash
# 상대방 것을 쓰겠다 (내 작업을 버림)
git checkout --theirs Assets/_Scenes/Stage_01.unity

# 내 것을 쓰겠다 (상대방 작업을 버림)
git checkout --ours Assets/_Scenes/Stage_01.unity
```

선택한 후:
```bash
git add Assets/_Scenes/Stage_01.unity
git commit -m "[Fix] Stage_01 씬 충돌 해결 (내 것 선택)"
```

⚠️ 선택하지 않은 쪽의 작업은 다시 해야 한다. 그러니까 **충돌을 안 내는 게 답이다.**

---

## 3. Git 규칙

### 브랜치 전략 (심플하게)

```
main              ← 항상 실행 가능한 상태. 여기로 머지한다.
 ├── yongwoo      ← 용우 작업 브랜치
 ├── memberB      ← B 작업 브랜치
 └── memberC      ← C 작업 브랜치
```

- `main`: 합쳐진 결과물. 항상 빌드가 되는 상태를 유지한다.
- 각자 브랜치: 자기 브랜치에서 자유롭게 작업하고, 완성되면 main에 머지한다.

### 브랜치 만들기 & 사용하기 (처음이면 여기 보기)

```bash
# 1. 처음 한 번만: 자기 브랜치 만들기
git checkout -b yongwoo          # 자기 이름으로 브랜치 생성 & 이동

# 2. 평소 작업 시작할 때: 자기 브랜치로 이동
git checkout yongwoo

# 3. 작업 중: 평소처럼 커밋
git add .
git commit -m "[Add] 플레이어 대시 기능"

# 4. 내 브랜치에 푸시
git push origin yongwoo

# 5. main에 합치기 (기능이 완성됐을 때만!)
git checkout main                # main 브랜치로 이동
git pull origin main             # 다른 사람이 올린 것 먼저 받기
git merge yongwoo                # 내 브랜치를 main에 합치기
# (충돌이 있으면 여기서 해결)
git push origin main             # main에 푸시

# 6. 다시 내 브랜치로 돌아가서 계속 작업
git checkout yongwoo
git merge main                   # main의 최신 상태를 내 브랜치에도 반영
```

### 핵심 습관

- main에 머지하기 전에 **반드시** `git pull origin main` 먼저 한다.
- main에 합칠 때는 **빌드(컴파일)가 되는 상태에서만** 한다.
- **하루에 한 번**은 main을 pull해서 내 브랜치에 반영한다 (안 하면 나중에 충돌 폭발).

### 절대 하지 말 것

| 금지 | 이유 |
|------|------|
| `git push --force` | 다른 사람 커밋이 사라진다 |
| 컴파일 에러 상태로 main 머지 | 다른 사람이 Unity를 열 수 없게 된다 |
| Library, Temp 폴더 Git에 올리기 | 저장소 용량 폭발 + 충돌 (.gitignore 확인) |
| .meta 파일 삭제 | 에셋 연결이 전부 끊어진다 |

### 커밋 메시지 규칙

```
[태그] 한줄 설명

[Add]      새 기능/파일 추가
[Fix]      버그 수정
[Update]   기존 기능 수정·개선
[Delete]   파일/기능 삭제
[Move]     파일 이동 (WIP → 공용 폴더 등)
[Art]      아트 에셋 추가/수정
[Audio]    사운드 에셋 추가/수정
```

예시:
```
[Add] 플레이어 대시 기능 구현
[Fix] 적이 벽 뚫고 이동하는 버그 수정
[Art] Stage01 타일맵 스프라이트 추가
[Move] EnemyDrone 스크립트 _WIP에서 Enemy로 이동
```

---

## 4. 씬/레이어/판정 공통 규칙

이 섹션은 현재 프로젝트의 2D 플랫포머 액션 작업 시 공통으로 따르는 씬 구성 기준이다.

### 기본 방향

- 프로젝트는 2D 플랫포머 액션 기준으로 작업한다.
- 레이어는 "오브젝트 종류"보다 "화면에서의 역할"과 "게임플레이 판정" 기준으로 나눈다.
- 2D 앞뒤 정렬은 `Z` 축이 아니라 `Sorting Layer + Order in Layer`로 관리한다.
- 외부 에셋 원본은 직접 수정하지 않고, 필요하면 복사해서 사용한다.

### Sorting Layer 규칙

현재 프로젝트의 표준 Sorting Layer 순서는 아래와 같다.

1. `Background`
2. `Ground`
3. `Player`
4. `Enemy`
5. `Effect`
6. `Foreground`

사용 기준은 아래와 같다.

- `Background`: 하늘, 먼 건물, 시장 배경 레이어처럼 뒤에 깔리는 모든 배경
- `Ground`: 바닥, 벽, 플랫폼, 뒤쪽 구조물, 지형성 오브젝트
- `Player`: 플레이어 비주얼
- `Enemy`: 적 비주얼
- `Effect`: 공격 이펙트, 피격 이펙트, 먼지, 번쩍임 등
- `Foreground`: 캐릭터 앞을 가리는 간판, 기둥, 천막 등

추가 규칙:

- 배경을 이어붙이지 말고, 같은 위치에 겹쳐놓고 `Order in Layer`로만 깊이를 나눈다.
- 같은 계열의 오브젝트는 `Order in Layer`를 10 단위로 띄워서 사용한다.
- 자판기/간판 같은 오브젝트도 종류로 레이어를 정하지 말고, 화면에서 뒤에 있으면 `Ground`, 앞을 가리면 `Foreground`로 둔다.

### 배경 배치 규칙

배경 에셋은 아래 규칙으로 배치한다.

- 배경 레이어 이미지는 하나의 부모 오브젝트 아래에 둔다.
- 부모 오브젝트 기준으로 자식들의 `localPosition`은 모두 `(0, 0, 0)`으로 맞춘다.
- 자식들의 `localScale`은 모두 `(1, 1, 1)`로 맞춘다.
- 모든 배경은 `Sorting Layer = Background`로 둔다.
- 깊이 차이는 `Order in Layer`로만 조정한다.

예시:

- `1.png = 0`
- `2.png = 10`
- `3.png = 20`
- `4.png = 30`
- `5.png = 40`
- `Overlay_illumination.png = 50`

### Unity Layer 규칙

현재 프로젝트의 표준 Unity Layer는 아래와 같다.

- `Ground`
- `Player`
- `Enemy`
- `Projectile`
- `Trigger`
- `Trap`

사용 기준은 아래와 같다.

- `Ground`: 바닥, 벽, 플랫폼, 충돌해야 하는 지형
- `Player`: 플레이어 본체 충돌체
- `Enemy`: 적 본체 충돌체
- `Projectile`: 플레이어/적/보스가 발사하는 탄, 화살, 에너지탄 같은 투사체 본체
- `Trigger`: 문, 체크포인트, 상호작용 범위, 이벤트 감지 영역
- `Trap`: 가시, 전기 바닥, 독성 바닥, 낙하 함정 같은 환경 위험물

### 충돌/판정 규칙

- 초반 프로토타입 단계에서는 충돌 레이어를 과하게 나누지 않는다.
- 우선은 `Player`와 `Enemy` 본체 콜라이더가 이동/피격 역할을 같이 맡아도 된다.
- 투사체는 `Projectile` 레이어를 기본으로 사용한다.
- 아군/적군 구분은 레이어를 더 쪼개기보다, 우선 각 투사체 스크립트에서 owner/대상 판정으로 처리한다.
- `Projectile`끼리의 충돌은 기본적으로 막고, `Trigger`와의 충돌도 필요한 경우에만 허용한다.
- 전투가 복잡해질 때만 `Attack`, `Hurtbox` 같은 세부 판정을 추가 검토한다.
- `Trap`은 적이나 플레이어를 다치게 하는 환경 요소를 의미한다.
- `Trigger`는 물리적으로 막지 않고 감지만 하는 영역에 사용한다.
- 배경 전용 오브젝트에는 콜라이더를 붙이지 않는 것을 기본 원칙으로 한다.

### 프리팹/오브젝트 구성 원칙

- 캐릭터 루트와 비주얼, 충돌체, 체크용 자식 오브젝트는 분리한다.
- 예시 구조:
  - `PlayerRoot`
  - `Visual`
  - `BodyCollider`
  - `GroundCheck`
- 적도 같은 원칙으로 구성한다.
- 시각 표현용 오브젝트와 실제 충돌용 오브젝트를 분리하면 수정이 훨씬 쉽다.

### 작업 시 주의사항

- 새 아트 에셋을 바로 씬에 넣기 전에 먼저 어떤 Sorting Layer에 들어갈지 정한다.
- 지형성 오브젝트인지, 앞가림 오브젝트인지 먼저 판단한 뒤 배치한다.
- Unity Layer는 물리/판정용이므로, 화면 정렬 용도로 사용하지 않는다.
- Sorting Layer는 화면 표시용이므로, 충돌 규칙을 기대하지 않는다.
- 새 시스템을 추가해 레이어 체계가 바뀌면, `TagManager.asset` 변경과 함께 이 문서도 같이 갱신한다.

---

# 여기까지가 핵심. 아래는 참고사항.

---

## (참고) 네이밍 컨벤션

강제는 아니지만, 맞춰두면 나중에 파일 찾기가 편하다.

### 스크립트 (C#)

| 항목 | 규칙 | 예시 |
|------|------|------|
| 클래스명 | PascalCase | `PlayerController`, `EnemyPatrol` |
| public 변수 | camelCase | `moveSpeed`, `jumpForce` |
| private 변수 | _camelCase | `_currentHealth`, `_isGrounded` |
| 메서드 | PascalCase | `TakeDamage()`, `StartDialogue()` |

### 에셋 파일

접두어를 붙이면 파일 목록에서 종류를 바로 구분할 수 있다:

| 접두어 | 종류 | 예시 |
|--------|------|------|
| `spr_` | 스프라이트 | `spr_player_idle_01.png` |
| `anim_` | 애니메이션 | `anim_player_run.anim` |
| `pfb_` | 프리팹 | `pfb_enemy_drone.prefab` |
| `bgm_` | 배경음악 | `bgm_stage01.wav` |
| `sfx_` | 효과음 | `sfx_player_dash.wav` |

---

## (참고) 코드 습관

여유가 되면 지키면 좋은 것들:

- 인스펙터에 노출할 변수는 `public` 대신 `[SerializeField] private`을 쓴다.
- `Find()`, `FindObjectOfType()` 같은 탐색 함수는 `Update()`에서 쓰지 않는다 (성능 문제).

---

## 6. 작업 흐름 요약

```
1. 자기 브랜치에서 작업 시작
2. _WIP/내이름/ 폴더와 개인 Dev 씬에서 자유롭게 개발
3. 기능이 완성되면 → _WIP에서 공용 폴더로 파일 이동 (Unity Editor 안에서!)
4. 공용 파일이나 남의 파일 수정할 때는 팀톡에 선언 → 완료 후 완료 선언
5. 커밋 메시지 규칙에 맞게 커밋 & 내 브랜치에 푸시
6. main에 머지 (빌드 되는 상태에서만!)
```

---

## 자주 하는 실수 & 해결법

| 실수 | 증상 | 해결 |
|------|------|------|
| 탐색기에서 파일 이동 | 프리팹 참조 깨짐, Missing 에러 | Unity Editor 안에서만 이동할 것 |
| .meta 파일 삭제 | 에셋 연결 끊어짐 | .meta는 절대 건드리지 않는다 |
| Unity 버전 다름 | pull 받을 때마다 파일 변경됨 | 전원 같은 버전 통일 |
| Library 폴더 올림 | 저장소 용량 폭발, 충돌 | .gitignore 확인 |
| 씬 동시 수정 | 머지 불가 충돌 | 작업 전 팀톡 선언 |
| 컴파일 에러 상태로 main 머지 | 다른 사람 Unity 못 열음 | 빌드 확인 후 머지 |
| `push --force` | 다른 사람 커밋 삭제됨 | 절대 쓰지 않기 |
| main을 오래 안 당김 | 나중에 충돌 한꺼번에 터짐 | 하루 한 번 pull & merge |

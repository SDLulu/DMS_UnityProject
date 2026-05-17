# 런타임 구조 문서

이 문서는 `Assets/_WIP/yongwoo/Scripts` 런타임 커스텀 코드의 현재 구조를 의미 단위로 풀어쓴 문서다.
목적은 "지금 뭐가 어디서 돌아가는지"를 먼저 정확히 파악하고, 그 다음에 중복되거나 흐린 책임을 정리할 기준을 세우는 것이다.

---

## 전체 한줄 지도

- `Input`: 입력 자산 접근, 입력 모드 전환, 리바인드 저장
- `UI / Dialogue`: UI 패널 표시, 대화 데이터, 대화 재생 생명주기
- `Player Core`: 이동, 체력/사망, 애니메이션, 손 조준, 슬로우 모션, 통합 설정
- `Player Combat / Weapons`: 공격 입력 허브, 칼/총, 타격 판정, 플레이어 투사체
- `Boss`: 보스 설정 데이터, 상태 기계, 체력 창구, 애니메이션, 보스 투사체
- `Prototype`: 정식 무기 구조와 별도로 손맛 검증용 전투/애니메이션
- `World / Utility`: 카메라 추적, 패럴랙스, 공통 데미지 인터페이스, 런타임 시각 유틸, 프리팹 자동 저장

---

## 1. 입력

### 이 묶음의 역할

게임 안의 입력을 한 군데에서 읽고, 플레이/대화/UI 모드를 나누고, 키 리바인드 저장까지 담당한다.
즉 "지금 어떤 입력을 받을 수 있는가"의 기준점이다.

### 파일별 역할

#### `GameInput`

- Input System 액션 에셋 `Resources/Input/InputSystem_Actions`를 로드하는 단일 진입점이다.
- `Player`, `Dialogue`, `UI` 액션 맵을 잡고, 런타임 코드가 바로 디바이스를 읽지 않게 막는다.
- 이동, 점프, 대시, 공격, 상호작용, 무기 교체, 슬로우 모션, 대화 진행/스킵, 마우스 위치를 속성으로 노출한다.
- 입력 모드 전환도 여기서 한다.
  - `EnableGameplay()`
  - `EnableDialogue()`
  - `DisableAllGameplayInput()`
- 리바인드도 여기서 시작한다.
  - 액션 찾기
  - 바인딩 인덱스 찾기
  - 인터랙티브 리바인드 시작
  - 오버라이드 JSON 저장/로드

#### `GameInputSettingsStore`

- 입력 설정을 파일로 저장하는 얇은 저장소다.
- 실제 저장 포맷은 JSON 하나고, 현재는 `bindingOverridesJson`만 저장한다.
- 경로는 `Application.persistentDataPath/game-input-settings.json`.
- `GameInput`이 직접 파일 I/O를 몰라도 되게 분리한 계층이다.

#### `GameInputSettingsPanel`

- 씬에 있는 버튼/텍스트 UI를 실제 입력 설정과 연결하는 뷰 컨트롤러다.
- `BindingRow` 리스트를 가지고 각 행에
  - 라벨
  - 현재 바인딩 텍스트
  - 리바인드 버튼
  - 리셋 버튼
  를 묶는다.
- 리바인드 시작, 개별 리셋, 전체 리셋, 상태 문구 갱신까지 담당한다.
- 저장은 직접 하지 않고 `GameInput`과 `GameInputSettingsStore`를 호출한다.

### 데이터 흐름

- `GameInput`이 액션 자산을 로드한다.
- `GameInputSettingsPanel`은 `GameInput.Instance`에서 현재 바인딩 표시 문자열을 읽는다.
- 사용자가 리바인드하면 `GameInput.StartInteractiveRebind()`를 호출한다.
- 완료되면 `GameInput.SaveSettings()` -> `GameInputSettingsStore.Save()`로 저장된다.

### 현재 구조의 의도

- "입력 읽기"와 "입력 저장"과 "입력 UI"를 분리하려는 의도는 분명하다.
- 특히 `GameInput`을 통과하지 않고 각 스크립트가 직접 Input System 액션을 들지 않게 막은 점은 구조상 좋다.

### 겹치거나 애매한 부분

- 입력 설정 패널 구성 책임이 `GameInputSettingsPanel`과 `UIManager`에 나뉘어 있다.
- `GameInputSettingsPanel`은 실제 리바인드 동작을 담당하는데, 어떤 행이 어떤 액션인지의 지식은 상당 부분 `UIManager`가 들고 있다.
- 그래서 입력 UI는 "패널 스스로 아는 구조"가 아니라 "외부에서 조립해줘야 하는 구조"에 가깝다.

### 정리 후보

- 입력 설정 패널의 행 정의를 `UIManager`에서 빼고 `GameInputSettingsPanel` 내부 정의나 별도 정의 객체로 옮기기
- `GameInput`은 입력/모드 전환/리바인드 진입점만 알고, UI 슬롯 이름 같은 화면 구조 지식은 모르도록 유지하기
- 없는 액션 이름이나 잘못된 바인딩 인덱스를 더 명확하게 드러내는 진단 경로 만들기

### 보류할 부분

- 입력 시스템 자체를 싱글톤에서 DI 구조로 바꾸는 것은 지금 단계에서 과하다.
- 현재는 손맛 검증 단계라 `GameInput.Instance` 중심 구조를 유지해도 된다.

---

## 2. UI / 대화

### 이 묶음의 역할

화면에 뭔가를 보여주는 패널 중에서, 특히 대화와 입력 설정 패널을 실제 씬 UI와 연결한다.
또한 대화 재생 중에 플레이어 조작과 카메라를 어떻게 잠그고 복구할지도 이 묶음에서 처리한다.

### 파일별 역할

#### `UIManager`

- 씬에 이미 배치된 UI를 찾아서 연결하고, 표시/숨김을 관리하는 허브다.
- 직접 새 UI를 생성하지 않고 `scene-authored UI`를 연결하는 쪽에 가깝다.
- 현재 맡는 일:
  - 입력 설정 패널 루트 찾기
  - 입력 설정 패널 컴포넌트 찾기
  - 대화 패널 찾기
  - 설정 패널 열기/닫기
  - 설정 패널 버튼과 행을 씬 계층에서 찾아 `GameInputSettingsPanel`에 넘기기

#### `DialogueData`

- 대화 한 줄 데이터인 `DialogueLineData`
- 대화 묶음 ScriptableObject인 `DialogueSequence`
- 실행 시 옵션 오버라이드를 담는 `DialoguePlaybackContext`
를 정의한다.
- 즉 대화 시스템의 데이터 계약이다.

#### `DialogueManager`

- 대화 재생의 생명주기를 관리한다.
- 외부는 `DialogueManager.TryPlay(...)`만 호출하면 되고,
  내부에서
  - 입력 모드 전환
  - 플레이어 조작 잠금
  - 카메라 추적 끄기
  - 대화 종료 후 원복
  을 처리한다.
- 이 코드의 핵심은 "대화를 재생한다"보다 "대화 중 게임 상태를 어떻게 바꾸고 복구하느냐"에 있다.

#### `DialoguePanel`

- 실제 대사 UI를 그리는 순수 뷰에 가장 가깝다.
- 하는 일:
  - 이름 표시
  - 본문 타이핑 출력
  - 좌우 초상 교체
  - 힌트 문구 표시
  - Space/Enter 진행, Tab/Esc 스킵 읽기
- UI 계층 이름인 `DialogueRoot`, `Name`, `BodyText`, `HintText`, `LeftPortrait`, `RightPortrait`를 기준으로 자동 바인딩도 한다.

#### `NpcDialogueInteractable`

- NPC가 직접 플레이어와 거리, 상호작용 키를 감지해서 `DialogueManager`를 호출하는 진입점이다.
- NPC 한 개가 자기 대화 재생 책임을 가지는 구조다.

### 데이터 흐름

- NPC -> `NpcDialogueInteractable`
- 상호작용 성공 -> `DialogueManager.TryPlay(DialogueSequence, DialoguePlaybackContext)`
- `DialogueManager`
  - `GameInput.EnableDialogue()`
  - 필요 시 `PlayerInteraction.SetGameplayControlEnabled(false)`
  - 필요 시 `SimpleCameraFollow.enabled = false`
- `DialoguePanel.Play(...)`
- 대화 종료 시 `DialogueManager`가 다시 입력/카메라/플레이어 상태를 복구

### 현재 구조의 의도

- `DialogueManager`는 흐름과 상태를,
- `DialoguePanel`은 화면 표시를,
- `DialogueData`는 데이터 포맷을 맡는 식으로 나누려는 의도가 보인다.
- 이 방향은 맞다. 특히 대화 패널이 직접 플레이어를 잠그지 않는 점은 좋다.

### 겹치거나 애매한 부분

- `UIManager`도 대화 패널을 알고 있고, `DialogueManager`도 대화 패널을 직접 관리한다.
- 즉 대화 UI의 "소유자"가 완전히 하나로 고정된 느낌은 아니다.
- `DialoguePanel`은 뷰인데도 입력을 직접 읽는다. 순수 뷰라기보다 "작은 발표기 + 입력 처리기"다.
- 자동 바인딩 로직과 계층 이름 의존이 많아서 씬 이름이 바뀌면 깨질 여지가 있다.

### 정리 후보

- 대화 UI 접근의 공식 관문을 `DialogueManager` 하나로 더 분명히 만들기
- `DialoguePanel`에서 입력 읽기를 분리하고, 진행/스킵 판단은 `DialogueManager`로 올릴지 검토
- `UIManager`는 진짜 허브만 하게 줄이고, 대화 관련 세부 구성은 덜 알게 만들기

### 보류할 부분

- 대화 시스템 전체를 이벤트 기반으로 다시 짜는 건 아직 이르다.
- 지금은 `DialogueManager -> DialoguePanel` 직결 구조만 명확히 다듬는 정도가 현실적이다.

---

## 3. 플레이어 코어

### 이 묶음의 역할

플레이어라는 오브젝트가 "움직이고, 맞고, 죽고, 다시 살아나고, 보이고, 조준하고, 슬로우 모션을 켜는" 핵심 몸통이다.

### 파일별 역할

#### `PlayerConfig`

- 플레이어 튜닝값 묶음이다.
- 세부 묶음:
  - `PlayerMovementConfig`
  - `PlayerAttackConfig`
  - `PlayerHealthConfig`
  - `PlayerColliderConfig`
  - `PlayerCameraConfig`
- `PlayerConfigLoader`는 기본값 채우기, 범위 보정, deep clone을 담당한다.
- 즉 "숫자와 설정의 저장 형태"다.

#### `PlayerRuntimeConfig`

- `PlayerConfig`를 실제 런타임 컴포넌트들에 뿌리는 브리지다.
- 연결 대상:
  - `SimplePlayerController`
  - `SimplePlayerCombat`
  - `PlayerInteraction`
  - `BoxCollider2D`
  - `SimpleCameraFollow`
- 역할은 "컴포넌트 하나하나가 설정 파일을 직접 몰라도 되게 하는 것"이다.
- 현재 플레이어 설정 구조의 조립 허브다.

#### `SimplePlayerController`

- 플레이어 코어 중 가장 큰 중심 파일이다.
- 담당:
  - 좌우 이동
  - 코요테 타임
  - 점프 버퍼
  - 중력 조절
  - 점프 컷
  - 정점 보정
  - 지면 붙이기
  - 대시
  - 구르기
  - 웅크리기
  - 방향 전환
- 입력은 `Update`에서 읽고, 물리 적용은 `FixedUpdate`에서 한다.
- 특수 액션은 `queued`로 예약했다가 물리 틱에서 확정한다.
- 이동 구조를 물리적으로 꽤 신경 써서 만든 파일이다.

#### `PlayerInteraction`

- 플레이어 외부 상호작용 창구다.
- 외부 시스템은 플레이어 내부 구현 대신 이 컴포넌트와 통신한다.
- 담당:
  - 체력
  - 무적 시간
  - 피격 넉백
  - 색상 플래시
  - 사망
  - 리스폰
  - 런타임 조작 잠금/복구
- `Damaged`, `Died`, `HealthChanged`, `Respawned` 이벤트를 낸다.
- 사실상 플레이어의 "생명/상태 표면 API"다.

#### `PlayerAnimationDriver`

- 이동/전투/피격 상태를 읽어서 애니메이터 상태로 번역한다.
- 상태 소스:
  - `SimplePlayerController`
  - `SimplePlayerCombat`
  - `PlayerInteraction`
  - `Rigidbody2D`
- `AttackPerformed` 이벤트를 구독해서 공격 애니메이션을 재생한다.
- 즉 입력이나 전투 판정이 아니라 "표현만 책임지는 층"이다.

#### `PlayerHand`

- 플레이어 손 오브젝트를 마우스 방향으로 회전시킨다.
- 무기 스프라이트는 이 오브젝트 자식으로 붙어 손과 함께 회전한다.
- 출력:
  - `AimDirection`
  - `IsAimingLeft`
- `SimplePlayerCombat`과 무기 스크립트가 이 값을 사용한다.

#### `PlayerSlowMotion`

- 슬로우 모션 키를 누를 때 `Time.timeScale`을 낮춘다.
- 아주 얇은 기능 파일이다.
- 전역 시간에 손대는 기능이라 역할은 작지만 영향 범위는 넓다.

### 데이터 흐름

- `PlayerRuntimeConfig`가 설정 묶음을 각 컴포넌트에 적용
- `SimplePlayerController`가 입력을 읽어 물리 상태 생산
- `PlayerHand`가 마우스 기반 조준 방향 생산
- `SimplePlayerCombat`은 조준 방향과 공격 입력을 읽음
- `PlayerInteraction`은 피격/사망/리스폰 처리
- `PlayerAnimationDriver`는 위 상태들을 읽어 애니메이션 출력
- `PlayerSlowMotion`은 별도로 전역 시간 배율 조절

### 현재 구조의 의도

- 플레이어를 "이동", "전투", "상호작용/체력", "애니메이션", "조준", "설정 브리지"로 나누려는 의도가 확실하다.
- 이건 현재 프로젝트 규칙인 "하나의 스크립트는 하나의 역할"과도 잘 맞는다.

### 겹치거나 애매한 부분

- `PlayerRuntimeConfig`가 꽤 많은 컴포넌트를 알고 있다.
  - 설정 브리지 역할은 맞지만, 사실상 플레이어 조립 관리자처럼 커질 수 있다.
- `PlayerInteraction`은 체력 관리뿐 아니라 "조작 On/Off"도 같이 맡는다.
  - 외부 상호작용 창구라는 의미에서는 맞지만, 체력 계층과 제어 계층이 같이 있다.
- `SimplePlayerController`는 이동 전용이지만 규모가 커서, 내부적으로는 이미 `입력 버퍼`, `지면 판정`, `특수 액션`, `중력 계산`이 한 파일 안에 같이 있다.

### 정리 후보

- `SimplePlayerController` 내부를 나중에 기능 블록 기준으로 더 잘게 나눌 수 있다.
  - 점프/중력
  - 대시/롤
  - 지면 판정
  지금은 파일 분리보다 내부 구조 정리가 먼저다.
- `PlayerInteraction`의 조작 잠금 책임을 별도 작은 계층으로 뺄지 검토
- `PlayerRuntimeConfig`가 계속 커지면 "설정 적용 전용 브리지"인지 "플레이어 조립 허브"인지 경계를 다시 잡아야 한다.

### 보류할 부분

- 플레이어 코어는 지금 게임의 중심이다.
- 특히 `SimplePlayerController`는 손맛이 걸려 있어서 구조만 보고 섣불리 쪼개면 위험하다.
- 이 묶음은 리팩토링보다 먼저 "테스트 기반"이나 "행동 보존 기준"을 잡고 들어가야 한다.

---

## 4. 플레이어 전투 / 무기

### 이 묶음의 역할

공격 입력을 실제 무기 동작으로 바꾸고, 근접/원거리 판정을 분리해서 관리한다.

### 파일별 역할

#### `SimplePlayerCombat`

- 플레이어 전투의 입력 허브다.
- 담당:
  - 무기 교체
  - 공격 입력 감지
  - 현재 무기에 공격 위임
  - 공격 이벤트 발행
- 실제 판정은 하지 않고 `SwordWeapon` 또는 `GunWeapon`으로 넘긴다.

#### `SwordWeapon`

- 칼 공격 쿨다운과 히트박스 활성 시간만 관리한다.
- `SlashHitbox`를 켜고 끄는 타이머 제어기다.

#### `SlashHitbox`

- 칼 공격의 실제 판정체다.
- 트리거 충돌이 일어나면 `IDamageReceiver`를 찾아 데미지를 전달한다.
- 이미 맞은 대상을 `HashSet`으로 기억해서 한 번 휘두를 때 중복 타격을 막는다.

#### `GunWeapon`

- 총 공격 쿨다운, 총구 화염, 투사체 생성 담당이다.
- 실제 총알은 `SimplePlayerProjectile`에게 맡긴다.

#### `SimplePlayerProjectile`

- 플레이어 투사체 한 발의 생명주기를 담당한다.
- 이동, 충돌, 수명 종료, 데미지 전달이 전부 여기 있다.
- 프리팹 비주얼이 부족하면 폴백 비주얼도 세팅한다.

### 데이터 흐름

- `GameInput.AttackPressed`
- `SimplePlayerCombat`
  - 현재 무기 판단
  - 조준 방향은 `PlayerHand.AimDirection`
- 칼이면 `SwordWeapon.Attack()`
  - `SlashHitbox.Activate()`
  - 충돌 시 `IDamageReceiver.ReceiveHit()`
- 총이면 `GunWeapon.Attack()`
  - 투사체 생성
  - `SimplePlayerProjectile.Launch()`
  - 충돌 시 `IDamageReceiver.ReceiveHit()`

### 현재 구조의 의도

- 공격 입력 허브
- 무기별 행동
- 판정체
를 나누려는 의도는 좋다.
- 특히 근접과 원거리가 "공격 허브는 공통, 실행 방식은 무기별"로 나뉜 점은 확장에 유리하다.

### 겹치거나 애매한 부분

- `SimplePlayerCombat`이 현재는 사실상 "검/총 토글러"다.
- 무기 시스템이라기보다 두 개 무기 전환 데모에 가깝다.
- `SlashHitbox`와 `SimplePlayerProjectile` 둘 다 비슷한 데미지 전달 로직을 갖고 있다.
  - 대상 찾기
  - 자기 자신 공격 방지
  - `IDamageReceiver` 호출
- 즉 판정 전달 규칙이 두 군데로 퍼져 있다.

### 정리 후보

- 데미지 전달 공통 유틸 또는 공통 헬퍼로 합치기
- `SimplePlayerCombat`을 무기 슬롯 관리자와 공격 입력 허브 중 어디로 볼지 명확히 정하기
- 무기가 늘어나면 `SwordWeapon`, `GunWeapon`을 직접 들고 있는 구조는 빨리 한계가 온다. 그 시점에는 공통 무기 인터페이스 검토

### 보류할 부분

- 지금은 무기가 2개뿐이라 인터페이스 추상화부터 넣는 건 오버엔지니어링일 수 있다.
- 다만 데미지 전달 규칙 중복은 비교적 빨리 정리해도 안전하다.

---

## 5. 보스

### 이 묶음의 역할

보스 프리팹 하나 안에서 보스 전투를 자립적으로 굴리기 위한 묶음이다.
데이터, 체력, 상태 기계, 패턴 실행, 애니메이션, 투사체가 모두 여기에 들어 있다.

### 파일별 역할

#### `BossConfig`

- 보스 튜닝값 전체 묶음이다.
- 계층:
  - `BossCoreConfig`
  - `BossPhaseConfig`
  - `BossPatternConfig`
- 보스 전체 수치, 경기장 범위, 페이즈 색, 패턴 쿨다운/거리/텔레그래프/실행/회복 값이 들어 있다.
- `BossConfigLoader`는 기본값 생성, deep clone, sanitize를 담당한다.

#### `BossController`

- 보스 전투의 중심 허브다.
- 상태:
  - `Roam`
  - `Telegraph`
  - `Execute`
  - `Recover`
  - `Defeated`
- 패턴 타입:
  - `DashStrike`
  - `LeapSlam`
  - `ProjectileFan`
- 하는 일:
  - 타깃 추적
  - 경기장 안 이동
  - 패턴 후보 선택
  - 페이즈 갱신
  - 접촉 데미지
  - 패턴별 실행
  - 디버그 히트박스 비주얼
  - 보스 몸체 비주얼 방향 조정
- 이 프로젝트 보스 로직의 진짜 심장이다.

#### `BossInteraction`

- 보스 외부 상호작용 창구다.
- 플레이어 쪽의 `PlayerInteraction`과 거의 같은 역할을 보스 쪽에서 수행한다.
- 담당:
  - 체력
  - 피격
  - 무적
  - 데미지 플래시
  - 사망
  - 리스폰
  - 상태 이벤트 발행

#### `BossAnimationDriver`

- `BossController`와 `BossInteraction` 상태를 읽어 보스 애니메이션만 갱신한다.
- 표현 계층 분리 용도다.

#### `BossProjectile`

- 보스가 쏘는 탄 한 발의 이동, 충돌, 수명 담당이다.
- 충돌 대상은 `PlayerInteraction` 쪽을 노린다.

### 데이터 흐름

- `BossConfig`가 수치 기준
- `BossController`가 상태 기계와 패턴 실행
- 패턴 실행 중 필요하면
  - 접촉 데미지 직접 적용
  - 대시/점프 히트박스 판정
  - `BossProjectile.Configure()`로 탄 생성
- 피격/사망은 `BossInteraction`
- 애니메이션은 `BossAnimationDriver`

### 현재 구조의 의도

- 예전 만능 보스 디렉터 구조를 줄이고, 보스 프리팹 스스로 전투를 끝내려는 의도가 강하다.
- `BossController`와 `BossInteraction`을 분리한 것도 플레이어 구조와 대칭을 맞추려는 선택으로 보인다.

### 겹치거나 애매한 부분

- `BossController`가 매우 많은 걸 안다.
  - 상태 기계
  - 패턴 정의 해석
  - 이동
  - 접촉 데미지
  - 투사체 발사
  - 히트박스 디버그 표시
  - 체력 연동
- 즉 "보스 전투 허브"를 넘어 "보스 전투 전체 구현"에 가깝다.
- `BossInteraction`은 플레이어 쪽 `PlayerInteraction`과 구조가 매우 비슷하다.
  - 체력
  - 무적
  - 플래시
  - 사망
  - 리스폰
  거의 대칭이다.
- 보스 투사체와 플레이어 투사체도 생명주기와 데미지 전달 구조가 닮아 있다.

### 정리 후보

- `BossController` 내부를 패턴 실행 단위로 분리하기
  - 상태 기계
  - 패턴 실행기
  - 히트박스 디버그
- `PlayerInteraction` / `BossInteraction` 공통 구조를 나중에 공통 체력 베이스로 묶을지 검토
- `BossProjectile` / `SimplePlayerProjectile` 공통 투사체 규칙 통합 검토

### 보류할 부분

- 보스 전투는 시각/판정/행동이 얽혀 있어 한 번에 건드리기 위험하다.
- 특히 `BossController`는 지금 리팩토링보다 "패턴별 단위 테스트 가능한 구조"로 옮길 시점을 기다리는 게 안전하다.

---

## 6. 프로토타입

### 이 묶음의 역할

정식 플레이어 무기 구조와 별도로, 빠르게 손맛을 검증하려고 만든 독립 실험 축이다.

### 파일별 역할

#### `BlindHuntressPrototypeCombat`

- 정식 `SimplePlayerCombat + Sword/Gun` 구조를 거치지 않고,
  스킬 기반 근접 전투 실험을 바로 돌리는 코드다.
- 내부에 `SkillType`, `SkillConfig`가 있고, 공격 종류별 판정 크기/지속시간/대시 속도/중력 제어까지 다 품고 있다.
- 즉 "프로토타입 전투 패키지"가 한 파일에 들어 있다.

#### `BlindHuntressPrototypeAnimationDriver`

- 위 프로토타입 전투 상태와 플레이어 상태를 읽어 실험용 애니메이션을 돌린다.

### 데이터 흐름

- `GameInput.AttackPressed`
- `BlindHuntressPrototypeCombat`
  - 자체 스킬 상태 관리
  - OverlapBox 계열 판정
  - `IDamageReceiver` 호출
- `BlindHuntressPrototypeAnimationDriver`가 이를 표현으로 변환

### 현재 구조의 의도

- 정식 구조가 완성되기 전에 손맛 검증을 빠르게 하려는 실험용 축이다.
- "전부 객체로 통일" 원칙보다 "빨리 움직여 보기" 쪽에 더 무게를 둔 코드다.

### 겹치거나 애매한 부분

- 정식 플레이어 전투 구조와 개념이 겹친다.
  - 공격 쿨다운
  - 공격 판정
  - 애니메이션 상태
  - 이동 중 특수 공격
- 즉 현재 저장소에는
  - 정식 방향 전투 구조
  - 프로토타입 전투 구조
  두 개가 동시에 공존한다.

### 정리 후보

- 이 묶음을 계속 유지할지, 정식 구조에 흡수할지 빨리 결정해야 한다.
- 유지한다면 "실험 전용"임을 더 분명히 하고,
- 흡수한다면 어떤 감각과 로직을 정식 구조로 가져올지 선별해야 한다.

### 보류할 부분

- 프로토타입은 일부러 지저분해도 괜찮은 영역이다.
- 다만 정식 구조와 오래 공존하면 중복 판단을 흐리게 만든다.

---

## 7. 월드 / 유틸 / 공통

### 이 묶음의 역할

전투나 플레이어 핵심 규칙은 아니지만, 런타임 동작을 보조하는 공통 조각들이다.

### 파일별 역할

#### `SimpleCameraFollow`

- 카메라가 플레이어를 추적하는 런타임 컴포넌트다.
- 오프셋, 룩어헤드, 추적 속도를 조정한다.
- `PlayerRuntimeConfig`가 설정을 적용하고, `DialogueManager`는 필요 시 끄고 켠다.

#### `SimpleParallaxBackground`

- 배경 레이어를 카메라 이동에 따라 서로 다른 비율로 움직이는 표현용 컴포넌트다.
- 씬 전용 뷰 계층에 가깝다.

#### `IDamageReceiver`

- 데미지 받을 수 있는 대상의 최소 계약이다.
- 플레이어, 보스, 앞으로의 적이 이 인터페이스를 구현하면 무기나 투사체는 구체 타입을 몰라도 된다.

#### `RuntimeSpriteUtility`

- 런타임 폴백용 흰색/원형 스프라이트와 언릿 머티리얼을 만든다.
- 디버그 히트박스나 즉석 프로토타입 비주얼에 쓰인다.

#### `PrefabAutoSaveUtility`

- 에디터 플레이 중 조정한 플레이어/보스 튜닝값을 프리팹 자산으로 다시 저장하는 백엔드 유틸이다.
- 런타임 코드 폴더 안에 있지만 사실상 에디터 저장 흐름용이다.

### 현재 구조의 의도

- 공통 인터페이스는 아주 얇게 유지하고,
- 카메라/배경/폴백 비주얼/자동 저장 같은 주변 도구는 별도 파일로 떨어뜨리려는 의도다.

### 겹치거나 애매한 부분

- `PrefabAutoSaveUtility`는 위치만 런타임 폴더에 있고 실제 성격은 에디터에 가깝다.
- `RuntimeSpriteUtility`는 편하지만, 런타임 폴백 비주얼이 늘어나면 임시 자산과 정식 자산 경계가 흐려질 수 있다.

### 정리 후보

- `PrefabAutoSaveUtility`는 장기적으로 에디터 전용 계층으로 더 명확히 분리 검토
- `IDamageReceiver`를 중심으로 데미지 전달 공통 규칙을 더 모을 여지가 있다

### 보류할 부분

- 카메라와 패럴랙스는 현재 역할이 명확해서 당장 건드릴 이유가 크지 않다.

---

## 지금 보이는 중복 / 정리 후보 요약

### 1. 입력 설정 UI 책임이 두 군데에 나뉨

- `GameInputSettingsPanel`은 동작을 아는데
- `UIManager`가 행 정의와 씬 계층 조립을 같이 쥔다.
- 결과적으로 입력 패널의 진짜 주인이 애매하다.

### 2. 플레이어/보스 상호작용 계층이 거의 대칭 구조

- `PlayerInteraction`
- `BossInteraction`
- 둘 다 체력, 무적, 플래시, 사망, 리스폰, 이벤트를 가진다.
- 공통 베이스 후보가 보인다.

### 3. 플레이어/보스 투사체 구조가 닮아 있음

- `SimplePlayerProjectile`
- `BossProjectile`
- 이동, 수명, 충돌, 데미지 전달이 거의 같은 계열이다.

### 4. 데미지 전달 규칙이 판정체마다 흩어짐

- `SlashHitbox`
- `SimplePlayerProjectile`
- `BlindHuntressPrototypeCombat`
- `BossController` 일부 직접 타격
- 공통 규칙은 `IDamageReceiver`인데, 대상 찾기와 자기 자신 예외 처리 로직은 여러 군데 흩어져 있다.

### 5. 정식 구조와 프로토타입 구조가 동시에 존재

- 정식:
  - `SimplePlayerCombat`
  - `SwordWeapon`
  - `GunWeapon`
  - `PlayerAnimationDriver`
- 프로토타입:
  - `BlindHuntressPrototypeCombat`
  - `BlindHuntressPrototypeAnimationDriver`
- 둘 다 오래 유지하면 중복 판단이 흐려진다.

### 6. 큰 허브가 이미 보이기 시작한 곳

- `SimplePlayerController`
- `BossController`
- `PlayerRuntimeConfig`
- `UIManager`

이 파일들은 지금 당장은 필요해서 커졌지만, 계속 기능이 붙으면 가장 먼저 정리 비용이 커질 후보들이다.

---

## 지금 당장 쳐내기보다 먼저 기준을 잡아야 하는 것

- 플레이어 이동 손맛이 걸린 `SimplePlayerController`
- 보스 패턴 전체를 품은 `BossController`
- 실험용인지 정식 편입 대상인지 아직 확정되지 않은 `BlindHuntressPrototypeCombat`

이 셋은 "코드가 커서"가 아니라 "게임 감각이 직접 걸려 있어서" 섣불리 자르면 위험하다.

---

## 다음에 보기 좋은 순서

1. `Input + UI` 묶음
   - 책임 경계가 가장 흐리고, 비교적 안전하게 정리 후보를 고를 수 있다.
2. `Player Combat / Weapons`
   - 데미지 전달 중복과 무기 구조 경계를 보기 좋다.
3. `PlayerInteraction vs BossInteraction`
   - 공통 베이스 후보를 판단하기 좋다.
4. `Prototype vs 정식 전투 구조`
   - 무엇을 남기고 무엇을 버릴지 결정하기 좋다.
5. 마지막에 `SimplePlayerController`, `BossController`
   - 핵심 감각 코드라 가장 늦게 들어가는 편이 안전하다.

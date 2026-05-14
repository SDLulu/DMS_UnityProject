# 현재 게임씬 튜토리얼 구성 가이드

현재 씬을 그대로 게임씬으로 보고, 이미 만들어둔 맵 위에 튜토리얼 진행을 얹는다.
장소 이동은 씬 전환이 아니라 **플레이어 위치 이동**으로 처리한다.

---

## 핵심 방향

- 새 씬을 여러 개 만들지 않는다.
- 현재 씬 안의 기존 장소들을 그대로 쓴다.
- 장소 이동은 `Player`를 다음 `SpawnPoint`로 옮기는 방식으로 한다.
- 하이어라키는 "장소별 오브젝트"와 "진행용 트리거"가 섞이지 않게만 정리한다.

---

## 추천 하이어라키

```text
Scene
├── Runtime
│   ├── MainCamera
│   ├── Player
│   ├── UI
│   └── Managers
├── Map
│   ├── AccessZone
│   ├── PlayerRoom
│   ├── Plaza
│   └── Alley
├── TutorialFlow
│   ├── SpawnPoints
│   ├── Triggers
│   ├── Interactables
│   └── Gates
└── Debug
```

### `Map`

이미 만들어둔 맵 오브젝트를 장소별로만 묶는다.

예시:

```text
Map
├── AccessZone      # 접속 구역 / 튜토리얼 방
├── PlayerRoom      # 현실 복귀 후 방
├── Plaza           # 광장
└── Alley           # 브로커 골목
```

### `TutorialFlow`

튜토리얼 진행에 필요한 것만 둔다.

```text
TutorialFlow
├── SpawnPoints
│   ├── Spawn_AccessStart
│   ├── Spawn_PlayerRoom
│   ├── Spawn_Plaza
│   └── Spawn_Alley
├── Triggers
│   ├── Trigger_MoveGuide
│   ├── Trigger_JumpDashGuide
│   ├── Trigger_FirstEnemy
│   ├── Trigger_HomeCore
│   └── Trigger_AccessDenied
├── Interactables
│   ├── HOME_Core
│   ├── Terminal_Debt
│   ├── Door_ToPlaza
│   ├── Door_ToAlley
│   └── Broker
└── Gates
    ├── Gate_Room03
    └── Gate_Room04
```

---

## 시나리오 진행 순서

첫 구현은 아래 흐름만 되면 된다.

```text
1. AccessZone 시작
2. 이동 안내
3. 점프 / 대시 안내
4. 첫 적 처치
5. HOME_Core 상호작용
6. 강제 복귀 텍스트
7. PlayerRoom 위치로 이동
8. Terminal_Debt 확인
9. Door_ToPlaza로 Plaza 이동
10. CityGate 접근 거부
11. Door_ToAlley로 Alley 이동
12. Broker 대화
13. ChipDevice 조사 후 튜토리얼 종료
```

---

## 장소 이동 처리

문, 코어, 트리거가 플레이어를 다음 스폰 포인트로 옮긴다.

처리 순서:

```text
1. 입력 잠깐 끄기
2. 필요하면 화면 암전
3. Player 위치 = target SpawnPoint 위치
4. Rigidbody2D 속도 = 0
5. 카메라 위치 즉시 보정
6. 목표 문구 갱신
7. 입력 다시 켜기
```

처음에는 암전 없이 바로 이동해도 된다.
단, 위치 이동 후 속도 초기화는 꼭 한다.

---

## 튜토리얼에서 꼭 필요한 오브젝트

### AccessZone

- `Spawn_AccessStart`
- `Trigger_MoveGuide`
- `Trigger_JumpDashGuide`
- `Enemy_First`
- `HOME_Core`

### PlayerRoom

- `Spawn_PlayerRoom`
- `Terminal_Debt`
- `Door_ToPlaza`

### Plaza

- `Spawn_Plaza`
- `Trigger_AccessDenied`
- `Door_ToAlley`

### Alley

- `Spawn_Alley`
- `Broker`
- `ChipDevice`

---

## 지금 만들 때 우선순위

1. 스폰 포인트부터 찍는다.
2. 플레이어 위치 이동이 되는지 확인한다.
3. HOME 코어, 단말기, 문, 브로커 상호작용을 연결한다.
4. 튜토리얼 안내 문구를 붙인다.
5. 적/문 잠금/방 클리어를 붙인다.
6. 마지막에 글리치, 암전, 사운드, 카메라 흔들림을 넣는다.

---

## 작업 기준

- 맵 오브젝트는 `Map` 아래에 둔다.
- 진행용 트리거와 상호작용은 `TutorialFlow` 아래에 둔다.
- 장소 이동용 빈 오브젝트는 전부 `Spawn_*` 이름을 쓴다.
- 문은 `Door_To장소이름`으로 쓴다.
- 임시 오브젝트도 나중에 찾을 수 있게 이름을 붙인다.


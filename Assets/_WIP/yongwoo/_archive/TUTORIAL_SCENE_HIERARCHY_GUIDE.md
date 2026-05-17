# 튜토리얼 씬 구성 가이드

하나의 씬(`Yongwoo_Stage`)에서 플레이어를 스폰 포인트로 이동시켜 장소를 전환한다.

---

## 현재 씬 하이어라키

```
Yongwoo_Stage
├── Main Camera
├── Player
├── UI (HUD, DialogueUI, EventSystem)
├── 튜토리얼씬 (Background, Grid, 적, 테스트프롭)
├── 주인공집씬 (집밖배경, 집 그리드, 집물건)
├── 광장씬 (광장배경, 광장그리드, 광장물건)
├── 마켓씬 [비활성] — 골목길 용도 예정
├── 물건모음씬
├── 튜토리얼진행
│   ├── 시퀀스
│   ├── 트리거
│   ├── 스폰포인트 (스폰_접속구역, 스폰_주인공집, 스폰_광장, 스폰_골목길)
│   ├── 상호작용
│   └── 게이트
├── SystemLogUI [비활성]
├── CommsUI
└── ScreenFadeUI
```

---

## 이름 규칙

오브젝트 이름은 한글 기본.

| 종류 | 접두어 | 예시 |
|---|---|---|
| 스폰 포인트 | `스폰_` | 스폰_접속구역, 스폰_주인공집 |
| 트리거 | `트리거_` | 트리거_이동안내, 트리거_첫적 |
| 게이트 | `게이트_` | 게이트_3방, 게이트_4방 |
| 상호작용 | 역할 그대로 | HOME코어, 단말기, 문_광장으로, 브로커 |
| 시퀀스 | `시퀀스_` | 시퀀스_시작연출, 시퀀스_접속실패 |

---

## 기즈모

`튜토리얼진행` 하위 오브젝트에 `TutorialMarker`를 붙이면 씬 뷰에서 보인다.

| MarkerType | 색상 | 대상 |
|---|---|---|
| SpawnPoint | 시안 | 스폰 포인트 |
| Trigger | 노랑 | 트리거 |
| Gate | 빨강 | 게이트 |
| Interactable | 초록 | 상호작용 오브젝트 |

---

## 진행 체크리스트

### Phase A: 시퀀서 확장 (코드)

- [x] WaitForInput 스텝 (입력 대기: Move, Jump, Dash, Attack, Interact, AnyKey)
- [x] TeleportPlayer 스텝 (플레이어를 스폰 포인트로 이동)
- [x] SnapCamera 스텝 (카메라 즉시 이동) + `SimpleCameraFollow.SnapToTarget()`
- [x] WaitForEnemiesDead 스텝 (대상 전멸 대기)
- [x] FadeOut / FadeIn 스텝 + `ScreenFade.cs` 생성
- [x] PlaySequence 스텝 (다른 시퀀스 체이닝)

### Phase B: 독립 오브젝트 (코드)

- [x] `TutorialGate.cs` — 적 전멸 시 문 열림
- [x] `Interactable.cs` — E키 상호작용 + 프롬프트
- [x] `TutorialMarker.cs` — 씬 뷰 기즈모 표시

### Phase C: 에디터 세팅 (씬)

- [x] `튜토리얼진행` 하이어라키 구성 (시퀀스, 트리거, 스폰포인트, 상호작용, 게이트)
- [x] 스폰 포인트 4개 배치 + TutorialMarker 부착
- [x] ScreenFadeUI 배치 (Canvas sortOrder=9999, 검은 Image, CanvasGroup alpha=0)
- [ ] 스폰 포인트 위치 직접 조정 ← **수동 작업 필요**

### Phase D: 접속구역 콘텐츠 (프롤로그 튜토리얼 7방)

- [x] Room 1: 시작 연출 + 이동 튜토리얼 (시퀀스_시작연출)
- [x] Room 2: 점프/대시 튜토리얼 (시퀀스_점프대시, 트리거_점프대시안내)
- [x] Room 3: 첫 적 (시퀀스_첫적, 트리거_첫적, 게이트_3방)
- [x] Room 4: 연속 전투 (시퀀스_연속전투, 트리거_연속전투, 게이트_4방)
- [x] Room 5: 원거리 적 / 회피 (시퀀스_원거리적, 트리거_원거리적)
- [x] Room 6: HOME코어 상호작용 (시퀀스_HOME코어, HOME코어)
- [x] Room 7: 접속 실패 → 주인공집 텔레포트 (시퀀스_접속실패, 트리거_접속실패)

### Phase E: 나머지 구역 콘텐츠

- [x] 주인공집: 복귀 연출 (트리거_집복귀연출) + 단말기 + 문_광장으로
- [x] 광장: 접근 거부 (트리거_접근거부) + 문_골목길로
- [x] 골목길: 브로커 대화 (트리거_브로커대화) + 칩장치

### 수동 작업 필요

- [ ] 트리거/상호작용/게이트: 씬 뷰에서 각 방에 맞게 위치 조정
- [ ] 게이트_3방, 게이트_4방: 인스펙터에서 `enemies` 배열에 적 오브젝트 연결
- [ ] HOME코어, 단말기, 문_광장으로, 문_골목길로, 칩장치: 해당 맵 영역 내 배치

---

## 장소 이동 시퀀스 패턴

```
LockPlayer → FadeOut → TeleportPlayer(스폰포인트) → SnapCamera → FadeIn → UnlockPlayer
```

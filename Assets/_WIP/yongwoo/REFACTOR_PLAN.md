# 구조 정리 계획

## 현재 문제

BossEncounterDirector가 사실상 만능 객체로, 본래 각자가 해야 할 일을 전부 떠안고 있다.

| BossEncounterDirector가 하는 일 | 진짜 담당해야 할 곳 |
|---|---|
| 보스 스폰/디스폰 | 보스 프리팹 자체 or 스폰 트리거 |
| 보스 상태 (전투/사망) | 보스가 알아서 |
| 플레이어 사망 → 리셋 | GameManager |
| 플레이어 조작 잠금/해제 | GameManager |
| 컷씬 연출 (카메라, Timeline) | 씬별 컷씬 연출 (Timeline 자체 or 컷씬 전용 시스템) |
| 대사 표시 | DialogueManager |
| HUD 연동 | HUD가 이벤트 구독해서 알아서 |

대화 데이터 모델도 중복(`EncounterDialogueLine` ↔ `DialogueLineData`)이고, 패널을 여러 곳에서 직접 참조한다.

## 목표 구조

```
보스 → 자기 상태는 자기가 관리 (전투, 사망 등)
GameManager → 플레이어 사망/리셋, 조작 잠금 등 게임 흐름
DialogueManager → 모든 대사 표시의 유일한 관문
  └── DialoguePanel → 순수 뷰
컷씬 → Timeline 자체로 해결 (보스 등장 씬에 별도 구성)
HUD → 이벤트 구독으로 자립
디버그 패널 → 보스 소환, 컷씬 연출 등 개별 기능 독립 테스트 가능
```

## 단계별 정리

### Phase 1: 대화 시스템 정리

대화 시스템은 다른 것과 독립적이라 먼저 떼어내기 쉽다.

- 데이터 모델 통합: `EncounterDialogueLine` 제거 → `DialogueLineData` 하나로
- Panel 이름 정리: `EncounterDialoguePanel` → `DialoguePanel`
- Timeline 대화 트랙 바인딩을 `DialogueManager`로 변경
- BossEncounterDirector에서 `dialoguePanel` 직접 참조 제거
- 중복 유틸리티 정리 (`FindDescendantByName` 등 3중 복사 제거)

### Phase 2: 보스 자립

보스가 자기 상태를 스스로 관리하도록.

- 보스 스폰은 프리팹 or 트리거 기반 (BossEncounterDirector 불필요)
- 보스 전투 상태, 사망 처리는 보스 컴포넌트 내부에서 완결
- 컷씬용 보스 프리팹을 따로 두는 방안 검토

### Phase 3: GameManager 도입

게임 흐름 제어를 한 곳으로.

- 플레이어 사망 → 리셋/체크포인트
- 플레이어 조작 잠금/해제
- 게임 상태 전환 (탐색, 전투, 컷씬 등)

### Phase 4: 컷씬 구조

- Timeline 자체로 연출 완결 (카메라, 대사 클립 배치)
- 대화 클립 도달 시 Director.Pause → DialogueManager.Play → Resume
- 보스 등장 씬에 컷씬 전용 구성

### Phase 5: BossEncounterDirector 해체

Phase 1~4가 끝나면 BossEncounterDirector에 남는 일이 없다. 제거하거나 얇은 트리거로 축소.

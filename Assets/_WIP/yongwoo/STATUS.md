# 작업 상태

> 다음 세션 시작 시 가장 먼저 읽는다. 세션 끝에 갱신한다.

**마감:** 2026-05-20  **컨셉:** 슈퍼핫 시간 메카닉 + 카타나제로 2D 액션 (심상세계 SF)
**스코프 아웃:** 보스, 리플레이 시스템

---

## 입력 매핑

| 액션       | 키보드        | 게임패드      |
|------------|---------------|---------------|
| Move       | WASD / 방향키 | 왼스틱        |
| Jump       | Space         | A (South)     |
| Dash       | Left Shift    | 왼스틱 클릭   |
| Attack     | 마우스 좌클릭 | X (West)      |
| SlowMotion | 마우스 우클릭 | RT            |
| WeaponSwap | Q             | LB            |
| Interact   | E             | Y (North)     |

---

## 지금 어디까지 (현재 작업 위치)

- 2026-05-17 정적 점검 기준: `Yongwoo_Stage` 열림, 씬 dirty 없음, Unity 콘솔 error 없음
- 튜토리얼 시퀀스(Yongwoo_Stage) 구현 완료, 미세 조정/플레이 검증 단계
- 시작연출 → 접속대화 → 첫적/연속전투/원거리적 → HOME코어 → 주인공집/광장/골목길 → 칩접속 오브젝트 구성 확인
- 이번 점검에서는 플레이 모드 실행 안 함: 현재 대량 미커밋 변경이 있고, 규칙상 play 전 체크포인트 커밋 필요

## 시간 제어 3층 구조 (중요)

- **FreezeTime** — 전투 카메라 연출(Time.timeScale=0). 적 카메라 포커싱 중 사용
- **LockPlayer** — 컷씬 전환(컨트롤러 비활성). 검은 화면, 텔레포트 등
- **LockInput** — 대화 입력만 차단(컨트롤러는 살림). 공중낙하 중 대화 등

## 핵심 설계 결정 (왜 이렇게 했는지)

- `Interactable.interactOnce` 기본 `false` (대부분 반복형이 기본, 일회성만 명시)
- 일회성: HOME코어, 칩장치
- 채무자 047 회수실패 → `스폰_재접속_047` (-14, -30)으로 복귀
- 적 카메라 포커스: 이름 기반(Brawler/Swordsman/Gunner)으로 매칭
- 빌더 패턴: `TutorialContentBuilder`는 비파괴 갱신만 (수동 위치 유지)
- 한글 오브젝트 이름 기본
- 시스템로그 검은 배경은 `SystemLogPanel.useBackdrop`로 분리. 일반 칩 로그는 배경 없이 표시, 암전은 `ScreenFade` 담당
- 현재 진행 차단은 `TutorialGate` 클래스명을 유지하되 시나리오상 `ProgressBlocker` 역할로 취급

---

## 플레이어 기능 (락 — 더 추가 안 함)

현재 만들어진 기능까지로 플레이어 스코프 확정. 새 기능 X, 기존 정리만.

- [x] 시간 메카닉 (Time.timeScale 전환)
- [x] 슬로우 모션 토글 (우클릭)
- [x] Hand + 마우스 회전
- [x] 이동 / 점프
- [x] 마우스 방향 대쉬 + 대쉬 프리뷰 라인
- [ ] Shift 대쉬 → **질풍참 스타일**로 변경 (대쉬 + 공격 동시, 겐지 swift strike 느낌)
- [x] 기본 칼 공격 (좌클릭, `SwordWeapon` / `SlashHitbox`)
- [x] 사망 → 자동 리스폰 (`PlayerInteraction`, 현재 0.75초 딜레이)
- [ ] SimplePlayerCombat 정리 (기존 칼/총 스왑 구조 제거)

## 적

- [x] 적 HP / 피격 / 사망 공통 창구 (`EnemyInteraction`)
- [x] DeadRevolver 적 4종 프리팹: Brawler / Swordsman / Gunner / ShieldBearer
- [x] 근접 히트박스 / 원거리 투사체 판정
- [ ] 순찰형 적

## HP 시스템 (인스펙터 친화)

지금은 테스트용으로 하드코딩됨. 인스펙터에서 쉽게 바꿀 수 있게 정리한다.

- [x] 플레이어 HP — 인스펙터 노출, 기본 1 (일격사)
- [x] 적 HP — 인스펙터 노출, 종류별 기본값
- [x] HP 변경은 `IDamageReceiver.ReceiveHit` → `PlayerInteraction` / `EnemyInteraction`에서 처리
- [ ] `PlayerConfig` / 컴포넌트 직렬화 값 중복이 실제 튜닝 소스 하나로 유지되는지 플레이 검증

## 다이얼로그 / 트리거

- [x] DialogueTrigger / 시퀀스 시스템
- [ ] DialogueUI 개선 (일러스트, 이름, 표정)
- [ ] 카메라 연출 (줌인/줌아웃/흔들림)

## 스테이지 / 폴리시

- [ ] 시간 정지 비주얼 피드백 (화면 색조)
- [ ] 스테이지 전환 (씬 로드)
- [ ] 타이틀 화면
- [ ] 클리어 / 엔딩
- [ ] 히트 이펙트, 사운드 연동
- [ ] 전체 씬 흐름 연결 (Title → 거주구역 → 뒷골목 → Stage → Ending)
- [ ] 팀원 씬 머지 + 테스트

## 확정 미정 (다음에 결정)

플레이어/적 정리 끝난 뒤에 결정:
- 무기 시스템 (Q스왑 유지? 칼만? 무기 종류 몇 개?)
- 적 사망 시 드롭 여부
- 카타나제로식 클리어 리플레이 (스코프 아웃 가능성)

---

## 알려진 미해결

- 정적 점검상 `TutorialGate`와 `SceneEventSequence.WaitForEnemiesDead`는 `activeInHierarchy == false`를 사망 조건으로 본다. 현재 `EnemyInteraction` 사망은 오브젝트를 비활성화하지 않으므로 전투 후 진행 차단이 안 풀릴 위험 있음. 플레이 검증 또는 조건 수정 필요.
- `SimplePlayerCombat`에 칼/총 스왑 구조가 아직 남아 있음. 플레이어 스코프 확정 기준으로는 칼 중심 정리가 필요.
- PlayMode 검증 미실행. 실행 전 현재 미커밋 변경을 커밋하거나 별도 체크포인트가 필요.

---

## 세션 종료 시 갱신 규칙

이 파일을 다음 항목으로 덮어쓴다:
1. **지금 어디까지** — 다음 세션이 이어 작업할 위치
2. **핵심 설계 결정** — 이번 세션에서 새로 정한 것 추가
3. **남은 작업** 체크박스 — 완료한 거 체크
4. **알려진 미해결** — 다음 세션이 손대야 할 미해결 항목

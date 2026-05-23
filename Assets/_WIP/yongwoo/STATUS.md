# 작업 상태

> 다음 세션 시작 시 가장 먼저 읽는다. 세션 끝에 갱신한다.

**마감:** 2026-05-20  **컨셉:** 슈퍼핫 시간 메카닉 + 카타나제로 2D 액션 (심상세계 SF)
**스코프 아웃:** 리플레이 시스템

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
- 2026-05-18 빌드 오류 대응: SPUM 에디터 전용 `UnityEditor.U2D.Sprites` 참조가 Player 빌드에 섞이지 않도록 가드 추가
- 2026-05-20 Day1 버그 수정 완료: 슬로우 중 대시 속도 scaled time 적용, `DeathZone` 낙사 트리거 추가, `ScreenGlitchOverlay.ResetGlitch()` 경로 추가
- 튜토리얼 시퀀스(Yongwoo_Stage) 구현 완료, 미세 조정/플레이 검증 단계
- 시작연출 → 접속대화 → 첫적/연속전투/원거리적 → HOME코어 → 주인공집/광장/골목길 → 칩접속 오브젝트 구성 확인
- Day1 작업 전 체크포인트 커밋 생성: `1c656891c` (`chore: checkpoint before day1 fixes`)
- Day1 제한 검증 완료: `safe_refresh --compile true`, Unity 콘솔 error 없음, DeathZone 사망→부활 런타임 확인, ResetGlitch alpha=0 확인
- 2026-05-20 NPC 이미지 제작 준비: Codex skill `dms-npc-sprite-pipeline` 생성 완료. 상인/로봇/행인/브로커 인게임 스프라이트는 `Idle`/`Talk` 2상태, 브로커 통신 초상화는 별도 `Idle` 1장으로 진행
- 2026-05-20 NPC 이미지 v1 드래프트/필터/AI 노이즈 제거본 생성. 2026-05-21 적용본 정리 시 기존 `Assets/_WIP/yongwoo/Art/NPC/GeneratedDrafts`는 제거하고 필요한 원본만 새 런타임 폴더로 복사
- 2026-05-21 AI 픽셀아트 축소/정리 오픈소스 후보 재조사: 단순 스냅 계열보다 `ComfyUI-PixelArt-Detector`(MIT), `ComfyUI-AI-Pixel-Art-Enhancer`(Apache-2.0), `SD-piXL`(MIT), `stable-diffusion-webui-pixelization`/`comfy_pixelization` 계열이 더 유력. DMS NPC에는 ComfyUI-PixelArt-Detector를 1순위로 비교 테스트
- 2026-05-21 NPC 이미지 씬 적용: 기존 `Assets/_WIP/yongwoo/Art/NPC/GeneratedDrafts` 제거, 새 폴더 `Assets/_WIP/yongwoo/Art/NPC/Runtime_AINoiseRemoval_20260521` 생성. `Yongwoo_Stage`의 `노점상`/`게이트설명_AI`/`행인` 상호작용 오브젝트에 `Visual` 자식과 idle 스프라이트 연결, `CommsUI/CommsRoot/PortraitImage`에 브로커 초상화 연결
- 2026-05-21 NPC idle/talk 애니메이션 적용: `Runtime_AINoiseRemoval_20260521/Frames`, `Animations`, `Controllers` 생성. 상인/로봇/행인/브로커 4종 모두 `Idle`/`Talk` 클립과 `Talking` bool 컨트롤러 생성, `노점상`/`게이트설명_AI`/`행인`은 `Interactable.visualAnimator`에 연결해 상호작용 시 talk 전환
- 2026-05-21 NPC 대화 초상화 적용: `Sprites/merchant_portrait.png`, `robot_guide_portrait.png`, `passerby_portrait.png` 추가. `CommsPanel`은 `브로커`/`노점상`/`행인`/`안내 AI`/`경비 AI` speaker에 맞춰 초상화를 표시하고, `주인공` speaker는 초상화 없음. `SpeakerText`는 런타임 speaker 이름표로 표시
- 2026-05-21 CommsUI 기본 placeholder 텍스트 제거: `CommsRoot/Monitor/MonitorText`의 기본 `BROKERs` 제거, `SpeakerText`/`BodyText` 기본값도 빈 문자열 상태 확인
- 2026-05-21 브로커 트리거 시각 배치: `Yongwoo_Stage`의 `튜토리얼진행/트리거/트리거_브로커대화` 아래에 `브로커_스탠딩`(broker idle/talk controller)과 `접속코어`(`Chest_0` SpriteRenderer/Animator 복제) 자식 추가
- 2026-05-21 Day4까지 플레이 검증 완료. 다음 작업은 같은 `Yongwoo_Stage` 안에서 `스폰_재접속_047` 이후 붕괴된 접속구역 컨셉의 보스 스테이지/보스 제작
- 2026-05-21 DeathZone 분리: `DeathZone 튜토리얼`은 `스폰_접속구역`, `DeathZone 보스`는 `스폰_재접속_047`로 리스폰 위치를 갱신하도록 `DeathZone.respawnPoint` 연결
- 2026-05-21 플레이어 벽면 점프 반복 방지: `SimplePlayerController.IsGrounded()`에서 Ground 접촉점이 플레이어 콜라이더 중심보다 아래쪽이고 접촉 normal이 세로 방향일 때만 바닥으로 인정하도록 수정. PlayMode 검증은 아직 필요
- 2026-05-21 Player/Enemy 레이어 몸통 충돌 비활성화, 구르기 중 `PlayerInteraction.ReceiveHit()` 무시 처리 추가. DeadRevolver 근접 히트박스는 레이어 충돌 비활성화 후에도 작동하도록 overlap 직접 샘플링으로 보강
- 2026-05-20 일시정지 UI 기능 추가: `Yongwoo_Stage` HUD 우상단 `PauseButton`, `PauseMenuRoot`, `PauseMenuController` 연결
- `Yongwoo_Title` 목업 타이틀 씬 생성. 타이틀 `게임 시작` → `Yongwoo_Stage`, 일시정지 `타이틀로` → `Yongwoo_Title` 로드 구조
- 일시정지 기능은 정적 연결 확인과 Unity 콘솔 error 없음까지 확인. 기존 대량 미커밋 씬 변경 때문에 PlayMode 관통 검증은 보류
- 2026-05-20 외부 참고 레포 `octopus7/ChatGPT-Images-2` 분석만 수행. DMS 코드/씬/에셋 변경 없음
- 2026-05-20 Codex 전역 스킬 `ai-image-noise-removal` 설치. 호출명은 `AI이미지 노이즈제거`, 기본 `detail_level`은 1(Clean Simplified)
- 2026-05-21 unity-cli 버전 불일치 수정: 로컬 CLI `0.3.19`에 맞춰 embedded connector 패키지 `Packages/com.youngwoocho02.unity-cli-connector`를 `0.3.19`로 갱신. `unity-cli status/list` 정상 확인
- 2026-05-22 골목길 연출 1차 구현: `트리거_브로커대화` 진입 시 브로커 대면 대화 12스텝 실행, `칩장치` 상호작용 시 재접속 연출 16스텝 실행. 칩접속은 FadeOut → 시스템로그 → `스폰_재접속_047` 텔레포트/SnapCamera → FadeIn → 브로커 대사 → UnlockPlayer 흐름
- 2026-05-22 Git 정리: 현재 `yongwoo` 작업분을 커밋하고 `origin/yongwoo`와 `main`에 반영하는 단계
- 2026-05-23 **보스 스코프 아웃 철회**. 마감 이후 추가 작업으로 최종 보스 1개 진행. 디자인 확정안은 `보스시나리오.md` 섹션 3 참고
- 2026-05-23 보스 패턴 7개 디테일 1차 시안 작성 (`보스시나리오.md` 섹션 3 하단). P1 단발/연사/확산, P2 분열체 A(근접)·B(원거리) 비대칭. 슬로우 자원 = `Hp UI` 5칸 재활용. 다음 작업: 1차 시안 검토 → 슬로우 자원 시스템 구현 → 보스 프리팹/AI 구현
- 2026-05-23 슬로우 자원 시스템 구현 완료: `PlayerSlowMotion`에 5칸 자원 + 1칸=1초 소비 + 1칸/4초 회복. `SlowGaugeUI` 컴포넌트 신규 — `Hp UI` 5칸 Image 동기화. Unity 측 와이어링은 사용자 수동 작업 필요(SlowGaugeUI 추가 + Image 5개 배열 할당)
- 2026-05-23 칼 사거리 실측: `SlashHitbox` Polygon 0.432×0.237 × Visual scale 3 ≈ 1.30u 폭. 캐릭터 중심에서 ~1.5u 도달. 시안 가정 1.0~1.5u와 일치, 패턴 거리값 보정 불필요
- 2026-05-23 보스 프리팹 골격 코드 작성: `Scripts/Prefabs/Boss/`에 `BossInteraction`(HP=5, 텔포 무적 플래그, 경직·넉백 없음), `BossTeleporter`(선딜→사라짐→0.4s 무적→출현), `IBossPattern`(인터페이스+Context), `BossPatternRunner`(슬롯 3개 순환+텔포 사이클) 4종. 다음 작업: P1 패턴 컴포넌트 3종(단발/연사/확산) 구현 + 발사체 프리팹 + 보스 프리팹 조립

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
- NPC 아트 계약: 인게임 NPC는 2행 스프라이트시트(`idle`, `talk`)를 기본으로 하고, 브로커 CommsPanel용 headshot은 인게임 시트와 분리한다
- NPC 원본 AI 이미지는 바로 64px 셀로 축소하지 않고, 먼저 AI 노이즈 제거 원본해상도본을 픽셀 그리드 스냅/팔레트 축소 툴로 정리한 뒤 최종 스프라이트 크기를 결정한다
- 2026-05-21 현재 적용본은 원본해상도 AI 노이즈 제거 이미지에서 투명 PNG 프레임/초상화를 추출해 사용한다. `CommsPanel`은 speaker가 `브로커`/`Broker`일 때 `PortraitImage`를 자동 표시한다
- 2026-05-21 NPC talk 전환은 새 시스템을 만들지 않고 `Interactable`의 선택 필드(`visualAnimator`, `talkingParameter`)로 처리한다. 연결된 상호작용 시퀀스가 `IsPlaying`인 동안 `Talking=true`, 종료 시 `false`
- 2026-05-21 Comms UI 초상화 매핑은 `CommsPanel.ResolvePortrait()`의 speaker 문자열 포함 규칙으로 처리한다. 기본 placeholder 텍스트만 제거하고, 이름표와 본문 대사 텍스트는 유지
- 플레이어 바닥 판정은 `Rigidbody2D.GetContacts`의 접촉점과 normal을 기준으로 한다. 타일맵 옆면 접촉은 점프 리셋 조건에서 제외한다
- 플레이어와 적의 몸통 물리 충돌은 `Physics2D.IgnoreLayerCollision(Player, Enemy, true)`로 끈다. 피격은 몸통 충돌이 아니라 적 공격 판정이 `PlayerInteraction.ReceiveHit()`를 호출하는 구조로 유지한다
- 구르기 무적은 별도 타이머를 만들지 않고 `SimplePlayerController.IsRolling` 동안 `PlayerInteraction.ReceiveHit()`가 false를 반환하는 방식으로 처리한다
- 일시정지는 `PauseMenuController`가 `Time.timeScale=0`과 UI 표시를 소유. 재개 시 기존 timeScale/fixedDeltaTime과 gameplay input enabled 상태를 복원
- `PlayerSlowMotion`은 일시정지 중 timeScale을 다시 올리지 않도록 `PauseMenuController.IsPaused`를 먼저 확인

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
- [x] 타이틀 화면 (목업 `Yongwoo_Title`)
- [ ] 클리어 / 엔딩
- [ ] 보스 스테이지 + 보스 *(디자인 확정 — `보스시나리오.md` 섹션 3. 골격 코드 작성됨(BossInteraction/Teleporter/PatternRunner/IBossPattern). 패턴 구현체·프리팹 조립 남음)*
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
- 전체 튜토리얼 PlayMode 관통 검증은 아직 필요. Day1은 짧은 런타임 확인만 완료.
- 일시정지/타이틀 로드 PlayMode 검증은 아직 필요. 현재 `Yongwoo_Stage.unity`에 기존 미커밋 변경이 섞여 있어 규칙상 play 전 별도 체크포인트 정리가 필요.
- SPUM 빌드 오류 수정 후 Player build 재검증은 아직 미실행. Unity refresh/compile은 Day1에서 에러 없이 통과.

---

## 세션 종료 시 갱신 규칙

이 파일을 다음 항목으로 덮어쓴다:
1. **지금 어디까지** — 다음 세션이 이어 작업할 위치
2. **핵심 설계 결정** — 이번 세션에서 새로 정한 것 추가
3. **남은 작업** 체크박스 — 완료한 거 체크
4. **알려진 미해결** — 다음 세션이 손대야 할 미해결 항목

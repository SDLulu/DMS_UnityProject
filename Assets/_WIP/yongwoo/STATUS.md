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

- 2026-05-23 `Yongwoo_Title` 씬 bake 방식: Play 시 자동 세팅 제거. `TitleUI`/`ScanLine` 컴포넌트·값은 씬에 저장, 에디터 인스펙터에서 조절. 재 bake: `DMS → Yongwoo → Setup Title UI Effects`
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
- 2026-05-23 보스전 게임플레이 로직을 `보스전로직.md`로 분리. `보스시나리오.md`는 스토리/대사/톤 전용. 패턴 표, 행동 룰, 확정 가정, 시퀀스 step, 구현 매핑 표는 모두 `보스전로직.md`에 있고 다음 구현은 이 문서를 기준으로 진행한다
- 2026-05-23 P1 패턴 컴포넌트 3종 + 발사체 구현: `BossProjectile`(timeScale 적용 슬로우 영향), `BossPatternBase`(텔레그래프→선딜→액티브→후딜 공통 4페이즈, 텔레그래프 종료 시점 조준 락), `BossPatternStraightShot`(P1-1 단발 탄속 14), `BossPatternVolley`(P1-2 연사 4발 탄속 12 간격 0.15s), `BossPatternSpread`(P1-3 확산 5way 탄속 10 인접 30°). 수치 디폴트는 `보스전로직.md` §2.2 표 그대로. 다음 작업: 보스 발사체 프리팹 + 보스 본체 프리팹 조립 + 텔레그래프 비주얼
- 2026-05-23 보스전 감각 확인용 HTML 프로토타입 추가: `Assets/_WIP/yongwoo/BossHtmlPrototype/index.html`. `보스전로직.md` 기준으로 P1 단발/연사/확산+텔포, 슬로우 자원, P2 분열체 A/B 미리보기를 캔버스로 구현. Unity 씬/프리팹 변경 없음
- 2026-05-23 보스전 설계 재정리: `보스전로직.md`를 P3 3분열 원형 → P2 2분열 축약 → P1 6패턴 통합형 순서로 개편. P3는 A(추격/근접), B(탄막/예측), C(공간장악/장판·벽) 3역할. `보스시나리오.md`도 3분열 전환/마지막 대사에 맞춰 갱신
- 2026-05-23 HTML 보스 프로토타입 P3-first 반영 완료: `BossHtmlPrototype/index.html` 기본 시작을 P3 3분열로 변경하고 P1/P2/P3 버튼, P1 6패턴 통합형, P2 2분열 축약형, P3 A/B/C 3분열 패턴을 구현. Playwright로 P1/P2/P3 캔버스 렌더와 `render_game_to_text` 상태 확인, 콘솔 에러 없음
- 2026-05-23 HTML 보스 프로토타입 라이브 튜닝 패널 추가: 사이드바에서 페이즈별 패턴을 선택하고 텔레그래프/선딜/후딜/탄속/발수/간격/각도/대시시간/대시속도/반경/장판지연 값을 즉시 조정 가능. `paramDump`와 `render_game_to_text.selectedParams`로 현재 값을 복사해 문서/Unity 이식에 반영할 수 있음. 대시 베기는 짧던 기본값을 P1 0.32s×14u/s, P2/P3 0.30s×18.5u/s로 상향
- 2026-05-23 HTML 보스 프로토타입 튜닝 반영: 대시 베기 속도 기본값을 P1/P2/P3 모두 100u/s로 상향하고 튜너 최대값을 140까지 확장. P3-C 레이저 벽은 `laserWidth` 파라미터를 추가해 텔레그래프/활성 폭을 1.52u(기존 0.38u의 약 4배)로 확대
- 2026-05-23 슬로우 게이지 UI 연동 + 엣지케이스 정리: `Hp UI` Animator의 "New Animation" looping 클립(t=0~1.38, bar_0~bar_11, 12프레임)을 `SlowGaugeUI`로 normalizedTime 직접 제어. wrap point 회피 위해 `fullNormalizedTime=0.92`, 코드에 `Clamp(0, 0.9999)` 안전마진. 시작 깜빡임 방지로 `Start()` 즉시 동기화. `PlayerSlowMotion`에 래치 + `minActivationCharges`(기본 1) 추가 — 0칸 도달 시 강제 해제, 재발동은 1칸 회복 후. 소비/회복 2배 가속(0.5s/2s)
- 2026-05-23 세계관 분리: `세계관.md` 신규 — 게임 정체성/핵심 용어/등장 인물/in-game 인터페이스 의미/가치 대비/톤. `시나리오.md`에서 컨셉·등장 인물 섹션 제거하고 포인터로 대체. 문서 인덱스(세계관/시나리오/보스시나리오/보스전로직/STATUS/AGENTS) 정리됨
- 2026-05-23 Unity 보스 P1 프로토타입 구현: `BossPatternDashSlash`(dashSpeed 100), `BossPatternDelayedBlast`/`BossBlastZone`, `BossPatternPredictShot` 추가. `BossPatternBase`는 파생 패턴 여러 개를 한 오브젝트에 붙일 수 있게 `[DisallowMultipleComponent]` 제거. `BossPatternRunner`는 player 미지정 시 `PlayerInteraction` 자동 탐색
- 2026-05-23 보스 P1 프리팹 조립 완료: `Tools/Yongwoo/Boss/Rebuild P1 Prototype Prefab` 빌더와 `Assets/_WIP/yongwoo/Prefabs/Boss/Boss_P1_Prototype.prefab`, `BossProjectile.prefab` 생성. 프리팹 6슬롯은 단발/연사/확산/대시베기/지연장판/예측탄 순서로 검증. `safe_refresh --compile true`, Unity 콘솔 error 없음. 아직 씬 배치/PlayMode 전투 검증은 미실행
- 2026-05-23 보스 이미지 초안 적용: 기존 NPC 적용본을 참고하되 재활용 실루엣이 보이지 않도록 어두운 데이터 코어/글리치 실루엣으로 재가공. `Art/Boss/Runtime_20260523/Sprites/`에 `boss_p1_idle`, `boss_clone_a_idle`, `boss_clone_b_idle`, `boss_clone_c_idle` 생성, QA 시트 `QA/boss_runtime_20260523_overview.png` 생성. 네 장 모두 Sprite 320x420, PPU 320, Point filter로 import. `Boss_P1_Prototype` Visual은 `boss_p1_idle` 참조
- 2026-05-23 보스 P1 씬 배치: `Yongwoo_Stage` `보스씬/보스/Boss_P1_Prototype` @ (58, -24.8). 디버그 텔포 키 4 → `스폰_보스방` (-12, -23.99) 추가. 키 3은 기존 `스폰_재접속_047` 유지
- 2026-05-23 보스 아레나 고정: `BossBattleArena` + `트리거_보스전입장`. 입장 시 Cuphead식 고정 카메라, **카메라 화면 크기**만큼 플레이어 클램프, 보스 텔포 5앵커 연결. `플레이어범위` 박스 제거
- 2026-05-23 보스 아레나 씬 기즈모: `TutorialMarker`에 `BossCameraAnchor`/`BossTeleportAnchor` 타입 추가, `TutorialGizmoDraw` 공통화, `BossBattleArena` 아레나 와이어 박스. `Tools/Yongwoo/Boss/Ensure Arena Tutorial Markers`로 `카메라앵커`/Anchor_01~05/입장 트리거 마커 연결. 규칙은 `AGENTS.md` §씬 배치 기즈모

- 2026-05-23 P1 텔포 앵커 배치: `보스전로직.md` §4.3 (4모서리+중앙 위) 기준 `Place P1 Teleport Anchors` 메뉴로 `Yongwoo_Stage` 적용·저장
- 2026-05-23 보스 조준감 수정: `BossPatternBase`가 텔레그래프 중 플레이어 방향으로 계속 회전하고, 단발/연사/확산/예측탄은 보스 중심이 아니라 `Muzzle` 기준으로 조준. `BossPatternRunner`는 P1 슬롯 누락 시 6패턴을 자동 보강
- 2026-05-23 P2/P3 Unity 전환 구현: `BossPhaseController`가 P1 사망→P2 2분열(각 4HP)→P3 3분열(각 3HP)을 처리. A/B/C 분열체 스프라이트 연결, 역할별 패턴 슬롯 지정, 대시/텔포 아레나 클램프 적용
- 2026-05-23 P2/P3 전용 패턴 추가: `BossPatternTeleportSlam`, `BossPatternLaserWall`/`BossLaserWallZone`, `BossPatternSafeZoneCollapse`/`BossSafeZoneCollapse`. PlayMode 강제 전환 스모크에서 P1→P2 2체→P3 3체 생성, Unity console error 없음
- 2026-05-23 보스전 가시성 1차: 보스 `Visual` 어두운 틴트(`BossBodyVisual.DarkTint`), P3 안전지대는 안전 원 대신 **아레나 나머지 빨간 위험**(`SpriteMask`+`VisibleOutsideMask`), 보스 탄 `defaultRadius` 0.03(기존 1/4)+`TrailRenderer`+Unlit 트레일. `BossProjectile.prefab`/`Boss_P1_Prototype` 반영, compile error 없음
- 2026-05-24 대시/레이저벽 텔레그래프 개선: 대시는 시작 시 조준 고정 + 선딜까지 붉은선 유지 후 4.5u/0.22s 돌진. 레이저벽은 텔레그래프+선딜(0.85s) 동안 미리보기 벽 표시 후 판정 `RuntimeSpriteUtility.WorldSizeToLocalScale`/`UniformWorldScale`로 코드 스프라이트 월드 크기 보정. 대시 텔레그래프·레이저벽·안전지대 위험 오버레이가 의도 크기로 표시됨. `BossVfxUtility`로 텔포 링/대시 잔상/모션 스트라이프 추가. `BossTeleporter`/`BossPatternDashSlash`/`BossPatternTeleportSlam` 가시성 강화
- 2026-05-24 `yongwoo` 커밋·푸시: `3b9101029` — P2/P3 전환·아레나·패턴 확장 + 타이틀 UI 연출
- 2026-05-24 보스 이펙트 업그레이드: HTML 프로토타입 감각에 맞춰 텔레그래프 펄스, 대시 잔상, 장판 PulseRing/HotCore, 레이저 HotCore, 안전지대 SafeRing을 런타임 이펙트로 추가. 보스 PNG 색을 살리기 위해 `BossBodyVisual.DarkTint`는 흰색 틴트로 변경
- 2026-05-24 보스 최소 idle 애니메이션 시트 생성: hatch-pet의 고정 셀/투명 atlas/QA 방식을 참고해 `Art/Boss/Runtime_20260523/AnimationSheets/boss_minimal_idle_4x4.png` 생성. 규격은 320x420 셀, 4프레임, 4행(P1/A/B/C), 전체 1280x1680. 역할별 4프레임 strip과 preview GIF, contact QA는 같은 폴더 하위에 있음
- 2026-05-24 보스 Hybrid 비주얼 1차: `BossPhaseController`가 idle frame sprite 배열(에디터 자동 로드/빌더 직렬화) + 런타임 자식 레이어(`Hybrid_Core`, `Hybrid_Halo`, `Hybrid_VerticalLine`, `Hybrid_GlitchBar_*`)를 생성해 몸통 bob/scale pulse, 코어 점멸, 링 흔들림, 글리치 바 점멸을 처리. 빌더도 P2/P3 패턴과 idle frame 배열을 다시 연결하도록 보강
- 2026-05-24 보스 피드백 레이어 추가: `BossPhaseController`가 `BossInteraction.Damaged/Died`를 받아 피격 코어 버스트(`Boss_ImpactSpark`), 카메라 shake, 분열/처치 버스트를 생성. `BossTeleporter`는 텔포 숨김 시점에 현재 자식 `SpriteRenderer`를 다시 수집해 Hybrid 레이어까지 함께 숨기도록 보강. 현재 `Yongwoo_Stage` dirty 때문에 refresh/compile 검증은 보류
- 2026-05-24 보스 투사체 Hybrid 강화: `BossProjectile`에 HotCore/Ring/Streak 자식 레이어, 네온 3색 trail gradient, 발사/충돌 spark burst, 자식 레이어 pulse를 추가. 루트 스케일은 흔들지 않아 판정 크기 변동을 피함. `Yongwoo_Stage` dirty 때문에 refresh/compile/PlayMode 검증은 아직 보류
- 2026-05-24 보스 아레나 Hybrid 프레임 추가: `BossBattleArena`가 전투 입장 시 `Boss_ArenaFX` 런타임 자식을 만들고 화면 경계선/코너/스캔라인을 pulse 처리. 씬 YAML을 직접 수정하지 않고 코드 생성 방식으로 적용. `Yongwoo_Stage` dirty 때문에 refresh/compile/PlayMode 검증은 아직 보류
- 2026-05-24 보스 반응성 레이어 추가: `BossEffectFade`를 확장/축소 fade 공용으로 보강하고, 보스 피격·분열·처치와 투사체 충돌에 `RingSprite` shockwave를 추가. 보스방 입장/P1→P2/P2→P3/P3 완료 시 기존 `ScreenGlitchOverlay.Pulse`를 짧게 호출해 화면 반응도 연결. `Yongwoo_Stage` dirty 때문에 refresh/compile/PlayMode 검증은 아직 보류
- 2026-05-24 보스 스토리 연출 훅 설치 완료: 영상 파일은 사용자가 제작하는 전제로 `CutsceneVideoPanel` + `SceneEventSequence.PlayCutsceneVideo` step 추가. `BossBattleEntryTrigger.beforeBattleSequence`로 보스 등장 대사를 전투 시작 전에 재생 가능, `BossPhaseController`에 P1→P2/P2→P3/최종 처치 시퀀스 슬롯 추가. 페이즈 전환 전체 동안 플레이어 조작/시간이 튀지 않도록 `BossPhaseController`에서 전역 조작 잠금 + 전환 freeze 보강. 메뉴가 없어도 시퀀스 참조가 비어 있으면 `BossStoryRuntimeSequenceFactory`가 런타임 기본 시퀀스를 생성. `StoryMemoryVisual` 추가로 기억조각/HOME 코어가 별도 아트 없이도 pulse/ring/glitch bar로 보이게 함. `Tools/Yongwoo/Boss/Install Story Sequence Hooks` 실행 및 씬 저장 완료 — `보스연출` 루트, 기억조각 2개, `HOME코어_회수가능`, 보스 등장/전환/HOME 회수 시퀀스, 비디오 슬롯 생성/연결 확인. `unity-scanner read`로 씬 반영 확인, Unity 콘솔 error 없음, PlayMode 검증은 체크포인트 전이라 보류
- 2026-05-24 보스 투사체 피격 보강: `BossProjectile`이 `OnTriggerEnter2D`만 의존하던 구조에 `Update`/`FixedUpdate`의 `Physics2D.OverlapCircleAll` 플레이어 샘플링과 `OnTriggerStay2D`를 추가. 플레이어와 월드가 동시에 겹치면 플레이어 피격을 먼저 시도하고, `ReceiveHit()`가 실제 성공했을 때만 탄을 소모. 빠른 키네마틱 트리거 탄환이 플레이어와 겹쳤는데 트리거 이벤트를 놓치는 경우에도 `PlayerInteraction.ReceiveHit()` 경로를 탄다. PlayMode에서 `BossProjectile` overlap 스캔을 플레이어 콜라이더 중심에 대해 강제 호출해 `ReceiveHit=true`, 체력 1→0, `IsDead=true` 확인. 자동 프레임 진행 기반 관통 검증은 추가 필요
- 2026-05-24 AI 영상 프롬프트팩 작성: `AI영상_프롬프트팩.md`에 인트로/기억 조각 1/기억 조각 2/엔딩 4개 복붙용 한글 프롬프트 정리. 방향은 대사·자막 없는 담백한 픽셀아트풍 컷신, 레퍼런스는 인트로 Image #1, 기억 조각 Image #2, 엔딩 Image #1+#2 기준. 스타일 일관성 흔들림을 줄이기 위해 4개 프롬프트 모두에 같은 `공통 고정 스타일` 블록과 검수 기준을 반복 삽입
- 2026-05-24 SFX 1차 레이어 구현: `YongwooAudioManager` 신규 런타임 매니저가 `Resources/Yongwoo/SFX` 임시 WAV 41종을 로드해 재생. 시스템로그/브로커통신은 기존 `SceneEventSequence.duration` 안에서 타이핑 출력 + 타이핑 tick SFX. 플레이어 점프/대시/롤/공격/무기전환/피격/사망/리스폰, 적 피격/사망, 상호작용/페이드/글리치/일시정지/타이틀, 보스 아레나/텔레그래프/발사/텔포/장판/투사체/피격/페이즈 전환에 1차 SFX 훅 연결. `ReadLints` 기준 수정 파일 linter error 없음. Unity refresh/PlayMode 청감 검증과 최종 사운드 교체는 아직 필요
- 2026-05-25 컷신 영상 자동 연결: 타이틀 `title` 영상은 `Yongwoo_Title`에서 배경 루프 재생, `intro` 영상은 `게임 시작` 클릭 후 `Yongwoo_Stage` 로드 전에 재생, 이후 스테이지 시작 시스템로그가 이어지도록 `TitleSceneController`를 보강. `SceneEventSequence.PlayCutsceneVideo`는 인스펙터 VideoClip이 비어 있으면 `Resources/Yongwoo/Videos`에서 시퀀스 이름 기준으로 자동 탐색한다. 파일명 규칙: `title`, `intro`, `memory_01`, `memory_02`, `boss_defeat` (각각 mp4/mov 등 Unity VideoClip import). 기억조각 1/2와 보스 처치 후 영상은 기존 `시퀀스_기억조각_01`, `시퀀스_기억조각_02`, `시퀀스_보스_처치후_HOME회수`의 `PlayCutsceneVideo` step에서 재생된다. `dotnet build Assembly-CSharp.csproj --no-restore` 통과, 현재 `Yongwoo_Stage` dirty 때문에 Unity refresh/PlayMode 검증은 보류
- 2026-05-25 `Resources/Yongwoo/Videos/README.md` 추가: 비디오 폴더 안에서 바로 볼 수 있는 파일명 규칙/수동 연결/확인 순서 문서. 현재 들어온 생성 시각 파일명 mp4들은 내용 확인 후 `title`, `intro`, `memory_01`, `memory_02`, `boss_defeat`로 이름을 맞춰야 자동 연결됨
- 2026-05-25 영상 소리/크기 조절값 추가: `TitleSceneController`에서 타이틀 배경 영상 `Muted`/`Volume`/`LayoutMode(FitInside, FillScreen, Stretch)`/`Scale` 조절 가능. `CutsceneVideoPanel`에서 인트로·기억조각·보스처치 컷신 `Mute Audio`/`Video Volume`/`Video Layout Mode`/`Video Scale` 조절 가능. 비디오 폴더 `README.md`에도 조절 위치와 의미 기록
- 2026-05-25 영상 직접 크기 조절 보강: `YongwooVideoLayoutMode.ManualRect` 추가. 기본값을 타이틀/컷신 모두 `ManualRect`로 변경해 재생 시 RectTransform을 덮어쓰지 않음. 직접 조절 대상은 타이틀 `TitleUI/TitleVideoBackground`, 컷신 `UI/HUD/CutsceneVideoRoot/VideoImage`
- 2026-05-25 영상 볼륨/비율 보강: `VideoPlayer` 오디오를 `AudioSource` 출력으로 명시 연결하고 `controlledAudioTrackCount`/`EnableAudioTrack`/볼륨을 재생 전후 적용해 Inspector 볼륨값이 먹도록 수정. `ManualRect`에서도 `Preserve Aspect` 옵션을 켜면 클립 비율(또는 `Manual Aspect`, 기본 16:9)로 Rect 크기를 보정
- 2026-05-25 인게임 영상 볼륨 조절 보강: 기존 볼륨값은 재생 시작 시점에만 적용되어 Play 중 Inspector 변경이 바로 안 먹을 수 있었음. `TitleSceneController.Update()`와 `CutsceneVideoPanel.Update()`에서 재생 중 매 프레임 `AudioSource.mute/volume` 또는 direct audio 값을 다시 적용하도록 수정. `Muted`/`Mute Audio`가 켜져 있으면 볼륨을 올려도 소리 안 남
- 2026-05-25 Unity VideoPlayer 공식 문서 기준 볼륨 재수정: `AudioSource.volume` 경로가 영상 내장 오디오에 영향이 없을 수 있어, 영상 볼륨/뮤트는 `VideoAudioOutputMode.Direct` + `SetDirectAudioVolume`/`SetDirectAudioMute`로 모든 제어 트랙에 직접 적용. `AudioSource`는 호환용 직렬화 참조로 남기되 실제 영상 음량 기준은 `Title Video Volume` / `Video Volume`
- 2026-05-25 스테이지 컷씬 3개 볼륨 분리: `memory_01`, `memory_02`, `boss_defeat`는 모두 `Yongwoo_Stage`의 `HUD/CutsceneVideoPanel`을 공유하지만, 자동 연결 시퀀스 이름에서 `YongwooStoryVideoKey`를 판정해 `Memory 01 Volume`, `Memory 02 Volume`, `Boss Defeat Volume`을 각각 읽는다. 수동으로 VideoClip을 꽂은 컷씬은 기존 공통 `Video Volume`을 사용. `unity-scanner read`로 `HUD` 패널 직렬화 확인, Unity 콘솔 error 없음
- 2026-05-25 타이틀→인트로 영상 버그 수정: `게임 시작` 시 타이틀 배경 VideoPlayer/AudioSource와 준비 코루틴을 명시 중지해 인트로 영상 위로 타이틀 영상 소리가 겹치지 않게 함. 컷씬 스킵 입력은 `Space` 하나만 허용하고, 컷씬 재생 중에는 `PauseMenuController`가 `Esc`를 무시하도록 막아 일시정지 메뉴 중첩과 재개 후 플레이어 멈춤 위험을 줄임. 스킵 안내 문구는 `SPACE : SKIP`
- 2026-05-25 타이틀/인트로 영상 playlist 확장: `TitleSceneController`에 `Title Video Playlist` / `Intro Video Playlist` 배열을 추가. 배열이 비어 있으면 `Resources/Yongwoo/Videos/title_01`~`title_09`, `intro_01`~`intro_09`를 자동 탐색하고, 그것도 없으면 기존 `title` / `intro` 단일 파일로 fallback. 타이틀은 여러 클립을 순환 재생, 인트로는 여러 클립을 순서대로 한 번 재생 후 스테이지 로드. `Resources/Yongwoo/Videos/README.md`에 파일명 규칙과 비어 있을 때 처리 순서 기록
- 2026-05-25 타이틀 화면 상용게임풍 1차 품질업: `Yongwoo_Title`을 배경/분위기/프레임/정보/메뉴 레이어로 재배치. `TitleUiEffectSetup` bake 도구가 `Atmosphere`, 좌측 타이틀 패널, 우측 메뉴 패널, 상하 룰, 상태 라벨, ECG 라인을 생성·정렬한다. `TitleUiMotion`은 `DEEP DIVE: HOME`, `START`/`QUIT`, 보조 라벨의 폰트·색·호버/idle 모션을 관리. `ScreenCapture` 기준 런타임 표시 확인, Unity 콘솔 error 없음
- 2026-05-25 타이틀 레퍼런스 반영 2차: 사용자가 준 사이버펑크 픽셀 타이틀 레퍼런스에서 배경/인물은 제외하고, 영상 위 오버레이만 남기는 방향으로 조정. 상단 중앙 `DEEP DIVE` 네온 로고 + 분홍 글리치 바, 중앙 `HOME` 코어 HUD + 청록 회로선, 하단 메뉴/ECG 라인 구조로 재배치. 큰 좌우 패널은 투명화해 영상 배경을 가리지 않음. `ScreenCapture` 기준 표시 확인, Unity 콘솔 error 없음
- 2026-05-25 타이틀 동적 셰이더 보강: 하단 ECG/펄스 `Background/ScanLine` 제거. `DMS/UI/TitleNeonOverlay` UI 셰이더 추가, `TitleNeon_Cyan`/`TitleNeon_Pink`/`TitleNeon_SoftOverlay` 머티리얼을 bake에서 자동 생성·연결. 네온 로고/글리치 바/HOME 코어/회로선에 시간 기반 scanline, sweep, flicker가 들어간다. PlayMode `ScreenCapture` 기준 영상 위 오버레이 확인, Unity 콘솔 error 없음
- 2026-05-25 보스 후반 시나리오 연출 1차 갱신: 기억조각 1/2와 보스 최종 처치 후 `boss_defeat` 영상/HOME 분석/엔딩 로그를 `SceneEventSequence`에 연결. 후속 피드백으로 아래 항목처럼 영상 중 대사·처치 전 마지막 기억·주인공집 엔딩 배경 구조로 재정리됨
- 2026-05-25 보스 후반 피드백 반영: `PlayCutsceneVideo`가 `waitForCompletion=false`일 때 영상 재생을 유지한 채 다음 step을 진행하고 `WaitForCutsceneVideo`로 끝을 기다릴 수 있게 확장. 기억조각 1/2 대사는 영상 중 겹쳐 나오도록 변경. 보스 최종 처치는 `시퀀스_보스_처치전_마지막기억`(영상+마지막 대사) → 실제 보스 소거 이펙트 → `시퀀스_보스_처치후_HOME회수`(HOME 분석/회수)로 분리했고, 엔딩 대사는 `스폰_주인공집`으로 이동 후 집 배경에서 출력되도록 수정. P3 마지막 보스 개체는 처치 전 영상/대사까지 시각적으로 남겨둔다. `dotnet build`, `safe_refresh --compile true`, 설치 메뉴 실행, 씬 저장, Unity 콘솔 error 없음
- 2026-05-25 보스전 후 주인공집이 안 보이던 카메라 락 원인 해결: `BossBattleArena.EnterBattle()` 후 `_isActive`와 `SimpleCameraFollow.LockToArenaPosition()`이 해제되지 않아 카메라가 아레나에 고정되고 `LateUpdate`가 플레이어를 아레나 Bounds로 계속 클램프했다. `BossBattleArena.ExitBattle()`을 추가해 아레나 활성/카메라 락/플레이어 클램프/보스 HP UI/아레나 FX를 해제하고 카메라 ortho size를 복원. `SceneEventSequence.ExitBossArena` step을 추가해 `시퀀스_보스_처치후_HOME회수`에서 `FadeOut` 직후, `스폰_주인공집` 텔레포트 직전에 호출되도록 반영. `dotnet build`, `safe_refresh --compile true`, 설치 메뉴 실행, 씬 저장, Unity 콘솔 error 없음
- 2026-05-25 보스 시나리오 연출 피드백 추가 반영: 기억조각 1/2는 영상 재생 중 아빠/딸 대사만 출력하고 `WaitForCutsceneVideo` 이후 주인공/브로커 반응이 나오도록 시퀀스 재정렬. 보스전 등장 시 `BossBattleArena.EnterIntroView()`가 전투 시작 전 카메라를 아레나 앵커에 먼저 고정해 주인공과 잔류 인격이 같은 화면에 보이게 함. 최종 처치 루틴 시작 시 보스 HP 바를 숨기고, 처치 전 영상 앞에는 `WhiteFlash`를 추가. 엔딩은 주인공집 배경 대사 후 암전 `END` 표시, `Space` 입력 시 `Yongwoo_Title`로 로드. 설치 메뉴 실행, 씬 저장, `dotnet build`, Unity 콘솔 error 없음
- 2026-05-25 기억조각/엔딩 글리치 피드백 반영: `시퀀스_기억조각_01` 아빠/딸 대사를 집에서 바깥도시를 바라보며 희망을 말하는 내용으로 교체. 보스 후 `스폰_주인공집` 복귀 직후 `GlitchFade(0)` step을 추가해 집 배경 엔딩부터는 보스방 글리치가 남지 않도록 수정. 설치 메뉴 재실행, 씬 저장, 시퀀스 데이터 확인 완료
- 2026-05-25 HOME 코어/END 대사창 피드백 반영: `HOME코어_회수가능`은 기존 `튜토리얼진행/상호작용/HOME코어`의 `Chest_0` 스프라이트를 계속 쓰되, 설치 메뉴 실행 때마다 보스 위치 근처로 재배치하도록 수정. 이전 저장 위치가 아레나 카메라 위쪽 밖이라 처치 후 안 보일 수 있었음. END 직전 `주인공: 없어.` 대사창은 `HideComms`를 즉시 넣어 암전 END 화면에 남지 않게 함. 시퀀스 데이터 확인 결과 회수 코어 위치 `(60.20, -37.24)`, 스프라이트 `Chest_0`, END 전 `HideComms` 확인

- 2026-05-25 Git 반영: `origin/main`/`origin/GWANGMO_Ver3`의 광모 플레이어·사운드 변경 3커밋을 `yongwoo`에 병합. `BossBattleArena`는 보스전 종료 시 카메라락 해제 로직과 광모 `P_PlayerController` 아레나 클램프 지원이 함께 반영됨. `safe_refresh --compile true`, Unity 콘솔 error 없음
- 2026-05-25 광모 플레이어 통합: 최신 `Yongwoo_Stage`는 유지하고 active Player만 `GWANGMO/Art/Player/Prefab/Player_ver02` 기반으로 교체. `SceneEventSequence` 28개는 `P_PlayerController`/새 `PlayerInteraction`/새 `PlayerSlowMotion` 참조로 재연결, 기존 active `SimplePlayerController` Player 제거. 안내 로그는 F 상호작용, 발도술 E, 회피 Shift/앞대쉬 Q 기준으로 반영
- 2026-05-25 광모 플레이어 레이어 차이 수정: 광모 씬은 `Player_ver02/Hitboxes`와 `WallCheck_Left/Right`가 `Default`인데, Yongwoo 씬 통합본은 Player 하위 전체가 `Player` 레이어로 저장되어 보스 피격/벽 체크가 광모 씬과 달라질 수 있었음. `Hitboxes`/`Attack1Hitbox`/`DashAttackHitbox`/`WallCheck_Left`/`WallCheck_Right`를 `Default`, 루트와 `GroundCheck`는 `Player`로 맞춤. `safe_refresh --compile true`, Unity 콘솔 error 없음, `dotnet build` 오류 0개

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
- [ ] 보스 스테이지 + 보스 *(P1/P2/P3 전환과 주요 패턴 구현됨. 등장/전환/처치/HOME 회수 연출 훅은 추가됨. 다음은 설치 메뉴 실행 후 영상 클립 연결, 사용자 플레이 기준 전투감 튜닝, PlayMode 검증)*
- [ ] 히트 이펙트, 사운드 연동 *(SFX 1차 훅/임시 WAV 완료, PlayMode 청감 검증·최종 음원 교체 필요)*
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
- `Boss_P1_Prototype`은 `Yongwoo_Stage` `보스씬/보스`에 배치됨. P1→P2→P3 강제 전환 스모크는 통과했지만, 실제 플레이 손맛 기준으로 조준선 길이/탄속/대시 속도/장판·레이저 압박은 튜닝 필요. VFX 스케일 버그는 2026-05-24 수정 — PlayMode 재검증 필요
- 보스 스토리 연출 훅은 코드/메뉴/씬 설치까지 완료. 다음에는 현재 작업분 체크포인트 후 PlayMode에서 기억조각 상호작용 시 영상 중 아빠/딸 대사만 나오고 영상 종료 후 주인공/브로커 대사가 나오는지, 보스 등장 시 주인공/잔류 인격이 같은 카메라에 잡히는지, 최종 처치 때 보스 HP 바 숨김→흰 플래시→`boss_defeat` 영상+잔류 인격 마지막 대사→보스 소거 이펙트→보스 위치 근처 `HOME코어_회수가능` 부상→HOME 분석→주인공집 복귀 시 글리치 제거→엔딩→암전 END(Comms 숨김)→Space 타이틀 복귀까지 관통 검증해야 함. 사용자가 제작한 영상 클립은 `시퀀스_기억조각_01`, `시퀀스_기억조각_02`, `시퀀스_보스_처치전_마지막기억`의 `PlayCutsceneVideo` step에 연결
- 보스전 종료 후 주인공집 이동 검증 시 `BossBattleArena.IsActive == false`, `SimpleCameraFollow.IsArenaLocked == false`, 플레이어 위치가 `스폰_주인공집` 근처인지 같이 확인해야 함
- 보스 투사체는 trigger + overlap 샘플링으로 피격 경로를 보강했고 PlayMode 강제 overlap 스캔은 통과. 실제 플레이에서 보스가 발사한 탄을 플레이어가 맞고 사망/리스폰하는 전체 흐름은 아직 관통 검증 필요
- SFX 1차 구현은 정적 linter 통과 상태. `Yongwoo_Stage` 기존 dirty/체크포인트 이슈 때문에 Unity refresh/PlayMode 검증은 아직 하지 않음. 다음 검증은 타이틀 시작음, 시스템로그/Comms 타이핑 속도, 타격음 반복 피로도, 보스 패턴 텔레그래프/발사음 볼륨 균형 순서로 확인
- 제작된 영상 파일은 `Assets/_WIP/yongwoo/Resources/Yongwoo/Videos/`에 생성 시각 이름으로 들어와 있음. 내용 확인 후 `title`, `intro`, `memory_01`, `memory_02`, `boss_defeat` 이름으로 맞춘 뒤 Unity refresh/PlayMode에서 타이틀 배경 루프, 타이틀 시작→인트로→시작 시스템로그, 기억조각 1/2 상호작용, 보스 처치 후 컷신과 각 컷씬 전용 볼륨 반영을 확인해야 함

---

## 세션 종료 시 갱신 규칙

이 파일을 다음 항목으로 덮어쓴다:
1. **지금 어디까지** — 다음 세션이 이어 작업할 위치
2. **핵심 설계 결정** — 이번 세션에서 새로 정한 것 추가
3. **남은 작업** 체크박스 — 완료한 거 체크
4. **알려진 미해결** — 다음 세션이 손대야 할 미해결 항목

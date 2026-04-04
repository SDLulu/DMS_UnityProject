# DMS 프로젝트

2D 액션 게임 프로젝트. Unity 6000.3.x (URP).

## 작업 규칙

개인 작업 규칙은 `Assets/_WIP/yongwoo/AGENTS.md`를 따른다.
해당 문서의 아키텍처 원칙, 커뮤니케이션 규칙, 작업 안전 규칙이 이 프로젝트의 기본 기준이다.

## 핵심 원칙 요약

- **시스템 개념 없음**: 게임 오브젝트(Player, Enemy 등)와 Manager(UIManager, DialogueManager 등)로만 구분한다.
- **빌드 퍼스트**: 손맛 먼저 검증, 구조는 나중에.
- **객체 간 통신**: Contact 클래스 또는 C# event 사용. Inspector 직접 연결 금지 (OnTrigger/GetComponent 허용).
- **Manager 기준**: "여러 객체를 한꺼번에 알아야 하나?" → Manager. "한 객체 안에서 끝나나?" → 객체 안에.

## 폴더 구조 (Assets/_WIP/yongwoo/)

- `Scripts/Prefabs/` — 게임 오브젝트 컴포넌트 (Player, Boss, Enemy, Camera 등)
- `Scripts/Managers/` — 여러 객체를 조율하는 Manager (UI, Input, Dialogue)
- `Scripts/Utility/` — 순수 유틸리티 (RuntimeSpriteUtility 등)
- `Scripts/Scene/` — 씬 전용 스크립트 (BossEncounter 등)
- `Editor/` — 에디터 전용 코드

## unity-cli

Unity 에디터 제어는 `unity-cli`를 사용한다.
- `unity-cli status` — 연결 확인
- `unity-cli exec "<C# code>"` — C# 코드 실행
- `unity-cli console --type error` — 에러 로그 확인
- `unity-cli editor play --wait` — 플레이 모드 진입

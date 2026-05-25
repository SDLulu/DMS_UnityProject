# DMS 컷신 영상 넣는 곳

이 폴더는 `CutsceneVideoPanel` / `SceneEventSequence.PlayCutsceneVideo`가 자동으로 찾는 영상 폴더다.

## 자동 연결 파일명

Unity에서 이 폴더 안의 영상 파일 이름을 아래처럼 맞추면 코드가 자동으로 찾아서 재생한다.

| 파일명 | 재생 위치 |
|---|---|
| `title_01.mp4`, `title_02.mp4` ... `title_09.mp4` | 타이틀 씬 배경 영상 playlist. 타이틀 화면에서 순서대로 반복 재생 |
| `title.mp4` | 타이틀 배경 단일 fallback 영상 |
| `intro_01.mp4`, `intro_02.mp4` ... `intro_09.mp4` | 타이틀 씬 `게임 시작` 클릭 후, `Yongwoo_Stage` 로드 전 순서대로 재생 |
| `intro.mp4` | 인트로 단일 fallback 영상 |
| `memory_01.mp4` | 보스 구역 `기억조각_01` 상호작용 |
| `memory_02.mp4` | 보스 구역 `기억조각_02` 상호작용 |
| `boss_defeat.mp4` | 보스 처치 후 HOME 회수/엔딩 컷신 |

확장자는 Unity가 VideoClip으로 import할 수 있으면 된다. 보통 mp4를 쓴다.

## 영상이 짧을 때 추가하는 법

긴 파일 하나로 합치지 말고 짧은 파일을 번호로 나눠 추가한다.

타이틀 배경:

```text
title_01.mp4
title_02.mp4
title_03.mp4
```

인트로:

```text
intro_01.mp4
intro_02.mp4
intro_03.mp4
```

타이틀은 `title_01`부터 발견한 순서대로 계속 순환한다. 인트로는 `intro_01`부터 발견한 순서대로 한 번씩 재생한 뒤 스테이지로 넘어간다.

비어 있으면 자동으로 다음 순서로 fallback한다.

1. `TitleSceneController`의 `Title Video Playlist` / `Intro Video Playlist`에 직접 넣은 클립
2. `Resources/Yongwoo/Videos/title_01` ... `title_09` 또는 `intro_01` ... `intro_09`
3. 기존 단일 파일 `title` 또는 `intro`
4. 그래도 없으면 해당 영상은 조용히 건너뜀

## 지금 해야 할 일

현재 폴더에 들어온 영상들이 생성 시각 이름이면, 실제 내용에 맞춰 위 이름 규칙 중 하나로 바꾼다.

예:

```text
20260524_130714_d271b9f9.mp4 -> title.mp4
20260524_130910_94593a99.mp4 -> intro.mp4
20260524_130936_4f364e72.mp4 -> memory_01.mp4
20260524_132018_e9848d89.mp4 -> memory_02.mp4
20260524_140629_a1cfb60a.mp4 -> boss_defeat.mp4
```

위 예시는 형식 설명용이다. 실제 매칭은 영상을 재생해서 내용 확인 후 정한다.

## 수동 연결도 가능

자동 파일명을 쓰지 않아도 된다. 각 `SceneEventSequence`의 `PlayCutsceneVideo` step에 VideoClip을 직접 꽂으면 그 클립이 우선 재생된다.

대상 시퀀스:

- `시퀀스_기억조각_01`
- `시퀀스_기억조각_02`
- `시퀀스_보스_처치후_HOME회수`

타이틀 배경 영상은 `Yongwoo_Title` 씬의 `TitleSceneController.titleVideoClip`에 직접 꽂을 수 있다.
인트로 영상은 `Yongwoo_Title` 씬의 `TitleSceneController.introVideoClip`에 직접 꽂을 수 있다.
여러 개를 직접 꽂고 싶으면 `Title Video Playlist` / `Intro Video Playlist` 배열을 쓴다. 배열에 값이 하나라도 있으면 자동 파일명보다 배열이 우선이다.

## 소리 / 크기 조절

### 타이틀 배경 영상

`Yongwoo_Title` 씬에서 `TitleUI` 오브젝트의 `TitleSceneController`를 본다.

조절값:

- `Title Video Muted`: 타이틀 배경 영상 소리 끄기/켜기
- `Title Video Volume`: 타이틀 배경 영상 볼륨
- `Intro Video Muted`: `게임 시작` 후 나오는 인트로 영상 소리 끄기/켜기
- `Intro Video Volume`: `게임 시작` 후 나오는 인트로 영상 볼륨
- `Title Video Layout Mode`: 화면 맞춤 방식
  - `ManualRect`: Scene 뷰에서 `TitleVideoBackground` RectTransform을 직접 조절한 값 그대로 사용
  - `FitInside`: 영상 전체가 보이게 맞춤. 빈 여백이 생길 수 있음
  - `FillScreen`: 화면을 꽉 채움. 가장자리 일부가 잘릴 수 있음
  - `Stretch`: 화면에 강제로 늘림. 비율이 찌그러질 수 있음
- `Title Video Scale`: 타이틀 배경 영상 크기 배율
- `Title Video Preserve Aspect`: `ManualRect`에서도 원본 비율 유지
- `Title Video Manual Aspect`: 영상 파일을 아직 못 읽을 때 쓸 수동 비율. 기본 1.7778 = 16:9

직접 조절하려면 `TitleUI/TitleVideoBackground`를 선택하고 Unity의 Rect Tool로 크기와 위치를 맞춘다. 이때 `Title Video Layout Mode`는 `ManualRect`로 둔다. 비율을 정확히 유지하려면 `Title Video Preserve Aspect`를 켠다.

Play 중에 `Title Video Volume`을 움직이면 타이틀 배경 영상에 바로 반영된다. `Intro Video Volume`은 `게임 시작`을 눌러 인트로 영상이 재생될 때 적용된다. 단, 각각의 `Muted`가 켜져 있으면 볼륨을 올려도 소리가 나지 않는다.

내장 영상 소리는 `VideoPlayer`의 Direct 오디오 트랙 볼륨으로 직접 제어한다. `AudioSource` 컴포넌트의 Volume 값을 따로 움직여도 영상 소리 기준값은 바뀌지 않는다.

### 기억조각 / 보스 처치 후 컷신

`Yongwoo_Stage` 씬에서 `HUD` 오브젝트의 `CutsceneVideoPanel`을 본다.

조절값:

- `Use Story Video Audio Overrides`: 자동 연결 컷신별 전용 볼륨 사용
- `Memory 01 Muted` / `Memory 01 Volume`: `memory_01` 컷신 소리
- `Memory 02 Muted` / `Memory 02 Volume`: `memory_02` 컷신 소리
- `Boss Defeat Muted` / `Boss Defeat Volume`: `boss_defeat` 컷신 소리
- `Mute Audio`: 수동으로 VideoClip을 직접 꽂은 컷신의 공통 소리 끄기/켜기
- `Video Volume`: 수동으로 VideoClip을 직접 꽂은 컷신의 공통 볼륨
- `Video Layout Mode`: 화면 맞춤 방식
- `Video Scale`: 컷신 영상 크기 배율
- `Video Preserve Aspect`: `ManualRect`에서도 원본 비율 유지
- `Video Manual Aspect`: 영상 파일을 아직 못 읽을 때 쓸 수동 비율. 기본 1.7778 = 16:9

직접 조절하려면 `UI/HUD/CutsceneVideoRoot/VideoImage`를 선택하고 Unity의 Rect Tool로 크기와 위치를 맞춘다. 이때 `Video Layout Mode`는 `ManualRect`로 둔다. 비율을 정확히 유지하려면 `Video Preserve Aspect`를 켠다.

자동 연결 컷신은 Play 중에 해당 컷신 전용 `Volume`을 움직이면 바로 반영된다. 수동 연결 컷신은 `Video Volume`을 움직이면 바로 반영된다. 단, 해당 `Muted`/`Mute Audio`가 켜져 있으면 볼륨을 올려도 소리가 나지 않는다.

내장 영상 소리는 `VideoPlayer`의 Direct 오디오 트랙 볼륨으로 직접 제어한다. `AudioSource` 컴포넌트의 Volume 값을 따로 움직여도 영상 소리 기준값은 바뀌지 않는다.

## 확인 순서

1. 파일명을 맞춘다.
2. Unity에서 import가 끝날 때까지 기다린다.
3. 타이틀 씬에서 `title_01`부터 순환 재생되는지 본다. 번호 영상이 없으면 `title` 단일 영상이 반복되는지 본다.
4. 타이틀에서 `게임 시작`을 눌러 `intro_01`부터 순서대로 재생 후 시작 시스템로그가 나오는지 본다. 번호 영상이 없으면 `intro` 단일 영상이 재생되는지 본다.
5. 보스 구역에서 기억조각 1/2 상호작용을 확인한다.
6. 보스 처치 후 `boss_defeat` 컷신을 확인한다.

<div>

<p align="center">
  <img src="src/Cadroue.UIShell/PAssets/PProgram/PProgramIcon.png" width="112" alt="Cadroue icon">
</p>

# Cadroue

**EN** · A Windows desktop application for preparing, queuing, and relaying FFmpeg media work.

<br>

**KO** · FFmpeg를 기반 미디어 파일 처리를 위한 윈도우 데스크톱 애플리케이션입니다.

<br>

> Built with the help of Claude and ChatGPT (Vibe coding). This is a personal project, so bugs may still exist.

<br><br>

<div align="center">
<a href="#english"><img alt="English" src="https://img.shields.io/badge/English-Read-blue?style=for-the-badge"></a>
&nbsp;&nbsp;
<a href="#korean"><img alt="한국어" src="https://img.shields.io/badge/한국어-읽기-brightgreen?style=for-the-badge"></a>
</div>

</div>

---

<a id="english"></a>

## English

> **Development status — version 1.6.3810**  
> Cadroue is under active development. Common combinations of operations have been tested, but bugs may remain in unusual or extreme workflows.

### What Cadroue is

Cadroue organizes media work around separate **Split**, **Edit**, **Audio**, **Convert**, **Merge**, and **Worklist** tabs. Each tab prepares a specific kind of job; the Worklist processes those jobs through FFmpeg and preserves their relationship to the original files.

A typical Cadroue workflow is more than one conversion:

1. Review several recordings and save different split, crop, or audio plans for each file.
2. Add the prepared work to a persistent queue.
3. Process urgent outputs before normal queued work without interrupting the job already running.
4. Relay completed outputs into another tab—for example, from Split to Audio and then into Merge.
5. Inspect related jobs together in the Worklist through their source lineage.

Cadroue stores reusable per-file preparation in `.cad` sidecars, either beside the media or inside the configured workspace. These records can preserve source identity, keyframes, split sections, edit settings, and audio-processing plans.

### Main features

#### Split

- Create multiple named sections from one media file.
- Set, split, rename, enable, disable, sort, and delete sections.
- Navigate to previous, nearest, and next keyframes.
- Save section plans and scanned keyframe data in the file's `.cad` record.
- Import recognized segments from LosslessCut `.llc` projects.
- Build output names from tokens such as the original name, section number, section name, date, time, prefix, and suffix.

#### Edit

- Crop visually in the preview.
- Rotate by 90°, 180°, or 270°.
- Flip horizontally or vertically.
- Adjust brightness and contrast.
- Save a different edit plan for each source file, or carry selected settings across all loaded files.

#### Audio

Audio processing is represented as an ordered list, so the sequence remains explicit:

- High-pass filter
- Low-pass filter
- Noise reduction
- Volume adjustment
- Loudness or dynamic normalization
- Optional two-pass loudness normalization

Video can remain stream-copied when only the audio needs processing, depending on the selected export settings.

#### Convert and export

- Smart export, remux-only, and re-encode modes.
- Independent video and audio inclusion, exclusion, copy, and encode choices.
- Built-in container choices for MP4, Matroska, MOV, WebM, M4A, MP3, WAV, FLAC, and OGG, plus access to formats exposed by FFmpeg.
- Video size, frame-rate, pixel-format, rate-control, quality, and encoder-specific controls.
- Software and hardware encoder definitions, used only when supported by the selected FFmpeg build.
- First-audio-track or all-audio-track selection.
- Importable and exportable Cadroue encoding presets.
- Encoder verification before work is queued.

#### Merge

- Create and rename independent merge groups.
- Reorder or remove files inside a group.
- Automatically group numbered files such as `Lecture (1).mp4`, `Lecture (2).mp4`, and `Lecture (3).mp4`.
- **Strict grouping** separates runs when a number is missing.
- **Loose grouping** ignores numbering gaps.
- Queue every group as its own output job.

#### Worklist

- Persistent file-backed queue records.
- Normal and high-priority work.
- Configurable parallel processing.
- Start, pause, resume, cancel, stop, remove, and clear controls.
- Optional automatic resume, retry limits, and pause-on-failure behavior.
- Recovery of work left running after an unexpected shutdown.
- Source, output, encoding, progress, speed, attempt, ownership, and relay details.
- Related jobs grouped by source lineage, including split and relayed work.

#### Relay and tabs

- Send completed outputs from one tab directly into another tab's Files list.
- The destination tab remains reviewable and does not start automatically.
- By default, the target Files list is cleared before the first delivery from a relay run; this can be disabled to collect outputs from several relays.
- A tab cannot relay into itself.
- Create multiple tabs of the same type and double-click a tab name to give it a distinct workflow name.
- Restore the previous tab layout and session on startup.

### Preview and processing

Cadroue separates **preview availability** from **FFmpeg processability**. A file may be processable even when the preview engine cannot display it, and the interface reports these states separately.

The standard preview uses Flyleaf. A locally built Flyleaf variant can optionally be installed from **Options → System** to extend contrast preview. FFmpeg remains authoritative for exported output.

### Requirements

#### Running Cadroue

- Windows 10 version 2004 / build 19041 or later
- `ffmpeg.exe` and `ffprobe.exe` available on `PATH`
- Alternatively, an FFmpeg folder can be selected under **Options → System** for export and compatible preview libraries

Published builds are self-contained, so they do not require a separately installed .NET runtime.

#### Building from source

- Windows
- .NET 10 SDK
- FFmpeg and FFprobe for media inspection and processing

### Getting started

1. Launch `Cadroue.exe`.
2. Open **Options → System** and confirm the workspace and FFmpeg configuration.
3. Create or select a Split, Edit, Audio, Convert, or Merge tab.
4. Drop media files or folders into the Files panel.
5. Select a file to preview and configure the operation.
6. Choose export settings or a saved preset.
7. Use:
   - **Add List** to queue the current work normally;
   - **Add All** to queue eligible loaded files or groups;
   - **Execute** to queue the current work at high priority.
8. Open the Worklist tab and start processing.

Audio-only files should be opened in an Audio tab.

### Building

From the repository root, create a self-contained Windows build with:

```shell
dotnet publish .\src\Cadroue.UIShell\Cadroue.UIShell.csproj --configuration Release --runtime win-x64 --self-contained true --output .\publish\win-x64 -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

To run the development version directly:

```shell
dotnet run --project .\src\Cadroue.UIShell\Cadroue.UIShell.csproj
```

The resulting published executable is written to:

```text
publish\win-x64\Cadroue.exe
```

For another Windows architecture, replace `win-x64` in the publish command and output path with the required runtime identifier, such as `win-arm64`.

### Repository structure

```text
src/
├─ Cadroue.Core/          Work records, scheduling, priorities, and encoder capabilities
├─ Cadroue.Media/         Media inspection, keyframes, .cad sidecars, and LosslessCut import
├─ Cadroue.ShellEngine/   FFmpeg command construction and process execution
└─ Cadroue.UIShell/       WPF application, tabs, panels, preview, options, and localization

localization/
├─ en.json
└─ ko.json
```

### Current limitations

- Cadroue currently targets Windows and has no general command-line interface.
- FFmpeg and FFprobe are not bundled with the normal application build.
- Preview support depends on the media format and available Flyleaf/FFmpeg libraries.
- Stream preservation is focused on video and audio; subtitle, attachment, chapter, and full metadata workflows are not yet the application's primary use case.
- Relaying prepares the next tab but does not automatically execute it.

### Reporting problems

When reporting a bug, include:

- Cadroue version
- Windows version
- FFmpeg build and location
- Source container, video codec, and audio codec
- Exact sequence of tabs and operations
- Whether the problem occurred during preview, queue preparation, or FFmpeg processing
- Relevant entries from the Log window

A small reproducible source file or a redacted `.cad` record is especially useful when the problem depends on a specific operation sequence.

### Technologies

Cadroue is built with:

- C# and .NET 10
- WPF
- FFmpeg and FFprobe
- Flyleaf
- SQLite
- SharpVectors

### License

No project license has been added to this repository yet. Until a license is chosen and included, reuse and distribution rights are not granted beyond the rights provided by GitHub's terms for viewing and forking a public repository.

---

<a id="korean"></a>

## 한국어

> **개발 상태 — 버전 1.6.3810**  
> Cadroue는 현재 활발하게 개발 중입니다. 일반적으로 가능한 작업 조합은 점검했지만, 드물거나 극단적인 작업 흐름에서는 버그가 남아 있을 수 있습니다.

### Cadroue란?

Cadroue는 미디어 작업을 서로 분리된 **분할(Split)**, **편집(Edit)**, **오디오(Audio)**, **변환(Convert)**, **병합(Merge)**, **작업 목록(Worklist)** 탭을 중심으로 구성합니다. 각 탭은 특정 종류의 작업을 준비하고, 작업 목록은 해당 작업을 FFmpeg로 처리하면서 원본 파일과의 관계를 보존합니다.

Cadroue의 일반적인 작업 흐름은 한 번의 변환에 그치지 않습니다.

1. 여러 녹화 파일을 검토하고 파일마다 서로 다른 분할, 자르기 또는 오디오 계획을 저장합니다.
2. 준비된 작업을 지속적으로 보존되는 대기열에 추가합니다.
3. 이미 실행 중인 작업을 중단하지 않은 채 긴급한 출력을 일반 대기 작업보다 먼저 처리합니다.
4. 완료된 출력을 다른 탭으로 전달합니다. 예를 들어 분할 탭에서 오디오 탭으로, 다시 병합 탭으로 전달할 수 있습니다.
5. 작업 목록에서 원본 계보를 기준으로 서로 관련된 작업을 함께 확인합니다.

Cadroue는 파일별로 다시 사용할 수 있는 준비 정보를 미디어 옆이나 설정된 작업 공간 안의 `.cad` 사이드카에 저장합니다. 이 기록에는 원본 식별 정보, 키프레임, 분할 구간, 편집 설정, 오디오 처리 계획이 보존될 수 있습니다.

### 주요 기능

#### 분할

- 하나의 미디어 파일에서 이름이 지정된 여러 구간을 만듭니다.
- 구간을 설정하고, 나누고, 이름을 바꾸고, 활성화하거나 비활성화하고, 정렬하고, 삭제합니다.
- 이전, 가장 가까운, 다음 키프레임으로 이동합니다.
- 구간 계획과 검색된 키프레임 데이터를 파일의 `.cad` 기록에 저장합니다.
- LosslessCut `.llc` 프로젝트에서 인식된 구간을 가져옵니다.
- 원래 이름, 구간 번호, 구간 이름, 날짜, 시간, 접두사, 접미사 등의 토큰으로 출력 이름을 구성합니다.

#### 편집

- 미리보기에서 시각적으로 화면을 자릅니다.
- 90°, 180°, 270°로 회전합니다.
- 가로 또는 세로로 뒤집습니다.
- 밝기와 대비를 조정합니다.
- 원본 파일마다 서로 다른 편집 계획을 저장하거나, 선택한 설정을 불러온 모든 파일에 이어서 적용합니다.

#### 오디오

오디오 처리는 순서가 명확하게 유지되도록 정렬된 목록으로 표현됩니다.

- 하이패스 필터
- 로우패스 필터
- 노이즈 감소
- 볼륨 조정
- 라우드니스 또는 동적 정규화
- 선택적인 2패스 라우드니스 정규화

선택한 내보내기 설정에 따라 오디오만 처리할 때 비디오는 스트림 복사 상태로 유지할 수 있습니다.

#### 변환 및 내보내기

- 스마트 내보내기, 리먹스 전용, 재인코딩 모드.
- 비디오와 오디오를 각각 포함하거나 제외하고, 복사하거나 인코딩하는 선택지.
- MP4, Matroska, MOV, WebM, M4A, MP3, WAV, FLAC, OGG의 기본 컨테이너 선택지와 FFmpeg가 제공하는 형식에 대한 접근.
- 비디오 크기, 프레임 레이트, 픽셀 형식, 레이트 제어, 품질, 인코더별 제어.
- 선택한 FFmpeg 빌드에서 지원되는 경우에만 사용되는 소프트웨어 및 하드웨어 인코더 정의.
- 첫 번째 오디오 트랙 또는 모든 오디오 트랙 선택.
- 가져오고 내보낼 수 있는 Cadroue 인코딩 프리셋.
- 작업을 대기열에 추가하기 전 인코더 검증.

#### 병합

- 서로 독립된 병합 그룹을 만들고 이름을 바꿉니다.
- 그룹 안의 파일 순서를 바꾸거나 파일을 제거합니다.
- `Lecture (1).mp4`, `Lecture (2).mp4`, `Lecture (3).mp4`처럼 번호가 붙은 파일을 자동으로 그룹화합니다.
- **엄격한 그룹화**는 중간 번호가 빠졌을 때 연속 구간을 분리합니다.
- **느슨한 그룹화**는 번호 간격을 무시합니다.
- 각 그룹을 독립된 출력 작업으로 대기열에 추가합니다.

#### 작업 목록

- 파일에 저장되는 지속적 대기열 기록.
- 일반 작업과 높은 우선순위 작업.
- 설정 가능한 병렬 처리.
- 시작, 일시 정지, 재개, 취소, 중지, 제거, 비우기 제어.
- 선택적인 자동 재개, 재시도 횟수 제한, 실패 시 일시 정지 동작.
- 예상치 못한 종료 후 실행 중으로 남은 작업 복구.
- 원본, 출력, 인코딩, 진행률, 속도, 시도 횟수, 소유 관계, 전달 세부 정보.
- 분할 작업과 전달된 작업을 포함하여 원본 계보에 따라 관련 작업을 그룹화합니다.

#### 전달과 탭

- 완료된 출력을 한 탭에서 다른 탭의 파일 목록으로 직접 전달합니다.
- 대상 탭은 검토 가능한 상태로 유지되며 자동으로 시작되지 않습니다.
- 기본적으로 한 번의 전달 실행에서 첫 파일이 도착하기 전에 대상 파일 목록을 비웁니다. 이 동작을 끄면 여러 전달의 출력을 한곳에 모을 수 있습니다.
- 탭은 자기 자신에게 전달할 수 없습니다.
- 같은 종류의 탭을 여러 개 만들 수 있으며, 탭 이름을 두 번 클릭하여 각 작업 흐름을 구분하는 이름을 지정할 수 있습니다.
- 시작할 때 이전 탭 배치와 세션을 복원합니다.

### 미리보기와 처리

Cadroue는 **미리보기 가능 여부**와 **FFmpeg 처리 가능 여부**를 구분합니다. 미리보기 엔진이 파일을 표시하지 못하더라도 처리할 수 있으며, 인터페이스는 이 두 상태를 서로 따로 보고합니다.

표준 미리보기에는 Flyleaf를 사용합니다. 대비 미리보기 기능을 확장하기 위해 로컬에서 빌드된 Flyleaf 변형을 **옵션 → 시스템**에서 선택적으로 설치할 수 있습니다. 내보낸 결과에서는 FFmpeg가 최종 기준입니다.

### 요구 사항

#### Cadroue 실행

- Windows 10 버전 2004 / 빌드 19041 이상
- `PATH`에서 사용할 수 있는 `ffmpeg.exe`와 `ffprobe.exe`
- 또는 내보내기와 호환 가능한 미리보기 라이브러리를 위해 **옵션 → 시스템**에서 FFmpeg 폴더를 선택할 수 있습니다.

배포 빌드는 자체 포함 방식이므로 별도로 설치된 .NET 런타임이 필요하지 않습니다.

#### 소스에서 빌드

- Windows
- .NET 10 SDK
- 미디어 검사와 처리를 위한 FFmpeg 및 FFprobe

### 시작하기

1. `Cadroue.exe`를 실행합니다.
2. **옵션 → 시스템**을 열고 작업 공간과 FFmpeg 설정을 확인합니다.
3. 분할, 편집, 오디오, 변환 또는 병합 탭을 만들거나 선택합니다.
4. 파일 패널에 미디어 파일이나 폴더를 끌어 놓습니다.
5. 미리 볼 파일을 선택하고 작업을 설정합니다.
6. 내보내기 설정이나 저장된 프리셋을 선택합니다.
7. 다음 기능을 사용합니다.
   - **목록 추가(Add List)**: 현재 작업을 일반 우선순위로 대기열에 추가합니다.
   - **전체 추가(Add All)**: 조건에 맞는 불러온 파일이나 그룹을 대기열에 추가합니다.
   - **실행(Execute)**: 현재 작업을 높은 우선순위로 대기열에 추가합니다.
8. 작업 목록 탭을 열고 처리를 시작합니다.

오디오 전용 파일은 오디오 탭에서 열어야 합니다.

### 빌드

저장소 루트에서 다음 명령을 사용하여 자체 포함 Windows 빌드를 만듭니다.

```shell
dotnet publish .\src\Cadroue.UIShell\Cadroue.UIShell.csproj --configuration Release --runtime win-x64 --self-contained true --output .\publish\win-x64 -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

개발 버전을 직접 실행하려면 다음 명령을 사용합니다.

```shell
dotnet run --project .\src\Cadroue.UIShell\Cadroue.UIShell.csproj
```

게시된 실행 파일은 다음 위치에 기록됩니다.

```text
publish\win-x64\Cadroue.exe
```

다른 Windows 아키텍처를 대상으로 빌드하려면 publish 명령과 출력 경로의 `win-x64`를 `win-arm64`와 같은 필요한 런타임 식별자로 바꿉니다.

### 저장소 구조

```text
src/
├─ Cadroue.Core/          작업 기록, 스케줄링, 우선순위, 인코더 기능
├─ Cadroue.Media/         미디어 검사, 키프레임, .cad 사이드카, LosslessCut 가져오기
├─ Cadroue.ShellEngine/   FFmpeg 명령 구성 및 프로세스 실행
└─ Cadroue.UIShell/       WPF 애플리케이션, 탭, 패널, 미리보기, 옵션, 현지화

localization/
├─ en.json
└─ ko.json
```

### 주의 사항

- Cadroue는 현재 Windows를 대상으로 하며 일반 명령줄 인터페이스가 없습니다.
- 일반 애플리케이션 빌드에는 FFmpeg와 FFprobe가 포함되지 않습니다.
- 미리보기 지원은 미디어 형식과 사용할 수 있는 Flyleaf/FFmpeg 라이브러리에 따라 달라집니다.
- 스트림 보존은 비디오와 오디오에 초점을 둡니다. 자막, 첨부 파일, 챕터, 전체 메타데이터 작업은 아직 이 애플리케이션의 주된 사용 사례가 아닙니다.
- 전달은 다음 탭을 준비하지만 자동으로 실행하지는 않습니다.

### 문제 보고

버그를 보고할 때 다음 정보를 포함해 주십시오.

- Cadroue 버전
- Windows 버전
- FFmpeg 빌드와 위치
- 원본 컨테이너, 비디오 코덱, 오디오 코덱
- 사용한 탭과 작업의 정확한 순서
- 문제가 미리보기, 대기열 준비, FFmpeg 처리 중 어느 단계에서 발생했는지
- 로그 창의 관련 항목

특정 작업 순서에 따라 문제가 발생하는 경우, 문제를 재현할 수 있는 작은 원본 파일이나 민감한 정보를 제거한 `.cad` 기록이 특히 유용합니다.

### 사용 기술

Cadroue는 다음 기술로 제작되었습니다.

- C# 및 .NET 10
- WPF
- FFmpeg 및 FFprobe
- Flyleaf
- SQLite
- SharpVectors

### 라이선스

아직 이 저장소에 프로젝트 라이선스가 추가되지 않았습니다. 라이선스가 선택되어 포함되기 전까지는 GitHub 약관이 공개 저장소의 열람과 포크에 대해 제공하는 권리를 넘어서는 재사용 및 배포 권한이 부여되지 않습니다.

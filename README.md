<div>

<p align="center">
  <img src="src/Cadroue.UIShell/PAssets/PProgram/PProgramIcon.png" width="112" alt="Cadroue icon">
</p>

# Cadroue

**EN** · A Windows desktop application for planning, routing, queuing, and processing FFmpeg media workflows.

<br>

**KO** · FFmpeg 작업을 계획하고 실행하는 것을 도와주는 Windows 데스크톱 프로그램입니다.

<br>

> Cadroue is a personal project developed with assistance from Claude and ChatGPT. It remains under active development, so bugs may still exist.

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

> **Development status**  
> Cadroue is under active development. Common workflows and operation combinations have been tested, but unusual inputs or complex processing chains may still expose bugs.

> **Documentation version:** 2.9.10600

### What Cadroue is

Cadroue is a Windows desktop application for planning, routing, queuing, and processing FFmpeg media workflows. Work is organized into separate **Split**, **Edit**, **Audio**, **Convert**, **Merge**, **Funnel**, and **Worklist** tabs. You can open several tabs of the same type and give each one a workflow-specific name.

Each processing tab prepares a particular kind of job. The Worklist executes those jobs through FFmpeg while preserving their relationship to the original media and to earlier or later Cadroue jobs. Resizable and collapsible panels let each tab adapt to the task at hand.

### Main features

#### Split

- Create multiple named sections from one media file.
- Set, split, rename, enable, disable, reorder, and delete sections.
- Navigate to the previous, nearest, or next keyframe.
- Zoom and seek on the timeline, optionally display an audio waveform, and choose whether sections may overlap.
- Save section plans and scanned keyframe data in the file's `.cad` record.
- Import recognized segments from LosslessCut `.llc` projects.
- Construct output names from tokens such as the original name, section number, section name, date, time, prefix, and suffix.

#### Edit

- Draw and adjust a crop directly on the preview.
- Match a fixed aspect ratio.
- Rotate by 90°, 180°, or 270°.
- Flip horizontally or vertically.
- Adjust brightness and contrast.
- Save a separate edit plan for each source file or keep selected settings persistent while loading other files.

#### Audio

Audio processing is represented as an ordered list, so the processing sequence remains explicit. Available steps include:

- High-pass filtering
- Low-pass filtering
- Parametric equalization with built-in presets and custom frequency bands
- Noise reduction
- Volume adjustment
- Loudness or dynamic normalization
- Optional two-pass loudness normalization

Steps can be enabled, disabled, reordered, inspected, and saved per file. A normalization viewer compares estimated before-and-after waveforms, and persistent processing settings can be applied to newly loaded files. When only audio requires processing, the video stream can remain copied when the chosen export settings permit it.

#### Convert and export

- Smart export, remux-only, and re-encode modes.
- Independent video and audio inclusion, exclusion, stream-copy, and encoding choices.
- Built-in container choices for MP4, Matroska, MOV, WebM, M4A, MP3, WAV, FLAC, and OGG, together with access to formats supported by the selected FFmpeg build.
- Video size, frame rate, pixel format, rate control, quality, and encoder-specific controls.
- Output placement beside the source, in a subfolder or sibling folder, or in a custom location.
- Audio sample-rate and channel controls, plus reactive video sizing that follows clip orientation.
- Software and hardware encoder definitions, used only when the selected FFmpeg build supports them.
- First-audio-track or all-audio-track selection.
- Importable and exportable Cadroue encoding presets.
- Encoder verification before work is added to the queue.
- Configurable behavior when an output path already exists.

#### Merge

- Create and rename independent merge groups.
- Reorder or remove files within each group.
- Automatically group numbered files such as `Lecture (1).mp4`, `Lecture (2).mp4`, and `Lecture (3).mp4`.
- Use **Strict grouping** to separate a run when a number is missing.
- Use **Loose grouping** to ignore numbering gaps.
- Queue each group as an independent output job.

#### Funnel

- Route loaded files to other tabs according to ordered filename rules.
- Match filenames by contained text, starting text, ending text, or extension.
- Combine filename conditions with AND or OR and choose case-sensitive or case-insensitive matching.
- Use regular expressions against the filename with or without its extension.
- Send each file to the destination assigned to the first matching rule.
- Preserve Funnel rules in the current session and in saved window presets.

#### Worklist

- Persistent, file-backed queue records.
- Normal-priority and high-priority work.
- Configurable parallel processing.
- Start, pause, resume, cancel, stop, remove, clear-done, and clear-all controls.
- Optional automatic resume, retry limits, and pause-on-failure behavior.
- Recovery of work left running after an unexpected shutdown.
- Detailed source, output, encoding, progress, speed, attempt, ownership, and relay information.
- Related jobs grouped by source lineage, including split outputs and relayed work.
- Multiple Worklist tabs, each providing a processing station over the shared queue.

#### Relay, windows, and sessions

- Send completed outputs from one tab directly into another tab's Files list.
- Optionally add relayed inputs to the destination tab's Worklist automatically.
- Keep the destination tab reviewable; ordinary relay does not start it unless automatic relay is enabled there.
- By default, remove each source file from the tab it came from after relay while leaving the destination tab untouched. This behavior can be disabled.
- Prevent a tab from relaying into itself.
- Create multiple tabs of the same type and double-click a tab name to assign a distinct workflow name.
- Drag a tab outside the current window to move it into another Cadroue window or open it in a new Cadroue instance.
- Save, load, import, and export named window presets containing the complete tab arrangement and tab settings.
- Restore the previous tab layout and session at startup.
- Resize or collapse tab panels, hide the Export panel, and choose horizontal or vertical tabs.
- Customize global, active-tab, timeline, and Split-tab keyboard shortcuts.

#### Timeline, workspace, and maintenance

- Show an audio waveform on individual timelines or enable waveforms by default.
- Configure timeline strip order, keyframe spacing and scan delay, section overlap, and map/viewfinder placement.
- Select, import, export, and remove JSON section-colour palettes.
- Keep `.cad` records beside their media or centrally in the workspace.
- Inspect workspace use, clear completed work records, clear workspace records, and automatically remove old completed records while preserving scheduled and running work.

### Preview and processing

Cadroue separates **preview availability** from **FFmpeg processability**. A file may still be processable even when the preview engine cannot display it, and the interface reports these states separately.

The standard preview uses Flyleaf. A locally built Flyleaf variant can optionally be installed from **Options → System** to add contrast preview. FFmpeg remains authoritative for exported output, so a preview may differ slightly from the final encode.

Enabled edit and audio steps are compiled into ordered FFmpeg filter chains. Ordinary combinations are processed in a single FFmpeg export operation rather than being repeatedly rendered through separate intermediate files. Two-pass loudness normalization is the intentional exception because it performs an analysis pass before the final encode.

### Requirements

#### Running Cadroue

- Windows 10 version 2004 / build 19041 or later
- FFmpeg and FFprobe available on `PATH`
- Alternatively, an FFmpeg folder selected under **Options → System**

Published builds are self-contained and do not require a separately installed .NET runtime. FFmpeg and FFprobe are not bundled.

#### Building from source

- Windows
- .NET 10 SDK
- FFmpeg and FFprobe for media inspection and processing

### Getting started

1. Launch Cadroue.
2. Open **Options → System** and confirm the workspace and FFmpeg configuration.
3. Create or select a Split, Edit, Audio, Convert, Merge, Funnel, or Worklist tab.
4. Drop media files or folders into a tab's Files panel.
5. Select a file to preview and configure the operation.
6. Choose export settings or a saved preset where applicable.
7. Use:
   - **Add List** to queue the current work normally;
   - **Add All** to queue eligible loaded files or groups;
   - **Execute** to queue the current work at high priority.
8. Open a Worklist tab and start processing, or enable automatic resume.

Audio-only files can be opened only in an Audio tab. A Funnel tab routes files but does not process media itself.

### Building

From the repository root, create a self-contained Windows build with:

```shell
dotnet publish .\src\Cadroue.UIShell\Cadroue.UIShell.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

To run the development version directly:

```shell
dotnet run --project .\src\Cadroue.UIShell\Cadroue.UIShell.csproj
```

For another Windows architecture, replace `win-x64` in the publish command with the required runtime identifier, such as `win-arm64`.

### Repository structure

```text
src/
├─ Cadroue.Core/            Work records, scheduling contracts, priorities, presets, and shared state models
├─ Cadroue.Application/     Application services and work-item planning
├─ Cadroue.Media/           Media inspection, keyframes, .cad sidecars, and LosslessCut import
├─ Cadroue.Infrastructure/  Persistence, queue storage, detection, interprocess relay, and diagnostics
├─ Cadroue.ShellEngine/     FFmpeg command construction and process execution
└─ Cadroue.UIShell/         WPF application, tabs, panels, preview, options, and localization

localization/
├─ en.json
└─ ko.json

tests/
└─ Cadroue.Tests/           Automated unit and integration tests

docs/                       Generated logic maps
```

### Current limitations

- Cadroue currently targets Windows and has no general command-line interface.
- FFmpeg and FFprobe are not bundled with the normal application build.
- Preview support depends on the media format and the available Flyleaf and FFmpeg libraries.
- Contrast preview requires the optional local Flyleaf build, although exported output still receives the configured contrast adjustment.
- Stream preservation is focused on video and audio; subtitle, attachment, chapter, and complete metadata workflows are not yet the application's primary use case.
- A relay destination is prepared rather than executed unless automatic relay is enabled for that destination.

### Reporting problems

When reporting a bug, include:

- Cadroue version
- Windows version
- FFmpeg build and location
- Source container, video codec, and audio codec
- Exact sequence of tabs and operations
- Whether the problem occurred during preview, queue preparation, relay, or FFmpeg processing
- Relevant entries from the Log window

A small reproducible source file or a redacted `.cad` record is especially useful when the problem depends on a particular operation sequence.

### Technologies

Cadroue is built with:

- C# and .NET 10
- WPF
- FFmpeg and FFprobe
- Flyleaf
- SQLite
- SharpVectors

### License

Cadroue is licensed under the [Mozilla Public License 2.0](https://www.mozilla.org/MPL/2.0/). The MPL-2.0 permits use, modification, and distribution, including in commercial software, while requiring source files derived from MPL-licensed files to remain available under the same license.

Licenses and attribution for bundled libraries and assets, including Flyleaf and Phosphor Icons, are listed in [Third-Party Notices](THIRD-PARTY-NOTICES.md).

---

<a id="korean"></a>

## 한국어

> **개발 상태**  
> Cadroue는 현재도 기능 개선 중입니다. 대체적인 작업 및 기능은 확인되었으나, 문제나 버그가 있을 수 있습니다.
>
> 이 문서는 **2.9.10600** 버전을 기준으로 작성했습니다.

### 소개

Cadroue는 FFmpeg를 사용하여 여러 파일을 단계별로 처리하는 작업 환경을 제공합니다. 파일 별로 분할·편집·오디오 처리 계획을 미리 설정한 다음, 원하는 작업을 실행할 수 있습니다. 급한 작업을 먼저 처리하거나, 앞 단계에서 만든 결과물을 다른 탭으로 전달하는 것도 가능합니다.

이 프로그램은 **분할(Split)**, **편집(Edit)**, **오디오(Audio)**, **변환(Convert)**, **병합(Merge)**, **퍼널(Funnel)**, **작업 목록(Worklist)** 등의 탭으로 구성되어 있습니다. 작업을 준비하는 탭에서 작업 계획을 준비 한 다음, 작업 목록 탭에서 FFmpeg 처리를 실행합니다. 파일 이름 규칙에 따라 알맞은 탭으로 분배하는 기능도 있으며, 같은 종류의 탭을 여러 개 열어 각기 다른 용도로 사용할 수도 있습니다.

파일별 설정은 `.cad` 파일의 형태로 보관되며, 원본 미디어와 동일한 위치에 보관할 지, 혹은 전용 작업 공간에 보관할 지 선택할 수 있습니다. 분석된 키프레임, 분할 구간, 편집 값, 오디오 처리 순서등이 기록되기 때문에 처리 계획을 미리 설정한 후에 나중에 실제 인코딩을 진행할 수도 있습니다.

### 탭별 기능

#### 분할

- 하나의 미디어를 여러 구간으로 나누고 각 구간에 이름을 붙일 수 있습니다.
- 구간을 새로 만들거나 다시 나누고, 순서를 바꾸고, 필요 없는 구간을 끄거나 삭제할 수 있습니다.
- 이전 키프레임, 현재 위치에서 가장 가까운 키프레임, 다음 키프레임으로 이동할 수 있습니다.
- 타임라인을 확대해 세밀하게 탐색할 수 있으며, 오디오 파형 표시와 구간 겹침 허용 여부를 상황에 맞게 바꿀 수 있습니다.
- 찾아낸 키프레임과 분할 계획을 `.cad` 기록에 저장합니다.
- LosslessCut의 `.llc` 프로젝트에 들어 있는 구간을 가져올 수 있습니다.
- 원본 이름, 구간 번호, 구간 이름, 날짜, 시간, 접두사, 접미사 등을 조합해 출력 파일 이름을 만들 수 있습니다.

#### 편집

- 미리보기 화면에서 직접 크롭 영역을 그려 조절할 수 있습니다.
- 지정한 화면비에 맞춰 크롭 영역을 고정할 수 있습니다.
- 90°, 180°, 270° 회전과 가로·세로 뒤집기를 지원합니다.
- 밝기와 대비를 조정할 수 있습니다.
- 파일마다 별도의 편집 계획을 저장할 수도 있고, 선택한 값을 유지한 채 다른 파일을 불러와 같은 설정을 연속해서 적용할 수도 있습니다.

#### 오디오

오디오 처리는 적용 순서가 보이는 단계 목록으로 구성됩니다. 각 단계는 켜고 끌 수 있으며, 순서를 바꾸거나 설정을 확인한 뒤 파일별 계획으로 저장할 수 있습니다.

지원되는 단계는 다음과 같습니다.

- 하이패스 필터
- 로우패스 필터
- 프리셋과 사용자 지정 밴드를 지원하는 파라메트릭 이퀄라이저
- 노이즈 감소
- 볼륨 조정
- 라우드니스 정규화 또는 동적 정규화
- 선택적으로 사용하는 2패스 라우드니스 정규화

정규화 뷰어를 열면 처리 전후의 예상 파형을 확대해 비교할 수 있습니다. 자주 쓰는 설정은 새로 불러오는 파일에도 계속 적용되도록 고정할 수 있습니다. 오디오만 손보는 작업에서는 출력 설정이 허용하는 한 비디오 스트림을 다시 인코딩하지 않고 그대로 복사합니다.

#### 변환과 출력

- 스마트 내보내기, 리먹스 전용, 재인코딩 모드를 제공합니다.
- 비디오와 오디오를 각각 제외하거나 포함할 수 있고, 스트림 복사와 인코딩 여부도 따로 선택할 수 있습니다.
- MP4, Matroska, MOV, WebM, M4A, MP3, WAV, FLAC, OGG를 기본 선택지로 제공하며, 사용 중인 FFmpeg가 지원하는 다른 형식도 선택할 수 있습니다.
- 해상도, 프레임 레이트, 픽셀 형식, 비트레이트·품질 제어 방식과 인코더별 옵션을 설정할 수 있습니다.
- 오디오 샘플 레이트와 채널 수를 지정할 수 있으며, 세로·가로 영상 방향에 맞춰 해상도가 반응하도록 설정할 수 있습니다.
- 결과물은 원본과 같은 위치, 하위 폴더, 원본 폴더 옆의 별도 폴더 또는 사용자가 고른 경로에 저장할 수 있습니다.
- 소프트웨어 인코더와 하드웨어 인코더는 현재 선택된 FFmpeg 빌드가 실제로 지원할 때만 사용됩니다.
- 첫 번째 오디오 트랙만 넣거나 모든 오디오 트랙을 포함할 수 있습니다.
- Cadroue 인코딩 프리셋을 파일로 내보내거나 다시 가져올 수 있습니다.
- 작업을 대기열에 넣기 전에 선택한 인코더를 사용할 수 있는지 확인합니다.
- 같은 경로에 출력 파일이 이미 있을 때 어떻게 처리할지도 설정할 수 있습니다.

#### 병합

- 서로 독립된 병합 그룹을 여러 개 만들고 이름을 붙일 수 있습니다.
- 그룹 안에서 파일 순서를 바꾸거나 항목을 제거할 수 있습니다.
- `Lecture (1).mp4`, `Lecture (2).mp4`, `Lecture (3).mp4`처럼 번호가 이어지는 파일을 자동으로 한 그룹에 묶을 수 있습니다.
- **엄격한 그룹화**에서는 중간 번호가 빠지면 그 지점에서 다른 묶음으로 나눕니다.
- **느슨한 그룹화**에서는 번호가 건너뛰어도 같은 묶음으로 봅니다.
- 각 그룹은 서로 독립된 출력 작업으로 작업 목록에 들어갑니다.

#### 퍼널

퍼널은 파일을 직접 처리하지 않고, 파일 이름을 기준으로 다른 탭에 분배합니다.

- 파일 이름에 특정 글자가 들어가는지, 특정 글자로 시작하거나 끝나는지, 확장자가 무엇인지에 따라 규칙을 만들 수 있습니다.
- 여러 조건을 AND 또는 OR로 묶을 수 있으며 대소문자 구분 여부도 선택할 수 있습니다.
- 확장자를 포함한 전체 파일 이름 또는 확장자를 뺀 이름에 정규식을 적용할 수 있습니다.
- 파일은 위에서부터 처음 일치한 규칙의 대상 탭으로 전달됩니다.
- 규칙은 현재 세션과 창 프리셋에 함께 저장됩니다.

#### 작업 목록

- 대기열은 파일에 저장되므로 프로그램을 닫았다가 다시 열어도 남아 있습니다.
- 일반 우선순위와 높은 우선순위를 구분합니다.
- 동시에 실행할 작업 수를 설정할 수 있습니다.
- 시작, 일시 정지, 재개, 취소, 중지, 선택 항목 제거, 완료 항목 비우기, 전체 비우기를 지원합니다.
- 자동 재개, 실패 작업 재시도 횟수, 실패 시 대기열 일시 정지를 설정할 수 있습니다.
- 프로그램이 예기치 않게 종료되었을 때 실행 중으로 남은 기록을 복구합니다.
- 원본과 출력 경로, 인코딩 설정, 진행률, 처리 속도, 시도 횟수, 어느 탭에서 만든 작업인지, 어디로 전달되는지를 자세히 볼 수 있습니다.
- 분할 결과와 릴레이로 이어진 후속 작업을 같은 원본 기준으로 묶어 보여 줍니다.
- 작업 목록 탭을 여러 개 열 수 있으며, 각 탭은 같은 대기열을 대상으로 별도의 처리 창구처럼 사용할 수 있습니다.

### 작업 이어가기와 창 구성

Cadroue에서는 한 탭에서 끝난 결과물을 다른 탭의 입력 파일로 바로 보낼 수 있습니다. 예를 들어 분할된 파일을 오디오 탭으로 넘긴 뒤, 오디오 처리가 끝난 결과를 병합 탭으로 이어 보낼 수 있습니다.

일반 전달은 대상 탭에 파일을 넣어 검토할 수 있는 상태까지만 준비합니다. 대상 탭에서 **자동 릴레이**를 켜 두면 전달받은 파일을 해당 탭의 작업 목록에 바로 등록할 수 있습니다. 기본 설정에서는 전달이 끝난 원본 파일을 출발 탭의 파일 목록에서 제거하지만, 대상 탭의 기존 파일은 건드리지 않습니다. 이 동작은 옵션에서 끌 수 있으며, 탭이 자기 자신을 전달 대상으로 삼는 것은 허용되지 않습니다.

같은 종류의 탭을 여러 개 만들어 서로 다른 용도로 사용할 수 있습니다. 탭 이름을 두 번 클릭하면 작업 흐름에 맞는 이름을 붙일 수 있습니다. 탭을 창 밖으로 끌어내면 다른 Cadroue 창으로 옮기거나 새 Cadroue 인스턴스에서 열 수 있습니다.

현재 창의 탭 구성과 각 탭의 설정은 **창 프리셋**으로 저장할 수 있습니다. 이름을 붙여 저장한 프리셋을 다시 불러올 수 있고, 파일로 내보내거나 가져올 수도 있습니다. 프로그램을 시작할 때 직전 세션의 탭 구성을 복원하도록 설정하는 것도 가능합니다.

작업 화면의 패널 너비는 직접 조절할 수 있고, 당장 쓰지 않는 패널은 접어 둘 수 있습니다. 출력 패널만 따로 숨기는 것도 가능합니다. 탭 목록은 가로와 세로 배치 중에서 선택할 수 있으며, 전역·현재 탭·타임라인·분할 탭에 쓰는 키보드 단축키도 사용 습관에 맞게 다시 지정할 수 있습니다.

### 타임라인과 작업 공간 관리

타임라인은 단순한 재생 막대에 그치지 않습니다. 패널별로 오디오 파형을 켜거나 끌 수 있고, 새 패널의 기본 표시 여부도 지정할 수 있습니다. 맵과 뷰파인더의 배치 순서, 키프레임 간격과 검색 지연 시간, 구간 겹침 허용 여부를 옵션에서 조정할 수 있습니다. 분할 구간의 색상 팔레트는 JSON 파일로 내보내거나 가져와 다른 환경에서도 재사용할 수 있습니다.

작업 공간에는 대기열 기록과 파일별 `.cad` 기록이 쌓입니다. 옵션 화면에서 사용량을 확인하고, 완료·실패 기록만 골라 정리하거나 작업 공간 기록을 비울 수 있습니다. 오래된 기록을 자동으로 삭제하도록 설정해 두어도 예약되었거나 실행 중인 작업은 정리 대상에서 제외됩니다.

### 미리보기와 실제 출력

미리보기가 되지 않는 파일이라고 해서 반드시 FFmpeg 처리까지 불가능한 것은 아닙니다. Cadroue는 **화면에서 재생할 수 있는지**와 **FFmpeg로 작업할 수 있는지**를 별개의 상태로 판단해 표시합니다.

기본 미리보기 엔진은 Flyleaf입니다. **옵션 → 시스템**에서 로컬 Flyleaf 빌드를 설치하면 대비 조정 미리보기를 추가로 사용할 수 있습니다. 다만 최종 출력은 항상 FFmpeg의 처리 결과를 기준으로 하므로, 미리보기와 완성 파일 사이에는 약간의 차이가 생길 수 있습니다.

활성화된 편집 단계와 오디오 단계는 적용 순서에 맞춰 하나의 FFmpeg 필터 체인으로 구성됩니다. 보통의 조합은 중간 파일을 단계마다 다시 만드는 대신 한 번의 FFmpeg 출력 과정에서 함께 처리합니다. 2패스 라우드니스 정규화만은 먼저 분석한 뒤 최종 출력을 만들어야 하므로 의도적으로 두 번의 패스를 사용합니다.

### 실행 환경

#### Cadroue 실행

- Windows 10 버전 2004 / 빌드 19041 이상
- `PATH`에서 찾을 수 있는 FFmpeg와 FFprobe
- 또는 **옵션 → 시스템**에서 직접 지정한 FFmpeg 폴더

배포용 빌드는 자체 포함 방식이므로 .NET 런타임을 따로 설치할 필요가 없습니다. FFmpeg와 FFprobe는 Cadroue에 포함되어 있지 않습니다.

#### 소스에서 빌드

- Windows
- .NET 10 SDK
- 미디어 분석과 처리를 위한 FFmpeg 및 FFprobe

### 처음 사용하기

1. Cadroue를 실행합니다.
2. **옵션 → 시스템**에서 작업 공간과 FFmpeg 위치를 확인합니다.
3. 필요한 분할, 편집, 오디오, 변환, 병합, 퍼널 또는 작업 목록 탭을 만들거나 선택합니다.
4. 파일 패널에 미디어 파일이나 폴더를 끌어 놓습니다.
5. 파일을 선택해 미리 본 뒤 필요한 작업을 설정합니다.
6. 필요한 경우 출력 설정이나 저장된 프리셋을 선택합니다.
7. 작업을 대기열에 넣습니다.
   - **목록 추가(Add List)**: 현재 항목을 일반 우선순위로 추가합니다.
   - **모두 추가(Add All)**: 현재 탭에서 조건을 충족하는 파일이나 그룹을 한꺼번에 추가합니다.
   - **실행(Execute)**: 현재 항목을 높은 우선순위로 추가합니다.
8. 작업 목록 탭에서 처리를 시작하거나 자동 재개를 켭니다.

오디오만 들어 있는 파일은 오디오 탭에서만 열 수 있습니다. 퍼널 탭은 파일을 분류해 다른 탭으로 보낼 뿐, 자체적으로 인코딩이나 변환을 실행하지 않습니다.

### 빌드

저장소 루트에서 다음 명령을 실행하면 자체 포함 Windows 빌드를 만들 수 있습니다.

```shell
dotnet publish .\src\Cadroue.UIShell\Cadroue.UIShell.csproj --configuration Release --runtime win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

개발 버전을 바로 실행하려면 다음 명령을 사용합니다.

```shell
dotnet run --project .\src\Cadroue.UIShell\Cadroue.UIShell.csproj
```

다른 Windows 아키텍처를 대상으로 하려면 명령의 `win-x64`를 `win-arm64`와 같은 알맞은 런타임 식별자로 바꾸면 됩니다.

### 저장소 구성

```text
src/
├─ Cadroue.Core/            작업 기록, 예약 규칙, 우선순위, 프리셋, 공유 상태 모델
├─ Cadroue.Application/     애플리케이션 서비스와 작업 항목 계획
├─ Cadroue.Media/           미디어 분석, 키프레임, .cad 기록, LosslessCut 가져오기
├─ Cadroue.Infrastructure/  영속화, 대기열 저장, 환경 감지, 프로세스 간 전달, 진단
├─ Cadroue.ShellEngine/     FFmpeg 명령 구성과 프로세스 실행
└─ Cadroue.UIShell/         WPF 애플리케이션, 탭, 패널, 미리보기, 옵션, 현지화

localization/
├─ en.json
└─ ko.json

tests/
└─ Cadroue.Tests/           단위 테스트와 통합 테스트

docs/                       생성된 로직 맵
```

### 현재 제한 사항

- 현재는 Windows만 지원하며, 일반 사용자를 위한 명령줄 인터페이스는 제공하지 않습니다.
- FFmpeg와 FFprobe는 기본 애플리케이션 빌드에 포함되지 않습니다.
- 미리보기 가능 범위는 파일 형식과 사용할 수 있는 Flyleaf·FFmpeg 라이브러리에 따라 달라집니다.
- 대비 값은 최종 출력에 적용되지만, 화면에서 대비 변화를 미리 보려면 선택적으로 설치하는 로컬 Flyleaf 빌드가 필요합니다.
- 스트림 보존 기능은 비디오와 오디오를 중심으로 설계되어 있습니다. 자막, 첨부 파일, 챕터, 전체 메타데이터를 완전하게 유지하는 작업은 아직 주된 사용 범위가 아닙니다.
- 일반 릴레이는 대상 탭에 파일을 전달하는 데까지만 수행합니다. 자동 릴레이를 켜 둔 대상에서만 전달된 입력이 작업 목록에 바로 등록됩니다.

### 문제 보고

문제를 제보할 때 다음 내용을 함께 적어 주시면 원인을 찾는 데 도움이 됩니다.

- Cadroue 버전
- Windows 버전
- 사용한 FFmpeg 빌드와 설치 위치
- 원본 파일의 컨테이너, 비디오 코덱, 오디오 코덱
- 사용한 탭과 작업의 정확한 순서
- 문제가 미리보기, 대기열 준비, 릴레이, FFmpeg 실행 중 어느 단계에서 발생했는지
- 로그 창에서 확인되는 관련 항목

특정 파일이나 작업 순서에서만 문제가 생긴다면, 재현 가능한 작은 미디어 파일이나 개인 정보를 제거한 `.cad` 기록을 함께 제공하는 것이 가장 유용합니다.

### 사용 기술

Cadroue는 다음 기술을 사용합니다.

- C# 및 .NET 10
- WPF
- FFmpeg 및 FFprobe
- Flyleaf
- SQLite
- SharpVectors

### 라이선스

Cadroue는 [Mozilla Public License 2.0](https://www.mozilla.org/MPL/2.0/)에 따라 배포됩니다. MPL-2.0은 상업용 소프트웨어를 포함한 사용, 수정 및 재배포를 허용하며, MPL 라이선스가 적용된 파일에서 파생된 소스 파일은 같은 라이선스로 공개하도록 요구합니다.

Flyleaf와 Phosphor Icons를 포함하여 함께 배포되는 라이브러리 및 에셋의 라이선스와 저작자 표시는 [제3자 고지](THIRD-PARTY-NOTICES.md)에서 확인할 수 있습니다.
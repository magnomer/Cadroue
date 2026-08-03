# Cadroue Logic Maps — Authoring and Rendering Guideline

## 1. Status and authority

This document defines the authoritative format, content rules, visual behavior, validation rules, and maintenance procedure for Cadroue logic maps.

A logic map explains how a program operation proceeds without reproducing its source code. It follows an operation from its initiating event through the methods that govern each step, its decisions, state changes, visible updates, persistence, external processes, and final result.

The compact logic-map source is authoritative. Generated HTML is a disposable visual representation and must never be edited manually.

## 2. Purpose

Logic maps must allow a maintainer to answer all of the following without reading an entire implementation path:

1. What starts this operation?
2. What happens next?
3. Which exact method governs each action?
4. Where does the flow branch, repeat, wait, or run in parallel?
5. Which in-memory, visual, persisted, queue, process, or logging state changes?
6. Where does the operation end?
7. Which other logic maps continue or share the flow?

A logic map is not a class diagram, call graph, sequence diagram, architectural evaluation, or substitute for the source code. It describes operational logic and binds each described action directly to its governing implementation.

## 3. Non-negotiable principles

### 3.1 Keep the action and its governing method together

Every executable action must be paired directly with the method that performs or governs it.

Correct:

```text
- Create the internal section record @ splitCreate.CreateSection(...)
- Add the section to the active item @ sceneTab.AddSection(...)
```

Incorrect:

```text
Actions
- Create the internal section record
- Add the section to the active item

Methods
- splitCreate.CreateSection(...)
- sceneTab.AddSection(...)
```

The incorrect form forces the reader to reconstruct ownership and makes the documentation easier to desynchronize.

### 3.2 Describe logic, not implementation statements

The action text states what the program does. It must not reproduce expressions, assignments, loops, LINQ, WPF markup, or other source statements.

Correct:

```text
- Reject a boundary outside the media duration @ sectionRules.ValidateBoundary(...)
```

Incorrect:

```text
- if (position < 0 || position > duration) return false @ sectionRules.ValidateBoundary(...)
```

### 3.3 Document the present implementation truthfully

Do not reorganize a map to resemble a desired architecture. When one method currently performs validation, mutation, visual refresh, and logging, each responsibility must still be shown as a separate action bound to that same method.

```text
- Validate the requested position @ shell.CreateSection(...)
- Create the section record @ shell.CreateSection(...)
- Refresh the visible section list @ shell.CreateSection(...)
- Mark the project as modified @ shell.CreateSection(...)
```

This repetition is intentional. It reveals actual responsibility without adding an evaluation.

### 3.4 Use the mandatory two-part information architecture

Every complete map belongs to exactly one of these sections:

1. **I. UI** — maps initiated by or directly bound to interface events. UI maps are presented under every tab in which that event can occur: Split, Edit, Convert, Audio, Merge, Funnel, Worklist, or Global interface. A shared control is not hidden in a generic bucket; its map appears in each applicable tab collection.
2. **II. Functionality** — operational logic that is not specific to one tab type. Queue claiming, FFmpeg execution, persistence, media analysis, relay transport, and similar shared operations belong here.

Tab-specific work creation or editing logic must not be placed under Functionality merely because it eventually calls a shared service. The initiating surface determines the UI map; a separate linked Functionality map may document the reusable downstream operation.

### 3.5 One map covers one complete operation

A map normally begins with one user action or internal event and ends when the operation reaches an observable or durable result.

Examples:

- Create a section
- Remove a section
- Import a LosslessCut project
- Add files to the queue
- Claim queued work
- Start FFmpeg processing
- Handle processing completion
- Save a project
- Restore a project

Do not create one enormous map for an entire tab or subsystem.

### 3.6 Generated HTML must preserve action–method proximity

The renderer must show the governing method within the same visual action row as the action. A method may also appear in search results or an inspector, but those secondary views must never replace the visible in-node reference.

### 3.7 The initial view must remain readable

The generated HTML opens at a readable one-to-one scale. It must not automatically shrink an entire map to the viewport when doing so makes card text or implementation references difficult to read. An explicit overview or fit control may temporarily reduce the scale; it is not the default reading mode. When a map is wider or taller than the viewport, the reader pans through it at readable scale.

### 3.8 Connections must use card-free routing corridors

Connections use orthogonal routes through the vertical gaps between node rows or the outer margins of the map. A connection must not pass behind or through a card. The renderer should order nodes to reduce crossings and allocate separate routing lanes when several connections share a row gap. Crossings between connections are acceptable only when the graph cannot be laid out clearly without them; unconstrained curves across the card field are prohibited.

### 3.9 UI-event coverage is code-audited

Every current UI event binding must have a code-bound map. The audited set includes:

- Qualified C# subscriptions such as `button.Click +=`, including Cadroue-defined coordination events such as `PViewerMediaChange`, `PActionRun`, `PFlowSectionChange`, and `LScheduleChange`.
- Bare framework or application subscriptions on the current control or window, such as `PreviewKeyDown +=`, `Closed +=`, `Loaded +=`, and `DispatcherUnhandledException +=`.
- Routed-event registrations through `AddHandler(...)`, including window-level drag/drop routes and delegated list-button clicks.
- XAML event attributes such as `Click="Handler"` or `MouseMove="Handler"`.
- WPF event and lifecycle overrides such as `OnMouseWheel`, `OnPreviewKeyDown`, and `OnSourceInitialized`.

Each code-bound UI map declares an `@event-ref`. Strict generation compares those references with the current `src/Cadroue.UIShell` source. A newly added event without a map, a duplicated event reference, or a map for an event that no longer exists blocks generation.

Coverage is measured from the code, not from a desired map count. The count may grow or shrink with the interface, but complete coverage must always be 100 percent.

## 4. Directory structure

Use the following structure:

```text
docs/
└── logic-maps/
    ├── index.html
    ├── manifest.json
    ├── source/
    │   ├── ui/
    │   │   ├── split/
    │   │   ├── edit/
    │   │   ├── convert/
    │   │   ├── audio/
    │   │   ├── merge/
    │   │   ├── funnel/
    │   │   ├── worklist/
    │   │   ├── global/
    │   │   └── shared/
    │   └── functionality/
    │       ├── application-lifecycle/
    │       ├── media-discovery-and-import/
    │       ├── keyframes-and-waveform/
    │       ├── queue-and-scheduling/
    │       ├── processing-and-ffmpeg/
    │       ├── relay-and-routing/
    │       ├── project-persistence/
    │       ├── preferences-and-tools/
    │       ├── logging/
    │       └── shared-synchronization/
    ├── maps/
    ├── assets/
    └── render/
        ├── generate.py
        ├── generate.ps1
        ├── guideline.md
        ├── site.css
        └── site.js
```

Rules:

- `source/` contains authoritative `.lmap` files.
- UI source files may live under `ui/shared/` when one implementation is used by several tabs, but `@tabs` must enumerate every applicable tab. The generated index presents that map under each named tab.
- `functionality/` contains only operations that are not specific to one tab type.
- `index.html`, `maps/`, `assets/`, and `manifest.json` are generated output.
- `render/generate.py` validates syntax, implementation ownership, UI-event coverage, and generation in one command.
- `render/generate.ps1` is an optional Windows launcher for `generate.py`; it is not a second generator.
- `render/site.css` and `render/site.js` are copied into generated `assets/`.
- Generated files may be deleted and recreated at any time; `source/` and `render/` must never be removed by generation.
- No generated HTML may contain operational information absent from `.lmap` source or verified source-code metadata.

Generate the site from `docs/logic-maps/` with:

```text
python render/generate.py --strict
```

On Windows:

```powershell
.\render\generate.ps1 --strict
```

The generator uses only Python's standard library. `--strict` is required for a completed map set.

## 5. Source format

Logic maps use the line-oriented `.lmap` format. It is deliberately small, dense, readable in a text editor, stable in diffs, and straightforward to parse.

### 5.1 Encoding and general rules

- Encoding: UTF-8 without a required byte-order mark.
- Line endings: either LF or CRLF; the generator normalizes them internally.
- Blank lines are ignored.
- Lines beginning with `#` are comments.
- Indentation is optional and has no semantic meaning.
- Identifiers use lowercase kebab-case or dot-separated lowercase kebab-case.
- Display text uses sentence case.
- Backticks around implementation references are optional in source and must not affect parsing.

### 5.2 Required header

Every complete map begins with section metadata. A code-bound UI event uses:

```text
@format 1
@id ui-event.example
@title Add button — click
@section I. UI
@area UI event
@tabs Split, Edit, Convert, Audio, Merge, Funnel
@event-ref cs|PPanels/PList.cs|PListButtonBuild|pButton|Click|1
@entry input
@summary Handle the add-button click and follow its registered code path.
```

A shared functional operation uses `@section II. Functionality`, a numbered functional `@area`, and no `@tabs`.

Header fields:

| Field | Required | Meaning |
|---|---:|---|
| `@format` | Yes | Logic-map format version. Initially `1`. |
| `@id` | Yes | Globally unique stable identifier. |
| `@title` | Yes | Human-readable title. |
| `@section` | Yes | Exactly `I. UI` or `II. Functionality`. |
| `@area` | Yes | `UI event`, `UI workflow`, or the numbered Functionality group. |
| `@tabs` | UI maps | Comma-separated tabs in which the UI event or workflow can occur. |
| `@event-ref` | Direct UI-event maps | Stable code binding produced from the current C#, XAML, or override event route. |
| `@entry` | Yes | Identifier of the first node. |
| `@summary` | Yes | One-sentence scope statement. |
| `@flow` | No | Preferred primary direction: `TB` or `LR`. Default: `TB`. |
| `@related` | No | Comma-separated related map IDs. |
| `@tag` | No | Search tags. May appear more than once. |
| `@allow-cycle` | No | Map-level permission for an intentional cycle. Default: `false`. |

`@event-ref` forms are generated and validated as follows:

```text
cs|<UIShell-relative .cs file>|<containing method>|<event source>|<event name>|<ordinal>
addhandler|<UIShell-relative .cs file>|<containing method>|<event target>|<routed event>|<handler>|<ordinal>
xaml|<UIShell-relative .xaml file>|<event name>|<handler>|<ordinal>
override|<UIShell-relative .cs file>|<override method>
```

### 5.3 Implementation aliases

Aliases shorten repeated type names while preserving exact ownership.

```text
@alias shell = Cadroue.UIShell.UIShell
@alias splitCreate = Cadroue.Application.LSplitItemsCreate
@alias sceneTab = Cadroue.Core.LSceneTabRecord
```

Rules:

- Alias names are unique within the file.
- Alias values use the fully qualified type name when available.
- The generator displays a shortened reference in the node and exposes the resolved full name in a tooltip or details view.
- Aliases must not hide ambiguity. Two types with the same short name require distinct aliases.
- An alias is not an architectural grouping; it is only a source-writing abbreviation.

### 5.4 Node declaration

A node begins with:

```text
[input] Input <input>
```

The syntax is:

```text
[node-id] Display title <kind>
```

Allowed kinds:

| Kind | Use |
|---|---|
| `input` | User action, command, callback, timer, startup event, or other initiating event. |
| `process` | Interpretation, validation, calculation, creation, transformation, coordination, or state mutation. |
| `decision` | A point with two or more conditional outcomes. |
| `storage` | Persistent or durable in-memory storage work whose role should be visually explicit. |
| `external` | FFmpeg, filesystem, operating system, renderer, network, or another external boundary. |
| `output` | Final visible, in-memory, persisted, scheduled, or emitted result. |
| `error` | Rejection, failure handling, rollback, cleanup after failure, or terminal error. |
| `note` | Explanatory context that is not itself an executable step. Use sparingly. |

A node may contain actions, states, notes, and outgoing connections.

### 5.5 Action line

An executable action uses:

```text
- Action description @ implementation-reference
```

Examples:

```text
- Capture the current playhead position @ shell.GetCurrentPosition(...)
- Create the section record @ splitCreate.CreateSection(...)
- Insert the section into the active tab @ sceneTab.AddSection(...)
```

Rules:

- The action begins with a precise verb.
- The action describes one meaningful responsibility.
- The implementation reference is mandatory.
- The action and implementation reference form one indivisible documentation unit.
- Do not put several unrelated actions on one line.
- Do not use “handle,” “process,” “manage,” or “update” when a more exact verb is available.
- Do not add an evaluation such as “incorrectly,” “too broadly,” or “should instead.” Logic maps record behavior, not criticism.

### 5.6 Multiple implementation references for one action

When one conceptual action is genuinely governed by several methods, continuation references may be attached directly beneath it:

```text
- Rebuild and select the visible section
  @ shell.RebuildSections(...)
  @ shell.SelectSection(...)
```

Use this only when separating the action would misrepresent one indivisible operation. Otherwise create one action line per responsibility.

The renderer must keep all continuation references inside the same action row or action group.

### 5.7 Resulting state line

A state or outcome that is not itself executable uses `=`:

```text
= The section is visible in the active split panel.
= The section exists in the active project model.
= The project is marked as modified.
```

State lines do not require a method because the governing methods must already appear in preceding action lines. Do not use state lines to conceal an undocumented state-changing action.

### 5.8 Note line

A non-executable clarification uses `!`:

```text
! This path is used only when an active media item exists.
```

Notes must be factual, brief, and necessary. They must not become an alternative prose document inside the map.

### 5.9 Unconditional connection

Use `>` to connect the current node to another node:

```text
> validate
```

Optional direction hints:

```text
> visual [left]
> storage [right]
> result [down]
```

Allowed hints are `left`, `right`, `up`, and `down`.

Direction hints are preferences, not absolute coordinates. The renderer may adjust them to prevent overlap, but it should preserve the intended branch relationship whenever possible.

### 5.10 Conditional connection

A conditional connection uses:

```text
? "Valid request" > create
? "Invalid request" > rejected
```

Rules:

- Every outgoing edge from a decision node must have a concise condition label.
- Labels describe the state that selects the branch, not the code expression that tests it.
- Avoid labels such as `true`, `false`, `yes`, and `no` unless their meaning is already explicit in the node title.
- The generated edge label must remain visible without selecting or hovering over the edge.

### 5.11 Parallel flow

Two or more unconditional connections from the same node represent parallel or independently required continuations:

```text
> visual [left]
> storage [right]
```

The branches may rejoin by connecting to the same later node:

```text
[visual] Visual update <process>
- Draw the new section @ shell.DrawSection(...)
> result

[storage] Data state <storage>
- Mark the project as modified @ shell.MarkProjectModified(...)
> result
```

Do not use parallel edges merely to make a map visually symmetrical. They must represent logically separate continuations.

### 5.12 Cycles and repeated paths

A cycle must be intentional and explicit:

```text
@allow-cycle true
```

The returning edge must use the `loop` marker:

```text
> wait-for-work [loop]
```

Without both declarations, generation fails. This prevents accidental cycles from producing unreadable maps.

### 5.13 Shared subflows

Use a map link when a flow continues in another complete map:

```text
> map:project.mark-modified
```

Use a shared fragment when several maps require the same small, stable sequence:

```text
> fragment:shared.mark-project-modified
```

Shared fragments use the same `.lmap` syntax but begin with:

```text
@format 1
@fragment shared.mark-project-modified
@title Mark project as modified
@entry mark
```

Rules:

- Use a complete map link when the referenced operation has its own meaningful entry and result.
- Use a fragment only for a short reusable sequence.
- The generated HTML must visually distinguish a local node, an expanded fragment, and a link to another map.
- Do not copy the same shared flow into many files merely to avoid a reference.
- Do not create fragments for single trivial actions.

## 6. Implementation-reference rules

### 6.1 Preferred reference form

Use a type-qualified method reference:

```text
@ shell.CreateSection(...)
```

The alias resolves to a fully qualified type. The ellipsis means “implementation reference” rather than a literal invocation.

### 6.2 Overloaded methods

When a method name is overloaded and the intended implementation cannot be resolved uniquely, include the parameter types:

```text
@ shell.LoadProject(String, Boolean)
```

Do not include parameter names or generic constraints unless required to remove ambiguity.

### 6.3 Constructors, properties, events, and external operations

Methods are preferred because the map describes governed actions. When another symbol is the actual boundary, use an explicit prefix:

```text
- Create a new scene record @ constructor LSceneTabRecord(...)
- Read the active project path @ property shell.ActiveProjectPath
- Receive the process exit notification @ event LRunner.Exited
- Execute the FFmpeg command @ external ffmpeg
```

Do not label ordinary field reads or incidental property assignments unless they are logically significant to the operation.

### 6.4 Asynchronous methods

Use the actual method name, including `Async` when present:

```text
- Persist the updated project @ depot.SaveAsync(...)
```

The action text describes the logical action. Do not add “asynchronously” unless concurrency or waiting behavior is itself important to the flow.

### 6.5 Lambdas and anonymous handlers

Do not use raw lambda text. Bind the action to the containing named method and append a short location qualifier only when necessary:

```text
- Apply the completion callback @ shell.StartProcessing(...) / completion callback
```

If anonymous logic is substantial enough to require several map actions, it should be given a named implementation method before the map is considered maintainable. The map may temporarily use an unresolved marker, but it must be reported by validation.

### 6.6 Unresolved implementation

Use `@ ?` only during initial investigation:

```text
- Refresh the section labels @ ?
```

The HTML must display unresolved actions prominently, and validation must return a nonzero status in strict mode. A completed map contains no unresolved implementation references.

## 7. Choosing node boundaries

Create a new node when at least one of the following is true:

- Control crosses a meaningful responsibility boundary.
- A decision creates different outcomes.
- State is committed to a different subsystem.
- An external process or filesystem operation begins.
- A failure or rejection path diverges.
- Two logical continuations run independently.
- The current node would otherwise exceed approximately six action rows.

Keep actions in the same node when they form one short, linear responsibility and separating them would add navigation without clarifying the logic.

A node should normally contain one to six action rows. More than eight action rows is a validation warning and usually indicates that the node should be divided.

## 8. Starting and ending a map

### 8.1 Entry

The entry node must identify the actual initiating boundary:

- User clicks a control.
- User changes a field.
- Application starts.
- A file watcher reports a change.
- A queued work item becomes claimable.
- FFmpeg exits.
- A timer or retry interval elapses.
- Another map transfers control to this operation.

Do not begin at an arbitrary internal helper merely because it is convenient to document.

### 8.2 Result

A complete map ends with one or more explicit outcomes:

- UI state is synchronized.
- Project state is changed.
- Data is persisted.
- Work is queued, claimed, completed, cancelled, or failed.
- An output file is verified.
- An error is presented and cleanup is complete.
- Control is transferred to another named map.

Terminal nodes must use the `output` or `error` kind unless they are explicit links to another map.

## 9. Failure, cancellation, and cleanup

Failure paths are first-class logic and must not be reduced to a note.

A map must show a failure branch when the operation can materially change behavior because of:

- Invalid user input
- Missing media or project state
- Unsupported capability
- Filesystem failure
- Process-start failure
- Nonzero FFmpeg exit
- Cancellation
- Timeout
- Stale or conflicting state
- Persistence failure

Where applicable, distinguish:

```text
? "Completed" > verify-output
? "Cancelled" > cancellation-cleanup
? "Failed" > failure-cleanup
```

Cleanup actions must be bound to their governing methods like any other action.

## 10. State categories

The action text should make the affected state clear when ambiguity is possible. Use ordinary language rather than tags in every line, but the following categories guide authors and rendering:

- **UI state**: controls, visible sections, selection, labels, progress, enabled state.
- **Model state**: in-memory project, scene, work, segment, preference, or renderer state.
- **Queue state**: grouping, priority, claim, phase, retry, relay, cancellation.
- **Disk state**: project files, sidecars, schedules, caches, presets, logs.
- **Process state**: FFmpeg, probe, renderer, worker, external tool lifecycle.
- **Logging state**: emitted trace, user-visible log, diagnostic entry.

The renderer may infer and display small state badges from node kinds or explicit metadata, but it must not move an action away from its implementation reference.

## 11. File naming and map identity

Use lowercase kebab-case filenames:

```text
section-creation.lmap
section-removal.lmap
losslesscut-import.lmap
processing-completion.lmap
```

Map IDs combine the area and operation:

```text
split.section-creation
queue.add-files
processing.completion
project.save
```

Rules:

- A map ID remains stable when the title changes.
- Renaming a map ID requires updating every map and fragment reference.
- File location and map area should agree.
- Do not place version numbers in filenames or map IDs.

## 12. Authoring procedure

Follow this order for every map.

### Step 1 — Identify the operation boundary

State the initiating event and the final observable or durable result in one sentence.

### Step 2 — Classify the map

Place an interface-triggered path in `I. UI` and list every applicable tab in `@tabs`. Place a reusable operation in `II. Functionality` only when it is not specific to one tab type.

For a direct UI binding, copy the exact code-derived `@event-ref`; never invent or approximate it.

### Step 3 — Trace the actual implementation

Follow event handlers, commands, coordinators, services, models, persistence, external processes, callbacks, and refresh paths. Do not infer ownership from method names alone.

### Step 4 — List meaningful actions in execution order

Write one exact action per line before arranging boxes.

### Step 5 — Bind every action immediately

Attach the verified governing method or other implementation boundary to the same line.

### Step 6 — Group actions into nodes

Use responsibility, decisions, external boundaries, and state commits to choose node boundaries.

### Step 7 — Add branches, parallel paths, and map links

Use labeled conditional edges and explicit map or fragment references.

### Step 8 — Record terminal states

Use state lines in output or error nodes to state what is true when the path ends.

### Step 9 — Generate and inspect HTML

Check readability, crossings, branch labels, action–method proximity, scrolling, dark mode, and narrow-window behavior.

### Step 10 — Run strict validation

A map set is complete only when required fields, node references, implementation references, terminal paths, and direct UI-event coverage all pass. The console must report complete coverage, such as `UI event coverage: 492/492`; the exact number is determined from the current code.

## 13. HTML generation contract

The generator must create a static visual site that can be opened directly from the filesystem without a web server.

Required output:

```text
docs/logic-maps/index.html
docs/logic-maps/maps/<area>/<map-name>.html
docs/logic-maps/assets/...
```

Generation requirements:

1. Parse every `.lmap` file.
2. Validate syntax and graph integrity before rendering.
3. Resolve aliases.
4. Attempt to verify C# symbols against `src/**/*.cs`.
5. Scan current C#, XAML, and UI overrides and verify complete `@event-ref` coverage.
6. Generate `I. UI` in the fixed tab order and `II. Functionality` in numbered functional groups.
7. Generate one page per unique map, while displaying shared UI maps under every applicable tab.
8. Generate a searchable implementation index.
9. Record a source hash for each generated map.
10. Report errors, warnings, and UI-event coverage in the console; exit nonzero on any validation error.

The generated site must not require a CDN, package installation at viewing time, network access, a local server, or a browser extension. All CSS, JavaScript, icons, and fonts must be local or use system-font fallbacks.

## 14. HTML information architecture

### 14.1 Site shell

The desktop layout uses three clear regions:

1. **Top bar** — site title, current area and map, global search, theme control.
2. **Navigation sidebar** — compact links to the authoritative tab and functionality collections, with map counts. The searchable index holds the complete catalogue rather than repeating hundreds of links on every map page.
3. **Map workspace** — map heading, metadata, visual graph, and optional collapsible details.

A permanent third inspector column is not required. Detailed symbol information may open in a lightweight side sheet or popover so the main graph remains dominant.

### 14.2 Map heading

Each map page shows:

- Title
- Summary
- Section
- Applicable tabs or functionality area
- Source path
- Related maps
- Validation status
- Generated source hash or freshness indicator

Metadata must remain compact and visually subordinate to the graph.

### 14.3 Navigation

The sidebar must:

- Show **I. UI** first, with Split, Edit, Convert, Audio, Merge, Funnel, Worklist, and Global interface in that exact order.
- Show **II. Functionality** second, grouped by numbered `@area`.
- Present a shared UI map under every tab named by `@tabs` on the index.
- Link directly to each complete tab or functionality collection without duplicating the full catalogue inside every map page.
- Keep the current location visible in the top-bar breadcrumb and page metadata.
- Remain usable with keyboard navigation.

### 14.4 Search

Global search must find:

- Map titles and summaries
- Node titles
- Action descriptions
- Aliases and full type names
- Method, constructor, property, event, and external references
- Tags

Selecting a result opens the map and highlights the exact node and action row.

## 15. Visual design standard

The HTML must be straightforward in structure and highly modern in finish. Modernity must come from proportion, typography, spacing, crisp rendering, restrained color, and polished interaction—not decorative complexity.

### 15.1 Prohibited visual approaches

Do not use:

- Skeuomorphic panels
- Heavy gradients
- Neon glows
- Glassmorphism that reduces contrast
- Large decorative illustrations
- Excessive animation
- Rainbow coloring of ordinary nodes
- Thick shadows or oversized borders
- Dense toolbars around the graph
- Method references hidden only in hover states

### 15.2 Page background and surfaces

Use a quiet neutral canvas with elevated white or near-black surfaces.

Recommended light variables:

```text
--page:        #F4F6FA
--surface:     #FFFFFF
--surface-2:   #F8FAFC
--text:        #182033
--muted:       #667085
--border:      #D9E0EA
--edge:        #98A2B3
--focus:       #4F46E5
```

Recommended dark variables:

```text
--page:        #0D1117
--surface:     #151B23
--surface-2:   #1B2430
--text:        #E6EDF3
--muted:       #9AA7B5
--border:      #2C3746
--edge:        #718096
--focus:       #818CF8
```

Exact values may be adjusted as a set, but contrast and semantic consistency must be preserved.

### 15.3 Typography

Use a local system stack:

```text
Inter, "Segoe UI Variable", "Segoe UI", system-ui, sans-serif
```

Use a local monospace stack for implementation references:

```text
"Cascadia Code", "SFMono-Regular", Consolas, monospace
```

Guidance:

- Page title: 24–30 px, semibold.
- Node title: 14–16 px, semibold.
- Action text: 13–14 px, regular or medium.
- Method reference: 11.5–12.5 px, monospace.
- Edge label: 11–12 px, medium.
- Avoid very light font weights.
- Use comfortable line height, approximately 1.4–1.55.

### 15.4 Node cards

A node is rendered as a modern card:

- Width normally 300–380 px; may expand to 520 px for long references.
- Border radius: 12–16 px.
- Border: 1 px neutral.
- Shadow: subtle and diffuse, visible mainly against the page canvas.
- Internal padding: 14–18 px.
- Title separated from actions by spacing or a subtle divider.
- No large filled header bars.
- Node kind shown through a small icon, label, and 3–4 px accent strip.

Recommended semantic accents:

```text
input:     #0EA5E9
process:   #6366F1
decision:  #F59E0B
storage:   #8B5CF6
external:  #64748B
output:    #10B981
error:     #EF4444
note:      #94A3B8
```

Accent color must be used sparingly. The card surface remains neutral.

### 15.5 Action–method rows

Each action and its implementation reference are rendered in one visually bounded row.

Desktop presentation:

```text
Create the internal section record
LSplitItemsCreate.CreateSection(...)
```

The action appears first. The method appears immediately beneath it or aligned at the right when enough width exists. Both remain inside the same row background and spacing boundary.

Requirements:

- Each row has a subtle hover and keyboard-focus state.
- Method text uses monospace and a lightly tinted chip or inset surface.
- Long references wrap or elide with an accessible full-value tooltip.
- The method cannot be moved to a separate legend or inspector.
- Selecting a method highlights every map occurrence without obscuring the current action.
- Multiple references attached to one action appear as a compact vertical stack within that action row.

### 15.6 Edges and arrowheads

Edges must be clean and readable:

- Prefer orthogonal or gently rounded paths.
- Use consistent 1.5–2 px strokes.
- Use compact filled arrowheads.
- Keep sufficient clearance from card borders and text.
- Route around nodes rather than through them.
- Minimize crossings through automatic layout and direction hints.
- Highlight the incoming and outgoing path of the focused node.

Conditional labels appear as compact surface-colored pills placed on the edge. Labels are always visible.

Parallel branches should leave the source node from distinct ports and rejoin cleanly when they share a later node.

### 15.7 Decisions

Decision nodes remain card-shaped for consistency; they do not need a large diamond. Their identity comes from:

- Decision kind label and icon
- Amber accent
- Labeled conditional edges
- Slightly stronger edge-port emphasis

A diamond may be used only when it does not reduce space for the bound action–method rows.

### 15.8 Graph canvas

The graph canvas must provide:

- Fit-to-map on first open
- Zoom controls
- Mouse-wheel or trackpad zoom with sensible limits
- Drag-to-pan when the graph exceeds the viewport
- A reset or fit button
- Clear current zoom percentage
- Optional minimap only for genuinely large maps
- No forced horizontal scrolling for ordinary maps

The graph should open at a readable scale rather than fitting an enormous map into an unreadably small viewport.

### 15.9 Motion

Use restrained transitions of approximately 120–180 ms for:

- Hover emphasis
- Focus movement
- Sidebar expansion
- Theme changes
- Opening a details sheet

Do not animate initial graph layout, continuously pulse nodes, or move edges after the map becomes visible. Honor `prefers-reduced-motion`.

## 16. Responsive behavior

### Desktop

- Persistent navigation sidebar.
- Map canvas uses the remaining width.
- Action and method may align in two columns when the node is wide enough.

### Narrow desktop and tablet

- Sidebar collapses to an overlay or compact rail.
- Nodes keep readable minimum width.
- Action and method stack vertically.
- Direction hints may be relaxed to prevent excessive horizontal scrolling.

### Mobile

Mobile is an inspection fallback, not the primary authoring target.

- Navigation becomes a drawer.
- The graph may use a primarily vertical layout.
- Cards fill most of the viewport width.
- Method references wrap rather than shrink below readable size.
- Pan and zoom controls remain touch accessible.

## 17. Accessibility

Generated HTML must meet the following minimum requirements:

- Keyboard access to navigation, nodes, action rows, search results, theme control, zoom, and details.
- Visible focus rings.
- Text and essential controls meet WCAG AA contrast.
- Color is never the only indicator of node kind, warning, or branch meaning.
- Nodes and edges have accessible labels.
- Screen readers receive the map title, node order, actions, implementation references, and outgoing branch labels.
- Reduced-motion preferences are honored.
- Zooming the browser to 200% does not hide content or controls.

The DOM should preserve a logical reading order independent of the graph coordinates.

## 18. Light mode, dark mode, and printing

- Support light, dark, and system modes.
- Store the viewer's explicit choice locally.
- Ensure all semantic accents remain distinguishable in both themes.
- Provide print CSS that removes navigation and controls, places the title and metadata above the graph, and avoids splitting a node across pages where possible.
- Printed output may simplify shadows and backgrounds but must retain action–method pairing and branch labels.

## 19. Validation rules

### 19.1 Errors that block generation

- Missing required header field
- Invalid `@section` value
- UI map without `@tabs`, or an unknown tab name
- Direct UI event with no map
- Duplicate or stale `@event-ref`
- Duplicate map ID
- Duplicate node ID within a map
- Missing entry node
- Connection to an unknown node, map, or fragment
- Operational action without an implementation reference
- Invalid node kind
- Invalid direction hint
- Conditional edge without a label
- Unmarked cycle
- Fragment with an invalid entry or terminal path
- Malformed alias declaration
- Alias used but not declared
- Syntax that cannot be parsed unambiguously

### 19.2 Strict-mode errors

- `@ ?` unresolved implementation reference
- C# symbol not found
- Ambiguous overloaded method reference
- Referenced source type found in more than one unresolved namespace
- Complete map with no terminal output, error, or map transfer

### 19.3 Warnings

- More than eight action rows in one node
- Node title longer than approximately 48 characters
- Action text longer than approximately 120 characters
- More than four outgoing edges from one node
- Unreachable node
- Related map ID not found
- Repeated identical sequence that may warrant a shared fragment
- Map with excessive total node count
- Direction hints that the renderer cannot honor
- Generated source hash differs from the current source

Warnings do not change the documented behavior and must not be silently promoted into architectural recommendations.

## 20. Console output

The generator must print a compact summary:

```text
Logic maps: 608 parsed, 608 generated
Fragments: 1 parsed
Errors: 0
Warnings: 0
Unresolved symbols: 0
UI event coverage: 492/492
Output: docs/logic-maps/index.html
```

Errors and warnings include the source path and line number.

```text
ERROR source/split/section-creation.lmap:18
Action has no implementation reference.

WARNING source/queue/claim-work.lmap:42
Node "claim-and-persist" contains 10 action rows.
```

## 21. Generated implementation index

The HTML site must include an implementation index that groups verified references by fully qualified symbol.

Example:

```text
Cadroue.Application.LSplitItemsCreate.CreateSection(...)
- Split / Section creation / Create the section record
- Import / LosslessCut import / Convert a segment into a section
```

Requirements:

- Every occurrence links to the exact map node and action row.
- Alias display never prevents access to the full type name.
- Unresolved and ambiguous symbols appear in separate clearly marked groups.
- This index supplements the map; it does not replace in-node method references.

## 22. Freshness and generated-file integrity

Each generated map stores:

- Logic-map source path
- Hash of the source content
- Generation format version
- Generation time
- Optional repository commit identifier when available

On generation, the index reports stale or mismatched output. The HTML must display a discreet warning when its embedded source hash does not match a manifest generated from the current source set.

Generated HTML is never the place to correct a title, action, method, branch, or note. Correct the `.lmap` source and regenerate.

## 23. Complete illustrative source

The following example demonstrates the format. Names are illustrative; production maps must use symbols verified against the current Cadroue source.

```text
@format 1
@id split.section-creation
@title Section creation
@section I. UI
@area UI workflow
@tabs Split
@entry input
@summary Create a section at the current timeline position and synchronize model, view, and modified state.
@flow TB
@related project.mark-modified
@tag section
@tag split

@alias shell = Cadroue.UIShell.SplitView
@alias sectionRules = Cadroue.Application.SectionRules
@alias splitCreate = Cadroue.Application.LSplitItemsCreate
@alias sceneTab = Cadroue.Core.LSceneTabRecord

[input] Input <input>
- Receive the Add Section command @ shell.OnSectionAdd(...)
- Capture the current timeline position @ shell.GetCurrentPosition(...)
> validate

[validate] Validate request <decision>
- Confirm that an active media item exists @ sectionRules.HasActiveMedia(...)
- Validate the requested section boundary @ sectionRules.ValidateBoundary(...)
? "Request is valid" > create
? "Request is invalid" > rejected

[create] Section creation <process>
- Create the internal section record @ splitCreate.CreateSection(...)
- Insert the section into the active tab @ sceneTab.AddSection(...)
> visual [left]
> storage [right]

[visual] Visual synchronization <process>
- Draw the new section in the split panel @ shell.DrawSection(...)
- Select the new section @ shell.SelectSection(...)
> result

[storage] Data state <storage>
- Mark the active project as modified @ shell.MarkProjectModified(...)
> result

[rejected] Rejected request <error>
- Present the boundary rejection to the user @ shell.ShowSectionError(...)
= No section is created.

[result] Result <output>
= The new section is visible.
= The new section exists in the active project model.
= The project is marked as modified.
```

The corresponding HTML must show:

- Input above validation.
- Validation with two permanently labeled outgoing branches.
- Creation followed by left and right branches for visual and data-state synchronization.
- Every action immediately paired with its implementation reference.
- Rejected and successful terminal states visually distinct but structurally consistent.

## 24. Map review checklist

A map is ready only when every answer below is yes.

### Scope

- Does the map cover one identifiable operation?
- Does it begin at the real initiating event?
- Does every path reach a result, failure, or named continuation?

### Logic

- Are actions ordered according to actual execution or actual logical dependency?
- Are all meaningful decisions shown?
- Are branch labels descriptive rather than code-like?
- Are parallel paths genuinely independent?
- Are cancellation and cleanup represented where material?

### Ownership

- Is every executable action directly paired with its governing method or exact implementation boundary?
- Are aliases resolvable to full type names?
- Are overloaded methods unambiguous?
- Are unresolved references absent from a completed map?

### Content quality

- Does each action describe one precise responsibility?
- Is actual code omitted?
- Is the current implementation represented without evaluation or redesign?
- Is repeated shared logic linked rather than copied excessively?

### HTML

- Are action and method visible together inside the same row?
- Are labels readable at the initial zoom?
- Are branches and arrow directions immediately understandable?
- Are crossings minimized?
- Does the page remain straightforward despite its modern presentation?
- Does it work offline, in light and dark mode, with keyboard navigation, and at narrow widths?

## 25. Governing rule

When a choice must be made between visual elegance and logical clarity, logical clarity wins. When a choice must be made between compact source and explicit action ownership, explicit ownership wins. The generated HTML may simplify navigation and presentation, but it must never separate what the program does from the method that governs it.

# LMAP Format 1 Specification

## 1. Scope and authority

LMAP is Cadroue's UTF-8, line-oriented source format for logic maps. An `.lmap` file is authoritative graph source; generated HTML is only a presentation of that graph.

Format 1 has a closed grammar. A generator must reject unknown directives, unsupported format numbers, malformed identifiers, invalid topology, and syntax not defined by this document. No manifest, registry, hard-coded source-file list, or generated source catalogue is part of LMAP. Source discovery is recursive below `docs/LogicMaps/source/`.

## 2. File classes and document identifiers

Every `.lmap` file must declare exactly one of:

- `@id <document-id>` — complete map;
- `@fragment <document-id>` — shared fragment.

Map IDs and fragment IDs share one global namespace. A document ID therefore cannot identify both a map and a fragment.

Document IDs use:

```text
[a-z][a-z0-9-]*(.[a-z][a-z0-9-]*)*
```

Examples:

```text
functionality.media-loading
shared.preview-fallback
```

## 3. Lexical rules

- Files are UTF-8. UTF-8 BOM is accepted but not required.
- LF and CRLF are accepted.
- Blank lines are ignored.
- A line whose first non-whitespace character is `#` is a comment.
- Indentation has no semantic meaning.
- Directives and aliases must appear before the first node declaration.
- `@tag` may repeat. Other headers may occur only once.
- Unknown directives are errors.
- Node IDs use lowercase kebab-case:

```text
[a-z][a-z0-9-]*
```

- Alias names use:

```text
[A-Za-z][A-Za-z0-9-]*
```

## 4. Format declaration

Every source file must contain exactly:

```text
@format 1
```

Any other value is unsupported and is an error.

## 5. Complete-map preamble

A complete map requires exactly these fields:

```text
@format 1
@id functionality.example
@title Example operation
@section Functionality
@area Example area
@entry start
@summary Trace the example operation from initiation to result.
```

Required complete-map headers:

| Header | Meaning |
|---|---|
| `@format` | Must be `1`. |
| `@id` | Global LMAP document ID. |
| `@title` | Human-readable map title. |
| `@section` | Top-level generated navigation section. |
| `@area` | Navigation group when `@tabs` is absent. |
| `@entry` | Comma-separated local root-node IDs. |
| `@summary` | Concise map scope. |

Optional complete-map headers:

| Header | Meaning |
|---|---|
| `@tabs` | Comma-separated navigation groups. If present, the map is listed under each group instead of only `@area`. |
| `@event-ref` | One exact verified UI-event binding. Requires `@tabs`. |
| `@related` | Comma-separated existing map/fragment document IDs. |
| `@tag` | Repeatable search metadata. |

No other headers are valid on a complete map. In particular, Format 1 has no `@flow` directive; map layout is top-to-bottom.

## 6. Shared-fragment preamble

A fragment is a complete reusable graph document, not an opaque label. It requires:

```text
@format 1
@fragment shared.example
@title Shared example
@entry start
@summary Perform the reusable example flow.
```

Required fragment headers are `@format`, `@fragment`, `@title`, `@entry`, and `@summary`.

Optional fragment headers are `@related` and repeatable `@tag`.

A fragment cannot declare `@section`, `@area`, `@tabs`, or `@event-ref`. Fragments obey the same node, edge, reference, crossing, junction, cycle, and reachability rules as complete maps.

Generation must produce an inspectable HTML graph for every fragment. A `fragment:` continuation links to that generated graph; it does not replace the fragment with an unexplained endpoint. Fragment actions are included in the implementation index.

Generated fragment pages use:

```text
docs/LogicMaps/maps/fragments/<fragment-id components>.html
```

For example, `shared.preview-fallback` becomes `fragments/shared/preview-fallback.html` below the generated maps root.

## 7. Aliases

An implementation alias maps a short source name to a fully-qualified C# type:

```text
@alias viewer = Cadroue.UIShell.PPanels.PViewer
@alias media-load = Cadroue.Media.LMediaLoad
```

Syntax:

```text
@alias <alias-name> = <namespace>.<type>
```

Aliases are file-local, unique, and preamble-only. The right side must be a dotted C# identifier path. In strict mode, referenced types and members must exist in `src/**/*.cs` where the source validator can resolve them.

## 8. Nodes

Node declaration:

```text
[node-id] Display title <kind>
```

Allowed kinds:

| Kind | Meaning |
|---|---|
| `input` | Initiating user action, command, startup action, callback, delivery, or equivalent trigger. |
| `process` | Processing, coordination, transformation, interpretation, or state-changing step. |
| `decision` | Explicit multi-way conditional branch. |
| `storage` | Storage or retained-state operation that must be visible. |
| `external` | External boundary such as filesystem, FFmpeg, ffprobe, OS, or renderer. |
| `output` | Terminal or resulting state/output. |
| `error` | Rejection/failure state or terminal error. |
| `note` | Non-executable explanatory graph content. |
| `junction` | Topology-only convergence/routing point. It is not a process card. |

Node IDs must be unique within the file.

### 8.1 Decision invariant

A `decision` must have at least two outgoing edges. Every outgoing edge must be conditional, and condition labels within that decision must be unique.

### 8.2 Note invariant

A `note` may contain states and notes, but cannot contain executable actions.

### 8.3 Junction invariant

A `junction`:

- cannot be an `@entry`;
- cannot contain actions, states, or notes;
- must have exactly one unconditional outgoing edge;
- may receive multiple incoming local edges.

A non-junction node may not have multiple incoming local edges. Shared paths must converge explicitly through a junction before reaching the common operation.

Example:

```text
[a] A <process>
- Do A @ owner.A(...)
> join

[b] B <process>
- Do B @ owner.B(...)
> join

[join] Paths converge <junction>
> result

[result] Result <output>
= Complete.
```

## 9. Node body syntax

### 9.1 Action

```text
- Action description @ implementation-reference
```

An action must have a nonempty implementation reference.

### 9.2 Additional implementation owner

Immediately following an action:

```text
  @ additional-reference
```

A continuation reference without a preceding action is invalid.

### 9.3 State

```text
= Resulting state text.
```

### 9.4 Note text

```text
! Factual clarification.
```

## 10. Implementation-reference grammar

`?` is the only unresolved-reference token. In strict mode it is an error; otherwise it is an unresolved warning.

A normal callable reference is:

```text
<alias>.<member>(...)
```

The descriptors `external` and `constructor` use the same callable form:

```text
external <alias>.<member>(...)
constructor <alias>.<member>(...)
```

The descriptors `property` and `event` use a non-call form:

```text
property <alias>.<member>
event <alias>.<member>
```

An optional location qualifier may follow a valid reference:

```text
<reference> / <qualifier>
```

A qualifier is nonempty and may contain letters, digits, spaces, and `_.:#()+-/`. A complete reference may be surrounded by one pair of backticks. Unbalanced backticks, trailing undeclared syntax, missing aliases, missing required `(...)`, or `(...)` on `property`/`event` references are errors.

The validator resolves the alias and member only after the complete reference matches this grammar; it must not accept a valid-looking prefix while silently ignoring trailing text.

## 11. Edges

### 11.1 Unconditional local edge

```text
> target-node
```

### 11.2 Conditional local edge

```text
? "Accepted" > accepted-path
? "Rejected" > rejected-path
```

### 11.3 Complete-map continuation

```text
> map:functionality.other-map
```

The map ID must exist.

### 11.4 Fragment continuation

```text
> fragment:shared.example
```

The fragment ID must exist.

Routing markers are valid only on local node edges, never on `map:` or `fragment:` continuations.

## 12. Routing markers

Format 1 supports exactly three markers:

```text
> target [left]
> target [right]
> earlier-step [loop]
```

`[left]` and `[right]` request a real outer-side detour in generated layout. They do not remove the edge from source-order crossing validation and cannot be used to legitimize bad same-rank ordering.

A non-loop local edge that skips one or more derived ranks must declare `[left]` or `[right]`. This makes the bypass explicit in source rather than letting the renderer invent a long route.

`[loop]` has cycle semantics, not merely visual semantics; see §14.

`[up]` and `[down]` are not part of Format 1 and are errors.

## 13. Entry roots and derived rank

`@entry` is a comma-separated list of unique local node IDs:

```text
@entry browse, drop, startup-restore
```

Every entry must exist, cannot be a junction, and cannot have an incoming local edge.

For layout validation, derived rank is the longest path distance from any entry after `[loop]` edges are removed. Declaration order is the authoritative preferred left-to-right order among nodes of the same derived rank.

## 14. Cycles and `[loop]`

Cycles must be explicit.

The graph obtained by removing every `[loop]` edge must be acyclic. Therefore, an ordinary unmarked/left/right edge may not participate in a remaining directed cycle.

Every `[loop]` edge must actually close an existing local path: after all `[loop]` edges are removed, there must be a path from the loop edge's target back to its source. A `[loop]` marker on an edge that does not close such a path is invalid.

This makes `[loop]` the declared cycle-closing edge and gives the renderer an unambiguous non-primary route. If a loop returns to a flow point that already has a primary incoming edge, the loop and primary path must converge through a `junction`; the ordinary no-implicit-fan-in rule still applies.

## 15. Crossing and convergence discipline

LMAP source must eliminate avoidable crossings before HTML routing.

The validator enforces these structural rules:

1. shared incoming paths must use an explicit junction;
2. the non-loop primary graph must be acyclic;
3. long rank-skipping edges must explicitly detour left or right;
4. all adjacent-rank edges—including `[left]`/`[right]` edges—are checked against source declaration order for pairwise inversions;
5. an inversion that can be removed by same-rank ordering is an error and cannot be waived with a routing marker.

Correction priority is:

1. reorder same-rank nodes;
2. represent genuine convergence with a junction;
3. represent genuine bypasses with `[left]` or `[right]`;
4. represent genuine cycles with `[loop]`;
5. only then may the renderer handle residual geometric conflicts.

The validator's source-order check is a layered structural invariant, not a mathematical proof that an arbitrary directed graph is planar. Generated rendering must still minimize residual crossings. If a residual unrelated crossing is unavoidable after the source invariants and available routing alternatives are exhausted, it must be visually distinguished from a true merge; a renderer must never make an unrelated crossing look like a junction.

## 16. Reachability

Starting from every declared entry, the validator follows local edges, including `[loop]` edges. Any local node not reachable from an entry produces a warning.

`map:` and `fragment:` references are validated as document continuations and are not traversed as local nodes.

## 17. UI-event binding

`@event-ref` is optional and valid only on complete maps. A map with `@event-ref` must also declare `@tabs`.

Format 1 accepts exactly these forms, where `N` is a positive 1-based occurrence number:

```text
cs|<relative.cs>|<method>|<target>|<event>|N
addhandler|<relative.cs>|<method>|<target>|<routed-event>|<handler>|N
xaml|<relative.xaml>|<event>|<handler>|N
override|<relative.cs>|<method>
```

`<method>`, `<event>`, and `<handler>` are C# identifiers. `<target>` and `<routed-event>` may be dotted identifiers. The path is relative to `src/Cadroue.UIShell/` and must have the shown extension.

Malformed references, duplicate references, and references no longer found by source inspection are errors.

`@event-ref` is not a coverage registry. The generator must not require every UI event in the application to have a logic map and must not generate UI-event coverage counters.

## 18. `@related`

`@related` contains comma-separated global LMAP document IDs. Each value must satisfy the document-ID grammar. A missing target is a warning.

Because maps and fragments share one document-ID namespace, an unprefixed `@related` value is unambiguous.

## 19. Navigation and source discovery

The generator recursively discovers every `*.lmap` below `docs/LogicMaps/source/`.

Complete-map navigation is derived from source metadata:

- `@section` → top-level section;
- `@tabs`, when present → one or more navigation groups;
- otherwise `@area` → navigation group;
- `@title` → displayed title;
- `@summary` and `@tag` → search metadata.

Fragments are presented as shared fragments and link to their generated graph pages.

No map filename, map ID, fragment ID, or source catalogue may be hard-coded into the generator. No `manifest.json` or equivalent registry may be created or maintained.

## 20. Validation severity

Generation fails when errors exist. Errors include, among other things:

- unsupported `@format`;
- unknown, duplicate, or class-inappropriate headers;
- invalid or duplicate document/node/alias identifiers;
- map/fragment document-ID collision;
- malformed aliases or implementation references;
- unknown local/map/fragment targets;
- invalid node kinds or routing markers;
- invalid decision, note, or junction topology;
- implicit convergence into a normal node;
- invalid entries;
- cycles not explicitly closed by `[loop]`;
- meaningless `[loop]` edges;
- long unhinted rank-skipping edges;
- avoidable adjacent-rank crossings;
- malformed, duplicate, or stale `@event-ref` values;
- unresolved implementation ownership in strict mode.

Warnings include unreachable nodes, unresolved implementation ownership in non-strict mode, and missing `@related` targets.

## 21. Generation contract

`docs/LogicMaps/render/generate.py` owns validation and HTML generation. `generate.ps1` is only a launcher/helper and does not implement HTML-generation logic.

The generator uses the Python standard library and does not specify a fixed Python installation location.

Source and generator files:

```text
docs/LogicMaps/
├── SpecificationLmap.md
├── source/
└── render/
```

Generated output:

```text
docs/
├── MapsLogic.html
└── LogicMaps/
    ├── assets/
    │   ├── site.css
    │   └── site.js
    └── maps/
        ├── NavigationLogic.html
        ├── ImplementationIndex.html
        ├── fragments/
        │   └── ...
        └── <source-relative complete-map path>.html
```

`MapsLogic.html` is the only generated HTML outside `docs/LogicMaps/maps/`.

Shared navigation is generated once and referenced by content pages rather than embedded into every page. Shared CSS/JS are generated once under `docs/LogicMaps/assets/`; common styles/scripts must not be copied inline into each HTML page.

Adding, removing, or renaming source documents updates shared navigation/index output without requiring unchanged content pages to be rewritten.

## 22. Minimal complete-map example

```text
@format 1
@id functionality.example-load
@title Example load
@section Functionality
@area Example
@entry browse, drop
@summary Load an example source from either supported initiator.
@tag example

@alias owner = Cadroue.Application.ExampleOwner

[browse] Browse <input>
- Accept the browsed source @ owner.Browse(...)
> request-join

[drop] Drop <input>
- Accept the dropped source @ owner.Drop(...)
> request-join

[request-join] Requests converge <junction>
> validate

[validate] Validate source <decision>
- Validate the requested path @ owner.Validate(...)
? "Accepted" > load
? "Rejected" > rejected

[load] Load source <process>
- Load the accepted source @ owner.Load(...)
> complete

[rejected] Request rejected <output>
= No load is started.

[complete] Load complete <output>
= The source is available.
```

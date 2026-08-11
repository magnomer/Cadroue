#!/usr/bin/env python3
"""Validate Cadroue .lmap files and generate the offline logic-map site.

This program intentionally uses only the Python standard library.  Run it from
any directory with:  python render/generate.py --strict
"""

from __future__ import annotations

import argparse
import hashlib
import html
import json
import os
import re
import sys
from collections import defaultdict, deque
from dataclasses import dataclass, field
from pathlib import Path

RENDER_ROOT = Path(__file__).resolve().parent
MAP_ROOT = RENDER_ROOT.parent
SOURCE_ROOT = MAP_ROOT / "source"
DOCS_ROOT = MAP_ROOT.parent
DEFAULT_MAP_ROOT = MAP_ROOT
CODE_ROOT = DOCS_ROOT.parent / "src"
KINDS = {"input", "process", "decision", "storage", "external", "output", "error", "note", "junction"}
EDGE_MARKERS = {"loop"}
FORMAT_VERSION = "1"
MAP_ID_RE = re.compile(r"^[a-z][a-z0-9-]*(?:\.[a-z][a-z0-9-]*)*$")
NODE_ID_RE = re.compile(r"^[a-z][a-z0-9-]*$")
ALIAS_TARGET_RE = re.compile(r"^[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)+$")
MAP_REQUIRED_HEADERS = {"format", "id", "title", "section", "area", "entry", "summary"}
MAP_OPTIONAL_HEADERS = {"tabs", "event-ref", "related"}
FRAGMENT_REQUIRED_HEADERS = {"format", "fragment", "title", "entry", "summary"}
FRAGMENT_OPTIONAL_HEADERS = {"related"}
KNOWN_HEADERS = MAP_REQUIRED_HEADERS | MAP_OPTIONAL_HEADERS | FRAGMENT_REQUIRED_HEADERS | FRAGMENT_OPTIONAL_HEADERS | {"tag"}
UI_EVENT_NAMES = set("""Click MouseLeftButtonDown MouseLeftButtonUp MouseRightButtonDown MouseRightButtonUp MouseMove MouseEnter MouseLeave MouseWheel PreviewMouseMove PreviewMouseLeftButtonDown PreviewMouseLeftButtonUp PreviewMouseWheel PreviewMouseDown PreviewMouseUp PreviewMouseRightButtonDown PreviewMouseRightButtonUp LostMouseCapture KeyDown PreviewKeyDown KeyUp PreviewTextInput TextChanged SelectionChanged Selected Unselected Checked Unchecked ValueChanged DragStarted DragDelta DragCompleted DragEnter DragOver DragLeave Drop GiveFeedback GotFocus LostFocus LostKeyboardFocus Loaded Unloaded Closed Closing SizeChanged IsVisibleChanged LayoutUpdated DropDownOpened DropDownClosed StatusChanged Deactivated ContextMenuOpening ContextMenuClosing RequestNavigate SeekCompleted ScrollChanged""".split())


def html_root() -> Path:
    """Return the generated-HTML root below the currently configured site root."""
    return MAP_ROOT / "maps"


@dataclass
class Action:
    text: str
    references: list[str]
    line: int


@dataclass
class Edge:
    target: str
    label: str
    marker: str | None
    conditional: bool
    line: int


@dataclass
class Node:
    id: str
    title: str
    kind: str
    line: int
    explanation: str = ""
    technical_label: str = ""
    actions: list[Action] = field(default_factory=list)
    states: list[str] = field(default_factory=list)
    notes: list[str] = field(default_factory=list)
    edges: list[Edge] = field(default_factory=list)


@dataclass
class LogicMap:
    path: Path
    raw: str
    headers: dict[str, str] = field(default_factory=dict)
    tags: list[str] = field(default_factory=list)
    aliases: dict[str, str] = field(default_factory=dict)
    nodes: list[Node] = field(default_factory=list)


class Reporter:
    def __init__(self, strict: bool) -> None:
        self.strict = strict
        self.errors: list[str] = []
        self.warnings: list[str] = []
        self.unresolved = 0

    def issue(self, level: str, path: Path, line: int, message: str) -> None:
        record = f"{level} {path}:{line}\n{message}"
        (self.errors if level == "ERROR" else self.warnings).append(record)

    def unresolved_reference(self, source: LogicMap, action: Action) -> None:
        self.unresolved += 1
        self.issue("ERROR" if self.strict else "WARNING", source.path, action.line,
                   "Unresolved implementation reference.")


def parse_map(path: Path, reporter: Reporter) -> LogicMap:
    source = LogicMap(path, path.read_text(encoding="utf-8-sig"))
    current: Node | None = None
    last_action: Action | None = None
    for number, original in enumerate(source.raw.splitlines(), 1):
        line = original.strip()
        if not line or line.startswith("#"):
            continue

        if current is None and line.startswith("@alias"):
            alias = re.fullmatch(r"@alias\s+([A-Za-z][A-Za-z0-9-]*)\s*=\s*(\S.+)", line)
            if alias is None:
                reporter.issue("ERROR", path, number, "Malformed @alias directive.")
                continue
            name, target = alias.group(1), alias.group(2).strip()
            if name in source.aliases:
                reporter.issue("ERROR", path, number, f"Duplicate alias '{name}'.")
            elif not ALIAS_TARGET_RE.fullmatch(target):
                reporter.issue("ERROR", path, number, f"Alias '{name}' has an invalid fully-qualified C# type: {target}")
            else:
                source.aliases[name] = target
            continue

        if current is None and line.startswith("@"):
            header = re.fullmatch(r"@([a-z][a-z0-9-]*)\s+(.+)", line)
            if header is None:
                reporter.issue("ERROR", path, number, f"Malformed directive: {line}")
                continue
            key, value = header.group(1), header.group(2).strip()
            if key not in KNOWN_HEADERS:
                reporter.issue("ERROR", path, number, f"Unknown directive '@{key}'.")
                continue
            if key == "tag":
                source.tags.append(value)
            elif key in source.headers:
                reporter.issue("ERROR", path, number, f"Duplicate header '@{key}'.")
            else:
                source.headers[key] = value
            continue

        declaration = re.fullmatch(r"\[([^]]+)]\s+(.+?)\s+<([a-z]+)>", line)
        if declaration:
            current = Node(*declaration.groups(), number)
            source.nodes.append(current)
            last_action = None
            continue
        if current is None:
            reporter.issue("ERROR", path, number, "Content appears outside a node.")
            continue

        # Inside a node, only a continuation implementation reference may begin
        # with '@ '. Header and alias directives are preamble-only.
        if line.startswith("@") and not line.startswith("@ "):
            reporter.issue("ERROR", path, number, "Directives must appear before the first node declaration.")
            continue
        if line == "[Technical explanation]":
            if current.kind == "junction":
                reporter.issue("ERROR", path, number, f"Junction '{current.id}' cannot contain a technical explanation block.")
            elif current.technical_label:
                reporter.issue("ERROR", path, number, f"Node '{current.id}' has more than one technical explanation block.")
            elif not current.explanation:
                reporter.issue("ERROR", path, number, f"Node '{current.id}' must place its simple explanation before [Technical explanation].")
            else:
                current.technical_label = "Technical explanation"
            last_action = None
        elif line.startswith("~"):
            explanation = line[1:].strip()
            if current.technical_label:
                reporter.issue("ERROR", path, number, "Simple explanation must appear before [Technical explanation].")
            elif not explanation:
                reporter.issue("ERROR", path, number, "Simple explanation is empty.")
            elif current.explanation:
                reporter.issue("ERROR", path, number, f"Node '{current.id}' has more than one simple explanation.")
            else:
                current.explanation = explanation
            last_action = None
        elif line.startswith("-"):
            if not current.technical_label:
                reporter.issue("ERROR", path, number, "Action must appear inside an explicit [Technical explanation] block.")
            parts = re.split(r"\s+@\s+", line[1:].strip(), maxsplit=1)
            if len(parts) != 2 or not parts[1].strip():
                reporter.issue("ERROR", path, number, "Action has no implementation reference.")
                continue
            last_action = Action(parts[0].strip(), [parts[1].strip()], number)
            current.actions.append(last_action)
        elif line.startswith("@ "):
            if last_action is None:
                reporter.issue("ERROR", path, number, "Continuation implementation reference is detached from an action.")
            else:
                last_action.references.append(line[2:].strip())
        elif line.startswith("="):
            if not current.technical_label:
                reporter.issue("ERROR", path, number, "State must appear inside an explicit [Technical explanation] block.")
            current.states.append(line[1:].strip())
        elif line.startswith("!"):
            if not current.technical_label:
                reporter.issue("ERROR", path, number, "Note must appear inside an explicit [Technical explanation] block.")
            current.notes.append(line[1:].strip())
        else:
            conditional = re.fullmatch(r'\?\s+"([^"]+)"\s+>\s+(\S+)(?:\s+\[([a-z-]+)])?', line)
            direct = re.fullmatch(r">\s+(\S+)(?:\s+\[([a-z-]+)])?", line)
            if conditional:
                current.edges.append(Edge(conditional.group(2), conditional.group(1), conditional.group(3), True, number))
            elif direct:
                current.edges.append(Edge(direct.group(1), "", direct.group(2), False, number))
            else:
                reporter.issue("ERROR", path, number, f"Syntax cannot be parsed: {line}")
    return source


def csharp_symbols(code_root: Path) -> dict[str, set[str]]:
    """Return members declared by each fully-qualified C# type.

    The validator distinguishes multiple top-level types in one file and skips
    positional records that end with a semicolon. It remains a pragmatic source
    validator rather than a substitute for compilation.
    """
    result: dict[str, set[str]] = defaultdict(set)
    namespace_re = re.compile(r"\bnamespace\s+([A-Za-z_][\w.]*)\s*;")
    type_re = re.compile(r"\b(?:class|record(?:\s+(?:class|struct))?|struct|interface)\s+([A-Za-z_]\w*)\b")
    symbol_re = re.compile(r"(?:\b(?:public|private|protected|internal|static|virtual|override|abstract|sealed|async|partial|new|extern|unsafe|readonly)\s+)*[\w<>,?\[\]. ]+\s+([A-Za-z_]\w*)\s*(?:\(|=>|\{)")
    for file in code_root.rglob("*.cs"):
        text = file.read_text(encoding="utf-8")
        masked = mask_csharp(text)
        namespace = namespace_re.search(masked)
        if namespace is None:
            continue
        declarations = list(type_re.finditer(masked))
        for position, found in enumerate(declarations):
            next_type = declarations[position + 1].start() if position + 1 < len(declarations) else len(masked)
            opening = masked.find("{", found.end(), next_type)
            semicolon = masked.find(";", found.end(), next_type)
            full_name = f"{namespace.group(1)}.{found.group(1)}"
            result.setdefault(full_name, set())
            if opening < 0 or (semicolon >= 0 and semicolon < opening):
                continue
            depth, end = 0, None
            for index in range(opening, len(masked)):
                if masked[index] == "{":
                    depth += 1
                elif masked[index] == "}":
                    depth -= 1
                    if depth == 0:
                        end = index
                        break
            if end is None:
                continue
            body = masked[opening + 1:end]
            result[full_name].update(match.group(1) for match in symbol_re.finditer(body))
    return result


def mask_csharp(text: str) -> str:
    """Mask comments and string/character literals while preserving offsets."""
    output = list(text)
    index = 0
    while index < len(text):
        if text.startswith("//", index):
            end = text.find("\n", index)
            end = len(text) if end < 0 else end
            for position in range(index, end):
                output[position] = " "
            index = end
            continue
        if text.startswith("/*", index):
            end = text.find("*/", index + 2)
            end = len(text) - 2 if end < 0 else end
            for position in range(index, min(end + 2, len(text))):
                if output[position] != "\n":
                    output[position] = " "
            index = min(end + 2, len(text))
            continue
        if text[index] in {'"', "'"}:
            quote = text[index]
            verbatim = quote == '"' and index > 0 and text[index - 1] == "@"
            end = index + 1
            while end < len(text):
                if verbatim and text.startswith('""', end):
                    end += 2
                    continue
                if not verbatim and text[end] == "\\":
                    end += 2
                    continue
                if text[end] == quote:
                    end += 1
                    break
                end += 1
            for position in range(index, min(end, len(text))):
                if output[position] != "\n":
                    output[position] = " "
            index = end
            continue
        index += 1
    return "".join(output)


def csharp_methods(text: str) -> list[tuple[str, str, str]]:
    """Return method name, original text, and masked text for each method body."""
    masked = mask_csharp(text)
    pattern = re.compile(
        r"(?m)^[ \t]*(?:(?:public|private|protected|internal|static|virtual|override|abstract|sealed|async|partial|new|extern|unsafe|readonly)\s+)*"
        r"(?:[A-Za-z_][\w<>,.?\[\] ]*\s+)?(?P<name>[A-Za-z_]\w*)\s*\([^;{}]*\)\s*(?P<tail>\{|=>)")
    controls = {"if", "for", "foreach", "while", "switch", "catch", "using", "lock", "return", "new"}
    methods: list[tuple[str, str, str]] = []
    for found in pattern.finditer(masked):
        name = found.group("name")
        if name in controls:
            continue
        if found.group("tail") == "=>":
            end = masked.find(";", found.end())
            end = len(masked) if end < 0 else end + 1
        else:
            opening = masked.find("{", found.end() - 1)
            depth = 0
            end = None
            for position in range(opening, len(masked)):
                if masked[position] == "{":
                    depth += 1
                elif masked[position] == "}":
                    depth -= 1
                    if depth == 0:
                        end = position + 1
                        break
            if end is None:
                end = len(masked)
        methods.append((name, text[found.start():end], masked[found.start():end]))
    return methods


def ui_event_references(code_root: Path) -> set[str]:
    """Scan the complete event-wiring surface of the UI project.

    Qualified subscriptions include both framework events and Cadroue's own
    UI notifications. Bare subscriptions are limited to known framework or
    application events so arithmetic ``+=`` expressions are not mistaken for
    event wiring. Routed ``AddHandler`` registrations, XAML bindings, and UI
    override callbacks are audited as separate references.
    """
    ui_root = code_root / "Cadroue.UIShell"
    references: set[str] = set()
    qualified_subscription = re.compile(
        r"(?P<lhs>[A-Za-z_][\w.]*)\.(?P<event>[A-Za-z_]\w*)\s*\+=")
    bare_subscription = re.compile(
        r"(?<![.\w])(?P<event>[A-Za-z_]\w*)\s*\+=")
    routed_handler = re.compile(
        r"(?:(?P<target>[A-Za-z_][\w.]*)\.)?AddHandler\s*\(\s*"
        r"(?P<event>[A-Za-z_][\w.]*Event)\s*,\s*new\s+"
        r"(?P<delegate>[A-Za-z_]\w*)\s*\(\s*(?P<handler>[A-Za-z_]\w*)")
    bare_event_names = UI_EVENT_NAMES | {"DispatcherUnhandledException"}

    for file in sorted(ui_root.rglob("*.cs")):
        text = file.read_text(encoding="utf-8")
        relative = file.relative_to(ui_root).as_posix()
        for method_name, method_text, method_masked in csharp_methods(text):
            counts: dict[tuple[str, str], int] = defaultdict(int)
            qualified_spans: list[tuple[int, int]] = []
            for found in qualified_subscription.finditer(method_masked):
                key = (found.group("lhs"), found.group("event"))
                counts[key] += 1
                qualified_spans.append(found.span())
                references.add(
                    f"cs|{relative}|{method_name}|{key[0]}|{key[1]}|{counts[key]}")
            for found in bare_subscription.finditer(method_masked):
                if any(start <= found.start() < end for start, end in qualified_spans):
                    continue
                event_name = found.group("event")
                if event_name not in bare_event_names:
                    continue
                key = ("this", event_name)
                counts[key] += 1
                references.add(
                    f"cs|{relative}|{method_name}|this|{event_name}|{counts[key]}")

            routed_counts: dict[tuple[str, str, str], int] = defaultdict(int)
            for found in routed_handler.finditer(method_masked):
                key = (
                    found.group("target") or "this",
                    found.group("event"),
                    found.group("handler"),
                )
                routed_counts[key] += 1
                references.add(
                    f"addhandler|{relative}|{method_name}|{key[0]}|{key[1]}|"
                    f"{key[2]}|{routed_counts[key]}")

            declaration = method_text.split("(", 1)[0]
            if method_name.startswith("On") and " override " in f" {declaration} ":
                references.add(f"override|{relative}|{method_name}")

    for file in sorted(ui_root.rglob("*.xaml")):
        text = file.read_text(encoding="utf-8")
        if not re.search(r'x:Class="[^"]+"', text):
            continue
        relative = file.relative_to(ui_root).as_posix()
        counts: dict[tuple[str, str], int] = defaultdict(int)
        for found in re.finditer(r'\b([A-Za-z][A-Za-z0-9]*)="([A-Za-z_][A-Za-z0-9_]*)"', text):
            event_name, handler = found.group(1), found.group(2)
            if event_name not in UI_EVENT_NAMES:
                continue
            key = (event_name, handler)
            counts[key] += 1
            references.add(f"xaml|{relative}|{event_name}|{handler}|{counts[key]}")
    return references


def map_tabs(item: LogicMap) -> list[str]:
    return [part.strip() for part in item.headers.get("tabs", "").split(",") if part.strip()]


def map_entries(item: LogicMap) -> list[str]:
    return [part.strip() for part in item.headers.get("entry", "").split(",") if part.strip()]


def normalize_reference(reference: str) -> str:
    raw = reference.strip()
    if raw.startswith("`") or raw.endswith("`"):
        if len(raw) < 2 or not (raw.startswith("`") and raw.endswith("`")):
            return ""
        raw = raw[1:-1].strip()
    return raw


def parse_implementation_reference(reference: str) -> tuple[str, str | None, str | None] | None:
    """Parse the complete Format-1 implementation-reference grammar.

    Returns (alias, member, descriptor). ``?`` is handled by the caller and is
    intentionally not a normal reference.
    """
    raw = normalize_reference(reference)
    if not raw or raw == "?":
        return None
    match = re.fullmatch(
        r"(?:(external|constructor|property|event)\s+)?"
        r"([A-Za-z][A-Za-z0-9-]*)\.([A-Za-z_][A-Za-z0-9_]*)"
        r"(\(\.\.\.\))?"
        r"(?:\s*/\s*([A-Za-z0-9_.:#()+\-/ ]+))?",
        raw,
    )
    if match is None:
        return None
    descriptor, alias, member, call, qualifier = match.groups()
    if descriptor in {"property", "event"}:
        if call is not None:
            return None
    elif call is None:
        return None
    if qualifier is not None and not qualifier.strip():
        return None
    return alias, member, descriptor


def reference_parts(reference: str) -> tuple[str, str | None]:
    parsed = parse_implementation_reference(reference)
    return (parsed[0], parsed[1]) if parsed else ("", None)


def valid_event_reference_syntax(reference: str) -> bool:
    parts = reference.split("|")
    if not parts:
        return False
    family = parts[0]
    identifier = re.compile(r"^[A-Za-z_][A-Za-z0-9_]*$")
    dotted = re.compile(r"^[A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)*$")

    def ui_path(value: str, suffix: str) -> bool:
        if not value.endswith(suffix) or value.startswith("/") or "\\" in value:
            return False
        if not re.fullmatch(r"[A-Za-z0-9_.\-/]+", value):
            return False
        return all(part not in {"", ".", ".."} for part in value.split("/"))

    positive = lambda value: value.isdigit() and int(value) >= 1
    if family == "cs" and len(parts) == 6:
        _, path, method, target, event_name, ordinal = parts
        return ui_path(path, ".cs") and bool(identifier.fullmatch(method)) and bool(dotted.fullmatch(target)) and bool(identifier.fullmatch(event_name)) and positive(ordinal)
    if family == "addhandler" and len(parts) == 7:
        _, path, method, target, event_name, handler, ordinal = parts
        return ui_path(path, ".cs") and bool(identifier.fullmatch(method)) and bool(dotted.fullmatch(target)) and bool(dotted.fullmatch(event_name)) and bool(identifier.fullmatch(handler)) and positive(ordinal)
    if family == "xaml" and len(parts) == 5:
        _, path, event_name, handler, ordinal = parts
        return ui_path(path, ".xaml") and bool(identifier.fullmatch(event_name)) and bool(identifier.fullmatch(handler)) and positive(ordinal)
    if family == "override" and len(parts) == 3:
        _, path, method = parts
        return ui_path(path, ".cs") and bool(identifier.fullmatch(method))
    return False


def local_edges(item: LogicMap) -> list[tuple[str, Edge]]:
    node_ids = {node.id for node in item.nodes}
    return [(node.id, edge) for node in item.nodes for edge in node.edges if edge.target in node_ids]


def nonloop_adjacency(item: LogicMap) -> dict[str, list[str]]:
    adjacency: dict[str, list[str]] = defaultdict(list)
    for source, edge in local_edges(item):
        if edge.marker != "loop":
            adjacency[source].append(edge.target)
    return adjacency


def path_exists(adjacency: dict[str, list[str]], start: str, target: str) -> bool:
    pending = [start]
    seen: set[str] = set()
    while pending:
        current = pending.pop()
        if current == target:
            return True
        if current in seen:
            continue
        seen.add(current)
        pending.extend(adjacency.get(current, []))
    return False


def nonloop_cycle(item: LogicMap) -> bool:
    adjacency = nonloop_adjacency(item)
    state: dict[str, int] = {}

    def visit(node_id: str) -> bool:
        current = state.get(node_id, 0)
        if current == 1:
            return True
        if current == 2:
            return False
        state[node_id] = 1
        if any(visit(target) for target in adjacency.get(node_id, [])):
            return True
        state[node_id] = 2
        return False

    return any(visit(node.id) for node in item.nodes if state.get(node.id, 0) == 0)


def graph_ranks(item: LogicMap, entries: list[str]) -> dict[str, int]:
    """Return longest-path ranks for the acyclic graph after removing [loop]."""
    node_ids = {node.id for node in item.nodes}
    edges = [(source, edge) for source, edge in local_edges(item) if edge.marker != "loop"]
    ranks: dict[str, int] = {entry: 0 for entry in entries if entry in node_ids}
    for _ in range(max(1, len(item.nodes))):
        changed = False
        for source, edge in edges:
            if source not in ranks or edge.target in entries:
                continue
            candidate = ranks[source] + 1
            if candidate > ranks.get(edge.target, -1):
                ranks[edge.target] = candidate
                changed = True
        if not changed:
            break
    return ranks


def source_layout_crossings(item: LogicMap, entries: list[str]) -> tuple[int, list[tuple[str, str, str, str]]]:
    """Count avoidable adjacent-rank inversions in authoritative source order.

    Side-routing hints do not exempt an edge from this check. A hint may choose
    a route only after source ordering is already crossing-free. [loop] edges
    are excluded because they are explicitly outside the acyclic primary flow.
    """
    ranks = graph_ranks(item, entries)
    edges = [(source, edge) for source, edge in local_edges(item) if edge.marker != "loop"]
    rows: dict[int, list[str]] = defaultdict(list)
    for node in item.nodes:
        if node.id in ranks:
            rows[ranks[node.id]].append(node.id)
    total = 0
    examples: list[tuple[str, str, str, str]] = []
    for rank, left_row in rows.items():
        right_row = rows.get(rank + 1)
        if not right_row:
            continue
        left_pos = {node_id: index for index, node_id in enumerate(left_row)}
        right_pos = {node_id: index for index, node_id in enumerate(right_row)}
        layer_edges = [(source, edge.target) for source, edge in edges if source in left_pos and edge.target in right_pos]
        for index, first in enumerate(layer_edges):
            for second in layer_edges[index + 1:]:
                if first[0] == second[0] or first[1] == second[1]:
                    continue
                if (left_pos[first[0]] - left_pos[second[0]]) * (right_pos[first[1]] - right_pos[second[1]]) < 0:
                    total += 1
                    if len(examples) < 3:
                        examples.append((first[0], first[1], second[0], second[1]))
    return total, examples


def validate(all_maps: list[LogicMap], reporter: Reporter) -> tuple[dict[str, LogicMap], dict[str, LogicMap]]:
    maps: list[LogicMap] = []
    fragments: list[LogicMap] = []
    for item in all_maps:
        is_map = "id" in item.headers
        is_fragment = "fragment" in item.headers
        if is_map == is_fragment:
            reporter.issue(
                "ERROR", item.path, 1,
                "A .lmap source must declare exactly one of @id or @fragment, not both." if is_map
                else "A .lmap source must declare exactly one of @id or @fragment."
            )
            continue
        (maps if is_map else fragments).append(item)

    ids: dict[str, LogicMap] = {}
    fragment_ids: dict[str, LogicMap] = {}
    document_ids: dict[str, LogicMap] = {}
    for item in maps:
        for key in sorted(MAP_REQUIRED_HEADERS):
            if key not in item.headers:
                reporter.issue("ERROR", item.path, 1, f"Missing required header @{key}.")
        for key in item.headers:
            if key not in MAP_REQUIRED_HEADERS | MAP_OPTIONAL_HEADERS:
                reporter.issue("ERROR", item.path, 1, f"Header '@{key}' is not allowed on a complete map.")
        if item.headers.get("format") != FORMAT_VERSION:
            reporter.issue("ERROR", item.path, 1, f"Unsupported @format '{item.headers.get('format', '')}'. Format {FORMAT_VERSION} is required.")
        map_id = item.headers.get("id", "")
        if map_id and not MAP_ID_RE.fullmatch(map_id):
            reporter.issue("ERROR", item.path, 1, f"Invalid map ID '{map_id}'.")
        if map_id in document_ids:
            reporter.issue("ERROR", item.path, 1, f"Duplicate document ID '{map_id}'. Map and fragment IDs share one namespace.")
        else:
            document_ids[map_id] = item
        if map_id in ids:
            reporter.issue("ERROR", item.path, 1, f"Duplicate map ID '{map_id}'.")
        else:
            ids[map_id] = item
        if not item.headers.get("section", "").strip():
            reporter.issue("ERROR", item.path, 1, "Map section must not be empty.")
        if "tabs" in item.headers and not map_tabs(item):
            reporter.issue("ERROR", item.path, 1, "@tabs must contain at least one nonblank group.")

    for item in fragments:
        for key in sorted(FRAGMENT_REQUIRED_HEADERS):
            if key not in item.headers:
                reporter.issue("ERROR", item.path, 1, f"Missing required fragment header @{key}.")
        for key in item.headers:
            if key not in FRAGMENT_REQUIRED_HEADERS | FRAGMENT_OPTIONAL_HEADERS:
                reporter.issue("ERROR", item.path, 1, f"Header '@{key}' is not allowed on a shared fragment.")
        if item.headers.get("format") != FORMAT_VERSION:
            reporter.issue("ERROR", item.path, 1, f"Unsupported @format '{item.headers.get('format', '')}'. Format {FORMAT_VERSION} is required.")
        fragment_id = item.headers.get("fragment", "")
        if fragment_id and not MAP_ID_RE.fullmatch(fragment_id):
            reporter.issue("ERROR", item.path, 1, f"Invalid fragment ID '{fragment_id}'.")
        if fragment_id in document_ids:
            reporter.issue("ERROR", item.path, 1, f"Duplicate document ID '{fragment_id}'. Map and fragment IDs share one namespace.")
        else:
            document_ids[fragment_id] = item
        if fragment_id in fragment_ids:
            reporter.issue("ERROR", item.path, 1, f"Duplicate fragment ID '{fragment_id}'.")
        else:
            fragment_ids[fragment_id] = item

    symbols = csharp_symbols(CODE_ROOT)
    for item in all_maps:
        node_ids: dict[str, Node] = {}
        for node in item.nodes:
            if not NODE_ID_RE.fullmatch(node.id):
                reporter.issue("ERROR", item.path, node.line, f"Invalid node ID '{node.id}'. Node IDs must be lowercase kebab-case.")
            if node.id in node_ids:
                reporter.issue("ERROR", item.path, node.line, f"Duplicate node ID '{node.id}'.")
            node_ids[node.id] = node
            if node.kind not in KINDS:
                reporter.issue("ERROR", item.path, node.line, f"Invalid node kind '{node.kind}'.")
            if node.kind == "junction":
                if node.explanation or node.technical_label or node.actions or node.states or node.notes:
                    reporter.issue("ERROR", item.path, node.line, f"Junction '{node.id}' cannot contain actions, states, notes, or card explanations.")
                if len(node.edges) != 1 or node.edges[0].conditional:
                    reporter.issue("ERROR", item.path, node.line, f"Junction '{node.id}' must have exactly one unconditional outgoing edge.")
            else:
                if not node.explanation.strip():
                    reporter.issue("ERROR", item.path, node.line, f"Card '{node.id}' must contain an explicit simple explanation using '~'.")
                if node.technical_label != "Technical explanation":
                    reporter.issue("ERROR", item.path, node.line, f"Card '{node.id}' must contain an explicit [Technical explanation] block.")
                if not (node.actions or node.states or node.notes):
                    reporter.issue("ERROR", item.path, node.line, f"Card '{node.id}' has an empty [Technical explanation] block.")
            if node.kind == "note" and node.actions:
                reporter.issue("ERROR", item.path, node.line, f"Note node '{node.id}' cannot contain executable actions.")
            if node.kind == "decision":
                if len(node.edges) < 2:
                    reporter.issue("ERROR", item.path, node.line, f"Decision node '{node.id}' must have at least two outgoing conditional edges.")
                if any(not edge.conditional for edge in node.edges):
                    reporter.issue("ERROR", item.path, node.line, f"Decision node '{node.id}' has an unlabeled outgoing edge.")
                labels = [edge.label for edge in node.edges]
                if len(labels) != len(set(labels)):
                    reporter.issue("ERROR", item.path, node.line, f"Decision node '{node.id}' has duplicate condition labels.")
            for action in node.actions:
                for reference in action.references:
                    if reference.strip() == "?":
                        reporter.unresolved_reference(item, action)
                        continue
                    parsed = parse_implementation_reference(reference)
                    if parsed is None:
                        reporter.issue("ERROR", item.path, action.line, f"Invalid implementation reference syntax: {reference}")
                        continue
                    alias, member, _descriptor = parsed
                    if alias not in item.aliases:
                        reporter.issue("ERROR", item.path, action.line, f"Alias '{alias}' is used but not declared.")
                        continue
                    owner = item.aliases[alias]
                    if owner not in symbols:
                        reporter.issue("ERROR" if reporter.strict else "WARNING", item.path, action.line, f"C# type not found: {owner}")
                    elif member and member not in symbols[owner]:
                        reporter.issue("ERROR" if reporter.strict else "WARNING", item.path, action.line, f"C# member not found on declared type: {owner}.{member}")

        entries = map_entries(item)
        if not entries:
            reporter.issue("ERROR", item.path, 1, "Map or fragment has no entry node.")
        if len(entries) != len(set(entries)):
            reporter.issue("ERROR", item.path, 1, "@entry contains duplicate node IDs.")
        for entry in entries:
            if not NODE_ID_RE.fullmatch(entry):
                reporter.issue("ERROR", item.path, 1, f"Invalid entry node ID '{entry}'.")
            if entry not in node_ids:
                reporter.issue("ERROR", item.path, 1, f"Entry node '{entry}' does not exist.")
            elif node_ids[entry].kind == "junction":
                reporter.issue("ERROR", item.path, node_ids[entry].line, f"Entry node '{entry}' cannot be a junction.")

        incoming_local: dict[str, list[str]] = defaultdict(list)
        for node in item.nodes:
            for edge in node.edges:
                if edge.marker and edge.marker not in EDGE_MARKERS:
                    reporter.issue("ERROR", item.path, edge.line, f"Invalid edge marker '{edge.marker}'.")
                if edge.target.startswith("map:"):
                    if edge.marker:
                        reporter.issue("ERROR", item.path, edge.line, "Routing markers are only valid on local node edges.")
                    target_id = edge.target[4:]
                    if target_id not in ids:
                        reporter.issue("ERROR", item.path, edge.line, f"Unknown map '{edge.target}'.")
                elif edge.target.startswith("fragment:"):
                    if edge.marker:
                        reporter.issue("ERROR", item.path, edge.line, "Routing markers are only valid on local node edges.")
                    target_id = edge.target[9:]
                    if target_id not in fragment_ids:
                        reporter.issue("ERROR", item.path, edge.line, f"Unknown fragment '{edge.target}'.")
                else:
                    if not NODE_ID_RE.fullmatch(edge.target):
                        reporter.issue("ERROR", item.path, edge.line, f"Invalid local edge target '{edge.target}'.")
                    if edge.target not in node_ids:
                        reporter.issue("ERROR", item.path, edge.line, f"Unknown node '{edge.target}'.")
                    else:
                        incoming_local[edge.target].append(node.id)

        for entry in entries:
            if entry in incoming_local and entry in node_ids:
                reporter.issue("ERROR", item.path, node_ids[entry].line, f"Entry node '{entry}' must be a graph root and cannot have an incoming local edge.")
        for target, sources in incoming_local.items():
            if target in node_ids and len(sources) > 1 and node_ids[target].kind != "junction":
                reporter.issue("ERROR", item.path, node_ids[target].line, f"Converging paths must merge through an explicit junction before '{target}' (incoming from: {', '.join(sources)}).")

        # Cycle contract: removing [loop] edges must leave a DAG, and every
        # [loop] edge must actually close a path in that DAG.
        has_nonloop_cycle = nonloop_cycle(item)
        if has_nonloop_cycle:
            reporter.issue("ERROR", item.path, 1, "Local graph contains a cycle that is not explicitly closed by a [loop] edge.")
        adjacency = nonloop_adjacency(item)
        for source, edge in local_edges(item):
            if edge.marker == "loop" and not path_exists(adjacency, edge.target, source):
                reporter.issue("ERROR", item.path, edge.line, f"[loop] edge {source}->{edge.target} does not close an existing local path.")

        if not has_nonloop_cycle:
            ranks = graph_ranks(item, entries)
            for source, edge in local_edges(item):
                if edge.marker == "loop":
                    continue
                if source not in ranks or edge.target not in ranks:
                    continue
                delta = ranks[edge.target] - ranks[source]
                if delta != 1:
                    reporter.issue("ERROR", item.path, edge.line, f"Primary local edge {source}->{edge.target} spans {delta} rank(s). Crossing-free Format 1 requires every non-loop local edge to connect adjacent ranks; insert explicit topology nodes or split the scenario.")

            crossing_count, crossing_examples = source_layout_crossings(item, entries)
            if crossing_count:
                sample = "; ".join(f"{a}->{b} crosses {c}->{d}" for a, b, c, d in crossing_examples)
                reporter.issue("ERROR", item.path, 1, f"Source graph contains {crossing_count} crossing(s). Format 1 permits no primary-flow line crossing: reorder same-rank nodes, add explicit junctions, or split the scenario. {sample}")

        for related in filter(None, (part.strip() for part in item.headers.get("related", "").split(","))):
            if not MAP_ID_RE.fullmatch(related):
                reporter.issue("ERROR", item.path, 1, f"Invalid @related document ID '{related}'.")
            elif related not in ids and related not in fragment_ids:
                reporter.issue("WARNING", item.path, 1, f"Related document '{related}' does not exist.")

        valid_entries = [entry for entry in entries if entry in node_ids]
        if valid_entries:
            reachable, pending = set(), deque(valid_entries)
            while pending:
                current = pending.popleft()
                if current in reachable or current not in node_ids:
                    continue
                reachable.add(current)
                pending.extend(edge.target for edge in node_ids[current].edges if edge.target in node_ids)
            for node in item.nodes:
                if node.id not in reachable:
                    reporter.issue("WARNING", item.path, node.line, f"Node '{node.id}' is unreachable.")

    covered: dict[str, LogicMap] = {}
    for item in maps:
        event_reference = item.headers.get("event-ref")
        if not event_reference:
            continue
        if not valid_event_reference_syntax(event_reference):
            reporter.issue("ERROR", item.path, 1, f"Invalid @event-ref syntax: {event_reference}")
            continue
        if not map_tabs(item):
            reporter.issue("ERROR", item.path, 1, "A map with @event-ref must declare @tabs so its UI placement is explicit.")
        if event_reference in covered:
            reporter.issue("ERROR", item.path, 1, f"Duplicate UI event reference '{event_reference}'.")
        else:
            covered[event_reference] = item
    if covered:
        actual_events = ui_event_references(CODE_ROOT)
        for event_reference in sorted(set(covered) - actual_events):
            reporter.issue("ERROR", covered[event_reference].path, 1, f"UI event binding no longer exists: {event_reference}")
    return ids, fragment_ids


def h(value: object) -> str:
    return html.escape(str(value), quote=True)


def relative_source(item: LogicMap) -> str:
    return item.path.relative_to(SOURCE_ROOT).as_posix()


def resolved_reference(item: LogicMap, reference: str) -> str:
    prefix = ""
    raw = reference.strip().strip("`")
    prefixed = re.match(r"^(external|constructor|property|event)\s+", raw)
    if prefixed:
        prefix, raw = prefixed.group(1) + " ", raw[prefixed.end():]
    alias, dot, suffix = raw.partition(".")
    if alias not in item.aliases or not dot:
        return reference
    tail = suffix.split("/", 1)
    return f"{prefix}{item.aliases[alias]}.{tail[0]}" + (f" /{tail[1]}" if len(tail) == 2 else "")


def anchor(value: str) -> str:
    return re.sub(r"[^a-z0-9]+", "-", value.lower()).strip("-")


def natural_key(value: str) -> list[object]:
    """Sort source-provided labels naturally without a predefined taxonomy."""
    return [int(part) if part.isdigit() else part.casefold() for part in re.split(r"(\d+)", value)]


def navigation_groups(entry: dict[str, object]) -> list[str]:
    """Return source-declared display contexts: tabs when present, otherwise area."""
    tabs = [str(tab) for tab in entry.get("tabs", []) if str(tab).strip()]
    return tabs or [str(entry["area"])]


def context_badge_parts(context: str, title: str) -> tuple[str, str | None, str]:
    """Return badge text, optional title text, and a style kind for display contexts."""
    normalized_context = context.strip()
    normalized_title = title.strip()
    if normalized_context.casefold() == "common":
        return "Common", normalized_title, "common"
    match = re.fullmatch(r"In\s+(.+?)\s+tab", normalized_context, flags=re.IGNORECASE)
    if match:
        return f'In {match.group(1)}', None if normalized_context.casefold() == normalized_title.casefold() else normalized_title, "scenario"
    if normalized_context.casefold() == normalized_title.casefold():
        return normalized_context, None, "scenario"
    return normalized_context, normalized_title, "scenario"


def contextual_title_html(entry: dict[str, object], context: str) -> str:
    """Render context badges for both shared and scenario maps."""
    title = str(entry["title"])
    badge_text, title_text, badge_kind = context_badge_parts(context, title)
    badge = f'<span class="context-badge context-badge-{badge_kind}">{h(badge_text)}</span>'
    title_html = "" if title_text is None else f'<span class="context-title">{h(title_text)}</span>'
    return f'{badge}{title_html}'


def section_display_entries(entries: list[dict[str, object]]) -> list[tuple[str, dict[str, object]]]:
    """Flatten a major category into its source-declared situations/contexts."""
    display: list[tuple[str, dict[str, object]]] = []
    for entry in entries:
        for context in navigation_groups(entry):
            display.append((context, entry))
    return sorted(display, key=lambda pair: (natural_key(pair[0]), natural_key(str(pair[1]["title"]))))


def navigation_html(catalog: list[dict[str, object]], fragment_catalog: list[dict[str, object]] | None = None) -> str:
    """Build one flat situation list beneath each authoritative major category."""
    fragment_catalog = fragment_catalog or []
    sections: dict[str, list[dict[str, object]]] = defaultdict(list)
    for entry in catalog:
        sections[str(entry["section"])].append(entry)

    body: list[str] = [
        '<a class="nav-home" href="../../MapsLogic.html" target="_top">Logic maps</a>',
        '<a class="nav-home nav-secondary" href="ImplementationIndex.html" target="_top">Implementation index</a>',
    ]
    for section in sorted(sections, key=natural_key):
        body.append(f'<h2 class="nav-major">{h(section)}</h2><div class="nav-situations">')
        for context, entry in section_display_entries(sections[section]):
            label = contextual_title_html(entry, context)
            body.append(f'<a class="maplink situation-link" href="{h(entry["href"])}" target="_top">{label}</a>')
        body.append('</div>')
    if fragment_catalog:
        body.append('<h2 class="nav-major">Shared fragments</h2><div class="nav-situations">')
        for entry in sorted(fragment_catalog, key=lambda item: natural_key(str(item["title"]))):
            body.append(f'<a class="maplink situation-link" href="{h(entry["href"])}" target="_top"><span class="context-title">{h(entry["title"])}</span></a>')
        body.append('</div>')
    content = "".join(body)
    return f'<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>Navigation — Cadroue Logic Maps</title><link rel="stylesheet" href="../assets/site.css"></head><body class="navigation-page"><nav class="nav nav-document" aria-label="Logic map navigation">{content}</nav><script src="../assets/site.js"></script></body></html>'


def page(title: str, crumb: str, body: str, depth: int, *, docs_index: bool = False) -> str:
    if docs_index:
        asset_prefix = "LogicMaps/"
        navigation_src = "LogicMaps/maps/NavigationLogic.html"
    else:
        # Generated HTML (other than MapsLogic.html) lives below LogicMaps/maps/.
        # `depth` is the number of path components in the HTML path relative to
        # that maps/ root. Assets live one level above maps/, while the shared
        # navigation document lives at the maps/ root itself.
        asset_prefix = "../" * depth
        navigation_src = "../" * max(0, depth - 1) + "NavigationLogic.html"
    return f'<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>{h(title)} — Cadroue Logic Maps</title><link rel="stylesheet" href="{asset_prefix}assets/site.css"></head><body><header class="top"><div class="brand">Cadroue Logic Maps</div><div class="crumb">{h(crumb)}</div><div class="spacer"></div><button class="iconbtn" data-theme-toggle aria-label="Toggle light and dark theme">◐</button></header><div class="shell"><aside class="nav-host"><iframe class="nav-frame" src="{navigation_src}" title="Logic map navigation"></iframe></aside><main class="main">{body}</main></div><script src="{asset_prefix}assets/site.js"></script></body></html>'


def write_bytes_if_changed(path: Path, content: bytes) -> bool:
    if path.is_file() and path.read_bytes() == content:
        return False
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name(path.name + ".tmp")
    temporary.write_bytes(content)
    os.replace(temporary, path)
    return True


def write_text_if_changed(path: Path, content: str) -> bool:
    return write_bytes_if_changed(path, content.encode("utf-8"))


def stale_outputs_remove(expected: set[Path]) -> int:
    removed = 0
    for root in (MAP_ROOT / "assets", MAP_ROOT / "maps"):
        if not root.is_dir():
            continue
        for path in root.rglob("*"):
            if path.is_file() and path not in expected:
                path.unlink()
                removed += 1
        for directory in sorted((path for path in root.rglob("*") if path.is_dir()), key=lambda path: len(path.parts), reverse=True):
            if not any(directory.iterdir()):
                directory.rmdir()
    return removed


def map_output_relative(item: LogicMap) -> str:
    return relative_source(item).removesuffix(".lmap") + ".html"


def fragment_output_relative(item: LogicMap) -> str:
    fragment_id = item.headers["fragment"]
    return "fragments/" + fragment_id.replace(".", "/") + ".html"


def generate(ids: dict[str, LogicMap], fragments: dict[str, LogicMap], reporter: Reporter, *, only_map_ids: set[str] | None = None) -> tuple[int, int]:
    expected: set[Path] = set()
    updated = 0

    def emit_bytes(path: Path, content: bytes) -> None:
        nonlocal updated
        expected.add(path)
        updated += int(write_bytes_if_changed(path, content))

    def emit_text(path: Path, content: str) -> None:
        nonlocal updated
        expected.add(path)
        updated += int(write_text_if_changed(path, content))

    emit_bytes(MAP_ROOT / "assets" / "site.css", (RENDER_ROOT / "site.css").read_bytes())
    emit_bytes(MAP_ROOT / "assets" / "site.js", (RENDER_ROOT / "site.js").read_bytes())

    map_items = list(ids.values())
    fragment_items = list(fragments.values())
    implementations: dict[str, list[dict[str, object]]] = defaultdict(list)
    catalog: list[dict[str, object]] = []
    fragment_catalog: list[dict[str, object]] = []

    for item in map_items:
        relative = map_output_relative(item)
        entries = map_entries(item)
        digest = hashlib.sha256(item.raw.encode()).hexdigest()[:12]
        tabs = map_tabs(item)
        searchable = [item.headers["section"], item.headers["area"], *tabs, item.headers["title"], item.headers["summary"], *item.tags]
        context = f'{item.headers["section"]} / {", ".join(tabs) if tabs else item.headers["area"]} / {item.headers["title"]}'
        for node in item.nodes:
            searchable.append(node.title)
            for index, action in enumerate(node.actions):
                searchable.append(action.text)
                for ref in action.references:
                    full = resolved_reference(item, ref)
                    implementations[full].append({"context": context, "action": action.text, "relative": relative, "node": node.id, "index": index})
        catalog.append({"id": item.headers["id"], "title": item.headers["title"], "section": item.headers["section"], "area": item.headers["area"], "tabs": tabs, "event_ref": item.headers.get("event-ref"), "entries": entries, "summary": item.headers["summary"], "href": relative, "tags": item.tags, "hash": digest, "search": " ".join(searchable).lower()})

    for item in fragment_items:
        relative = fragment_output_relative(item)
        summary = item.headers["summary"]
        searchable = [item.headers["fragment"], item.headers["title"], summary, *item.tags]
        context = f'Shared fragment / {item.headers["title"]}'
        for node in item.nodes:
            searchable.append(node.title)
            for index, action in enumerate(node.actions):
                searchable.append(action.text)
                for ref in action.references:
                    full = resolved_reference(item, ref)
                    implementations[full].append({"context": context, "action": action.text, "relative": relative, "node": node.id, "index": index})
        fragment_catalog.append({"id": item.headers["fragment"], "title": item.headers["title"], "summary": summary, "href": relative, "search": " ".join(searchable).lower()})

    if only_map_ids is None:
        rendered_maps = map_items
    else:
        rendered_maps = []
        for item in map_items:
            output = html_root() / map_output_relative(item)
            if item.headers["id"] in only_map_ids or not output.is_file():
                rendered_maps.append(item)
    rendered_documents: list[tuple[LogicMap, str, bool]] = [
        (item, map_output_relative(item), False) for item in rendered_maps
    ] + [
        (item, fragment_output_relative(item), True) for item in fragment_items
    ]

    def render_document(item: LogicMap, relative: str, is_fragment: bool) -> None:
        output = html_root() / relative
        output.parent.mkdir(parents=True, exist_ok=True)
        nodes: list[str] = []
        edges: list[dict[str, object]] = []
        entries = map_entries(item)
        entry_set = set(entries)
        def append_local_edge(source_id: str, edge: Edge) -> None:
            edges.append({"from": source_id, "to": edge.target, "label": edge.label, "conditional": edge.conditional, "marker": edge.marker or ""})

        def continuation_html(edge: Edge) -> str:
            if edge.target.startswith("map:"):
                target_id = edge.target[4:]
                target = ids[target_id]
                target_output = html_root() / map_output_relative(target)
                kind = "Map"
            else:
                target_id = edge.target[9:]
                target = fragments[target_id]
                target_output = html_root() / fragment_output_relative(target)
                kind = "Fragment"
            href = os.path.relpath(target_output, output.parent).replace(os.sep, "/")
            condition = f'<span class="continuation-condition">{h(edge.label)}</span>' if edge.label else ""
            return f'<a class="continuation-link" href="{h(href)}">{condition}<span class="continuation-kind">{kind}</span><span class="continuation-target">{h(target.headers["title"])}</span></a>'

        for node in item.nodes:
            if node.kind == "junction":
                nodes.append(f'<div class="node junction" data-id="{h(node.id)}" tabindex="0" aria-label="{h(node.title)}" title="{h(node.title)}"></div>')
                for edge in node.edges:
                    append_local_edge(node.id, edge)
                continue
            actions = []
            for index, action in enumerate(node.actions):
                refs = "".join(f'<span class="ref" title="{h(resolved_reference(item, ref))}">{h(ref)}</span>' for ref in action.references)
                actions.append(f'<div class="action" tabindex="0" id="action-{h(node.id)}-{index}"><div class="action-text">{h(action.text)}</div>{refs}</div>')
            states = "".join(f'<div class="state">{h(value)}</div>' for value in node.states)
            notes = "".join(f'<div class="note">{h(value)}</div>' for value in node.notes)
            technical_content = f'<div class="actions">{"".join(actions)}</div><div class="states">{states}</div><div class="notes">{notes}</div>'
            continuations = []
            for edge in node.edges:
                if edge.target.startswith(("map:", "fragment:")):
                    continuations.append(continuation_html(edge))
                else:
                    append_local_edge(node.id, edge)
            continuation_block = f'<div class="continuations">{"".join(continuations)}</div>' if continuations else ""
            entry_class = " entry" if node.id in entry_set else ""
            entry_attribute = ' data-entry="true"' if node.id in entry_set else ""
            nodes.append(f'<section class="node {h(node.kind)}{entry_class}" data-id="{h(node.id)}"{entry_attribute} tabindex="0" aria-label="{h(node.title)}"><div class="nodehead"><div class="kind">{h(node.kind)}</div><div class="nodetitle">{h(node.title)}</div></div><div class="simple-explanation">{h(node.explanation)}</div><div class="technical-explanation"><div class="technical-label">{h(node.technical_label)}</div>{technical_content}</div>{continuation_block}</section>')

        digest = hashlib.sha256(item.raw.encode()).hexdigest()[:12]
        related = "".join(f'<span class="pill">{h(value.strip())}</span>' for value in item.headers.get("related", "").split(",") if value.strip())
        edge_json = h(json.dumps(edges, separators=(",", ":")))
        entry_json = h(json.dumps(entries, separators=(",", ":")))
        if is_fragment:
            title = item.headers["title"]
            summary = item.headers["summary"]
            crumb = f'Shared fragments / {title}'
            meta = f'<span class="pill">Shared fragment</span><span class="pill">{h(item.headers["fragment"])}</span>'
        else:
            title = item.headers["title"]
            summary = item.headers["summary"]
            tabs = map_tabs(item)
            placement = ", ".join(tabs) if tabs else item.headers["area"]
            crumb = f'{item.headers["section"]} / {placement} / {title}'
            event_pill = '<span class="pill">code-bound UI event</span>' if item.headers.get("event-ref") else ""
            meta = f'<span class="pill">{h(item.headers["section"])}</span><span class="pill">{h(placement)}</span>{event_pill}'
        body = f'<div class="heading"><h1>{h(title)}</h1><p class="summary">{h(summary)}</p><div class="meta">{meta}<span class="pill">source/{h(relative_source(item))}</span><span class="pill">valid</span><span class="pill">sha256 {digest}</span>{related}</div></div><div class="toolbar"><button class="tool" data-out aria-label="Zoom out">−</button><button class="tool" data-fit>Fit</button><button class="tool" data-in aria-label="Zoom in">+</button></div><div class="canvas" data-edges="{edge_json}" data-entries="{entry_json}"><div class="world"><svg class="edges"></svg><div class="nodes">{"".join(nodes)}</div></div></div>'
        depth = len(Path(relative).parts)
        emit_text(output, page(title, crumb, body, depth))

    for item, relative, is_fragment in rendered_documents:
        render_document(item, relative, is_fragment)

    emit_text(html_root() / "NavigationLogic.html", navigation_html(catalog, fragment_catalog))

    sections: list[str] = []
    grouped: dict[str, list[dict[str, object]]] = defaultdict(list)
    for entry in catalog:
        grouped[str(entry["section"])].append(entry)
    for section in sorted(grouped, key=natural_key):
        cards: list[str] = []
        for context, entry in section_display_entries(grouped[section]):
            title_html = contextual_title_html(entry, context)
            cards.append(
                f'<a class="mapcard" data-search="{h(entry["search"])}" href="LogicMaps/maps/{h(entry["href"])}">'
                f'<div class="card-title-row">{title_html}</div>'
                f'<span class="card-kind">{"Code-bound event" if entry["event_ref"] else "Logic map"}</span>'
                f'<p>{h(entry["summary"])}</p></a>'
            )
        sections.append(
            f'<section class="major-section" id="{anchor(section)}">'
            f'<div class="major-heading"><h1>{h(section)}</h1></div>'
            f'<div class="cards situation-cards">{"".join(cards)}</div></section>'
        )

    fragment_section = ""
    if fragment_catalog:
        cards = "".join(f'<a class="mapcard" data-search="{h(entry["search"])}" href="LogicMaps/maps/{h(entry["href"])}"><strong>{h(entry["title"])}</strong><span class="card-kind">Shared fragment</span><p>{h(entry["summary"])}</p></a>' for entry in sorted(fragment_catalog, key=lambda entry: natural_key(str(entry["title"]))))
        fragment_word = "fragment" if len(fragment_catalog) == 1 else "fragments"
        fragment_section = f'<section class="major-section"><div class="major-heading"><h1>Shared fragments</h1></div><section class="area"><h2>{len(fragment_catalog)} {fragment_word}</h2><div class="cards">{cards}</div></section></section>'

    implementation_html = []
    for symbol in sorted(implementations):
        uses = implementations[symbol]
        search = (symbol + " " + " ".join(f'{use["context"]} {use["action"]}' for use in uses)).lower()
        links = "".join(f'<a href="{h(use["relative"])}#action-{h(use["node"])}-{use["index"]}">{h(use["context"])} / {h(use["action"])} </a>' for use in uses)
        implementation_html.append(f'<div class="impl" data-search="{h(search)}"><code>{h(symbol)}</code>{links}</div>')

    map_word = "map" if len(map_items) == 1 else "maps"
    fragment_word = "fragment" if len(fragment_items) == 1 else "fragments"
    index_body = f'''<div class="index-wrap"><div class="hero"><h1>Cadroue logic maps</h1><p class="summary">{len(map_items)} verified {map_word} and {len(fragment_items)} shared {fragment_word}.</p><div class="hero-actions"><a class="button-link" href="LogicMaps/maps/ImplementationIndex.html">Implementation index</a></div><input class="search" data-filter-input type="search" placeholder="Search maps, fragments, actions, tabs, or tags" aria-label="Search logic maps"></div><div id="maps">{"".join(sections)}{fragment_section}</div><p class="empty" data-filter-empty>No matching maps or fragments.</p></div>'''
    emit_text(MAP_ROOT.parent / "MapsLogic.html", page("Index", "Logic Maps", index_body, 0, docs_index=True))

    document_count = len(map_items) + len(fragment_items)
    document_word = "source document" if document_count == 1 else "source documents"
    implementation_body = f'''<div class="index-wrap"><div class="hero"><h1>Implementation index</h1><p class="summary">{len(implementations)} implementation symbols referenced by {document_count} {document_word}.</p><input class="search" data-filter-input type="search" placeholder="Search implementation symbols, maps, fragments, or actions" aria-label="Search implementation index"></div><div id="implementations">{"".join(implementation_html)}</div><p class="empty" data-filter-empty>No matching implementation references.</p></div>'''
    emit_text(html_root() / "ImplementationIndex.html", page("Implementation index", "Implementation Index", implementation_body, 1))

    removed = 0
    if only_map_ids is None:
        removed = stale_outputs_remove(expected)
        for legacy in (MAP_ROOT / "index.html", MAP_ROOT / "assets" / "navigation.js"):
            if legacy.is_file():
                legacy.unlink()
                removed += 1
    return updated, removed


def source_paths_read() -> list[Path]:
    """Discover every authoritative .lmap recursively below source/."""
    if not SOURCE_ROOT.is_dir():
        return []
    return sorted(SOURCE_ROOT.rglob("*.lmap"))


def main() -> int:
    parser = argparse.ArgumentParser(description="Validate and render Cadroue logic maps.")
    parser.add_argument("--strict", action="store_true", help="Treat unresolved C# ownership as errors.")
    parser.add_argument("--map", dest="map_ids", action="append", metavar="MAP_ID", help="Validate and render only the named map page. May be repeated. Targeted generation renders only the named content page(s), rebuilds shared navigation/index documents from all sources, and does not remove unrelated generated files.")
    args = parser.parse_args()
    reporter = Reporter(args.strict)
    source_paths = source_paths_read()
    all_maps = [parse_map(path, reporter) for path in source_paths]
    targeted = set(args.map_ids or [])
    ids, fragments = validate(all_maps, reporter)
    missing_targets = sorted(targeted - set(ids))
    for map_id in missing_targets:
        reporter.issue("ERROR", SOURCE_ROOT, 1, f"Requested map ID does not exist: {map_id}")
    for issue in reporter.errors + reporter.warnings:
        print(issue, file=sys.stderr)
    if reporter.errors:
        print(f"Logic-map generation stopped: {len(reporter.errors)} error(s).", file=sys.stderr)
        return 1
    updated, removed = generate(ids, fragments, reporter, only_map_ids=targeted or None)
    if targeted:
        generated_count = sum(
            1 for item in ids.values()
            if item.headers["id"] in targeted
            or not (html_root() / (relative_source(item).removesuffix(".lmap") + ".html")).is_file()
        )
        print(f"Logic maps: {len(ids)} parsed, {len(targeted)} requested")
    else:
        generated_count = len(ids)
        print(f"Logic maps: {len(ids)} parsed, {generated_count} generated")
    print(f"Fragments: {len(fragments)} parsed")
    print(f"Errors: {len(reporter.errors)}")
    print(f"Warnings: {len(reporter.warnings)}")
    print(f"Unresolved symbols: {reporter.unresolved}")
    if targeted:
        print("Generation mode: targeted (shared navigation, MapsLogic.html, and ImplementationIndex.html rebuilt from all sources)")
    print(f"Generated files updated: {updated}")
    print(f"Stale generated files removed: {removed}")
    if targeted:
        for map_id in sorted(targeted):
            target = ids[map_id]
            relative = relative_source(target).removesuffix(".lmap") + ".html"
            print(f"Output: docs/LogicMaps/maps/{relative}")
    else:
        print("Output: docs/MapsLogic.html")
        print("Navigation: docs/LogicMaps/maps/NavigationLogic.html")
        print("Implementation index: docs/LogicMaps/maps/ImplementationIndex.html")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

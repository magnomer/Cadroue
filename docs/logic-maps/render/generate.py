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
import shutil
import sys
from collections import defaultdict, deque
from dataclasses import dataclass, field
from pathlib import Path
from typing import Iterable

RENDER_ROOT = Path(__file__).resolve().parent
MAP_ROOT = RENDER_ROOT.parent
SOURCE_ROOT = MAP_ROOT / "source"
CODE_ROOT = MAP_ROOT.parents[1] / "src"
KINDS = {"input", "process", "decision", "storage", "external", "output", "error", "note"}
EDGE_MARKERS = {"left", "right", "up", "down", "loop"}
SECTION_ORDER = ("I. UI", "II. Functionality")
TAB_ORDER = ("Split", "Edit", "Convert", "Audio", "Merge", "Funnel", "Worklist", "Global interface")
UI_EVENT_NAMES = set("""Click MouseLeftButtonDown MouseLeftButtonUp MouseRightButtonDown MouseRightButtonUp MouseMove MouseEnter MouseLeave MouseWheel PreviewMouseMove PreviewMouseLeftButtonDown PreviewMouseLeftButtonUp PreviewMouseWheel PreviewMouseDown PreviewMouseUp PreviewMouseRightButtonDown PreviewMouseRightButtonUp LostMouseCapture KeyDown PreviewKeyDown KeyUp PreviewTextInput TextChanged SelectionChanged Selected Unselected Checked Unchecked ValueChanged DragStarted DragDelta DragCompleted DragEnter DragOver DragLeave Drop GiveFeedback GotFocus LostFocus LostKeyboardFocus Loaded Unloaded Closed Closing SizeChanged IsVisibleChanged LayoutUpdated DropDownOpened DropDownClosed StatusChanged Deactivated ContextMenuOpening ContextMenuClosing RequestNavigate SeekCompleted ScrollChanged""".split())


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
        self.ui_events_total = 0
        self.ui_events_covered = 0

    def issue(self, level: str, path: Path, line: int, message: str) -> None:
        record = f"{level} {path}:{line}\n{message}"
        (self.errors if level == "ERROR" else self.warnings).append(record)

    def unresolved_reference(self, source: LogicMap, action: Action) -> None:
        self.unresolved += 1
        self.issue("ERROR" if self.strict else "WARNING", source.path, action.line,
                   "Unresolved implementation reference.")


def parse_map(path: Path, reporter: Reporter) -> LogicMap:
    source = LogicMap(path, path.read_text(encoding="utf-8"))
    current: Node | None = None
    last_action: Action | None = None
    for number, original in enumerate(source.raw.splitlines(), 1):
        line = original.strip()
        if not line or line.startswith("#"):
            continue
        alias = re.match(r"@alias\s+([A-Za-z][A-Za-z0-9-]*)\s*=\s*(\S.+)$", line)
        if alias:
            name, target = alias.group(1), alias.group(2).strip()
            if name in source.aliases:
                reporter.issue("ERROR", path, number, f"Duplicate alias '{name}'.")
            else:
                source.aliases[name] = target
            continue
        header = re.match(r"@([a-z-]+)\s+(.+)$", line)
        if current is None and header:
            key, value = header.group(1), header.group(2).strip()
            if key == "tag":
                source.tags.append(value)
            else:
                source.headers[key] = value
            continue
        declaration = re.match(r"\[([^]]+)]\s+(.+?)\s+<([a-z]+)>$", line)
        if declaration:
            current = Node(*declaration.groups(), number)
            source.nodes.append(current)
            last_action = None
            continue
        if current is None:
            reporter.issue("ERROR", path, number, "Content appears outside a node.")
            continue
        if line.startswith("-"):
            parts = re.split(r"\s+@\s+", line[1:].strip(), maxsplit=1)
            if len(parts) != 2 or not parts[1].strip():
                reporter.issue("ERROR", path, number, "Action has no implementation reference.")
                continue
            last_action = Action(parts[0].strip(), [parts[1].strip()], number)
            current.actions.append(last_action)
        elif line.startswith("@"):
            if last_action is None:
                reporter.issue("ERROR", path, number, "Continuation implementation reference is detached from an action.")
            else:
                last_action.references.append(line[1:].strip())
        elif line.startswith("="):
            current.states.append(line[1:].strip())
        elif line.startswith("!"):
            current.notes.append(line[1:].strip())
        else:
            conditional = re.match(r'\?\s+"([^"]+)"\s+>\s+(\S+)(?:\s+\[([a-z-]+)])?$', line)
            direct = re.match(r">\s+(\S+)(?:\s+\[([a-z-]+)])?$", line)
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


def reference_parts(reference: str) -> tuple[str, str | None]:
    plain = re.sub(r"\s*/.*$", "", reference).strip().strip("`")
    plain = re.sub(r"^(?:external|constructor|property|event)\s+", "", plain)
    match = re.match(r"([A-Za-z][\w-]*)(?:\.([A-Za-z_]\w*))?", plain)
    return (match.group(1), match.group(2)) if match else ("", None)


def validate(all_maps: list[LogicMap], reporter: Reporter) -> tuple[dict[str, LogicMap], dict[str, LogicMap]]:
    maps = [item for item in all_maps if "id" in item.headers]
    fragments = [item for item in all_maps if "fragment" in item.headers]
    ids: dict[str, LogicMap] = {}
    fragment_ids: dict[str, LogicMap] = {}
    for item in maps:
        for key in ("format", "id", "title", "section", "area", "entry", "summary"):
            if key not in item.headers:
                reporter.issue("ERROR", item.path, 1, f"Missing required header @{key}.")
        section = item.headers.get("section")
        if section not in SECTION_ORDER:
            reporter.issue("ERROR", item.path, 1, f"Invalid or missing map section '{section}'.")
        if section == "I. UI":
            tabs = map_tabs(item)
            if not tabs:
                reporter.issue("ERROR", item.path, 1, "UI map has no @tabs declaration.")
            for tab in tabs:
                if tab not in TAB_ORDER:
                    reporter.issue("ERROR", item.path, 1, f"Unknown UI tab '{tab}'.")
        elif item.headers.get("tabs"):
            reporter.issue("WARNING", item.path, 1, "Functionality map should not declare @tabs.")
        if item.headers.get("id") in ids:
            reporter.issue("ERROR", item.path, 1, f"Duplicate map ID '{item.headers['id']}'.")
        else:
            ids[item.headers.get("id", "")] = item
    for item in fragments:
        for key in ("format", "fragment", "title", "entry"):
            if key not in item.headers:
                reporter.issue("ERROR", item.path, 1, f"Missing required fragment header @{key}.")
        fragment_ids[item.headers.get("fragment", "")] = item
    symbols = csharp_symbols(CODE_ROOT)
    for item in all_maps:
        node_ids: dict[str, Node] = {}
        for node in item.nodes:
            if node.id in node_ids:
                reporter.issue("ERROR", item.path, node.line, f"Duplicate node ID '{node.id}'.")
            node_ids[node.id] = node
            if node.kind not in KINDS:
                reporter.issue("ERROR", item.path, node.line, f"Invalid node kind '{node.kind}'.")
            if node.kind == "decision" and any(not edge.conditional for edge in node.edges):
                reporter.issue("ERROR", item.path, node.line, f"Decision node '{node.id}' has an unlabeled outgoing edge.")
            for action in node.actions:
                for reference in action.references:
                    if reference == "?":
                        reporter.unresolved_reference(item, action)
                        continue
                    alias, member = reference_parts(reference)
                    if alias not in item.aliases:
                        reporter.issue("ERROR", item.path, action.line, f"Alias '{alias}' is used but not declared.")
                        continue
                    owner = item.aliases[alias]
                    if owner not in symbols:
                        reporter.issue("ERROR" if reporter.strict else "WARNING", item.path, action.line,
                                       f"C# type not found: {owner}")
                    elif member and member not in symbols[owner]:
                        reporter.issue("ERROR" if reporter.strict else "WARNING", item.path, action.line,
                                       f"C# member not found on declared type: {owner}.{member}")
        entry = item.headers.get("entry")
        if entry not in node_ids:
            reporter.issue("ERROR", item.path, 1, f"Entry node '{entry}' does not exist.")
        for node in item.nodes:
            for edge in node.edges:
                if edge.marker and edge.marker not in EDGE_MARKERS:
                    reporter.issue("ERROR", item.path, edge.line, f"Invalid edge marker '{edge.marker}'.")
                if edge.target.startswith("map:"):
                    if edge.target[4:] not in ids:
                        reporter.issue("ERROR", item.path, edge.line, f"Unknown map '{edge.target}'.")
                elif edge.target.startswith("fragment:"):
                    if edge.target[9:] not in fragment_ids:
                        reporter.issue("ERROR", item.path, edge.line, f"Unknown fragment '{edge.target}'.")
                elif edge.target not in node_ids:
                    reporter.issue("ERROR", item.path, edge.line, f"Unknown node '{edge.target}'.")
        for related in filter(None, (part.strip() for part in item.headers.get("related", "").split(","))):
            if related not in ids and related not in fragment_ids:
                reporter.issue("WARNING", item.path, 1, f"Related map '{related}' does not exist.")
        if entry in node_ids:
            reachable, pending = set(), deque([entry])
            while pending:
                current = pending.popleft()
                if current in reachable or current not in node_ids:
                    continue
                reachable.add(current)
                pending.extend(edge.target for edge in node_ids[current].edges if not edge.target.startswith(("map:", "fragment:")))
            for node in item.nodes:
                if node.id not in reachable:
                    reporter.issue("WARNING", item.path, node.line, f"Node '{node.id}' is unreachable.")
    covered: dict[str, LogicMap] = {}
    for item in maps:
        event_reference = item.headers.get("event-ref")
        if not event_reference:
            continue
        if item.headers.get("section") != "I. UI":
            reporter.issue("ERROR", item.path, 1, "@event-ref is only valid on UI maps.")
        if event_reference in covered:
            reporter.issue("ERROR", item.path, 1, f"Duplicate UI event coverage reference '{event_reference}'.")
        else:
            covered[event_reference] = item
    actual_events = ui_event_references(CODE_ROOT)
    missing = sorted(actual_events - set(covered))
    stale = sorted(set(covered) - actual_events)
    for event_reference in missing:
        reporter.issue("ERROR", SOURCE_ROOT, 1, f"UI event has no logic map: {event_reference}")
    for event_reference in stale:
        reporter.issue("ERROR", covered[event_reference].path, 1, f"UI event binding no longer exists: {event_reference}")
    reporter.ui_events_total = len(actual_events)
    reporter.ui_events_covered = len(actual_events & set(covered))
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


def navigation(maps: Iterable[LogicMap], current_id: str, depth: int) -> str:
    """Render compact section navigation.

    The complete searchable catalogue belongs on the index page. Repeating
    hundreds of map links inside every generated page makes both navigation
    and generation unnecessarily heavy, so map pages link to the authoritative
    tab and functionality collections instead.
    """
    prefix = "../" * depth
    items = list(maps)

    def anchor(value: str) -> str:
        return re.sub(r"[^a-z0-9]+", "-", value.lower()).strip("-")

    parts = [f'<a class="nav-home" href="{prefix}index.html">Logic maps</a>']
    parts.append('<h2 class="nav-major">I. UI</h2>')
    for index, tab in enumerate(TAB_ORDER, 1):
        count = sum(
            1 for item in items
            if item.headers.get("section") == "I. UI" and tab in map_tabs(item)
        )
        parts.append(
            f'<a class="nav-section" href="{prefix}index.html#ui-{anchor(tab)}">'
            f'<span>{index}. {h(tab)}</span><strong>{count}</strong></a>')

    parts.append('<h2 class="nav-major">II. Functionality</h2>')
    groups: dict[str, int] = defaultdict(int)
    for item in items:
        if item.headers.get("section") == "II. Functionality":
            groups[item.headers["area"]] += 1
    for area in sorted(groups):
        parts.append(
            f'<a class="nav-section" href="{prefix}index.html#functionality-{anchor(area)}">'
            f'<span>{h(area)}</span><strong>{groups[area]}</strong></a>')
    return "".join(parts)

def page(title: str, crumb: str, body: str, nav: str, depth: int) -> str:
    prefix = "../" * depth
    return f'''<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>{h(title)} — Cadroue Logic Maps</title><link rel="stylesheet" href="{prefix}assets/site.css"></head><body><header class="top"><div class="brand">Cadroue Logic Maps</div><div class="crumb">{h(crumb)}</div><div class="spacer"></div><button class="iconbtn" data-theme-toggle aria-label="Toggle light and dark theme">◐</button></header><div class="shell"><nav class="nav" aria-label="Logic map navigation">{nav}</nav><main class="main">{body}</main></div><script src="{prefix}assets/site.js"></script></body></html>'''


def generate(ids: dict[str, LogicMap], fragments: dict[str, LogicMap], reporter: Reporter) -> None:
    for generated in (MAP_ROOT / "index.html", MAP_ROOT / "manifest.json", MAP_ROOT / "assets", MAP_ROOT / "maps"):
        if generated.is_dir():
            shutil.rmtree(generated)
        elif generated.exists():
            generated.unlink()
    (MAP_ROOT / "assets").mkdir()
    (MAP_ROOT / "maps").mkdir()
    shutil.copy2(RENDER_ROOT / "site.css", MAP_ROOT / "assets" / "site.css")
    shutil.copy2(RENDER_ROOT / "site.js", MAP_ROOT / "assets" / "site.js")
    implementations: dict[str, list[dict[str, object]]] = defaultdict(list)
    manifest: list[dict[str, object]] = []
    maps = list(ids.values())
    for item in maps:
        relative = relative_source(item).removesuffix(".lmap") + ".html"
        output = MAP_ROOT / "maps" / relative
        output.parent.mkdir(parents=True, exist_ok=True)
        nodes, edges = [], []
        for node in item.nodes:
            actions = []
            for index, action in enumerate(node.actions):
                refs = "".join(f'<span class="ref" title="{h(resolved_reference(item, ref))}">{h(ref)}</span>' for ref in action.references)
                actions.append(f'<div class="action" tabindex="0" id="action-{h(node.id)}-{index}"><div class="action-text">{h(action.text)}</div>{refs}</div>')
                for ref in action.references:
                    full = resolved_reference(item, ref)
                    implementations[full].append({"section": item.headers["section"], "area": item.headers["area"], "tabs": map_tabs(item), "map": item.headers["title"], "action": action.text, "relative": relative, "node": node.id, "index": index})
            states = "".join(f'<div class="state">{h(value)}</div>' for value in node.states)
            notes = "".join(f'<div class="note">{h(value)}</div>' for value in node.notes)
            nodes.append(f'<section class="node {h(node.kind)}" data-id="{h(node.id)}" tabindex="0" aria-label="{h(node.title)}"><div class="nodehead"><div class="kind">{h(node.kind)}</div><div class="nodetitle">{h(node.title)}</div></div><div class="actions">{"".join(actions)}</div><div class="states">{states}</div><div class="notes">{notes}</div></section>')
            for edge in node.edges:
                if edge.target.startswith("map:"):
                    target = ids[edge.target[4:]]
                    virtual = "map-" + edge.target[4:].replace(".", "-")
                    edges.append({"from": node.id, "to": virtual, "label": "continues", "conditional": False})
                    target_relative = relative_source(target).removesuffix(".lmap") + ".html"
                    target_output = MAP_ROOT / "maps" / target_relative
                    target_href = os.path.relpath(target_output, output.parent).replace(os.sep, "/")
                    nodes.append(f'<section class="node output" data-id="{virtual}" tabindex="0"><div class="nodehead"><div class="kind">Linked map</div><div class="nodetitle"><a href="{h(target_href)}">{h(target.headers["title"])}</a></div></div><div class="states"><div class="state">{h(target.headers["summary"])}</div></div></section>')
                elif edge.target.startswith("fragment:"):
                    target = fragments[edge.target[9:]]
                    virtual = "fragment-" + edge.target[9:].replace(".", "-")
                    edges.append({"from": node.id, "to": virtual, "label": "shared flow", "conditional": False})
                    nodes.append(f'<section class="node note" data-id="{virtual}" tabindex="0"><div class="nodehead"><div class="kind">Shared fragment</div><div class="nodetitle">{h(target.headers["title"])}</div></div><div class="states"><div class="state">{h(target.headers.get("summary", "Shared reusable flow."))}</div></div></section>')
                else:
                    edges.append({"from": node.id, "to": edge.target, "label": edge.label, "conditional": edge.conditional})
        digest = hashlib.sha256(item.raw.encode()).hexdigest()[:12]
        related = "".join(f'<span class="pill">{h(value.strip())}</span>' for value in item.headers.get("related", "").split(",") if value.strip())
        tabs = map_tabs(item)
        placement = ", ".join(tabs) if tabs else item.headers["area"]
        event_pill = f'<span class="pill">code-bound UI event</span>' if item.headers.get("event-ref") else ""
        edge_json = h(json.dumps(edges, separators=(",", ":")))
        body = f'<div class="heading"><h1>{h(item.headers["title"])}</h1><p class="summary">{h(item.headers["summary"])}</p><div class="meta"><span class="pill">{h(item.headers["section"])}</span><span class="pill">{h(placement)}</span>{event_pill}<span class="pill">source/{h(relative_source(item))}</span><span class="pill">valid</span><span class="pill">sha256 {digest}</span>{related}</div></div><div class="toolbar"><button class="tool" data-out>−</button><button class="tool" data-fit>Fit</button><button class="tool" data-in>+</button></div><div class="canvas" data-edges="{edge_json}"><div class="world"><svg class="edges" aria-hidden="true"></svg><div class="nodes">{"".join(nodes)}</div></div></div>'
        depth = len(Path(relative).parts)
        crumb = f'{item.headers["section"]} / {placement} / {item.headers["title"]}'
        output.write_text(page(item.headers["title"], crumb, body, navigation(maps, item.headers["id"], depth), depth), encoding="utf-8")
        searchable = [item.headers["section"], item.headers["area"], *tabs, item.headers["title"], item.headers["summary"], *item.tags, *item.aliases.keys(), *item.aliases.values()]
        for node in item.nodes:
            searchable.append(node.title)
            for action in node.actions:
                searchable.extend((action.text, *action.references))
        manifest.append({"id": item.headers["id"], "title": item.headers["title"], "section": item.headers["section"], "area": item.headers["area"], "tabs": tabs, "event_ref": item.headers.get("event-ref"), "summary": item.headers["summary"], "href": f"maps/{relative}", "tags": item.tags, "hash": digest, "search": " ".join(searchable).lower()})

    sections: list[str] = []
    ui_entries = [entry for entry in manifest if entry["section"] == "I. UI"]
    sections.append('<section class="major-section"><div class="major-heading"><span>I</span><div><h1>UI</h1><p>Logic maps initiated by direct interface events, grouped by every tab in which each event can occur.</p></div></div>')
    for index, tab in enumerate(TAB_ORDER, 1):
        entries = sorted((entry for entry in ui_entries if tab in entry["tabs"]), key=lambda entry: str(entry["title"]))
        event_count = sum(1 for entry in entries if entry["event_ref"])
        cards = "".join(f'<a class="mapcard" data-search="{h(entry["search"])}" href="{h(entry["href"])}"><strong>{h(entry["title"])}</strong><span class="card-kind">{"Code-bound event" if entry["event_ref"] else "UI workflow"}</span><p>{h(entry["summary"])}</p></a>' for entry in entries)
        sections.append(f'<section class="area" id="ui-{re.sub(r"[^a-z0-9]+", "-", tab.lower()).strip("-")}"><h2>{index}. {h(tab)} · {len(entries)} maps</h2><p class="section-note">{event_count} maps are bound directly to current UI event registrations or overrides.</p><div class="cards">{cards}</div></section>')
    sections.append('</section>')

    sections.append('<section class="major-section"><div class="major-heading"><span>II</span><div><h1>Functionality</h1><p>Operational logic that is not specific to one tab type.</p></div></div>')
    functionality = [entry for entry in manifest if entry["section"] == "II. Functionality"]
    for area in sorted({entry["area"] for entry in functionality}):
        entries = sorted((entry for entry in functionality if entry["area"] == area), key=lambda entry: str(entry["title"]))
        cards = "".join(f'<a class="mapcard" data-search="{h(entry["search"])}" href="{h(entry["href"])}"><strong>{h(entry["title"])}</strong><span class="card-kind">Functionality</span><p>{h(entry["summary"])}</p></a>' for entry in entries)
        sections.append(f'<section class="area" id="functionality-{re.sub(r"[^a-z0-9]+", "-", area.lower()).strip("-")}"><h2>{h(area)} · {len(entries)} maps</h2><div class="cards">{cards}</div></section>')
    sections.append('</section>')

    implementation_html = []
    for symbol in sorted(implementations):
        uses = implementations[symbol]
        search = (symbol + " " + " ".join(f'{use["section"]} {use["area"]} {" ".join(use["tabs"])} {use["map"]} {use["action"]}' for use in uses)).lower()
        links = "".join(f'<a href="maps/{h(use["relative"])}#action-{h(use["node"])}-{use["index"]}">{h(use["section"])} / {h(", ".join(use["tabs"]) if use["tabs"] else use["area"])} / {h(use["map"])} / {h(use["action"])} </a>' for use in uses)
        implementation_html.append(f'<div class="impl" data-search="{h(search)}"><code>{h(symbol)}</code>{links}</div>')
    coverage = f'{reporter.ui_events_covered}/{reporter.ui_events_total}'
    index_body = f'''<div class="index-wrap"><div class="hero"><h1>Cadroue logic maps</h1><p class="summary">{len(maps)} verified maps and {len(fragments)} shared fragment. Direct UI-event coverage: <strong>{coverage}</strong>.</p><div class="coverage-strip"><span><strong>{len(ui_entries)}</strong> UI maps</span><span><strong>{len(functionality)}</strong> functionality maps</span><span><strong>{reporter.ui_events_total}</strong> current UI bindings audited</span></div><input class="search" type="search" placeholder="Search maps, actions, tabs, or implementation symbols" aria-label="Search logic maps"></div><div id="maps">{"".join(sections)}</div><section class="area"><h2>Implementation index · {len(implementations)}</h2><div id="implementations">{"".join(implementation_html)}</div></section><p class="empty">No matching maps or implementation references.</p></div><script>document.querySelector('.search').addEventListener('input',e=>{{const q=e.target.value.toLowerCase().trim();let shown=0;document.querySelectorAll('[data-search]').forEach(x=>{{const yes=!q||x.dataset.search.includes(q);x.style.display=yes?'':'none';if(yes)shown++}});document.querySelector('.empty').style.display=shown?'none':'block'}})</script>'''
    (MAP_ROOT / "index.html").write_text(page("Index", "I. UI / II. Functionality", index_body, navigation(maps, "", 0), 0), encoding="utf-8")
    (MAP_ROOT / "manifest.json").write_text(json.dumps({"format": 2, "ui_event_coverage": {"covered": reporter.ui_events_covered, "total": reporter.ui_events_total}, "maps": manifest}, indent=2) + "\n", encoding="utf-8")

def main() -> int:
    parser = argparse.ArgumentParser(description="Validate and render Cadroue logic maps.")
    parser.add_argument("--strict", action="store_true", help="Treat unresolved C# ownership as errors.")
    args = parser.parse_args()
    reporter = Reporter(args.strict)
    all_maps = [parse_map(path, reporter) for path in sorted(SOURCE_ROOT.rglob("*.lmap"))]
    ids, fragments = validate(all_maps, reporter)
    for issue in reporter.errors + reporter.warnings:
        print(issue, file=sys.stderr)
    if reporter.errors:
        print(f"Logic-map generation stopped: {len(reporter.errors)} error(s).", file=sys.stderr)
        return 1
    generate(ids, fragments, reporter)
    print(f"Logic maps: {len(ids)} parsed, {len(ids)} generated")
    print(f"Fragments: {len(fragments)} parsed")
    print(f"Errors: {len(reporter.errors)}")
    print(f"Warnings: {len(reporter.warnings)}")
    print(f"Unresolved symbols: {reporter.unresolved}")
    print(f"UI event coverage: {reporter.ui_events_covered}/{reporter.ui_events_total}")
    print("Output: docs/logic-maps/index.html")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

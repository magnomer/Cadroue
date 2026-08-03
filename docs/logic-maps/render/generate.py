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
    """Return methods/properties declared by each fully-qualified C# type.

    This is deliberately per-type, unlike a repository-wide word search.  It
    is a pragmatic validator, not a C# compiler; build remains authoritative.
    """
    result: dict[str, set[str]] = defaultdict(set)
    namespace_re = re.compile(r"\bnamespace\s+([A-Za-z_][\w.]*)\s*;")
    type_re = re.compile(r"\b(?:class|record(?:\s+(?:class|struct))?|struct|interface)\s+([A-Za-z_]\w*)\b")
    symbol_re = re.compile(r"(?:\b(?:public|private|protected|internal|static|virtual|override|abstract|sealed|async|partial|new)\s+)*[\w<>,?\[\]. ]+\s+([A-Za-z_]\w*)\s*(?:\(|=>|\{)")
    for file in code_root.rglob("*.cs"):
        text = file.read_text(encoding="utf-8")
        namespace = namespace_re.search(text)
        if namespace is None:
            continue
        for found in type_re.finditer(text):
            opening = text.find("{", found.end())
            if opening < 0:
                continue
            depth, end = 0, None
            for index in range(opening, len(text)):
                if text[index] == "{":
                    depth += 1
                elif text[index] == "}":
                    depth -= 1
                    if depth == 0:
                        end = index
                        break
            if end is None:
                continue
            full_name = f"{namespace.group(1)}.{found.group(1)}"
            body = text[opening + 1:end]
            result[full_name].update(match.group(1) for match in symbol_re.finditer(body))
    return result


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
        for key in ("format", "id", "title", "area", "entry", "summary"):
            if key not in item.headers:
                reporter.issue("ERROR", item.path, 1, f"Missing required header @{key}.")
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
    prefix = "../" * depth
    groups: dict[str, list[LogicMap]] = defaultdict(list)
    for item in maps:
        groups[item.headers["area"]].append(item)
    parts = [f'<a class="nav-home" href="{prefix}index.html">Logic maps</a>']
    for area in sorted(groups):
        parts.append(f"<h2>{h(area)}</h2>")
        for item in sorted(groups[area], key=lambda value: value.headers["title"]):
            target = relative_source(item).removesuffix(".lmap") + ".html"
            active = " active" if item.headers["id"] == current_id else ""
            parts.append(f'<a class="maplink{active}" href="{prefix}maps/{h(target)}">{h(item.headers["title"])}</a>')
    return "".join(parts)


def page(title: str, crumb: str, body: str, nav: str, depth: int) -> str:
    prefix = "../" * depth
    return f'''<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1"><title>{h(title)} — Cadroue Logic Maps</title><link rel="stylesheet" href="{prefix}assets/site.css"></head><body><header class="top"><div class="brand">Cadroue Logic Maps</div><div class="crumb">{h(crumb)}</div><div class="spacer"></div><button class="iconbtn" data-theme-toggle aria-label="Toggle light and dark theme">◐</button></header><div class="shell"><nav class="nav" aria-label="Logic map navigation">{nav}</nav><main class="main">{body}</main></div><script src="{prefix}assets/site.js"></script></body></html>'''


def generate(ids: dict[str, LogicMap], fragments: dict[str, LogicMap]) -> None:
    for generated in (MAP_ROOT / "index.html", MAP_ROOT / "manifest.json", MAP_ROOT / "assets", MAP_ROOT / "maps"):
        if generated.is_dir(): shutil.rmtree(generated)
        elif generated.exists(): generated.unlink()
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
                    implementations[full].append({"area": item.headers["area"], "map": item.headers["title"], "action": action.text, "relative": relative, "node": node.id, "index": index})
            states = "".join(f'<div class="state">{h(value)}</div>' for value in node.states)
            notes = "".join(f'<div class="note">{h(value)}</div>' for value in node.notes)
            nodes.append(f'<section class="node {h(node.kind)}" data-id="{h(node.id)}" tabindex="0" aria-label="{h(node.title)}"><div class="nodehead"><div class="kind">{h(node.kind)}</div><div class="nodetitle">{h(node.title)}</div></div><div class="actions">{"".join(actions)}</div><div class="states">{states}</div><div class="notes">{notes}</div></section>')
            for edge in node.edges:
                if edge.target.startswith("map:"):
                    target = ids[edge.target[4:]]
                    virtual = "map-" + edge.target[4:].replace(".", "-")
                    edges.append({"from": node.id, "to": virtual, "label": "continues", "conditional": False})
                    target_relative = relative_source(target).removesuffix(".lmap") + ".html"
                    nodes.append(f'<section class="node output" data-id="{virtual}" tabindex="0"><div class="nodehead"><div class="kind">Linked map</div><div class="nodetitle"><a href="../{h(target_relative)}">{h(target.headers["title"])}</a></div></div><div class="states"><div class="state">{h(target.headers["summary"])}</div></div></section>')
                elif edge.target.startswith("fragment:"):
                    target = fragments[edge.target[9:]]
                    virtual = "fragment-" + edge.target[9:].replace(".", "-")
                    edges.append({"from": node.id, "to": virtual, "label": "shared flow", "conditional": False})
                    nodes.append(f'<section class="node note" data-id="{virtual}" tabindex="0"><div class="nodehead"><div class="kind">Shared fragment</div><div class="nodetitle">{h(target.headers["title"])}</div></div><div class="states"><div class="state">{h(target.headers.get("summary", "Shared reusable flow."))}</div></div></section>')
                else:
                    edges.append({"from": node.id, "to": edge.target, "label": edge.label, "conditional": edge.conditional})
        digest = hashlib.sha256(item.raw.encode()).hexdigest()[:12]
        related = "".join(f'<span class="pill">{h(value.strip())}</span>' for value in item.headers.get("related", "").split(",") if value.strip())
        edge_json = h(json.dumps(edges, separators=(",", ":")))
        body = f'<div class="heading"><h1>{h(item.headers["title"])}</h1><p class="summary">{h(item.headers["summary"])}</p><div class="meta"><span class="pill">{h(item.headers["area"])}</span><span class="pill">source/{h(relative_source(item))}</span><span class="pill">valid</span><span class="pill">sha256 {digest}</span>{related}</div></div><div class="toolbar"><button class="tool" data-out>−</button><button class="tool" data-fit>Fit</button><button class="tool" data-in>+</button></div><div class="canvas" data-edges="{edge_json}"><div class="world"><svg class="edges" aria-hidden="true"></svg><div class="nodes">{"".join(nodes)}</div></div></div>'
        depth = len(Path(relative).parts)
        output.write_text(page(item.headers["title"], f'{item.headers["area"]} / {item.headers["title"]}', body, navigation(maps, item.headers["id"], depth), depth), encoding="utf-8")
        searchable = [item.headers["title"], item.headers["summary"], *item.tags, *item.aliases.keys(), *item.aliases.values()]
        for node in item.nodes:
            searchable.append(node.title)
            for action in node.actions:
                searchable.extend((action.text, *action.references))
        manifest.append({"id": item.headers["id"], "title": item.headers["title"], "area": item.headers["area"], "summary": item.headers["summary"], "href": f"maps/{relative}", "tags": item.tags, "hash": digest, "search": " ".join(searchable).lower()})
    sections = []
    for area in sorted({entry["area"] for entry in manifest}):
        entries = sorted((entry for entry in manifest if entry["area"] == area), key=lambda entry: str(entry["title"]))
        cards = "".join(f'<a class="mapcard" data-search="{h(entry["search"])}" href="{h(entry["href"])}"><strong>{h(entry["title"])}</strong><p>{h(entry["summary"])}</p></a>' for entry in entries)
        sections.append(f'<section class="area"><h2>{h(area)} · {len(entries)}</h2><div class="cards">{cards}</div></section>')
    implementation_html = []
    for symbol in sorted(implementations):
        uses = implementations[symbol]
        search = (symbol + " " + " ".join(f'{use["area"]} {use["map"]} {use["action"]}' for use in uses)).lower()
        links = "".join(f'<a href="maps/{h(use["relative"])}#action-{h(use["node"])}-{use["index"]}">{h(use["area"])} / {h(use["map"])} / {h(use["action"])} </a>' for use in uses)
        implementation_html.append(f'<div class="impl" data-search="{h(search)}"><code>{h(symbol)}</code>{links}</div>')
    index_body = f'''<div class="index-wrap"><div class="hero"><h1>Operational logic, one event at a time.</h1><p class="summary">{len(maps)} verified maps and {len(fragments)} shared fragment. Generated from compact <code>.lmap</code> sources.</p><input class="search" type="search" placeholder="Search maps, actions, or implementation symbols" aria-label="Search logic maps"></div><div id="maps">{"".join(sections)}</div><section class="area"><h2>Implementation index · {len(implementations)}</h2><div id="implementations">{"".join(implementation_html)}</div></section><p class="empty">No matching maps or implementation references.</p></div><script>document.querySelector('.search').addEventListener('input',e=>{{const q=e.target.value.toLowerCase().trim();let shown=0;document.querySelectorAll('[data-search]').forEach(x=>{{const yes=!q||x.dataset.search.includes(q);x.style.display=yes?'':'none';if(yes)shown++}});document.querySelector('.empty').style.display=shown?'none':'block'}})</script>'''
    (MAP_ROOT / "index.html").write_text(page("Index", "Index", index_body, navigation(maps, "", 0), 0), encoding="utf-8")
    (MAP_ROOT / "manifest.json").write_text(json.dumps({"format": 1, "maps": manifest}, indent=2) + "\n", encoding="utf-8")


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
    generate(ids, fragments)
    print(f"Logic maps: {len(ids)} parsed, {len(ids)} generated")
    print(f"Fragments: {len(fragments)} parsed")
    print(f"Errors: {len(reporter.errors)}")
    print(f"Warnings: {len(reporter.warnings)}")
    print(f"Unresolved symbols: {reporter.unresolved}")
    print("Output: docs/logic-maps/index.html")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())

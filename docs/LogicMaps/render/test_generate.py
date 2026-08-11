import tempfile
import unittest
from pathlib import Path

import generate as subject


def complete_card_layers(text: str) -> str:
    """Complete temporary test fixtures that are not testing card-layer errors."""
    lines = text.splitlines()
    out: list[str] = []
    index = 0
    declaration = subject.re.compile(r"^\[([^]]+)]\s+(.+?)\s+<([a-z]+)>$")
    while index < len(lines):
        match = declaration.fullmatch(lines[index].strip())
        if not match:
            out.append(lines[index])
            index += 1
            continue
        out.append(lines[index])
        kind = match.group(3)
        index += 1
        block: list[str] = []
        while index < len(lines) and not declaration.fullmatch(lines[index].strip()):
            block.append(lines[index])
            index += 1
        if kind != "junction":
            simple_index = next((i for i, line in enumerate(block) if line.strip().startswith("~")), None)
            if simple_index is None:
                block.insert(0, "~ Test fixture explanation.")
                simple_index = 0
            technical_index = next((i for i, line in enumerate(block) if line.strip() == "[Technical explanation]"), None)
            if technical_index is None:
                block.insert(simple_index + 1, "[Technical explanation]")
                technical_index = simple_index + 1
            if not any(line.strip().startswith(("-", "=", "!")) for line in block[technical_index + 1:]):
                block.insert(technical_index + 1, "= Test fixture technical explanation.")
        out.extend(block)
    return "\n".join(out) + ("\n" if text.endswith("\n") else "")


class GenerationStabilityTests(unittest.TestCase):
    def test_single_map_tab_change_updates_only_local_page_and_global_indexes(self) -> None:
        reporter = subject.Reporter(strict=True)
        source_paths = subject.source_paths_read()
        parsed = [subject.parse_map(path, reporter) for path in source_paths]
        ids = {item.headers["id"]: item for item in parsed if "id" in item.headers}
        fragments = {item.headers["fragment"]: item for item in parsed if "fragment" in item.headers}

        linked_targets = {
            edge.target[4:]
            for item in ids.values()
            for node in item.nodes
            for edge in node.edges
            if edge.target.startswith("map:")
        }
        changed = next((
            item for item in ids.values()
            if item.headers.get("event-ref")
            and len(subject.map_tabs(item)) == 1
            and item.headers["id"] not in linked_targets
        ), None)
        if changed is None:
            self.skipTest("No single-tab code-bound UI map source is available in this archive.")

        original_root = subject.MAP_ROOT
        original_tabs = changed.headers["tabs"]
        original_raw = changed.raw
        try:
            with tempfile.TemporaryDirectory() as temporary:
                subject.MAP_ROOT = Path(temporary)
                first_updated, first_removed = subject.generate(ids, fragments, reporter)
                self.assertGreater(first_updated, 3)
                self.assertEqual(0, first_removed)

                unchanged_updated, unchanged_removed = subject.generate(ids, fragments, reporter)
                self.assertEqual(0, unchanged_updated)
                self.assertEqual(0, unchanged_removed)

                changed.headers["tabs"] = "Edit" if original_tabs != "Edit" else "Split"
                changed.raw += "\n# simulated tab change"
                local_updated, local_removed = subject.generate(ids, fragments, reporter)
                self.assertEqual(4, local_updated)
                self.assertEqual(0, local_removed)
        finally:
            changed.headers["tabs"] = original_tabs
            changed.raw = original_raw
            subject.MAP_ROOT = original_root


class SourceDiscoveryTests(unittest.TestCase):
    def test_discovers_lmap_anywhere_below_source_root(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary) / "source"
            arbitrary = root / "experimental" / "nested" / "anything.lmap"
            arbitrary.parent.mkdir(parents=True)
            arbitrary.write_text("@format 1\n", encoding="utf-8")
            ignored = Path(temporary) / "outside.lmap"
            ignored.write_text("@format 1\n", encoding="utf-8")

            original_source_root = subject.SOURCE_ROOT
            try:
                subject.SOURCE_ROOT = root
                self.assertEqual([arbitrary], subject.source_paths_read())
            finally:
                subject.SOURCE_ROOT = original_source_root


    def test_arbitrary_source_subtree_generates_matching_html_path(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            source_root = root / "source"
            source = source_root / "experimental" / "nested" / "anything.lmap"
            source.parent.mkdir(parents=True)
            source.write_text(
                "@format 1\n"
                "@id functionality.test\n"
                "@title Arbitrary Source Test\n"
                "@section Functionality\n"
                "@area Media discovery and import\n"
                "@entry start\n"
                "@summary Verify recursive source discovery and output mirroring.\n"
                "[start] Start <input>\n"
                "> done\n"
                "[done] Done <output>\n"
                "= Complete\n",
                encoding="utf-8",
            )

            original_source_root = subject.SOURCE_ROOT
            original_map_root = subject.MAP_ROOT
            original_code_root = subject.CODE_ROOT
            original_event_reader = subject.ui_event_references
            try:
                subject.SOURCE_ROOT = source_root
                subject.MAP_ROOT = root / "out"
                subject.CODE_ROOT = root / "src"
                subject.ui_event_references = lambda _: set()

                source_paths = subject.source_paths_read()
                reporter = subject.Reporter(strict=True)
                parsed = [subject.parse_map(path, reporter) for path in source_paths]
                ids, fragments = subject.validate(parsed, reporter)
                self.assertFalse(reporter.errors)
                subject.generate(ids, fragments, reporter)
            finally:
                subject.SOURCE_ROOT = original_source_root
                subject.MAP_ROOT = original_map_root
                subject.CODE_ROOT = original_code_root
                subject.ui_event_references = original_event_reader

            self.assertTrue(
                (root / "out" / "maps" / "experimental" / "nested" / "anything.html").is_file()
            )


class NavigationSeparationTests(unittest.TestCase):
    def test_adding_map_updates_shared_documents_without_rewriting_existing_page(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            source_root = root / "source"
            first = source_root / "alpha" / "first.lmap"
            first.parent.mkdir(parents=True)
            first.write_text(
                "@format 1\n"
                "@id first\n"
                "@title First Map\n"
                "@section Custom Section\n"
                "@area Alpha Area\n"
                "@entry start\n"
                "@summary First map.\n"
                "[start] Start <output>\n"
                "= Done\n", encoding="utf-8")

            original_source_root = subject.SOURCE_ROOT
            original_map_root = subject.MAP_ROOT
            original_code_root = subject.CODE_ROOT
            original_event_reader = subject.ui_event_references
            try:
                subject.SOURCE_ROOT = source_root
                subject.MAP_ROOT = root / "docs" / "LogicMaps"
                subject.CODE_ROOT = root / "src"
                subject.ui_event_references = lambda _: set()

                reporter = subject.Reporter(strict=True)
                parsed = [subject.parse_map(path, reporter) for path in subject.source_paths_read()]
                ids, fragments = subject.validate(parsed, reporter)
                subject.generate(ids, fragments, reporter)
                first_page = subject.MAP_ROOT / "maps" / "alpha" / "first.html"
                before = first_page.read_bytes()

                second = source_root / "brand-new" / "second.lmap"
                second.parent.mkdir(parents=True)
                second.write_text(
                    "@format 1\n"
                    "@id second\n"
                    "@title Second Map\n"
                    "@section Entirely New Section\n"
                    "@area Brand New Area\n"
                    "@entry start\n"
                    "@summary Second map.\n"
                    "[start] Start <output>\n"
                    "= Done\n", encoding="utf-8")

                reporter2 = subject.Reporter(strict=True)
                parsed2 = [subject.parse_map(path, reporter2) for path in subject.source_paths_read()]
                ids2, fragments2 = subject.validate(parsed2, reporter2)
                updated, removed = subject.generate(ids2, fragments2, reporter2)
            finally:
                subject.SOURCE_ROOT = original_source_root
                subject.MAP_ROOT = original_map_root
                subject.CODE_ROOT = original_code_root
                subject.ui_event_references = original_event_reader

            self.assertEqual(before, first_page.read_bytes())
            self.assertTrue((root / "docs" / "MapsLogic.html").is_file())
            navigation = root / "docs" / "LogicMaps" / "maps" / "NavigationLogic.html"
            self.assertTrue(navigation.is_file())
            self.assertTrue((root / "docs" / "LogicMaps" / "maps" / "ImplementationIndex.html").is_file())
            nav_text = navigation.read_text(encoding="utf-8")
            self.assertIn("First Map", nav_text)
            self.assertIn("Second Map", nav_text)
            self.assertIn("Entirely New Section", nav_text)
            self.assertTrue((root / "docs" / "LogicMaps" / "maps" / "brand-new" / "second.html").is_file())
            self.assertGreaterEqual(updated, 3)
            self.assertEqual(0, removed)

    def test_content_page_references_navigation_document_instead_of_embedding_catalog(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            source = root / "source" / "nested" / "map.lmap"
            source.parent.mkdir(parents=True)
            source.write_text(
                "@format 1\n@id map\n@title Map\n@section Section\n@area Area\n"
                "@entry start\n@summary Summary.\n[start] Start <output>\n= Done\n",
                encoding="utf-8")
            reporter = subject.Reporter(strict=True)
            item = subject.parse_map(source, reporter)
            original_source_root = subject.SOURCE_ROOT
            original_map_root = subject.MAP_ROOT
            try:
                subject.SOURCE_ROOT = root / "source"
                subject.MAP_ROOT = root / "docs" / "LogicMaps"
                subject.generate({"map": item}, {}, reporter)
            finally:
                subject.SOURCE_ROOT = original_source_root
                subject.MAP_ROOT = original_map_root
            html = (root / "docs" / "LogicMaps" / "maps" / "nested" / "map.html").read_text(encoding="utf-8")
            self.assertIn('NavigationLogic.html', html)
            self.assertIn('<iframe class="nav-frame"', html)
            self.assertNotIn('class="maplink"', html)


class MultipleEntryTests(unittest.TestCase):
    def test_map_entries_accepts_multiple_roots(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            path = Path(temporary) / "multi.lmap"
            path.write_text(
                "@format 1\n"
                "@id functionality.test\n"
                "@title Test\n"
                "@section Functionality\n"
                "@area Media discovery and import\n"
                "@entry first, second\n"
                "@summary Test multiple entry parsing.\n"
                "[first] First <input>\n"
                "[second] Second <input>\n",
                encoding="utf-8",
            )
            reporter = subject.Reporter(strict=False)
            item = subject.parse_map(path, reporter)
            self.assertEqual(["first", "second"], subject.map_entries(item))

    def test_validation_reaches_every_declared_entry_tree(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            path = root / "multi.lmap"
            path.write_text(
                "@format 1\n"
                "@id functionality.test\n"
                "@title Test\n"
                "@section Functionality\n"
                "@area Media discovery and import\n"
                "@entry first, second\n"
                "@summary Test multiple entry reachability.\n"
                "[first] First <input>\n"
                "> first-result\n"
                "[first-result] First result <output>\n"
                "[second] Second <input>\n"
                "> second-result\n"
                "[second-result] Second result <output>\n",
                encoding="utf-8",
            )
            reporter = subject.Reporter(strict=False)
            item = subject.parse_map(path, reporter)
            original_code_root = subject.CODE_ROOT
            original_event_reader = subject.ui_event_references
            try:
                subject.CODE_ROOT = root / "src"
                subject.ui_event_references = lambda _: set()
                subject.validate([item], reporter)
            finally:
                subject.CODE_ROOT = original_code_root
                subject.ui_event_references = original_event_reader
            self.assertFalse(reporter.errors)
            self.assertFalse(any("unreachable" in warning for warning in reporter.warnings))


class EntryRenderingTests(unittest.TestCase):
    def test_declared_entries_are_embedded_in_generated_html(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            source = root / "multi.lmap"
            source.write_text(
                "@format 1\n"
                "@id functionality.test\n"
                "@title Test\n"
                "@section Functionality\n"
                "@area Media discovery and import\n"
                "@entry first, second\n"
                "@summary Test HTML entry metadata.\n"
                "[first] First <input>\n"
                "> result\n"
                "[second] Second <input>\n"
                "> result\n"
                "[result] Result <output>\n"
                "= Done\n",
                encoding="utf-8",
            )
            reporter = subject.Reporter(strict=False)
            item = subject.parse_map(source, reporter)
            original_map_root = subject.MAP_ROOT
            original_source_root = subject.SOURCE_ROOT
            try:
                subject.MAP_ROOT = root / "out"
                subject.SOURCE_ROOT = root
                updated, removed = subject.generate(
                    {"functionality.test": item}, {}, reporter,
                    only_map_ids={"functionality.test"},
                )
            finally:
                subject.MAP_ROOT = original_map_root
                subject.SOURCE_ROOT = original_source_root
            self.assertGreater(updated, 0)
            self.assertEqual(0, removed)
            html = (root / "out" / "maps" / "multi.html").read_text(encoding="utf-8")
            self.assertIn('data-entries="[&quot;first&quot;,&quot;second&quot;]"', html)
            self.assertEqual(2, html.count('data-entry="true"'))
            self.assertFalse((root / "out" / "index.html").exists())

    def test_declared_entry_cannot_have_incoming_local_edge(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            path = root / "bad-entry.lmap"
            path.write_text(
                "@format 1\n"
                "@id functionality.test\n"
                "@title Test\n"
                "@section Functionality\n"
                "@area Media discovery and import\n"
                "@entry first, second\n"
                "@summary Test root enforcement.\n"
                "[first] First <input>\n"
                "> second\n"
                "[second] Second <input>\n",
                encoding="utf-8",
            )
            reporter = subject.Reporter(strict=False)
            item = subject.parse_map(path, reporter)
            original_code_root = subject.CODE_ROOT
            original_event_reader = subject.ui_event_references
            try:
                subject.CODE_ROOT = root / "src"
                subject.ui_event_references = lambda _: set()
                subject.validate([item], reporter)
            finally:
                subject.CODE_ROOT = original_code_root
                subject.ui_event_references = original_event_reader
            self.assertTrue(any("must be a graph root" in error for error in reporter.errors))


class SourceClassificationTests(unittest.TestCase):
    def _validate_texts(self, texts: list[str]):
        temporary = tempfile.TemporaryDirectory()
        root = Path(temporary.name)
        items = []
        reporter = subject.Reporter(strict=False)
        for index, text in enumerate(texts):
            path = root / f"source-{index}.lmap"
            path.write_text(complete_card_layers(text), encoding="utf-8")
            items.append(subject.parse_map(path, reporter))
        original_code_root = subject.CODE_ROOT
        original_event_reader = subject.ui_event_references
        try:
            subject.CODE_ROOT = root / "src"
            subject.ui_event_references = lambda _: set()
            ids, fragments = subject.validate(items, reporter)
        finally:
            subject.CODE_ROOT = original_code_root
            subject.ui_event_references = original_event_reader
        return temporary, reporter, ids, fragments

    def test_unclassified_lmap_is_an_error(self) -> None:
        temporary, reporter, ids, fragments = self._validate_texts([
            "@format 1\n@entry start\n[start] Start <output>\n= Done\n"
        ])
        try:
            self.assertTrue(any("exactly one of @id or @fragment" in error for error in reporter.errors))
            self.assertFalse(ids)
            self.assertFalse(fragments)
        finally:
            temporary.cleanup()

    def test_lmap_cannot_be_both_map_and_fragment(self) -> None:
        temporary, reporter, _, _ = self._validate_texts([
            "@format 1\n@id one\n@fragment also-one\n@title Both\n@section Section\n@area Area\n@entry start\n@summary Both.\n[start] Start <output>\n= Done\n"
        ])
        try:
            self.assertTrue(any("not both" in error for error in reporter.errors))
        finally:
            temporary.cleanup()

    def test_duplicate_fragment_id_is_an_error(self) -> None:
        fragment = "@format 1\n@fragment shared\n@title Shared\n@entry start\n@summary Shared.\n[start] Start <output>\n= Done\n"
        temporary, reporter, _, fragments = self._validate_texts([fragment, fragment.replace("Shared", "Other")])
        try:
            self.assertTrue(any("Duplicate fragment ID 'shared'" in error for error in reporter.errors))
            self.assertEqual(["shared"], list(fragments))
        finally:
            temporary.cleanup()


class TargetedGenerationTests(unittest.TestCase):
    def test_targeted_generation_builds_shared_documents_and_any_missing_pages(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            source_root = root / "source"
            source_root.mkdir()
            template = "@format 1\n@id {id}\n@title {title}\n@section Section\n@area Area\n@entry start\n@summary Summary.\n[start] Start <output>\n= Done\n"
            (source_root / "one.lmap").write_text(template.format(id="one", title="One"), encoding="utf-8")
            (source_root / "two.lmap").write_text(template.format(id="two", title="Two"), encoding="utf-8")
            original_source_root = subject.SOURCE_ROOT
            original_map_root = subject.MAP_ROOT
            original_code_root = subject.CODE_ROOT
            original_event_reader = subject.ui_event_references
            try:
                subject.SOURCE_ROOT = source_root
                subject.MAP_ROOT = root / "docs" / "LogicMaps"
                subject.CODE_ROOT = root / "src"
                subject.ui_event_references = lambda _: set()
                reporter = subject.Reporter(strict=True)
                items = [subject.parse_map(path, reporter) for path in subject.source_paths_read()]
                ids, fragments = subject.validate(items, reporter)
                subject.generate(ids, fragments, reporter, only_map_ids={"one"})
            finally:
                subject.SOURCE_ROOT = original_source_root
                subject.MAP_ROOT = original_map_root
                subject.CODE_ROOT = original_code_root
                subject.ui_event_references = original_event_reader
            self.assertTrue((root / "docs" / "MapsLogic.html").is_file())
            navigation = root / "docs" / "LogicMaps" / "maps" / "NavigationLogic.html"
            self.assertTrue(navigation.is_file())
            self.assertIn("One", navigation.read_text(encoding="utf-8"))
            self.assertIn("Two", navigation.read_text(encoding="utf-8"))
            self.assertTrue((root / "docs" / "LogicMaps" / "maps" / "one.html").is_file())
            self.assertTrue((root / "docs" / "LogicMaps" / "maps" / "two.html").is_file())

    def test_targeted_generation_does_not_rewrite_existing_unrelated_page(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            source_root = root / "source"
            source_root.mkdir()
            template = "@format 1\n@id {id}\n@title {title}\n@section Section\n@area Area\n@entry start\n@summary Summary.\n[start] Start <output>\n= Done\n"
            (source_root / "one.lmap").write_text(template.format(id="one", title="One"), encoding="utf-8")
            (source_root / "two.lmap").write_text(template.format(id="two", title="Two"), encoding="utf-8")
            original_source_root = subject.SOURCE_ROOT
            original_map_root = subject.MAP_ROOT
            original_code_root = subject.CODE_ROOT
            original_event_reader = subject.ui_event_references
            try:
                subject.SOURCE_ROOT = source_root
                subject.MAP_ROOT = root / "docs" / "LogicMaps"
                subject.CODE_ROOT = root / "src"
                subject.ui_event_references = lambda _: set()
                reporter = subject.Reporter(strict=True)
                items = [subject.parse_map(path, reporter) for path in subject.source_paths_read()]
                ids, fragments = subject.validate(items, reporter)
                subject.generate(ids, fragments, reporter)
                unrelated = subject.MAP_ROOT / "maps" / "two.html"
                before = unrelated.read_bytes()
                subject.generate(ids, fragments, reporter, only_map_ids={"one"})
                self.assertEqual(before, unrelated.read_bytes())
            finally:
                subject.SOURCE_ROOT = original_source_root
                subject.MAP_ROOT = original_map_root
                subject.CODE_ROOT = original_code_root
                subject.ui_event_references = original_event_reader


class NavigationTaxonomyTests(unittest.TestCase):
    def test_tabs_are_used_for_any_source_declared_section(self) -> None:
        catalog = [{
            "id": "map", "title": "Map", "section": "Completely Custom Section",
            "area": "Fallback Area", "tabs": ["Custom Tab"], "href": "map.html",
            "summary": "Summary", "event_ref": None, "search": "map",
        }]
        html = subject.navigation_html(catalog, [])
        self.assertIn("Custom Tab", html)
        self.assertNotIn("Fallback Area", html)


class VirtualNodeIdentityTests(unittest.TestCase):
    def test_repeated_external_references_get_unique_collision_safe_ids(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            source_root = root / "source"
            source_root.mkdir()
            target = source_root / "target.lmap"
            target.write_text(
                "@format 1\n@id target\n@title Target\n@section Section\n@area Area\n@entry t\n@summary Target.\n[t] Target <output>\n= Done\n",
                encoding="utf-8")
            main = source_root / "main.lmap"
            main.write_text(
                "@format 1\n@id main\n@title Main\n@section Section\n@area Area\n@entry a, b\n@summary Main.\n"
                "[a] A <input>\n> map:target\n"
                "[b] B <input>\n> map:target\n",
                encoding="utf-8")
            original_source_root = subject.SOURCE_ROOT
            original_map_root = subject.MAP_ROOT
            original_code_root = subject.CODE_ROOT
            original_event_reader = subject.ui_event_references
            try:
                subject.SOURCE_ROOT = source_root
                subject.MAP_ROOT = root / "docs" / "LogicMaps"
                subject.CODE_ROOT = root / "src"
                subject.ui_event_references = lambda _: set()
                reporter = subject.Reporter(strict=True)
                items = [subject.parse_map(path, reporter) for path in subject.source_paths_read()]
                ids, fragments = subject.validate(items, reporter)
                self.assertFalse(reporter.errors)
                subject.generate(ids, fragments, reporter)
            finally:
                subject.SOURCE_ROOT = original_source_root
                subject.MAP_ROOT = original_map_root
                subject.CODE_ROOT = original_code_root
                subject.ui_event_references = original_event_reader
            html = (root / "docs" / "LogicMaps" / "maps" / "main.html").read_text(encoding="utf-8")
            self.assertEqual(1, html.count('data-id="__lmap_map_target"'))
            self.assertEqual(1, html.count('data-id="__lmap_map_target_2"'))
            self.assertNotIn('data-id="__lmap_map_target_3"', html)


class ThemeSynchronizationTests(unittest.TestCase):
    def test_theme_script_propagates_theme_to_navigation_iframe(self) -> None:
        script = (Path(subject.__file__).parent / "site.js").read_text(encoding="utf-8")
        self.assertIn("postMessage({type:'lmap-theme',theme},'*')", script)
        self.assertIn("event.data?.type==='lmap-theme'", script)
        self.assertIn("iframe.nav-frame", script)


class IndexDocumentTests(unittest.TestCase):
    def test_implementation_index_is_separate_from_mapslogic(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            source_root = root / "source"
            source_root.mkdir()
            source = source_root / "map.lmap"
            source.write_text(
                "@format 1\n@id map\n@title Map\n@section Functionality\n@area Media\n"
                "@entry start\n@summary Summary.\n"
                "@alias owner = Example.Owner\n"
                "[start] Start <process>\n"
                "- Do work @ owner.Run(...)\n"
                "= Done\n",
                encoding="utf-8",
            )
            reporter = subject.Reporter(strict=False)
            item = subject.parse_map(source, reporter)
            original_source_root = subject.SOURCE_ROOT
            original_map_root = subject.MAP_ROOT
            try:
                subject.SOURCE_ROOT = source_root
                subject.MAP_ROOT = root / "docs" / "LogicMaps"
                subject.generate({"map": item}, {}, reporter)
            finally:
                subject.SOURCE_ROOT = original_source_root
                subject.MAP_ROOT = original_map_root

            maps_index = (root / "docs" / "MapsLogic.html").read_text(encoding="utf-8")
            implementation_index = (root / "docs" / "LogicMaps" / "maps" / "ImplementationIndex.html").read_text(encoding="utf-8")
            navigation = (root / "docs" / "LogicMaps" / "maps" / "NavigationLogic.html").read_text(encoding="utf-8")
            self.assertNotIn('id="implementations"', maps_index)
            self.assertNotIn("Direct UI-event coverage", maps_index)
            self.assertIn("Implementation index", implementation_index)
            self.assertIn("Example.Owner.Run", implementation_index)
            self.assertIn('href="map.html#action-start-0"', implementation_index)
            self.assertIn("ImplementationIndex.html", navigation)

    def test_section_display_has_no_generator_number_prefix(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            source_root = root / "source"
            source_root.mkdir()
            source = source_root / "map.lmap"
            source.write_text(
                "@format 1\n@id map\n@title Map\n@section Functionality\n@area Media discovery and import\n"
                "@entry start\n@summary Summary.\n[start] Start <output>\n= Done\n",
                encoding="utf-8",
            )
            reporter = subject.Reporter(strict=True)
            item = subject.parse_map(source, reporter)
            original_source_root = subject.SOURCE_ROOT
            original_map_root = subject.MAP_ROOT
            try:
                subject.SOURCE_ROOT = source_root
                subject.MAP_ROOT = root / "docs" / "LogicMaps"
                subject.generate({"map": item}, {}, reporter)
            finally:
                subject.SOURCE_ROOT = original_source_root
                subject.MAP_ROOT = original_map_root
            maps_index = (root / "docs" / "MapsLogic.html").read_text(encoding="utf-8")
            self.assertIn('<div class="major-heading"><h1>Functionality</h1></div>', maps_index)
            self.assertNotIn('<div class="major-heading"><span>', maps_index)


class CardExplanationTests(unittest.TestCase):
    def test_all_project_cards_have_explicit_simple_and_technical_explanations(self) -> None:
        reporter = subject.Reporter(strict=True)
        items = [subject.parse_map(path, reporter) for path in subject.source_paths_read()]
        subject.validate(items, reporter)
        self.assertFalse(reporter.errors, "\n".join(reporter.errors))
        missing = [
            f"{item.path}:{node.line} [{node.id}]"
            for item in items
            for node in item.nodes
            if node.kind != "junction" and (
                not node.explanation.strip()
                or node.technical_label != "Technical explanation"
                or not (node.actions or node.states or node.notes)
            )
        ]
        self.assertFalse(missing, "Cards missing explicit simple/technical source content:\n" + "\n".join(missing))

    def test_missing_explanation_layer_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            source = root / "map.lmap"
            source.write_text(
                "@format 1\n@id map\n@title Map\n@section Functionality\n@area Area\n"
                "@entry start\n@summary Summary.\n"
                "[start] Start <output>\n"
                "~ Plain explanation.\n"
                "= Technical state without a block.\n",
                encoding="utf-8",
            )
            reporter = subject.Reporter(strict=False)
            item = subject.parse_map(source, reporter)
            original_code_root = subject.CODE_ROOT
            try:
                subject.CODE_ROOT = root / "src"
                subject.CODE_ROOT.mkdir()
                subject.validate([item], reporter)
            finally:
                subject.CODE_ROOT = original_code_root
            self.assertTrue(any("explicit [Technical explanation] block" in error for error in reporter.errors))

    def test_simple_explanations_do_not_expose_internal_member_names(self) -> None:
        reporter = subject.Reporter(strict=True)
        items = [subject.parse_map(path, reporter) for path in subject.source_paths_read()]
        exposed = []
        for item in items:
            for node in item.nodes:
                if node.kind == "junction":
                    continue
                if subject.re.search(r"\b[PL][A-Z][A-Za-z0-9]*\b|\b(?:Flyleaf|mpv|FFprobe|FFmpeg|PreviewTextInput|PreviewKeyDown|PreviewMouseMove|MouseLeftButtonDown|MouseLeftButtonUp|LostMouseCapture|LostFocus)\b", node.explanation):
                    exposed.append(f"{item.path}:{node.line} [{node.id}] {node.explanation}")
        self.assertFalse(exposed, "Implementation names leaked into simple explanations:\n" + "\n".join(exposed))

    def test_generated_card_places_simple_text_before_subdued_technical_box(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            source_root = root / "source"
            source_root.mkdir()
            source = source_root / "map.lmap"
            source.write_text(
                "@format 1\n@id map\n@title Map\n@section Functionality\n@area Area\n"
                "@entry start\n@summary Summary.\n@alias owner = Example.Owner\n"
                "[start] Internal operation <process>\n"
                "~ The program prepares the selected item.\n"
                "[Technical explanation]\n"
                "- Run the internal operation @ owner.Run(...)\n"
                "= Internal state is updated.\n",
                encoding="utf-8",
            )
            reporter = subject.Reporter(strict=False)
            item = subject.parse_map(source, reporter)
            original_source_root = subject.SOURCE_ROOT
            original_map_root = subject.MAP_ROOT
            try:
                subject.SOURCE_ROOT = source_root
                subject.MAP_ROOT = root / "docs" / "LogicMaps"
                subject.generate({"map": item}, {}, reporter)
            finally:
                subject.SOURCE_ROOT = original_source_root
                subject.MAP_ROOT = original_map_root
            rendered = (root / "docs" / "LogicMaps" / "maps" / "map.html").read_text(encoding="utf-8")
            simple = rendered.index('<div class="simple-explanation">The program prepares the selected item.</div>')
            technical = rendered.index('<div class="technical-explanation">')
            self.assertLess(simple, technical)
            self.assertIn('<div class="technical-label">Technical explanation</div>', rendered)
            self.assertIn('Run the internal operation', rendered[technical:])
            self.assertIn('owner.Run(...)', rendered[technical:])

    def test_generator_does_not_synthesize_explanations_or_linked_map_cards(self) -> None:
        generator = (Path(subject.__file__).parent / "generate.py").read_text(encoding="utf-8")
        self.assertNotIn("node.explanation or node.title", generator)
        self.assertNotIn("The outgoing line labels define this decision", generator)
        self.assertNotIn("Continue to “", generator)
        self.assertNotIn('data-virtual="true"', generator)
        self.assertIn("node.technical_label", generator)

    def test_technical_box_style_is_neutral_and_separate(self) -> None:
        style = (Path(subject.__file__).parent / "site.css").read_text(encoding="utf-8")
        self.assertIn(".simple-explanation", style)
        self.assertIn(".technical-explanation", style)
        self.assertIn(".technical-label", style)
        self.assertIn("border:1px solid color-mix(in srgb,var(--border)", style)


class GeneratedOutputTopologyTests(unittest.TestCase):
    def test_logicmaps_is_the_single_authoring_and_generated_root(self) -> None:
        logic_root = Path(subject.__file__).resolve().parent.parent
        self.assertEqual(logic_root.name, "LogicMaps")
        self.assertEqual(subject.DEFAULT_MAP_ROOT, logic_root)
        self.assertEqual(subject.SOURCE_ROOT, logic_root / "source")
        self.assertTrue((logic_root / "SpecificationLmap.md").is_file())
        legacy_name = "-".join(("logic", "maps"))
        self.assertFalse(any(path.is_dir() and path.name == legacy_name for path in logic_root.parent.iterdir()))

    def test_all_generated_html_except_mapslogic_is_below_logicmaps_maps(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            source_root = root / "source"
            source_root.mkdir()
            (source_root / "map.lmap").write_text(
                "@format 1\n@id map\n@title Map\n@section Functionality\n@area Area\n"
                "@entry start\n@summary Summary.\n[start] Start <output>\n= Done\n",
                encoding="utf-8",
            )
            reporter = subject.Reporter(strict=True)
            item = subject.parse_map(source_root / "map.lmap", reporter)
            original_source_root = subject.SOURCE_ROOT
            original_map_root = subject.MAP_ROOT
            try:
                subject.SOURCE_ROOT = source_root
                subject.MAP_ROOT = root / "docs" / "LogicMaps"
                subject.generate({"map": item}, {}, reporter)
            finally:
                subject.SOURCE_ROOT = original_source_root
                subject.MAP_ROOT = original_map_root

            html_files = sorted((root / "docs").rglob("*.html"))
            self.assertIn(root / "docs" / "MapsLogic.html", html_files)
            others = [path for path in html_files if path.name != "MapsLogic.html"]
            self.assertTrue(others)
            maps_root = root / "docs" / "LogicMaps" / "maps"
            self.assertTrue(all(path.is_relative_to(maps_root) for path in others))
            self.assertTrue((maps_root / "NavigationLogic.html").is_file())
            self.assertTrue((maps_root / "ImplementationIndex.html").is_file())
            self.assertTrue((maps_root / "map.html").is_file())


class MapInteractionAssetTests(unittest.TestCase):
    def test_pan_and_text_selection_have_distinct_start_surfaces(self) -> None:
        root = Path(subject.__file__).parent
        script = (root / "site.js").read_text(encoding="utf-8")
        style = (root / "site.css").read_text(encoding="utf-8")
        self.assertIn("event.target.closest('.node')", script)
        self.assertIn("canvas.addEventListener('selectstart'", script)
        self.assertIn(".canvas{user-select:none;cursor:grab}", style)
        self.assertIn(".node{user-select:text;cursor:auto}", style)

    def test_renderer_has_no_crossing_bridge_fallback(self) -> None:
        root = Path(subject.__file__).parent
        script = (root / "site.js").read_text(encoding="utf-8")
        style = (root / "site.css").read_text(encoding="utf-8")
        self.assertNotIn("crossingBridges", script)
        self.assertNotIn("edge-bridge", script)
        self.assertNotIn(".edge-bridge", style)
        self.assertIn("merge-junction", script)
        self.assertIn(".merge-junction", style)

    def test_renderer_preserves_source_order_and_keeps_primary_edges_between_rows(self) -> None:
        root = Path(subject.__file__).parent
        script = (root / "site.js").read_text(encoding="utf-8")
        generator = (root / "generate.py").read_text(encoding="utf-8")
        self.assertNotIn("reduceCrossings();", script)
        self.assertNotIn("adjacentPlan(fromLevel,toLevel)", script)
        self.assertNotIn("placeVirtualTargets();", script)
        self.assertNotIn('data-virtual="true"', generator)
        self.assertIn("if(to.level===from.level+1&&edge.marker!=='loop')", script)
        self.assertIn("rowMetrics[from.level].bottom+24", script)
        self.assertIn("rowMetrics[to.level].top-24", script)
        self.assertIn("orderedOutgoing(edge)", script)
        self.assertIn("orderedIncoming(edge)", script)
        self.assertIn("return edge.marker==='loop'||depth[edge.to]!==depth[edge.from]+1;", script)

    def test_edge_labels_use_collision_aware_placement(self) -> None:
        root = Path(subject.__file__).parent
        script = (root / "site.js").read_text(encoding="utf-8")
        self.assertIn("labelRectClear", script)
        self.assertIn("rectOverlaps", script)
        self.assertIn("segmentHitsRect", script)
        self.assertIn("const placed=[];", script)
        self.assertIn("addLabel(item,routes,placed)", script)


class JunctionTopologyTests(unittest.TestCase):
    def _validate_text(self, text: str):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            source = root / "map.lmap"
            source.write_text(complete_card_layers(text), encoding="utf-8")
            reporter = subject.Reporter(strict=True)
            item = subject.parse_map(source, reporter)
            original_code_root = subject.CODE_ROOT
            try:
                subject.CODE_ROOT = root / "src"
                subject.CODE_ROOT.mkdir()
                subject.validate([item], reporter)
            finally:
                subject.CODE_ROOT = original_code_root
            return reporter

    def test_convergence_requires_explicit_junction(self) -> None:
        reporter = self._validate_text(
            "@format 1\n@id x\n@title X\n@section Functionality\n@area Test\n@entry a, b\n@summary X.\n"
            "[a] A <input>\n> target\n\n[b] B <input>\n> target\n\n[target] Target <output>\n= Done.\n"
        )
        self.assertTrue(any("Converging paths must merge through an explicit junction" in error for error in reporter.errors))

    def test_explicit_junction_allows_shared_line(self) -> None:
        reporter = self._validate_text(
            "@format 1\n@id x\n@title X\n@section Functionality\n@area Test\n@entry a, b\n@summary X.\n"
            "[a] A <input>\n> join\n\n[b] B <input>\n> join\n\n[join] Paths converge <junction>\n> target\n\n[target] Target <output>\n= Done.\n"
        )
        self.assertFalse(reporter.errors)

    def test_junction_cannot_contain_actions_or_branch(self) -> None:
        reporter = self._validate_text(
            "@format 1\n@id x\n@title X\n@section Functionality\n@area Test\n@entry a\n@summary X.\n"
            "[a] A <input>\n> join\n\n[join] Bad join <junction>\n= Not allowed.\n> one\n> two\n\n[one] One <output>\n= Done.\n\n[two] Two <output>\n= Done.\n"
        )
        self.assertTrue(any("Junction 'join' cannot contain actions" in error for error in reporter.errors))
        self.assertTrue(any("must have exactly one unconditional outgoing edge" in error for error in reporter.errors))

    def test_source_order_crossing_is_rejected(self) -> None:
        reporter = self._validate_text(
            "@format 1\n@id x\n@title X\n@section Functionality\n@area Test\n@entry a, b\n@summary X.\n"
            "[a] A <input>\n> right\n\n[b] B <input>\n> left\n\n[left] Left <output>\n= Done.\n\n[right] Right <output>\n= Done.\n"
        )
        self.assertTrue(any("primary-flow line crossing" in error for error in reporter.errors))

    def test_loop_marker_is_serialized_for_cycle_routing(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            source_root = root / "source"
            source_root.mkdir()
            source = source_root / "map.lmap"
            source.write_text(complete_card_layers(
                "@format 1\n@id x\n@title X\n@section Functionality\n@area Test\n@entry start\n@summary X.\n"
                "[start] Start <input>\n> join\n\n[join] Join <junction>\n> work\n\n[work] Work <process>\n> again\n\n[again] Again <process>\n> join [loop]\n"
            ), encoding="utf-8")
            reporter = subject.Reporter(strict=True)
            item = subject.parse_map(source, reporter)
            original_source_root = subject.SOURCE_ROOT
            original_map_root = subject.MAP_ROOT
            original_code_root = subject.CODE_ROOT
            try:
                subject.SOURCE_ROOT = source_root
                subject.MAP_ROOT = root / "docs" / "LogicMaps"
                subject.CODE_ROOT = root / "src"
                subject.CODE_ROOT.mkdir()
                ids, fragments = subject.validate([item], reporter)
                self.assertFalse(reporter.errors)
                subject.generate(ids, fragments, reporter)
            finally:
                subject.SOURCE_ROOT = original_source_root
                subject.MAP_ROOT = original_map_root
                subject.CODE_ROOT = original_code_root
            rendered = (root / "docs" / "LogicMaps" / "maps" / "map.html").read_text(encoding="utf-8")
            self.assertIn('&quot;marker&quot;:&quot;loop&quot;', rendered)


class FormatGrammarTests(unittest.TestCase):
    def _validate(self, text: str, *, strict: bool = False):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            path = root / "test.lmap"
            path.write_text(complete_card_layers(text), encoding="utf-8")
            reporter = subject.Reporter(strict=strict)
            item = subject.parse_map(path, reporter)
            original_code_root = subject.CODE_ROOT
            original_event_reader = subject.ui_event_references
            try:
                subject.CODE_ROOT = root / "src"
                subject.CODE_ROOT.mkdir()
                subject.ui_event_references = lambda _: set()
                subject.validate([item], reporter)
            finally:
                subject.CODE_ROOT = original_code_root
                subject.ui_event_references = original_event_reader
            return reporter

    def _map(self, extra_headers: str = "", body: str = "[start] Start <output>\n= Done.\n") -> str:
        return "@format 1\n@id example\n@title Example\n@section Functionality\n@area Test\n@entry start\n@summary Example.\n" + extra_headers + body

    def test_unsupported_format_is_rejected(self) -> None:
        reporter = self._validate(self._map().replace("@format 1", "@format 2"))
        self.assertTrue(any("Unsupported @format '2'" in error for error in reporter.errors))

    def test_unknown_and_duplicate_headers_are_rejected(self) -> None:
        reporter = self._validate(self._map("@banana value\n@title Duplicate\n"))
        self.assertTrue(any("Unknown directive '@banana'" in error for error in reporter.errors))
        self.assertTrue(any("Duplicate header '@title'" in error for error in reporter.errors))

    def test_flow_directive_is_not_format_1(self) -> None:
        reporter = self._validate(self._map("@flow TB\n"))
        self.assertTrue(any("Unknown directive '@flow'" in error for error in reporter.errors))

    def test_identifiers_are_strict(self) -> None:
        reporter = self._validate(self._map().replace("@id example", "@id Example_bad").replace("[start]", "[Start_bad]").replace("@entry start", "@entry Start_bad"))
        self.assertTrue(any("Invalid map ID" in error for error in reporter.errors))
        self.assertTrue(any("Invalid node ID" in error for error in reporter.errors))

    def test_map_and_fragment_share_document_namespace(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            map_path = root / "map.lmap"
            fragment_path = root / "fragment.lmap"
            map_path.write_text(self._map().replace("@id example", "@id shared.id"), encoding="utf-8")
            fragment_path.write_text("@format 1\n@fragment shared.id\n@title Shared\n@entry start\n@summary Shared.\n[start] Start <output>\n= Done.\n", encoding="utf-8")
            reporter = subject.Reporter(strict=False)
            items = [subject.parse_map(map_path, reporter), subject.parse_map(fragment_path, reporter)]
            original_code_root = subject.CODE_ROOT
            try:
                subject.CODE_ROOT = root / "src"
                subject.CODE_ROOT.mkdir()
                subject.validate(items, reporter)
            finally:
                subject.CODE_ROOT = original_code_root
            self.assertTrue(any("share one namespace" in error for error in reporter.errors))

    def test_note_cannot_have_action_and_decision_requires_two_branches(self) -> None:
        text = self._map(body="[start] Start <note>\n- Execute @ owner.Run(...)\n> result\n[result] Result <decision>\n? \"Only\" > done\n[done] Done <output>\n= Done.\n")
        text = text.replace("@summary Example.\n", "@summary Example.\n@alias owner = Example.Owner\n")
        reporter = self._validate(text)
        self.assertTrue(any("Note node 'start' cannot contain executable actions" in error for error in reporter.errors))
        self.assertTrue(any("Decision node 'result' must have at least two" in error for error in reporter.errors))

    def test_implementation_reference_rejects_trailing_junk(self) -> None:
        text = self._map(body="[start] Start <process>\n- Run @ owner.Run(...) trailing-junk\n> done\n[done] Done <output>\n= Done.\n")
        text = text.replace("@summary Example.\n", "@summary Example.\n@alias owner = Example.Owner\n")
        reporter = self._validate(text)
        self.assertTrue(any("Invalid implementation reference syntax" in error for error in reporter.errors))

    def test_fragment_rejects_map_only_headers(self) -> None:
        reporter = self._validate("@format 1\n@fragment shared.example\n@title Shared\n@entry start\n@summary Shared.\n@section Wrong\n[start] Start <output>\n= Done.\n")
        self.assertTrue(any("not allowed on a shared fragment" in error for error in reporter.errors))


class CycleAndRoutingTests(unittest.TestCase):
    def _validate(self, body: str):
        text = "@format 1\n@id cycle.test\n@title Cycle\n@section Functionality\n@area Test\n@entry start\n@summary Cycle.\n" + body
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            path = root / "map.lmap"
            path.write_text(complete_card_layers(text), encoding="utf-8")
            reporter = subject.Reporter(strict=False)
            item = subject.parse_map(path, reporter)
            original_code_root = subject.CODE_ROOT
            try:
                subject.CODE_ROOT = root / "src"
                subject.CODE_ROOT.mkdir()
                subject.validate([item], reporter)
            finally:
                subject.CODE_ROOT = original_code_root
            return reporter

    def test_cycle_requires_loop_marker(self) -> None:
        reporter = self._validate("[start] Start <process>\n> next\n[next] Next <process>\n> start\n")
        self.assertTrue(any("cycle that is not explicitly closed" in error for error in reporter.errors))

    def test_loop_marker_must_close_real_path(self) -> None:
        reporter = self._validate("[start] Start <process>\n> next [loop]\n[next] Next <output>\n= Done.\n")
        self.assertTrue(any("does not close an existing local path" in error for error in reporter.errors))

    def test_valid_loop_closes_acyclic_primary_path(self) -> None:
        reporter = self._validate("[start] Start <input>\n> join\n[join] Loop join <junction>\n> work\n[work] Work <process>\n> again\n[again] Again <process>\n> join [loop]\n")
        self.assertFalse(reporter.errors)

    def test_non_loop_markers_are_rejected(self) -> None:
        for marker in ("left", "right", "up", "down"):
            reporter = self._validate(f"[start] Start <process>\n> done [{marker}]\n[done] Done <output>\n= Done.\n")
            self.assertTrue(any(f"Invalid edge marker '{marker}'" in error for error in reporter.errors))


class EventReferenceGrammarTests(unittest.TestCase):
    def test_event_reference_syntax_families(self) -> None:
        self.assertTrue(subject.valid_event_reference_syntax("cs|PList.cs|Build|button|Click|1"))
        self.assertTrue(subject.valid_event_reference_syntax("addhandler|PList.cs|Build|this|Button.ClickEvent|OnClick|2"))
        self.assertTrue(subject.valid_event_reference_syntax("xaml|Main.xaml|Click|OnClick|1"))
        self.assertTrue(subject.valid_event_reference_syntax("override|Window.cs|OnClosed"))
        self.assertFalse(subject.valid_event_reference_syntax("cs|PList.cs|Build|button|Click|0"))
        self.assertFalse(subject.valid_event_reference_syntax("madeup|PList.cs|Build"))


class FragmentRenderingTests(unittest.TestCase):
    def test_fragment_gets_graph_page_link_and_implementation_index_entry(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            source_root = root / "source"
            source_root.mkdir()
            (source_root / "fragment.lmap").write_text(
                "@format 1\n@fragment shared.example\n@title Shared example\n@entry start\n@summary Shared flow.\n"
                "@alias owner = Example.Owner\n[start] Shared start <process>\n- Run shared work @ owner.Run(...)\n> done\n[done] Shared done <output>\n= Done.\n",
                encoding="utf-8",
            )
            (source_root / "map.lmap").write_text(
                "@format 1\n@id main\n@title Main\n@section Functionality\n@area Test\n@entry start\n@summary Main.\n"
                "[start] Start <input>\n> fragment:shared.example\n",
                encoding="utf-8",
            )
            original_source_root = subject.SOURCE_ROOT
            original_map_root = subject.MAP_ROOT
            original_code_root = subject.CODE_ROOT
            try:
                subject.SOURCE_ROOT = source_root
                subject.MAP_ROOT = root / "docs" / "LogicMaps"
                subject.CODE_ROOT = root / "src"
                subject.CODE_ROOT.mkdir()
                reporter = subject.Reporter(strict=False)
                items = [subject.parse_map(path, reporter) for path in subject.source_paths_read()]
                ids, fragments = subject.validate(items, reporter)
                subject.generate(ids, fragments, reporter)
            finally:
                subject.SOURCE_ROOT = original_source_root
                subject.MAP_ROOT = original_map_root
                subject.CODE_ROOT = original_code_root
            fragment_html = root / "docs" / "LogicMaps" / "maps" / "fragments" / "shared" / "example.html"
            self.assertTrue(fragment_html.is_file())
            self.assertIn("Shared start", fragment_html.read_text(encoding="utf-8"))
            map_html = (root / "docs" / "LogicMaps" / "maps" / "map.html").read_text(encoding="utf-8")
            self.assertIn('href="fragments/shared/example.html"', map_html)
            implementation = (root / "docs" / "LogicMaps" / "maps" / "ImplementationIndex.html").read_text(encoding="utf-8")
            self.assertIn("Shared fragment / Shared example", implementation)
            self.assertIn("Example.Owner.Run", implementation)



class MediaTaxonomySourceTests(unittest.TestCase):
    def _source(self, family: str) -> Path:
        return Path(subject.__file__).parent.parent / "source" / family

    def test_media_loading_tree_is_program_intake_only(self) -> None:
        root = self._source("MediaLoading")
        self.assertEqual(
            {"Docket-insertion.lmap", "File-drops.lmap", "File-pickers.lmap", "Files-intake.lmap"},
            {path.name for path in (root / "Common").glob("*.lmap")},
        )
        self.assertEqual(
            {"In-Audio.lmap", "In-Edit.lmap", "In-Merge.lmap", "In-Worklist.lmap", "In-staged-workspace.lmap"},
            {path.name for path in root.glob("*.lmap")},
        )
        reporter = subject.Reporter(strict=True)
        items = [subject.parse_map(path, reporter) for path in sorted(root.rglob("*.lmap"))]
        self.assertFalse(reporter.errors, "\n".join(reporter.errors))
        self.assertEqual({"Media loading"}, {item.headers.get("section") for item in items})
        combined = "\n".join(path.read_text(encoding="utf-8") for path in root.rglob("*.lmap"))
        for forbidden in ("PViewerSourceOpen", "LMediaLoadAsync", "PViewerMediaRaise", "PViewerMediaClose", "PPlayerMediaApply"):
            self.assertNotIn(forbidden, combined)

    def test_media_loading_covers_all_program_intake_boundaries(self) -> None:
        root = self._source("MediaLoading")
        combined = "\n".join(path.read_text(encoding="utf-8") for path in root.rglob("*.lmap"))
        for implementation in ("PListPathsAdd", "PListMediaScan", "LDocketPathsAdd", "LDocketDeliveredAdd"):
            self.assertIn(implementation, combined)
        entries: set[str] = set()
        for path in root.rglob("*.lmap"):
            item = subject.parse_map(path, subject.Reporter(strict=False))
            entries.update(subject.map_entries(item))
        expected = {
            "files-add", "folder-add", "viewer-drop", "window-drop", "paths-submitted", "docket-request",
            "audio-items-add", "edit-items-add", "merge-group-drop", "merge-items-add", "worklist-relay",
            "delivered-output", "tracked-stage-output", "staged-run",
        }
        self.assertTrue(expected.issubset(entries), sorted(expected - entries))

    def test_preview_removal_and_selection_are_separate_categories(self) -> None:
        expected = {
            "MediaPreview": "Media preview",
            "MediaRemoval": "Media removal",
            "MediaSelection": "Media selection",
        }
        for family, section in expected.items():
            root = self._source(family)
            self.assertTrue(root.is_dir())
            reporter = subject.Reporter(strict=True)
            items = [subject.parse_map(path, reporter) for path in sorted(root.rglob("*.lmap"))]
            self.assertFalse(reporter.errors, "\n".join(reporter.errors))
            self.assertGreater(len(items), 0)
            self.assertEqual({section}, {item.headers.get("section") for item in items})

    def test_all_media_families_have_zero_primary_crossings(self) -> None:
        for family in ("MediaLoading", "MediaPreview", "MediaRemoval", "MediaSelection"):
            root = self._source(family)
            reporter = subject.Reporter(strict=True)
            items = [subject.parse_map(path, reporter) for path in sorted(root.rglob("*.lmap"))]
            ids, fragments = subject.validate(items, reporter)
            self.assertFalse(reporter.errors, "\n".join(reporter.errors))
            self.assertFalse(fragments)
            for item in ids.values():
                count, examples = subject.source_layout_crossings(item, subject.map_entries(item))
                self.assertEqual(0, count, f"{item.path}: {examples}")

    def test_navigation_exposes_distinct_media_categories(self) -> None:
        reporter = subject.Reporter(strict=True)
        roots = [self._source(name) for name in ("MediaLoading", "MediaPreview", "MediaRemoval", "MediaSelection")]
        items = [subject.parse_map(path, reporter) for root in roots for path in sorted(root.rglob("*.lmap"))]
        ids, _ = subject.validate(items, reporter)
        catalog = [{
            "id": item.headers["id"], "title": item.headers["title"], "section": item.headers["section"],
            "area": item.headers["area"], "tabs": subject.map_tabs(item), "href": "example.html",
            "summary": item.headers["summary"], "event_ref": item.headers.get("event-ref"), "search": "example",
        } for item in ids.values()]
        navigation = subject.navigation_html(catalog, fragment_catalog=[])
        for section in ("Media loading", "Media preview", "Media removal", "Media selection"):
            self.assertEqual(1, navigation.count(f'<h2 class="nav-major">{section}</h2>'))
        self.assertIn('<span class="context-badge context-badge-common">Common</span><span class="context-title">Files intake</span>', navigation)
        self.assertIn('<span class="context-badge context-badge-scenario">In Audio</span>', navigation)


class MediaFidelityRegressionTests(unittest.TestCase):
    def _read(self, family: str, relative: str) -> str:
        root = Path(subject.__file__).parent.parent / "source" / family
        return (root / relative).read_text(encoding="utf-8")

    def test_removal_models_replacement_then_clear_change_cleanup(self) -> None:
        source = self._read("MediaRemoval", "Common/Docket-removal.lmap")
        self.assertIn("PListDocketRemoveHandle", source)
        self.assertIn("PListClearChange", source)
        self.assertIn("PViewerMediaClose", source)
        self.assertIn("PFlowClear", source)
        self.assertLess(source.index("[replacement-select]"), source.index("[clear-change]"))
        self.assertLess(source.index("[clear-change]"), source.index("[close-viewer]"))

    def test_files_intake_is_scan_wrapper_and_docket_insertion_owns_add_notifications(self) -> None:
        intake = self._read("MediaLoading", "Common/Files-intake.lmap")
        insertion = self._read("MediaLoading", "Common/Docket-insertion.lmap")
        self.assertIn("PListPathsAdd", intake)
        self.assertIn("PListMediaScan", intake)
        self.assertIn("map:media-loading.common.docket-insertion", intake)
        self.assertIn("LDocketPathsAdd", insertion)
        self.assertIn("PListDocketAddHandle", insertion)
        self.assertIn("PListItemsAdd", insertion)
        self.assertLess(insertion.index("[added-notification]"), insertion.index("[items-added]"))
        self.assertNotIn("PViewerSourceOpen", insertion)

    def test_audio_preview_models_immediate_restore_against_committed_source(self) -> None:
        source = self._read("MediaPreview", "In-Audio.lmap")
        self.assertIn("PViewerSourceOpen", source)
        self.assertIn("PAudioPlanRestore", source)
        self.assertIn("property viewer.PViewerSourcePath", source)
        self.assertLess(source.index("PViewerSourceOpen"), source.index("PAudioPlanRestore"))
        self.assertIn("previous committed source", source)
        self.assertIn("no PViewerMediaChange subscription", source)
        self.assertNotIn("PListItemsAdd", source)

    def test_preview_commits_publish_before_final_playback_work(self) -> None:
        flyleaf = self._read("MediaPreview", "Common/Flyleaf-preview.lmap")
        mpv = self._read("MediaPreview", "Common/mpv-preview.lmap")
        self.assertLess(flyleaf.index("PViewerMediaRaise"), flyleaf.index("PViewerPreviewRestore"))
        self.assertLess(mpv.index("PViewerMediaRaise"), mpv.index("PViewerMpvPreviewApply"))

    def test_worklist_loading_covers_direct_insert_and_delivered_tracking(self) -> None:
        source = self._read("MediaLoading", "In-Worklist.lmap")
        self.assertIn("PListMediaScan", source)
        self.assertIn("LDocketPathsAdd", source)
        self.assertIn("LDocketDeliveredAdd", source)
        self.assertIn("PListDeliveredTrack", source)
        self.assertIn("map:media-loading.common.docket-insertion", source)
        self.assertNotIn("PViewerSourceOpen", source)

    def test_funnel_drain_is_media_removal_not_loading(self) -> None:
        removal = self._read("MediaRemoval", "In-Funnel.lmap")
        loading = "\n".join(path.read_text(encoding="utf-8") for path in (Path(subject.__file__).parent.parent / "source" / "MediaLoading").rglob("*.lmap"))
        self.assertIn("funnel-drain", removal)
        self.assertIn("map:media-removal.common.docket-removal", removal)
        self.assertNotIn("funnel-drain", loading)

    def test_file_drop_loading_excludes_reachable_direct_preview_branch(self) -> None:
        source = self._read("MediaLoading", "Common/File-drops.lmap")
        self.assertIn("current workspace constitution", source)
        self.assertIn("PDropPathsChange", source)
        self.assertNotIn("PViewerSourceOpen", source)
        self.assertIn("AllowedEffects", source)
        self.assertIn("Copy, Move, or Link", source)

    def test_staged_loading_and_preview_rejection_are_separate_maps(self) -> None:
        loading = self._read("MediaLoading", "In-staged-workspace.lmap")
        preview = self._read("MediaPreview", "In-staged-workspace.lmap")
        self.assertIn("LDocketPathsAdd", loading)
        self.assertIn("PListMediaScan or PListPathsAdd", loading)
        self.assertNotIn("PViewerSourceOpen", loading)
        self.assertIn("PViewerCommandSet", preview)
        self.assertIn("PViewerSourceOpen", preview)
        self.assertIn("inactive viewer", preview)

    def test_added_item_subscribers_remain_in_media_loading(self) -> None:
        edit = self._read("MediaLoading", "In-Edit.lmap")
        audio = self._read("MediaLoading", "In-Audio.lmap")
        merge = self._read("MediaLoading", "In-Merge.lmap")
        self.assertIn("PEditItemsHandle", edit)
        self.assertIn("PAudioItemsHandle", audio)
        self.assertIn("PMergeItemsHandle", merge)
        self.assertIn("PGroupAutoUpdate", merge)
        for source in (edit, audio, merge):
            self.assertNotIn("PViewerSourceOpen", source)

    def test_edit_preview_retains_failure_cargo_source_distinction(self) -> None:
        edit = self._read("MediaPreview", "In-Edit.lmap")
        self.assertIn("failure restoration targets the previously committed source", edit)
        self.assertIn("PViewerEditEligible=true", edit)

    def test_mpv_eligibility_is_edit_only_in_current_tab_constitution(self) -> None:
        viewer_gate = self._read("MediaPreview", "Common/Viewer-gate.lmap")
        completion = self._read("MediaPreview", "Common/Completion-routing.lmap")
        edit = self._read("MediaPreview", "In-Edit.lmap")
        self.assertIn("PViewerEditEligible", viewer_gate)
        self.assertIn("PViewerEditEligible", completion)
        self.assertIn("PViewerEditEligible=true", edit)
        self.assertIn("only Edit", edit)

    def test_backend_failure_models_obsolete_without_completion_raise(self) -> None:
        source = self._read("MediaPreview", "Common/Backend-failure.lmap")
        self.assertIn("LMediaLoadFailCurrent", source)
        self.assertIn("Obsolete", source)
        self.assertIn("does not raise LMediaLoadCompleted", source)

    def test_media_publication_does_not_claim_per_subscriber_exception_isolation(self) -> None:
        source = self._read("MediaPreview", "Common/Media-publication.lmap")
        self.assertIn("one try/catch", source)
        self.assertIn("not enumerated and isolated individually", source)
        self.assertIn("later in the invocation list do not run", source)
        self.assertIn("Audio intentionally does not attach PWorkspaceMediaHandle", source)

    def test_global_unload_uses_media_removal_workspace_clear_sequence(self) -> None:
        global_map = self._read("MediaRemoval", "In-Global-interface.lmap")
        workspace = self._read("MediaRemoval", "Common/Workspace-media-clear.lmap")
        self.assertIn("map:media-removal.common.workspace-media-clear", global_map)
        order = [workspace.index(token) for token in ("PViewerMediaClose", "PFlowClear", "PListClear", "PGroupClear")]
        self.assertEqual(order, sorted(order))


class SectionCreationScenarioSourceTests(unittest.TestCase):
    def _source_root(self) -> Path:
        return Path(subject.__file__).parent.parent / "source" / "SectionCreation"

    def test_section_creation_tree_has_common_and_split_creation_maps(self) -> None:
        root = self._source_root()
        self.assertTrue((root / "Common" / "Compass-buttons.lmap").is_file())
        self.assertTrue((root / "Common" / "Keyboard-shortcuts.lmap").is_file())
        self.assertTrue((root / "Common" / "Forward-section-plan.lmap").is_file())
        self.assertTrue((root / "Common" / "Apply-created-state.lmap").is_file())
        split = root / "In-Split-tab"
        self.assertEqual(
            {"Add-at-cursor.lmap", "Set-start-creates-section.lmap", "Set-end-creates-section.lmap", "Split-selected-section.lmap"},
            {path.name for path in split.glob("*.lmap")},
        )

    def test_section_creation_maps_are_one_category_and_crossing_free(self) -> None:
        root = self._source_root()
        reporter = subject.Reporter(strict=True)
        items = [subject.parse_map(path, reporter) for path in sorted(root.rglob("*.lmap"))]
        subject.validate(items, reporter)
        self.assertFalse(reporter.errors, "\n".join(reporter.errors))
        self.assertEqual({"Section creation"}, {item.headers.get("section") for item in items})
        for item in items:
            entries = subject.map_entries(item)
            count, examples = subject.source_layout_crossings(item, entries)
            self.assertEqual(0, count, f"{item.path}: {examples}")

    def test_section_creation_scope_does_not_reference_delete_or_name_operations(self) -> None:
        root = self._source_root()
        text = "\n".join(path.read_text(encoding="utf-8") for path in sorted(root.rglob("*.lmap")))
        self.assertNotIn("flow.PFlowSectionDelete", text)
        self.assertNotIn("flow.PFlowNameSet", text)
        self.assertNotIn("segment.LSegmentDelete", text)
        self.assertNotIn("segment.LSegmentNameSet", text)

    def test_section_creation_shared_effects_preserve_runtime_order(self) -> None:
        path = self._source_root() / "Common" / "Apply-created-state.lmap"
        text = path.read_text(encoding="utf-8")
        order = [
            "[replace-state]",
            "[flow-change]",
            "[section-panel]",
            "[workspace-history]",
            "[sidecar-save]",
            "[caller-log]",
        ]
        positions = [text.index(marker) for marker in order]
        self.assertEqual(positions, sorted(positions))


class SectionModificationScenarioSourceTests(unittest.TestCase):
    def _source_root(self) -> Path:
        return Path(subject.__file__).parent.parent / "source" / "SectionModification"

    def _read(self, relative: str) -> str:
        return (self._source_root() / relative).read_text(encoding="utf-8")

    def test_section_modification_tree_contains_only_direct_edit_manipulations(self) -> None:
        root = self._source_root()
        self.assertEqual(
            {"Compass-buttons.lmap", "Keyboard-shortcuts.lmap", "Apply-modified-state.lmap"},
            {path.name for path in (root / "Common").glob("*.lmap")},
        )
        self.assertEqual(
            {
                "Set-start-on-selected-section.lmap",
                "Set-end-on-selected-section.lmap",
                "Split-selected-section.lmap",
                "Toggle-section.lmap",
                "Reorder-section-by-drag.lmap",
                "Sort-sections.lmap",
            },
            {path.name for path in (root / "In-Split-tab").glob("*.lmap")},
        )

    def test_section_modification_maps_are_one_category_and_crossing_free(self) -> None:
        root = self._source_root()
        reporter = subject.Reporter(strict=True)
        items = [subject.parse_map(path, reporter) for path in sorted(root.rglob("*.lmap"))]
        subject.validate(items, reporter)
        self.assertFalse(reporter.errors, "\n".join(reporter.errors))
        self.assertEqual({"Section modification"}, {item.headers.get("section") for item in items})
        for item in items:
            entries = subject.map_entries(item)
            count, examples = subject.source_layout_crossings(item, entries)
            self.assertEqual(0, count, f"{item.path}: {examples}")

    def test_section_modification_scope_excludes_create_delete_and_name_mutators(self) -> None:
        root = self._source_root()
        source = "\n".join(path.read_text(encoding="utf-8") for path in sorted(root.rglob("*.lmap")))
        self.assertNotIn("flow.PFlowSectionAdd", source)
        self.assertNotIn("segment.LSegmentAdd", source)
        self.assertNotIn("flow.PFlowSectionDelete", source)
        self.assertNotIn("segment.LSegmentDelete", source)
        self.assertNotIn("flow.PFlowNameSet", source)
        self.assertNotIn("segment.LSegmentNameSet", source)

    def test_existing_boundary_maps_model_only_non_creation_branches(self) -> None:
        start = self._read("In-Split-tab/Set-start-on-selected-section.lmap")
        end = self._read("In-Split-tab/Set-end-on-selected-section.lmap")
        self.assertIn("LPieceFloorRead", start)
        self.assertIn("LPieceStartSet", start)
        self.assertIn("Creation branch excluded", start)
        self.assertIn("LPieceLimitRead", end)
        self.assertIn("LPieceEndSet", end)
        self.assertIn("Creation branch excluded", end)
        self.assertIn("map:section-modification.common.apply-modified-state", start)
        self.assertIn("map:section-modification.common.apply-modified-state", end)
        split = self._read("In-Split-tab/Split-selected-section.lmap")
        self.assertIn("LPieceDivide", split)
        self.assertIn("shortened left half", split)
        self.assertIn("section-creation.split-selected-section", split)

    def test_toggle_drag_and_sort_preserve_their_real_ui_specific_behaviors(self) -> None:
        toggle = self._read("In-Split-tab/Toggle-section.lmap")
        drag = self._read("In-Split-tab/Reorder-section-by-drag.lmap")
        sort = self._read("In-Split-tab/Sort-sections.lmap")
        self.assertIn("ClickCount is at least two", toggle)
        self.assertIn("LPieceHidden inverted", toggle)
        self.assertIn("system drag threshold", drag)
        self.assertIn("each live insertion", drag)
        self.assertIn("CurrentCultureIgnoreCase", sort)
        self.assertIn("LPiece value equality", sort)
        self.assertIn("MouseLeftButtonUp", drag)
        self.assertIn("LostMouseCapture", drag)

    def test_shared_apply_orders_publish_panel_history_then_sidecar(self) -> None:
        source = self._read("Common/Apply-modified-state.lmap")
        order = ["[replace-state]", "[flow-publish]", "[panel-update]", "[history-update]", "[sidecar-save]", "[caller-return]"]
        positions = [source.index(marker) for marker in order]
        self.assertEqual(positions, sorted(positions))
        self.assertIn("during live drag it intentionally returns without rebuilding", source)




class SectionNamingScenarioSourceTests(unittest.TestCase):
    def _source_root(self) -> Path:
        return Path(subject.__file__).parent.parent / "source" / "SectionNaming"

    def _read(self, relative: str) -> str:
        return (self._source_root() / relative).read_text(encoding="utf-8")

    def test_section_naming_tree_has_two_common_and_six_split_maps(self) -> None:
        root = self._source_root()
        self.assertEqual(
            {"Keyboard-shortcut.lmap", "Apply-name-state.lmap"},
            {path.name for path in (root / "Common").glob("*.lmap")},
        )
        self.assertEqual(
            {
                "Inline-editor-open.lmap",
                "Inline-editor-input.lmap",
                "Pending-inline-commit.lmap",
                "Switch-inline-edit-target.lmap",
                "Shortcut-popup.lmap",
                "Unnamed-section-display.lmap",
            },
            {path.name for path in (root / "In-Split-tab").glob("*.lmap")},
        )

    def test_section_naming_maps_are_one_category_and_crossing_free(self) -> None:
        root = self._source_root()
        reporter = subject.Reporter(strict=True)
        items = [subject.parse_map(path, reporter) for path in sorted(root.rglob("*.lmap"))]
        subject.validate(items, reporter)
        self.assertFalse(reporter.errors, "\n".join(reporter.errors))
        self.assertEqual({"Section naming"}, {item.headers.get("section") for item in items})
        for item in items:
            count, examples = subject.source_layout_crossings(item, subject.map_entries(item))
            self.assertEqual(0, count, f"{item.path}: {examples}")

    def test_inline_open_selects_section_before_building_editor(self) -> None:
        source = self._read("In-Split-tab/Inline-editor-open.lmap")
        for token in ("PFlowSectionSelect", "LSegmentSelect", "PFlowSegmentHandle", "PSectionEditorBuild"):
            self.assertIn(token, source)
        self.assertLess(source.index("PFlowSectionSelect"), source.index("PSectionEditorBuild"))
        self.assertIn("PWorkspaceSectionHandle", source)

    def test_inline_input_models_buffer_step_commit_cancel_and_deferred_focus(self) -> None:
        source = self._read("In-Split-tab/Inline-editor-input.lmap")
        for token in ("PSectionStepAttach", "PSectionEditCommit", "PSectionEditCancel", "PSectionEditClose", "PSectionFocusCheck"):
            self.assertIn(token, source)
        self.assertIn("DispatcherPriority.Input", source)
        self.assertIn("map:section-naming.common.apply-name-state", source)

    def test_pending_non_name_actions_commit_inline_editor_first(self) -> None:
        source = self._read("In-Split-tab/Pending-inline-commit.lmap")
        for token in ("PSectionDeleteHandle", "PSectionSortHandle", "PSectionUpHandle", "PSectionEditCommit"):
            self.assertIn(token, source)
        self.assertIn("color badge MouseLeftButtonDown", source)
        self.assertIn("before the non-naming action proceeds", source)

    def test_switching_inline_target_can_discard_old_uncommitted_buffer(self) -> None:
        source = self._read("In-Split-tab/Switch-inline-edit-target.lmap")
        self.assertIn("without first committing the old row", source)
        self.assertIn("does not call PSectionEditCommit", source)
        self.assertIn("old row", source)
        self.assertIn("PSectionFocusCheck", source)

    def test_keyboard_shortcut_is_configurable_and_reaches_name_section(self) -> None:
        source = self._read("Common/Keyboard-shortcut.lmap")
        self.assertIn("SectionRename", source)
        self.assertIn("defaults to A", source)
        self.assertIn("LBindingTokenFind", source)
        self.assertIn("nameSection", source)
        self.assertIn("PFlowNameShow", source)
        self.assertIn("TextBoxBase or PasswordBox", source)

    def test_shortcut_popup_only_commits_on_enter(self) -> None:
        source = self._read("In-Split-tab/Shortcut-popup.lmap")
        self.assertIn("commit only on Enter", source)
        self.assertIn("PFlowNameApply", source)
        self.assertIn("Escape", source)
        self.assertIn("without PFlowNameApply", source)
        self.assertIn("StaysOpen=false", source)
        self.assertIn("PFlowClose", source)
        self.assertIn("map:section-naming.common.apply-name-state", source)

    def test_shared_name_apply_orders_publish_history_save_then_log(self) -> None:
        source = self._read("Common/Apply-name-state.lmap")
        order = ["[replace-piece]", "[apply-state]", "[flow-publish]", "[panel-update]", "[history-update]", "[sidecar-save]", "[log-kind]"]
        positions = [source.index(marker) for marker in order]
        self.assertEqual(positions, sorted(positions))
        self.assertIn("ordinal", source)
        self.assertIn("previous", source)
        self.assertIn("affix", source)
        self.assertIn("no naming", source)

    def test_unnamed_placeholder_is_display_only(self) -> None:
        source = self._read("In-Split-tab/Unnamed-section-display.lmap")
        self.assertIn("not written into LPieceName", source)
        self.assertIn("Viewfinder", source)
        self.assertIn("placeholder", source)
        self.assertIn("prefix", source)
        self.assertIn("suffix", source)


if __name__ == "__main__":
    unittest.main()

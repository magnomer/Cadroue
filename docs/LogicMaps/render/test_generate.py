import tempfile
import unittest
from pathlib import Path

import generate as subject


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
            path.write_text(text, encoding="utf-8")
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

    def test_crossings_and_merges_have_distinct_rendering(self) -> None:
        root = Path(subject.__file__).parent
        script = (root / "site.js").read_text(encoding="utf-8")
        style = (root / "site.css").read_text(encoding="utf-8")
        self.assertIn("function crossingBridges(routes)", script)
        self.assertIn("edge-bridge-mask", script)
        self.assertIn("merge-junction", script)
        self.assertIn(".edge-bridge-mask", style)
        self.assertIn(".merge-junction", style)

    def test_layout_uses_local_crossing_optimization_and_shared_target_bundles(self) -> None:
        root = Path(subject.__file__).parent
        script = (root / "site.js").read_text(encoding="utf-8")
        self.assertIn("function adjacentCrossings(level)", script)
        self.assertIn("function improveLocalOrder(level)", script)
        self.assertIn("function adjacentPlan(fromLevel,toLevel)", script)
        self.assertIn("const groupKey=edge.to;", script)
        self.assertIn("bundle.channel", script)
        self.assertIn("function markerBias(marker)", script)
        self.assertIn("edge.marker==='left'||edge.marker==='right'||edge.marker==='loop'", script)
        self.assertIn("const primaryEdges=localEdges.filter(edge=>edge.marker!=='loop')", script)
        self.assertIn("if(to.level===from.level+1&&!edge.marker)", script)
        self.assertIn("for(let sweep=0;sweep<12;sweep++)", script)


class JunctionTopologyTests(unittest.TestCase):
    def _validate_text(self, text: str):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            source = root / "map.lmap"
            source.write_text(text, encoding="utf-8")
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
        self.assertTrue(any("avoidable primary-lane crossing" in error for error in reporter.errors))

    def test_direction_hint_is_serialized_for_source_level_routing(self) -> None:
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            source_root = root / "source"
            source_root.mkdir()
            source = source_root / "map.lmap"
            source.write_text(
                "@format 1\n@id x\n@title X\n@section Functionality\n@area Test\n@entry a\n@summary X.\n"
                "[a] A <input>\n> result [right]\n\n[result] Result <output>\n= Done.\n",
                encoding="utf-8",
            )
            reporter = subject.Reporter(strict=True)
            item = subject.parse_map(source, reporter)
            original_source_root = subject.SOURCE_ROOT
            original_map_root = subject.MAP_ROOT
            try:
                subject.SOURCE_ROOT = source_root
                subject.MAP_ROOT = root / "docs" / "LogicMaps"
                subject.generate({"x": item}, {}, reporter)
            finally:
                subject.SOURCE_ROOT = original_source_root
                subject.MAP_ROOT = original_map_root
            rendered = (root / "docs" / "LogicMaps" / "maps" / "map.html").read_text(encoding="utf-8")
            self.assertIn('&quot;marker&quot;:&quot;right&quot;', rendered)


class FormatGrammarTests(unittest.TestCase):
    def _validate(self, text: str, *, strict: bool = False):
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            path = root / "test.lmap"
            path.write_text(text, encoding="utf-8")
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
            path.write_text(text, encoding="utf-8")
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

    def test_up_and_down_markers_are_rejected(self) -> None:
        for marker in ("up", "down"):
            reporter = self._validate(f"[start] Start <process>\n> done [{marker}]\n[done] Done <output>\n= Done.\n")
            self.assertTrue(any(f"Invalid edge marker '{marker}'" in error for error in reporter.errors))

    def test_hint_does_not_hide_avoidable_crossing(self) -> None:
        text = (
            "@format 1\n@id x\n@title X\n@section Functionality\n@area Test\n@entry a, b\n@summary X.\n"
            "[a] A <input>\n> right [left]\n[b] B <input>\n> left\n[left] Left <output>\n= Done.\n[right] Right <output>\n= Done.\n"
        )
        with tempfile.TemporaryDirectory() as temporary:
            root = Path(temporary)
            path = root / "map.lmap"
            path.write_text(text, encoding="utf-8")
            reporter = subject.Reporter(strict=False)
            item = subject.parse_map(path, reporter)
            original_code_root = subject.CODE_ROOT
            try:
                subject.CODE_ROOT = root / "src"
                subject.CODE_ROOT.mkdir()
                subject.validate([item], reporter)
            finally:
                subject.CODE_ROOT = original_code_root
            self.assertTrue(any("avoidable primary-lane crossing" in error for error in reporter.errors))


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


if __name__ == "__main__":
    unittest.main()

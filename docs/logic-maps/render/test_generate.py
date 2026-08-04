import tempfile
import unittest
from pathlib import Path

import generate as subject


class GenerationStabilityTests(unittest.TestCase):
    def test_single_map_tab_change_updates_only_local_page_and_global_indexes(self) -> None:
        reporter = subject.Reporter(strict=True)
        source_paths = sorted(
            path
            for source_tree in subject.SOURCE_TREES
            if source_tree.is_dir()
            for path in source_tree.rglob("*.lmap")
        )
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
        changed = next(
            item for item in ids.values()
            if item.headers.get("event-ref")
            and len(subject.map_tabs(item)) == 1
            and item.headers["id"] not in linked_targets
        )

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
                self.assertEqual(3, local_updated)
                self.assertEqual(0, local_removed)
        finally:
            changed.headers["tabs"] = original_tabs
            changed.raw = original_raw
            subject.MAP_ROOT = original_root


if __name__ == "__main__":
    unittest.main()

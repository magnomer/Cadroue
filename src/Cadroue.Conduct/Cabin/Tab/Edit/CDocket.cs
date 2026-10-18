using System;
using System.Collections.Generic;
using System.Linq;

namespace Cadroue.Conduct;

// The docket reads every run gate shares over its items: each item is its path, the batch it arrived with and whether
// it is locked. Paths match ignoring case, and a run takes only unlocked items.
internal static class CDocket
{
    // The first item whose path matches, ignoring case; none without a path.
    internal static (string CDocketPath, Guid CDocketBatch, bool CDocketLocked)? CDocketFind(
        IReadOnlyList<(string CDocketPath, Guid CDocketBatch, bool CDocketLocked)> items,
        string? path)
    {
        if (path is null)
        {
            return null;
        }

        foreach (var item in items)
        {
            if (string.Equals(item.CDocketPath, path, StringComparison.OrdinalIgnoreCase))
            {
                return item;
            }
        }

        return null;
    }

    // Every unlocked item, or, given paths, every unlocked item whose path is among them, in docket order.
    internal static (string CDocketPath, Guid CDocketBatch)[] CDocketUnlockedRead(
        IReadOnlyList<(string CDocketPath, Guid CDocketBatch, bool CDocketLocked)> items,
        IReadOnlyList<string>? paths) =>
        items
            .Where(item => !item.CDocketLocked
                && (paths is null || paths.Contains(item.CDocketPath, StringComparer.OrdinalIgnoreCase)))
            .Select(item => (item.CDocketPath, item.CDocketBatch))
            .ToArray();
}

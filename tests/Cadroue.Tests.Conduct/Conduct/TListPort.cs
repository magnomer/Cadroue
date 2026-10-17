using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cadroue.Application;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

// A docket in memory: an add or removal tells the changes the engine tells, in its order. The locked paths of one add
// are one batch.
internal sealed class TListPort : LListPort
{
    private readonly List<(string TListPath, bool TListLocked, int TListBatch)> tListEntries = [];
    private int tListBatch;

    public event Action? LListChanged;

    public event Action<IReadOnlyList<string>>? LListAdded;

    public event Action<IReadOnlyList<LDocketEntry>>? LListEntriesAdded;

    public event Action<IReadOnlyList<string>>? LListRemoved;

    public LDocket LListDocket { get; } = new();

    public List<string> TListCalls { get; } = [];

    public Func<string, string>? TListName { get; set; }

    // Adds the paths as the user's add does; a locked path is a delivered one.
    public void TListPathsAdd(bool locked, params string[] paths)
    {
        tListBatch++;
        tListEntries.AddRange(paths.Select(path => (path, locked, tListBatch)));
        LListChanged?.Invoke();
        LListAdded?.Invoke(paths);
        LListEntriesAdded?.Invoke([]);
    }

    public IReadOnlyList<string> LListPathsRead() => tListEntries.Select(entry => entry.TListPath).ToArray();

    public IReadOnlyList<string> LListUnlockedRead() =>
        tListEntries.Where(entry => !entry.TListLocked).Select(entry => entry.TListPath).ToArray();

    public bool LListPathCheck(string lPath) => tListEntries.Any(entry => TListMatch(entry.TListPath, lPath));

    public bool LListLockCheck(string lPath) =>
        tListEntries.Any(entry => entry.TListLocked && TListMatch(entry.TListPath, lPath));

    public IReadOnlyList<(bool LListLocked, IReadOnlyList<string> LListPaths)> LListCardsRead()
    {
        var cards = new List<(bool, IReadOnlyList<string>)>();
        var shown = new HashSet<int>();
        foreach ((string path, bool locked, int batch) in tListEntries)
        {
            if (!locked)
            {
                cards.Add((false, [path]));
            }
            else if (shown.Add(batch))
            {
                cards.Add((true, tListEntries
                    .Where(entry => entry.TListLocked && entry.TListBatch == batch)
                    .Select(entry => entry.TListPath)
                    .ToArray()));
            }
        }

        return cards;
    }

    public Task<int> LListPathsAdd(IReadOnlyList<string> lPaths)
    {
        TListCalls.Add($"add {string.Join(",", lPaths)}");
        TListPathsAdd(false, lPaths.ToArray());
        return Task.FromResult(lPaths.Count);
    }

    public void LListPathsRemove(IReadOnlyList<string> lPaths)
    {
        TListCalls.Add($"remove {string.Join(",", lPaths)}");
        tListEntries.RemoveAll(entry => lPaths.Any(path => TListMatch(entry.TListPath, path)));
        LListChanged?.Invoke();
        LListRemoved?.Invoke(lPaths);
    }

    public void LListClose() => TListCalls.Add("close");

    public LListPort TListCreate(Func<string, string> name)
    {
        TListName = name;
        return this;
    }

    private static bool TListMatch(string left, string right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
}

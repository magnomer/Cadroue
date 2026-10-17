using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The convert runs' decisions: whether the preset lets a run go, then which docket items run. One per atelier.
public sealed class CConvert
{
    private readonly LConvertPort _cConvertPort;

    internal CConvert(LConvertPort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        _cConvertPort = port;
    }

    // The single run: the first item whose path is the current one, ignoring case, runs while unlocked. Without a valid
    // preset nothing runs and false is answered; otherwise the port is asked once, even with nothing picked.
    public bool CConvertRun(
        bool presetValid,
        string presetName,
        string? path,
        IReadOnlyList<(string CConvertPath, Guid CConvertBatch, bool CConvertLocked)> items,
        bool urgent,
        Guid relayTarget,
        Guid sourceTab)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (!presetValid)
        {
            return false;
        }

        _cConvertPort.LConvertRun(
            presetName,
            items
                .Where(item => path is not null
                    && string.Equals(item.CConvertPath, path, StringComparison.OrdinalIgnoreCase))
                .Take(1)
                .Where(item => !item.CConvertLocked)
                .Select(item => (item.CConvertPath, item.CConvertBatch))
                .ToArray(),
            urgent,
            relayTarget,
            sourceTab);
        return true;
    }

    // Every unlocked item runs, or, given paths, every unlocked item whose path is among them, in docket order, at
    // normal priority. Without a valid preset nothing runs and false is answered.
    public bool CConvertAllRun(
        bool presetValid,
        string presetName,
        IReadOnlyList<(string CConvertPath, Guid CConvertBatch, bool CConvertLocked)> items,
        IReadOnlyList<string>? paths,
        Guid relayTarget,
        Guid sourceTab)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (!presetValid)
        {
            return false;
        }

        _cConvertPort.LConvertRun(
            presetName,
            items
                .Where(item => !item.CConvertLocked
                    && (paths is null || paths.Contains(item.CConvertPath, StringComparer.OrdinalIgnoreCase)))
                .Select(item => (item.CConvertPath, item.CConvertBatch))
                .ToArray(),
            false,
            relayTarget,
            sourceTab);
        return true;
    }
}

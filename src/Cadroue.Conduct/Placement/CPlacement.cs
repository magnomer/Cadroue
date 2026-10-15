using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The subwindow placement decisions: what is kept, what is imported from the preferences, and the trace of
// every failure. The driver hands in its store reads and writes and never tests an answer.
public sealed class CPlacement
{
    private readonly LPlacementPort _cPlacementPort;

    internal CPlacement(LPlacementPort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        _cPlacementPort = port;
    }

    // Only a kept rectangle is written; the answer is the write's.
    public bool CPlacementSave(
        string key,
        double left,
        double top,
        double width,
        double height,
        Func<string, double, double, double, double, bool> write)
    {
        ArgumentNullException.ThrowIfNull(write);
        return CPlacementSaveCheck(left, top, width, height) && write(key, left, top, width, height);
    }

    // Each placement the preferences once held is saved where none is held; a failure ends the import, traced.
    public void CPlacementImport(
        IReadOnlyList<(string, string)> entries,
        Func<string, bool> held,
        Func<string, double, double, double, double, bool> save)
    {
        ArgumentNullException.ThrowIfNull(held);
        ArgumentNullException.ThrowIfNull(save);
        try
        {
            foreach ((string key, double left, double top, double width, double height) in
                _cPlacementPort.LPlacementPreferenceRead(entries))
            {
                if (CPlacementCarryCheck(held(key)) && save(key, left, top, width, height))
                {
                    _cPlacementPort.LPlacementCarriedRecord(key);
                }
            }
        }
        catch (Exception failure)
        {
            _cPlacementPort.LPlacementUncarriedRecord(failure);
        }
    }

    public void CPlacementUnreadableRecord(Exception failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        _cPlacementPort.LPlacementUnreadableRecord(failure.Message);
    }

    // Any failure but an unreadable catalogue names the key.
    public void CPlacementUnsavedRecord(string key) => _cPlacementPort.LPlacementUnsavedRecord(key);

    // An unreadable catalogue is named before the key it kept from saving.
    public void CPlacementBlockedRecord(string key, Exception unreadable)
    {
        CPlacementUnreadableRecord(unreadable);
        _cPlacementPort.LPlacementBlockedRecord(key);
    }

    // A rectangle is kept unless a corner is unknown or its size is not positive.
    internal static bool CPlacementSaveCheck(double left, double top, double width, double height) =>
        !(double.IsNaN(left) || double.IsNaN(top) || width <= 0 || height <= 0);

    // A placement is imported from the preferences only where none is held.
    internal static bool CPlacementCarryCheck(bool held) => !held;
}

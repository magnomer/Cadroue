using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The encoder subwindow's shared gate: it holds the trial and repertoire ports every encoder window reaches, opens
// each window's gates over that window's draft, and keeps the rate and progress helpers the window's rows share.
public sealed class CSEncoder
{
    private readonly LSTrialPort _csEncoderPort;
    private readonly LSRepertoirePort _csEncoderRepertoire;

    internal CSEncoder(LSTrialPort port, LSRepertoirePort repertoire)
    {
        ArgumentNullException.ThrowIfNull(port);
        ArgumentNullException.ThrowIfNull(repertoire);
        _csEncoderPort = port;
        _csEncoderRepertoire = repertoire;
    }

    // One window's gates over its own draft, read and saved through the window's read and save; smart says whether
    // the window's work may cut smart.
    public CSEncoderDraft CSEncoderDraftCreate(
        Func<(string, string, string)> read,
        Action<string, string, string, string, string, string, string> save,
        Func<(string, string, string, IReadOnlyDictionary<string, string>)> rates,
        bool smart) =>
        new(new LSEncoderOutlet(read, save, rates), _csEncoderPort, _csEncoderRepertoire, smart);

    internal static string CSEncoderStoredRead(string rate, string stored, string choice, string fallback) =>
        string.Equals(rate, stored, StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(choice)
            ? choice
            : fallback;

    // The rate a codec keeps: the asked rate when the codec offers it, else the codec's first mode.
    internal static string CSEncoderRateResolve(IReadOnlyList<string> modes, string rate) =>
        modes.Contains(rate) ? rate : modes[0];

    internal static double CSEncoderProgressResolve(int done, int total) =>
        total == 0 ? 1 : (double)done / total;
}

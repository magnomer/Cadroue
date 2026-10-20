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
    private readonly Func<string, LSEncoderPort> _csEncoderOpener;

    internal CSEncoder(LSTrialPort port, LSRepertoirePort repertoire, Func<string, LSEncoderPort> opener)
    {
        ArgumentNullException.ThrowIfNull(port);
        ArgumentNullException.ThrowIfNull(repertoire);
        ArgumentNullException.ThrowIfNull(opener);
        _csEncoderPort = port;
        _csEncoderRepertoire = repertoire;
        _csEncoderOpener = opener;
    }

    // One window's gates over its own draft; smart says whether the window's work may cut smart.
    public CSEncoderDraft CSEncoderDraftCreate(string preset, bool smart) =>
        new(_csEncoderOpener(preset), _csEncoderPort, _csEncoderRepertoire, smart);

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

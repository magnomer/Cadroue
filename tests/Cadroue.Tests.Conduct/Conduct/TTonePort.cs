using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

internal sealed class TTonePort : LTonePort
{
    public bool TToneFlyleaf { get; set; }

    public bool TToneMpv { get; set; }

    public List<string> TToneCalls { get; } = [];

    public (bool LToneFlyleaf, bool LToneMpv) LTonePreviewRead()
    {
        TToneCalls.Add("preview");
        return (TToneFlyleaf, TToneMpv);
    }
}

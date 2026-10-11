using System;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

public sealed class CAtelier
{
    internal CAtelier(LUsherPort usher)
    {
        ArgumentNullException.ThrowIfNull(usher);

        CAtelierUsher = new CUsher(usher);
    }

    public CUsher CAtelierUsher { get; }
}

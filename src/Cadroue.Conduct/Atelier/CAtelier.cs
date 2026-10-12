using System;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

public sealed class CAtelier
{
    internal CAtelier(LUsherPort usher, LLineagePort lineage)
    {
        ArgumentNullException.ThrowIfNull(usher);
        ArgumentNullException.ThrowIfNull(lineage);

        CAtelierUsher = new CUsher(usher);
        CAtelierLineage = new CLineage(lineage);
    }

    public CUsher CAtelierUsher { get; }

    public CLineage CAtelierLineage { get; }
}

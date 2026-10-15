using System;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

public sealed class CAtelier
{
    private readonly LWorkspacePort _cAtelierWorkspacePort;

    internal CAtelier(LUsherPort usher, LLineagePort lineage, LWorkspacePort workspace, LPlacementPort placement)
    {
        ArgumentNullException.ThrowIfNull(usher);
        ArgumentNullException.ThrowIfNull(lineage);
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(placement);

        CAtelierUsher = new CUsher(usher);
        CAtelierLineage = new CLineage(lineage);
        _cAtelierWorkspacePort = workspace;
        CAtelierPlacement = new CPlacement(placement);
        CAtelierInfo = new CInfo();
    }

    public CUsher CAtelierUsher { get; }

    public CLineage CAtelierLineage { get; }

    public CPlacement CAtelierPlacement { get; }

    public CInfo CAtelierInfo { get; }

    public CWorkspace CAtelierWorkspaceCreate() => new(_cAtelierWorkspacePort);
}

using System;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

public sealed class CAtelier
{
    private readonly LWorkspacePort _cAtelierWorkspacePort;

    internal CAtelier(LUsherPort usher, LLineagePort lineage, LWorkspacePort workspace)
    {
        ArgumentNullException.ThrowIfNull(usher);
        ArgumentNullException.ThrowIfNull(lineage);
        ArgumentNullException.ThrowIfNull(workspace);

        CAtelierUsher = new CUsher(usher);
        CAtelierLineage = new CLineage(lineage);
        _cAtelierWorkspacePort = workspace;
    }

    public CUsher CAtelierUsher { get; }

    public CLineage CAtelierLineage { get; }

    public CWorkspace CAtelierWorkspaceCreate() => new(_cAtelierWorkspacePort);
}

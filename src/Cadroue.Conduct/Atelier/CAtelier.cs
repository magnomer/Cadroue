using System;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

public sealed class CAtelier
{
    private readonly LWorkspacePort _cAtelierWorkspacePort;
    private readonly LViewerPort _cAtelierViewerPort;

    internal CAtelier(
        LUsherPort usher,
        LLineagePort lineage,
        LWorkspacePort workspace,
        LPlacementPort placement,
        LViewerPort viewer,
        LEditPort edit)
    {
        ArgumentNullException.ThrowIfNull(usher);
        ArgumentNullException.ThrowIfNull(lineage);
        ArgumentNullException.ThrowIfNull(workspace);
        ArgumentNullException.ThrowIfNull(placement);
        ArgumentNullException.ThrowIfNull(viewer);
        ArgumentNullException.ThrowIfNull(edit);

        CAtelierUsher = new CUsher(usher);
        CAtelierLineage = new CLineage(lineage);
        _cAtelierWorkspacePort = workspace;
        _cAtelierViewerPort = viewer;
        CAtelierPlacement = new CPlacement(placement);
        CAtelierInfo = new CInfo();
        CAtelierEdit = new CEdit(edit);
    }

    public CUsher CAtelierUsher { get; }

    public CLineage CAtelierLineage { get; }

    public CPlacement CAtelierPlacement { get; }

    public CInfo CAtelierInfo { get; }

    public CEdit CAtelierEdit { get; }

    // A viewer's workspace with the viewer's own trace gate: both hold state for one viewer.
    public CWorkspace CAtelierWorkspaceCreate() => new(_cAtelierWorkspacePort, new CViewer(_cAtelierViewerPort));

    public CInspector CAtelierInspectorCreate() => new();
}

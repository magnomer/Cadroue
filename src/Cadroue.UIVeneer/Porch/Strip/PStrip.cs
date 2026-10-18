using System.Windows;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

public sealed class PStrip
{
    public PStrip(QStrip lStrip)
    {
        QStrip = lStrip;
        QDeckCatalog lCatalog = QStrip.QStripDeck.QDeckCatalog;
        lCatalog.QDeckBuilderAdd("Split", (lWorkspace, lLayout) => new PSplitTab(lWorkspace, lLayout));
        lCatalog.QDeckBuilderAdd("Edit", (lWorkspace, lLayout) => new PEditTab(lWorkspace, lLayout));
        lCatalog.QDeckBuilderAdd("Fix", (lWorkspace, lLayout) => new PFixTab(lWorkspace, lLayout));
        lCatalog.QDeckBuilderAdd("Audio", (lWorkspace, lLayout) => new PAudioTab(lWorkspace, lLayout));
        lCatalog.QDeckBuilderAdd("Convert", (lWorkspace, lLayout) => new PConvertTab(lWorkspace, lLayout));
        lCatalog.QDeckBuilderAdd("Merge", (lWorkspace, lLayout) => new PMergeTab(lWorkspace, lLayout));
        lCatalog.QDeckBuilderAdd("Funnel", (lWorkspace, lLayout) => new PFunnelTab(lWorkspace, lLayout));
        lCatalog.QDeckBuilderAdd(
            "Worklist", (lWorkspace, lLayout) => new PWorklistTab(lWorkspace.QWorkspaceRoster!, lLayout));
    }

    public QStrip QStrip { get; }

    public PTabSurface? PStripSelected =>
        QStrip.QStripDeck.QDeckIndex.QDeckSurfaceRead<PTabSurface>(QStrip.QStripSelected);

    public QStripTab PStripAdd(QStripTab lStripTab, LSceneTabEntry? lPreferenceTabLayout = null)
    {
        QWorkspace lWorkspace = lStripTab.QStripTabWorkspace;
        PTabSurface pSurface =
            (PTabSurface)QStrip.QStripDeck.QDeckCatalog.QDeckSurfaceBuild(lStripTab, lPreferenceTabLayout);
        lWorkspace.QWorkspaceAttach(pSurface.PTabLayoutRead);
        pSurface.PTabStripAttach(QStrip, lStripTab);
        lWorkspace.QWorkspaceFrameBuild(
            pSurface,
            () => new PWorkspace(),
            lSource => lSource.QSourceAttach(new PSource()),
            lInfo => QInfoFace.QInfoFaceAttach(lInfo, new PInfo()),
            pSurface.PTabExportToggle,
            () => QIcon.QIconRead("/PAsset/PPanel/PExportToggle.svg"),
            pSurface.PTabClose,
            () => pSurface.PTabFlow!.PFlowClose(),
            () => pSurface.PTabViewer!.PViewerClose());
        QStrip.QStripDeck.QDeckIndex.QDeckSurfaceAdd(lStripTab, pSurface);
        QStrip.QStripAdd(lStripTab);
        return lStripTab;
    }
}

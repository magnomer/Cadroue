using System.Windows;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PCabin;
using Cadroue.UIVeneer.PHouse;
using Cadroue.UIVeneer.PWing;

namespace Cadroue.UIVeneer.PPorch;

public sealed class PStrip
{
    public PStrip(QStrip lStrip)
    {
        QStrip = lStrip;
        QStrip.QStripDeck.QDeckBuilderAdd("Split", (lWorkspace, lLayout) => new PSplitTab(lWorkspace, lLayout));
        QStrip.QStripDeck.QDeckBuilderAdd("Edit", (lWorkspace, lLayout) => new PEditTab(lWorkspace, lLayout));
        QStrip.QStripDeck.QDeckBuilderAdd("Fix", (lWorkspace, lLayout) => new PFixTab(lWorkspace, lLayout));
        QStrip.QStripDeck.QDeckBuilderAdd("Audio", (lWorkspace, lLayout) => new PAudioTab(lWorkspace, lLayout));
        QStrip.QStripDeck.QDeckBuilderAdd("Convert", (lWorkspace, lLayout) => new PConvertTab(lWorkspace, lLayout));
        QStrip.QStripDeck.QDeckBuilderAdd("Merge", (lWorkspace, lLayout) => new PMergeTab(lWorkspace, lLayout));
        QStrip.QStripDeck.QDeckBuilderAdd("Funnel", (lWorkspace, lLayout) => new PFunnelTab(lWorkspace, lLayout));
        QStrip.QStripDeck.QDeckBuilderAdd(
            "Worklist", (lWorkspace, lLayout) => new PWorklistTab(lWorkspace.QWorkspaceRoster!, lLayout));
    }

    public QStrip QStrip { get; }

    public PTabSurface? PStripSelected => QStrip.QStripDeck.QDeckSurfaceRead<PTabSurface>(QStrip.QStripSelected);

    public QStripTab PStripAdd(QStripTab lStripTab, LSceneTabEntry? lPreferenceTabLayout = null)
    {
        QWorkspace lWorkspace = lStripTab.QStripTabWorkspace;
        PTabSurface pSurface = (PTabSurface)QStrip.QStripDeck.QDeckSurfaceBuild(lStripTab, lPreferenceTabLayout);
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
        QStrip.QStripDeck.QDeckSurfaceAdd(lStripTab, pSurface);
        QStrip.QStripAdd(lStripTab);
        return lStripTab;
    }
}

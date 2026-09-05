using Cadroue.Core;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Cadroue.UIShell.PPanel;
using PFlowControl = Cadroue.UIShell.PFlow.PFlow;

namespace Cadroue.UIShell.PDeck;

public abstract partial class PTabSurface : UserControl
{
    internal static bool PTabCollapseCheck(UIElement pPanel) => pPanel switch
    {
        PList pListPanel => pListPanel.PListMinimizedCheck(),
        PProcessing pProcessingPanel => pProcessingPanel.PProcessingMinimizedCheck(),
        PInspector pInspectorPanel => pInspectorPanel.PInspectorMinimizedCheck(),
        PClinic pClinicPanel => pClinicPanel.PClinicMinimizedCheck(),
        PSection pSectionPanel => pSectionPanel.PSectionMinimizedCheck(),
        _ => false
    };

    internal static void PTabCollapseSet(UIElement pPanel, bool pCollapsed)
    {
        switch (pPanel)
        {
            case PList pListPanel: pListPanel.PListMinimizeSet(pCollapsed); break;
            case PProcessing pProcessingPanel: pProcessingPanel.PProcessingMinimizeSet(pCollapsed); break;
            case PInspector pInspectorPanel: pInspectorPanel.PInspectorMinimizeSet(pCollapsed); break;
            case PClinic pClinicPanel: pClinicPanel.PClinicMinimizeSet(pCollapsed); break;
            case PSection pSectionPanel: pSectionPanel.PSectionMinimizeSet(pCollapsed); break;
        }
    }

    private static double PTabCollapseRead(UIElement pPanel) => pPanel switch
    {
        PList => PList.PListStripWidth,
        PProcessing => PProcessing.PProcessingStripWidth,
        PInspector => PInspector.PInspectorStripWidth,
        PClinic => PClinic.PClinicStripWidth,
        PSection => PSection.PSectionStripWidth,
        _ => 0
    };

    private static void PTabCollapseAttach(
        UIElement pPanel,
        int pPanelIndex,
        PColumn pPanelLayout,
        Action pCollapseNotify)
    {
        double pStripWidth = PTabCollapseRead(pPanel);
        if (pStripWidth <= 0)
        {
            return;
        }

        void pTabCollapseApply(bool pCollapsed)
        {
            pPanelLayout.PColumnWidthSet(pPanelIndex, pCollapsed ? pStripWidth : 0);
            pCollapseNotify();
        }

        switch (pPanel)
        {
            case PList pListPanel: pListPanel.PListMinimizeChange += pTabCollapseApply; break;
            case PProcessing pProcessingPanel: pProcessingPanel.PProcessingMinimizeChange += pTabCollapseApply; break;
            case PInspector pInspectorPanel: pInspectorPanel.PInspectorMinimizeChange += pTabCollapseApply; break;
            case PClinic pClinicPanel: pClinicPanel.PClinicMinimizeChange += pTabCollapseApply; break;
            case PSection pSectionPanel: pSectionPanel.PSectionMinimizeChange += pTabCollapseApply; break;
        }
    }
}

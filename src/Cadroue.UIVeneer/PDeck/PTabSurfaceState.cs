using Cadroue.Core;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Cadroue.UIVeneer.PPanel;
using PFlowControl = Cadroue.UIVeneer.PFlow.PFlow;

namespace Cadroue.UIVeneer.PDeck;

public abstract partial class PTabSurface : UserControl
{
    private sealed class PTabGridState
    {
        private readonly ColumnDefinition? pExportColumn;
        private readonly ColumnDefinition? pExportSplitterColumn;
        private readonly UIElement? pExportPanel;
        private readonly UIElement? pExportSplitter;
        private readonly int? pExportPanelIndex;
        private bool pExportVisible = true;

        private PTabGridState(
            PColumn pTabLayout,
            IReadOnlyList<UIElement> pTabPanels,
            int? pExportPanelIndex,
            ColumnDefinition? pExportColumn,
            UIElement? pExportPanel,
            ColumnDefinition? pExportSplitterColumn,
            UIElement? pExportSplitter)
        {
            PTabLayout = pTabLayout;
            PTabPanels = pTabPanels;
            this.pExportPanelIndex = pExportPanelIndex;
            this.pExportColumn = pExportColumn;
            this.pExportPanel = pExportPanel;
            this.pExportSplitterColumn = pExportSplitterColumn;
            this.pExportSplitter = pExportSplitter;
        }

        public PColumn PTabLayout { get; }

        public IReadOnlyList<UIElement> PTabPanels { get; }

        public PAction? PTabAction { get; set; }

        public static PTabGridState PTabStateCreate(
            PColumn pTabLayout,
            IReadOnlyList<UIElement> pPanels,
            IReadOnlyList<ColumnDefinition> pColumnItems,
            IReadOnlyList<ColumnDefinition> pSplitterColumns,
            IReadOnlyList<UIElement> pSplitters)
        {
            for (int index = pPanels.Count - 1; index >= 0; index--)
            {
                if (pPanels[index] is PExport)
                {
                    int pSplitterIndex = index - 1;
                    return new PTabGridState(
                        pTabLayout,
                        pPanels,
                        index,
                        pColumnItems[index],
                        pPanels[index],
                        pSplitterIndex >= 0 ? pSplitterColumns[pSplitterIndex] : null,
                        pSplitterIndex >= 0 ? pSplitters[pSplitterIndex] : null);
                }
            }

            return new PTabGridState(pTabLayout, pPanels, null, null, null, null, null);
        }

        public bool PExportHidden => pExportPanelIndex is not null && !pExportVisible;

        public void PExportToggle() => PExportSet(pExportVisible);

        public void PExportSet(bool pExportHide)
        {
            if (pExportColumn is null || pExportPanel is null || pExportPanelIndex is null)
            {
                return;
            }

            if (pExportHide == !pExportVisible)
            {
                return;
            }

            if (pExportHide)
            {
                PTabLayout.PColumnHide(pExportPanelIndex.Value);
                pExportPanel.Visibility = Visibility.Collapsed;
                if (pExportSplitterColumn is not null)
                {
                    pExportSplitterColumn.Width = new GridLength(0);
                }

                if (pExportSplitter is not null)
                {
                    pExportSplitter.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                PTabLayout.PColumnShow(pExportPanelIndex.Value);
                pExportPanel.Visibility = Visibility.Visible;
                if (pExportSplitterColumn is not null)
                {
                    pExportSplitterColumn.Width = new GridLength(6);
                }

                if (pExportSplitter is not null)
                {
                    pExportSplitter.Visibility = Visibility.Visible;
                }
            }

            pExportVisible = !pExportHide;
        }
    }
}

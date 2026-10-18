using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer.PWing;

internal sealed class PExportPreset
{
    private const string PExportUserGroup = "$User";

    private readonly QExport lExport;
    private readonly PExportPresetCommand pExportCommand;
    private readonly StackPanel pPresetRowPanel = new();
    private readonly PExportPresetBox pExportBox;
    private readonly PExportPresetDrag pExportDrag;
    private readonly PExportPresetRow pExportRow;

    public PExportPreset(QExport lExport, PExportPresetCommand pExportCommand)
    {
        this.lExport = lExport;
        this.pExportCommand = pExportCommand;
        pExportBox = new PExportPresetBox(lExport);
        pExportDrag = new PExportPresetDrag(lExport, pPresetRowPanel, pExportBox, PExportPresetSelect);
        pExportRow = new PExportPresetRow(
            lExport, pPresetRowPanel, pExportBox, pExportDrag, pExportCommand, PExportPresetSelect);
    }

    internal void PExportPresetAttach() => pExportDrag.PExportDragAttach();

    internal UIElement PExportPresetBuild()
    {
        var pScroll = new ScrollViewer
        {
            Content = pPresetRowPanel,
            Background = Brushes.White,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            FocusVisualStyle = null
        };

        PExportPresetRebuild();
        return pScroll;
    }

    internal void PExportPresetRebuild()
    {
        LPreset lWorking = pExportCommand.PExportWorkingRead();
        pExportBox.PExportBoxSet(null);
        PExportListBuild(lWorking, PExportOrderResolve());
    }

    private static List<(string pPresetName, string? pGroupHeader, bool pGroupUserHeader, bool pGroupCollapsed)>
        PExportOrderResolve()
    {
        var pOrder =
            new List<(string pPresetName, string? pGroupHeader, bool pGroupUserHeader, bool pGroupCollapsed)>();
        string? pNativeGroupCurrent = null;
        bool pUserHeaderAdded = false;
        foreach (string lPresetName in LPresetTable.LPresetNames)
        {
            bool pPresetNative = LPresetTable.LPresetNativeCheck(lPresetName);
            string? pGroupName = pPresetNative ? LPresetTable.LPresetGroupRead(lPresetName) : null;
            bool pGroupHeader = pPresetNative
                && pGroupName is not null
                && !string.Equals(pNativeGroupCurrent, pGroupName, StringComparison.OrdinalIgnoreCase);
            bool pUserHeader = !pPresetNative && !pUserHeaderAdded;
            pNativeGroupCurrent = pGroupHeader ? pGroupName : pNativeGroupCurrent;
            pUserHeaderAdded = pUserHeaderAdded || pUserHeader;
            bool pCollapsed = pPresetNative && pGroupName is not null
                ? LPreference.LPreferenceStateCurrent.LPreferenceFoldRead(pGroupName)
                : LPreference.LPreferenceStateCurrent.LPreferenceFoldRead(
                    PExportUserGroup,
                    false);
            pOrder.Add((lPresetName, pGroupHeader ? pGroupName : null, pUserHeader, pCollapsed));
        }

        return pOrder;
    }

    private void PExportListBuild(
        LPreset lWorking,
        List<(string pPresetName, string? pGroupHeader, bool pGroupUserHeader, bool pGroupCollapsed)> pOrder)
    {
        pPresetRowPanel.Children.Clear();
        foreach ((string lPresetName, string? pGroupHeader, bool pUserHeader, bool pCollapsed) in pOrder)
        {
            if (pGroupHeader is not null)
            {
                pPresetRowPanel.Children.Add(PExportPresetGroup.PExportGroupBuild(
                    pGroupHeader,
                    pCollapsed,
                    () => PExportGroupToggle(pGroupHeader)));
            }

            if (pUserHeader)
            {
                pPresetRowPanel.Children.Add(PExportPresetGroup.PExportGroupBuild(
                    QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Group.User"),
                    pCollapsed,
                    PExportUserToggle));
            }

            Border pRow = pExportRow.PExportRowBuild(lPresetName, lWorking);
            pRow.Visibility = PLook.PLookVisible[!pCollapsed];
            pPresetRowPanel.Children.Add(pRow);
        }
    }

    private void PExportGroupToggle(string pGroupName)
    {
        bool pCollapsed = LPreference.LPreferenceStateCurrent.LPreferenceFoldRead(pGroupName);
        LPreference.LPreferenceFoldSet(pGroupName, !pCollapsed);
        PExportPresetRebuild();
    }

    private void PExportUserToggle()
    {
        bool pCollapsed = LPreference.LPreferenceStateCurrent.LPreferenceFoldRead(
            PExportUserGroup,
            false);
        LPreference.LPreferenceFoldSet(PExportUserGroup, !pCollapsed, false);
        PExportPresetRebuild();
    }

    private void PExportPresetSelect(string lPresetName)
    {
        PExportGroupShow(lPresetName);
        lExport.QExportSelect(lPresetName);
    }

    private void PExportGroupShow(string lPresetName)
    {
        if (LPresetTable.LPresetNativeCheck(lPresetName))
        {
            if (LPresetTable.LPresetGroupRead(lPresetName) is not string pGroupName
                || !LPreference.LPreferenceStateCurrent.LPreferenceFoldRead(pGroupName))
            {
                return;
            }

            LPreference.LPreferenceFoldSet(pGroupName, false);
        }
        else
        {
            if (!LPreference.LPreferenceStateCurrent.LPreferenceFoldRead(PExportUserGroup, false))
            {
                return;
            }

            LPreference.LPreferenceFoldSet(PExportUserGroup, false, false);
        }

        PExportPresetRebuild();
    }
}

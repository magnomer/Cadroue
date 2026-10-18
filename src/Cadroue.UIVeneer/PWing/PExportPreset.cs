using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;
using Cadroue.Application;

namespace Cadroue.UIVeneer.PWing;

public sealed partial class PExport
{
    private const string PExportPlusIcon = "/PAsset/PPanel/PExportPlus.svg";
    private const string PExportMinusIcon = "/PAsset/PPanel/PExportMinus.svg";
    private const string PExportSettingIcon = "/PAsset/PPanel/PExportSetting.svg";
    private const string PExportImportIcon = "/PAsset/PPanel/PExportImport.svg";
    private const string PExportExportIcon = "/PAsset/PPanel/PExportExport.svg";
    private const string PExportUserGroup = "$User";
    private const string PExportCheckIcon = "/PAsset/PPanel/PExportCheck.svg";
    private const string PExportCancelIcon = "/PAsset/PPanel/PExportCancel.svg";
    private const string PExportCollapseIcon = "/PAsset/PPanel/PExportCollapse.svg";
    private const string PExportExpandIcon = "/PAsset/PPanel/PExportExpand.svg";
    private static readonly Brush PExportApplyBrush = new SolidColorBrush(Color.FromRgb(0x2E, 0xA3, 0x66));
    private static readonly Brush PExportCancelBrush = new SolidColorBrush(Color.FromRgb(0xD1, 0x43, 0x43));

    private UIElement PExportPresetBuild()
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

    private void PExportPresetRebuild()
    {
        LPreset lWorking = PExportWorkingRead();
        PExportBoxSet(null);
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
                pPresetRowPanel.Children.Add(PExportGroupBuild(
                    pGroupHeader,
                    pCollapsed,
                    () => PExportGroupToggle(pGroupHeader)));
            }

            if (pUserHeader)
            {
                pPresetRowPanel.Children.Add(PExportGroupBuild(
                    QLocalizationCatalog.QLocalizationTextRead("ExportPreset.Group.User"),
                    pCollapsed,
                    PExportUserToggle));
            }

            Border pRow = PExportRowBuild(lPresetName, lWorking);
            pRow.Visibility = PLook.PLookVisible[!pCollapsed];
            pPresetRowPanel.Children.Add(pRow);
        }
    }
}

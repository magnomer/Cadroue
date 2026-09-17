using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PHouse;
using Cadroue.Application;

namespace Cadroue.UIVeneer.PPanel;

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
        pExportBoxCurrent = null;
        pPresetRowPanel.Children.Clear();
        string? pNativeGroupCurrent = null;
        bool pUserHeaderAdded = false;
        foreach (string lPresetName in LPreset.LPresetNames)
        {
            bool pPresetNative = LPreset.LPresetNativeCheck(lPresetName);
            string? pGroupName = pPresetNative ? LPreset.LPresetGroupRead(lPresetName) : null;
            if (pPresetNative
                && pGroupName is not null
                && !string.Equals(pNativeGroupCurrent, pGroupName, StringComparison.OrdinalIgnoreCase))
            {
                pPresetRowPanel.Children.Add(PExportGroupBuild(
                    pGroupName,
                    LPreference.LPreferenceStateCurrent.LPreferenceFoldRead(pGroupName),
                    () => PExportGroupToggle(pGroupName)));
                pNativeGroupCurrent = pGroupName;
            }
            else if (!pPresetNative && !pUserHeaderAdded)
            {
                pPresetRowPanel.Children.Add(PExportGroupBuild(
                    LLocalization.LLocalizationTextRead("ExportPreset.Group.User"),
                    LPreference.LPreferenceStateCurrent.LPreferenceFoldRead(
                        PExportUserGroup,
                        false),
                    PExportUserToggle));
                pUserHeaderAdded = true;
            }

            Border pRow = PExportRowBuild(lPresetName, lWorking);
            bool pCollapsed = pPresetNative && pGroupName is not null
                ? LPreference.LPreferenceStateCurrent.LPreferenceFoldRead(pGroupName)
                : LPreference.LPreferenceStateCurrent.LPreferenceFoldRead(
                    PExportUserGroup,
                    false);
            pRow.Visibility = pCollapsed ? Visibility.Collapsed : Visibility.Visible;
            pPresetRowPanel.Children.Add(pRow);
        }
    }
}

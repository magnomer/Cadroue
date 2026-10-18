using System.Windows;
using System.Windows.Controls;
using Cadroue.Infrastructure;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PLogFilter
{
    private readonly QLog lLog;
    private readonly Control pLogCategoryPicker;
    private readonly CheckBox pLogVerboseBox;

    internal PLogFilter(QLog lLogOwner)
    {
        lLog = lLogOwner;
        pLogCategoryPicker = PLogCategoryBuild();
        pLogVerboseBox = PLogVerboseBuild();
    }

    internal DockPanel PLogFilterBuild()
    {
        var pLogFilterRow = new DockPanel { Margin = new Thickness(18, 9, 18, 12), LastChildFill = false };
        DockPanel.SetDock(pLogCategoryPicker, Dock.Left);
        DockPanel.SetDock(pLogVerboseBox, Dock.Right);
        pLogFilterRow.Children.Add(pLogCategoryPicker);
        pLogFilterRow.Children.Add(pLogVerboseBox);
        return pLogFilterRow;
    }

    private Control PLogCategoryBuild()
    {
        var pLogPicker = new Control
        {
            Width = 200,
            Height = PSField.PSFieldControlHeight,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        QPickerControl.QPickerControlAttach(
            pLogPicker,
            QPicker.QPickerItemsCreate(lLog.QLogCategoriesRead(), Array.Empty<string>()),
            QLocalizationCatalog.QLocalizationTextRead("Log.Category.All"),
            lLog.QLogCategorySet);
        return pLogPicker;
    }

    private CheckBox PLogVerboseBuild()
    {
        var pLogVerbose = new CheckBox
        {
            Content = QLocalizationCatalog.QLocalizationTextRead("Log.Verbose.Label"),
            VerticalAlignment = VerticalAlignment.Center,
            IsChecked = LTrace.LTraceVerbose,
            ToolTip = QLocalizationCatalog.QLocalizationTextRead("Log.Verbose.Tooltip")
        };

        QInteractionClick.QInteractionClickAttach(pLogVerbose);
        QCheckbox.QCheckboxAttach(pLogVerbose);
        PLogVerboseAttach(pLogVerbose);
        return pLogVerbose;
    }

    private void PLogVerboseAttach(CheckBox pLogVerbose)
    {
        pLogVerbose.Checked += (_, _) => lLog.QLogVerboseSet(true);
        pLogVerbose.Unchecked += (_, _) => lLog.QLogVerboseSet(false);
    }
}

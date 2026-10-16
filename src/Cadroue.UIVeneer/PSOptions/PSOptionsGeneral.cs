using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer;

internal sealed partial class PSOptions
{
    private static readonly LLocalizationChoice[] PSOptionsTabItems =
    {
        new("Split", "Tab.Split"),
        new("Edit", "Tab.Edit"),
        new("Audio", "Tab.Audio"),
        new("Convert", "Tab.Convert"),
        new("Merge", "Tab.Merge"),
        new("Worklist", "Tab.Worklist")
    };

    private static readonly LLocalizationChoice[] PSOptionsStartupItems =
    {
        new("LastSession", "Options.Startup.LastSession"),
        new("DefaultTab", "Options.Startup.DefaultTab")
    };

    private static readonly LLocalizationChoice[] PSOptionsTabsItems =
    {
        new("Horizontal", "Options.Layout.TabsHorizontal"),
        new("Vertical", "Options.Layout.TabsVertical")
    };

    private readonly Border psOptionsStartupMode;
    private Action? psOptionsStartupPicker;
    private readonly Control psOptionsTabPicker;
    private readonly CheckBox psMediaBox;
    private readonly CheckBox psOptionsConfirmBox;
    private readonly CheckBox psRelayClearBox;
    private readonly Border psOptionsTabsMode;
    private readonly ComboBox psOptionsLanguageCombo;

    private UIElement PSGeneralBuild()
    {
        var pPanel = new StackPanel();
        pPanel.Children.Add(PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead("Options.General.Startup"),
            PSOptionsStartupBuild(),
            PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Options.General.LastMedia"), psMediaBox)));
        pPanel.Children.Add(PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead("Options.General.Confirm"),
            PSField.PSFieldBuild(
                QLocalizationCatalog.QLocalizationTextRead("Options.General.DestructiveActions"),
                psOptionsConfirmBox),
            PSNotice.PSNoticeBuild(QLocalizationCatalog.QLocalizationTextRead("Options.General.DestructiveNotice"))));
        pPanel.Children.Add(PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead("Options.General.Relay"),
            PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Options.General.RelayClear"),
                psRelayClearBox),
            PSNotice.PSNoticeBuild(QLocalizationCatalog.QLocalizationTextRead("Options.General.RelayClearNotice"))));
        pPanel.Children.Add(PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead("Options.General.Layout"),
            PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Options.General.Tabs"),
                psOptionsTabsMode)));
        pPanel.Children.Add(PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead("Options.General.Language"),
            PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Options.General.Language"),
                psOptionsLanguageCombo)));
        return pPanel;
    }

    private UIElement PSOptionsStartupBuild()
    {
        psOptionsTabPicker.Margin = new Thickness(12, 0, 0, 0);
        psOptionsTabPicker.VerticalAlignment = VerticalAlignment.Center;

        var pRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            VerticalAlignment = VerticalAlignment.Center
        };
        pRow.Children.Add(psOptionsStartupMode);
        pRow.Children.Add(psOptionsTabPicker);

        void PSOptionsPickerUpdate() =>
            psOptionsTabPicker.Visibility =
                string.Equals(PSField.PSModeTextRead(psOptionsStartupMode), "DefaultTab", StringComparison.Ordinal)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        PSOptionsPickerUpdate();
        psOptionsStartupPicker = PSOptionsPickerUpdate;

        return PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Options.General.OpenWith"), pRow);
    }

    private static LLocalizationChoice[] PSOptionsLanguagesRead() =>
        LLocalization.LLocalizationLanguagesRead()
            .Select(pLanguage => new LLocalizationChoice(
                pLanguage.Key,
                "Localization.Language.Name",
                pLanguage.Value))
            .ToArray();
}

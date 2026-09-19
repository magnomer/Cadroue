using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.UIVeneer.PHouse;

using static Cadroue.UIVeneer.PSField;
using static Cadroue.UIVeneer.PSCombo;
using static Cadroue.UIVeneer.PSPlate;
using static Cadroue.UIVeneer.PSNotice;

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
    private readonly PPicker psOptionsTabPicker;
    private readonly CheckBox psMediaBox;
    private readonly CheckBox psOptionsConfirmBox;
    private readonly CheckBox psRelayClearBox;
    private readonly Border psOptionsTabsMode;
    private readonly ComboBox psOptionsLanguageCombo;

    private UIElement PSGeneralBuild()
    {
        var pPanel = new StackPanel();
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.General.Startup"),
            PSOptionsStartupBuild(),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.General.LastMedia"), psMediaBox)));
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.General.Confirm"),
            PSFieldBuild(
                LLocalization.LLocalizationTextRead("Options.General.DestructiveActions"),
                psOptionsConfirmBox),
            PSNoticeBuild(LLocalization.LLocalizationTextRead("Options.General.DestructiveNotice"))));
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.General.Relay"),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.General.RelayClear"), psRelayClearBox),
            PSNoticeBuild(LLocalization.LLocalizationTextRead("Options.General.RelayClearNotice"))));
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.General.Layout"),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.General.Tabs"), psOptionsTabsMode)));
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.General.Language"),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.General.Language"), psOptionsLanguageCombo)));
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
                string.Equals(PSModeTextRead(psOptionsStartupMode), "DefaultTab", StringComparison.Ordinal)
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        PSOptionsPickerUpdate();
        psOptionsStartupPicker = PSOptionsPickerUpdate;

        return PSFieldBuild(LLocalization.LLocalizationTextRead("Options.General.OpenWith"), pRow);
    }

    private static LLocalizationChoice[] PSOptionsLanguagesRead() =>
        LLocalization.LLocalizationLanguagesRead()
            .Select(pLanguage => new LLocalizationChoice(
                pLanguage.Key,
                "Localization.Language.Name",
                pLanguage.Value))
            .ToArray();
}

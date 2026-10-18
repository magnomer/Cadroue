using System.Windows;
using System.Windows.Controls;
using Cadroue.Core;
using Cadroue.Application;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer.PHouse;

namespace Cadroue.UIVeneer;

internal sealed class PSOptionsGeneral
{
    internal static readonly LLocalizationChoice[] PSOptionsTabItems =
    {
        new("Split", "Tab.Split"),
        new("Edit", "Tab.Edit"),
        new("Audio", "Tab.Audio"),
        new("Convert", "Tab.Convert"),
        new("Merge", "Tab.Merge"),
        new("Worklist", "Tab.Worklist")
    };

    internal static readonly LLocalizationChoice[] PSOptionsStartupItems =
    {
        new("LastSession", "Options.Startup.LastSession"),
        new("DefaultTab", "Options.Startup.DefaultTab")
    };

    internal static readonly LLocalizationChoice[] PSOptionsTabsItems =
    {
        new("Horizontal", "Options.Layout.TabsHorizontal"),
        new("Vertical", "Options.Layout.TabsVertical")
    };

    internal readonly Border psOptionsStartupMode;
    internal Action? psOptionsStartupPicker;
    internal readonly Control psOptionsTabPicker;
    internal readonly CheckBox psMediaBox;
    internal readonly CheckBox psOptionsConfirmBox;
    internal readonly CheckBox psRelayClearBox;
    internal readonly Border psOptionsTabsMode;
    internal readonly ComboBox psOptionsLanguageCombo;

    internal PSOptionsGeneral(
        Border psOptionsStartupMode,
        Control psOptionsTabPicker,
        CheckBox psMediaBox,
        CheckBox psOptionsConfirmBox,
        CheckBox psRelayClearBox,
        Border psOptionsTabsMode,
        ComboBox psOptionsLanguageCombo)
    {
        this.psOptionsStartupMode = psOptionsStartupMode;
        this.psOptionsTabPicker = psOptionsTabPicker;
        this.psMediaBox = psMediaBox;
        this.psOptionsConfirmBox = psOptionsConfirmBox;
        this.psRelayClearBox = psRelayClearBox;
        this.psOptionsTabsMode = psOptionsTabsMode;
        this.psOptionsLanguageCombo = psOptionsLanguageCombo;
    }

    internal static Control PSOptionsPickerBuild(IReadOnlyList<QPickerItem> pItems)
    {
        var pPicker = new Control
        {
            MinWidth = 260,
            Height = PSField.PSFieldControlHeight,
            HorizontalAlignment = HorizontalAlignment.Left
        };
        QPickerControl.QPickerControlAttach(
            pPicker,
            pItems,
            QLocalizationCatalog.QLocalizationTextRead("Options.Startup.NoTab"));
        return pPicker;
    }

    internal UIElement PSGeneralBuild()
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
        StackPanel pRow = PSGeneralRowBuild();
        PSOptionsPickerUpdate();
        PSOptionsPickerAttach();
        return PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Options.General.OpenWith"), pRow);
    }

    private StackPanel PSGeneralRowBuild()
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
        return pRow;
    }

    private void PSOptionsPickerUpdate() =>
        PSOptionsPickerApply(PSOptionsField.PSOptionsModeCheck(psOptionsStartupMode, "DefaultTab"));

    private void PSOptionsPickerApply(bool pShown) =>
        psOptionsTabPicker.Visibility = pShown ? Visibility.Visible : Visibility.Collapsed;

    private void PSOptionsPickerAttach() => psOptionsStartupPicker = PSOptionsPickerUpdate;

    internal static LLocalizationChoice[] PSOptionsLanguagesRead() =>
        LLocalization.LLocalizationLanguagesRead()
            .Select(pLanguage => new LLocalizationChoice(
                pLanguage.Key,
                "Localization.Language.Name",
                pLanguage.Value))
            .ToArray();
}

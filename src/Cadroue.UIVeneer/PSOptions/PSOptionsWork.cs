using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.UIDeportment;

using static Cadroue.UIVeneer.PSCombo;
using static Cadroue.UIVeneer.PSField;
using static Cadroue.UIVeneer.PSPlate;

namespace Cadroue.UIVeneer;

internal sealed partial class PSOptions
{
    private readonly CheckBox psOptionsFailureBox;
    private readonly CheckBox psOptionsRetryBox;
    private readonly Slider psOptionsRetrySlider;

    private UIElement PSWorkBuild()
    {
        UIElement pRetryRow = PSOptionsFieldBuild(
            LLocalization.LLocalizationTextRead("Options.Work.RetryLimit"),
            psOptionsRetrySlider,
            string.Empty);
        pRetryRow.IsEnabled = psOptionsRetryBox.IsChecked == true;
        psOptionsRetryBox.Checked += (_, _) => pRetryRow.IsEnabled = true;
        psOptionsRetryBox.Unchecked += (_, _) => pRetryRow.IsEnabled = false;

        var pPanel = new StackPanel();
        pPanel.Children.Add(PSPlateBuild(LLocalization.LLocalizationTextRead("Options.Work.Failure"),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.Work.OnFailure"), psOptionsFailureBox),
            PSFieldBuild(LLocalization.LLocalizationTextRead("Options.Work.Retry"), psOptionsRetryBox),
            pRetryRow));
        pPanel.Children.Add(PSPlateBuild(
            LSOptions.LSOptionsLevelTitle,
            lsOptions.LSOptionsLevelsRead().Select(PSOptionsLevelBuild).ToArray()));
        return pPanel;
    }

    private static UIElement PSOptionsLevelBuild(LSOptionsLevel pLevel) =>
        PSFieldBuild(
            pLevel.LSOptionsLevelText,
            PSComboBuild(pLevel.LSOptionsLevelIndex, pLevel.LSOptionsLevelChoice, LSOptions.LSOptionsLevelNames));
}

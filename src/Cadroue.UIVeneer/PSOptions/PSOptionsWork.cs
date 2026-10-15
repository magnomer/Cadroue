using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed partial class PSOptions
{
    private readonly CheckBox psOptionsFailureBox;
    private readonly CheckBox psOptionsRetryBox;
    private readonly Slider psOptionsRetrySlider;

    private UIElement PSWorkBuild()
    {
        UIElement pRetryRow = PSOptionsFieldBuild(
            QLocalizationCatalog.QLocalizationTextRead("Options.Work.RetryLimit"),
            psOptionsRetrySlider,
            string.Empty);
        pRetryRow.IsEnabled = psOptionsRetryBox.IsChecked == true;
        psOptionsRetryBox.Checked += (_, _) => pRetryRow.IsEnabled = true;
        psOptionsRetryBox.Unchecked += (_, _) => pRetryRow.IsEnabled = false;

        var pPanel = new StackPanel();
        pPanel.Children.Add(PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead("Options.Work.Failure"),
            PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Options.Work.OnFailure"),
                psOptionsFailureBox),
            PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Options.Work.Retry"), psOptionsRetryBox),
            pRetryRow));
        pPanel.Children.Add(PSPlate.PSPlateBuild(
            QSOptions.QSOptionsLevelTitle,
            lsOptions.QSOptionsLevelsRead().Select(PSOptionsLevelBuild).ToArray()));
        return pPanel;
    }

    private static UIElement PSOptionsLevelBuild(QSOptionsLevel pLevel) =>
        PSField.PSFieldBuild(
            pLevel.QSOptionsLevelText,
            PSCombo.PSComboBuild(pLevel.QSOptionsLevelIndex, pLevel.QSOptionsLevelChoice,
                QSOptions.QSOptionsLevelNames));
}

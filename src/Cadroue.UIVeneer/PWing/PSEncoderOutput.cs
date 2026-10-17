using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIVeneer.PHouse;
using Cadroue.Application;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

internal sealed partial class PSEncoder
{
    private UIElement PSOutputPlateBuild()
    {
        StackPanel pPanel = PSOutputPanelBuild();
        PSOutputPlateAttach();
        PSOutputSuffixUpdate();
        PSLocationModeUpdate();
        return PSPlate.PSPlateBuild(pPanel);
    }

    private void PSOutputPlateAttach()
    {
        psLocationBrowse!.Click += (_, _) => PSLocationCustomRead();
        psOutputContainerCombo.SelectionChanged += (_, _) => PSOutputContainerHandle();
        psOutputSuffixBox.LostFocus += (_, _) => PSOutputSuffixNormalize();
        psOutputCollisionCombo.SelectionChanged += (_, _) => PSOutputSuffixUpdate();
    }

    private StackPanel PSOutputPanelBuild()
    {
        var pPanel = new StackPanel();
        psLocationStatus = new TextBlock
        {
            Foreground = PSEncoderMutedBrush,
            Margin = new Thickness(12, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        };
        PSNameBoxPrepare();
        psLocationFolderLabel = PSField.PSFieldLabelBuild(string.Empty);
        psLocationBrowse = PSInline.PSInlineIconBuild(
            PSLocationBrowseIcon,
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Location.Browse"),
            new Thickness(8, 0, 0, 0));
        psLocationFolderRow = PSFieldLabelledBuild(psLocationFolderLabel, psLocationFolderBox, psLocationBrowse);
        pPanel.Children.Add(
            PSField.PSFieldBuild(QLocalizationCatalog.QLocalizationTextRead("Encoder.Field.Output.Name"), psNameBox));
        pPanel.Children.Add(PSNameRowBuild());
        pPanel.Children.Add(PSLocationFieldBuild(psLocationStatus));
        pPanel.Children.Add(psLocationFolderRow);
        pPanel.Children.Add(
            PSField.PSFieldBuild(
                QLocalizationCatalog.QLocalizationTextRead("Encoder.Field.Output.Container"),
                psOutputContainerCombo));
        pPanel.Children.Add(
            PSField.PSFieldBuild(
                QLocalizationCatalog.QLocalizationTextRead("Encoder.Field.Output.Extension"),
                psOutputExtensionCombo));

        psOutputSuffixLabel = PSField.PSFieldLabelBuild(string.Empty);
        psOutputSuffixRow = PSOutputSuffixBuild(psOutputSuffixLabel, psOutputSuffixBox);
        pPanel.Children.Add(
            PSField.PSFieldBuild(
                QLocalizationCatalog.QLocalizationTextRead("Encoder.Field.Output.Collision"),
                psOutputCollisionCombo));
        pPanel.Children.Add(psOutputSuffixRow);
        return pPanel;
    }

    private static UIElement PSOutputSuffixBuild(TextBlock pLabel, TextBox pBox)
    {
        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 9), MinHeight = PSField.PSFieldControlHeight };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(pLabel);
        pBox.MinHeight = PSField.PSFieldControlHeight;
        Grid.SetColumn(pBox, 1);
        pGrid.Children.Add(pBox);
        return pGrid;
    }

    private void PSOutputSuffixNormalize() =>
        psOutputSuffixBox.Text = lsEncoder.QSEncoderSuffixNormalize(
            PSCombo.PSComboTextRead(psOutputCollisionCombo),
            psOutputSuffixBox.Text);

    private void PSOutputSuffixUpdate()
    {
        string pMode = PSCombo.PSComboTextRead(psOutputCollisionCombo);
        string pSuffix = lsEncoder.QSEncoderSuffixSelect(pMode, psOutputSuffixBox.Text);
        bool pShown = QSEncoder.QSEncoderSuffixCheck(pMode);
        if (pShown)
        {
            PSOutputSuffixSet(pSuffix, pMode);
        }

        PSOutputRowApply(pShown);
    }

    private void PSOutputSuffixSet(string pSuffix, string pMode)
    {
        psOutputSuffixBox.Text = pSuffix;
        if (psOutputSuffixLabel is not null)
        {
            psOutputSuffixLabel.Text = QLocalizationCatalog.QLocalizationTextRead(QSEncoder.QSEncoderSuffixResolve(
                pMode));
        }
    }

    private void PSOutputRowApply(bool pShown)
    {
        if (psOutputSuffixRow is not null)
        {
            psOutputSuffixRow.Visibility = pShown ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private static LLocalizationChoice[] PSOutputExtensionRead(string pContainer)
    {
        IReadOnlyList<string> pExtensions = LPreset.LPresetExtensionsRead(pContainer);
        return pExtensions.Count == 0
            ? [new LLocalizationChoice(string.Empty, "Encoder.Location.Source")]
            : pExtensions.Select(pExtension => new LLocalizationChoice(pExtension)).ToArray();
    }

    private void PSOutputExtensionUpdate()
    {
        string pCurrent = PSCombo.PSComboTextRead(psOutputExtensionCombo);
        LLocalizationChoice[] pChoices = PSOutputExtensionRead(PSCombo.PSComboTextRead(psOutputContainerCombo));
        PSOutputExtensionApply(pChoices, PSOutputExtensionResolve(pChoices, pCurrent));
    }

    private static LLocalizationChoice? PSOutputExtensionResolve(LLocalizationChoice[] pChoices, string pCurrent) =>
        pChoices.FirstOrDefault(
            pChoice => string.Equals(pChoice.LLocalizationChoiceToken, pCurrent, StringComparison.Ordinal))
        ?? pChoices.FirstOrDefault();

    private void PSOutputExtensionApply(LLocalizationChoice[] pChoices, LLocalizationChoice? pSelected)
    {
        psOutputExtensionCombo.ItemsSource = pChoices;
        psOutputExtensionCombo.SelectedItem = pSelected;
    }

    private void PSOutputContainerHandle()
    {
        PSOutputExtensionUpdate();
        PSCodecContainerHandle();
        PSAudioContainerHandle();
    }
}

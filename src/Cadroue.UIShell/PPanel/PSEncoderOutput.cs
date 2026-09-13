using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIShell.PHouse;
using Cadroue.Application;

using static Cadroue.UIShell.PSCasement.PSField;
using static Cadroue.UIShell.PSCasement.PSCombo;
using static Cadroue.UIShell.PSCasement.PSInline;
using static Cadroue.UIShell.PSCasement.PSPlate;

namespace Cadroue.UIShell.PPanel;

internal sealed partial class PSEncoder
{
    private UIElement PSOutputPlateBuild()
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
        psLocationFolderLabel = PSFieldLabelBuild(string.Empty);
        psLocationBrowse = PSInlineIconBuild(
            PSLocationBrowseIcon,
            LLocalization.LLocalizationTextRead("Encoder.Location.Browse"),
            new Thickness(8, 0, 0, 0));
        psLocationBrowse.Click += (_, _) => PSLocationCustomRead();
        psLocationFolderRow = PSFieldLabelledBuild(psLocationFolderLabel, psLocationFolderBox, psLocationBrowse);
        pPanel.Children.Add(PSFieldBuild(LLocalization.LLocalizationTextRead("Encoder.Field.Output.Name"), psNameBox));
        pPanel.Children.Add(PSNameRowBuild());
        pPanel.Children.Add(PSLocationFieldBuild(psLocationStatus));
        pPanel.Children.Add(psLocationFolderRow);
        psOutputContainerCombo.SelectionChanged += (_, _) => PSOutputContainerHandle();
        pPanel.Children.Add(
            PSFieldBuild(
                LLocalization.LLocalizationTextRead("Encoder.Field.Output.Container"),
                psOutputContainerCombo));
        pPanel.Children.Add(
            PSFieldBuild(
                LLocalization.LLocalizationTextRead("Encoder.Field.Output.Extension"),
                psOutputExtensionCombo));

        psOutputSuffixLabel = PSFieldLabelBuild(string.Empty);
        psOutputSuffixRow = PSOutputSuffixBuild(psOutputSuffixLabel, psOutputSuffixBox);
        psOutputSuffixBox.LostFocus += (_, _) => PSOutputSuffixNormalize();
        psOutputCollisionCombo.SelectionChanged += (_, _) => PSOutputSuffixUpdate();
        pPanel.Children.Add(
            PSFieldBuild(
                LLocalization.LLocalizationTextRead("Encoder.Field.Output.Collision"),
                psOutputCollisionCombo));
        pPanel.Children.Add(psOutputSuffixRow);
        PSOutputSuffixUpdate();

        PSLocationModeUpdate();
        return PSPlateBuild(pPanel);
    }

    private static UIElement PSOutputSuffixBuild(TextBlock pLabel, TextBox pBox)
    {
        var pGrid = new Grid { Margin = new Thickness(0, 0, 0, 9), MinHeight = PSFieldControlHeight };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(pLabel);
        pBox.MinHeight = PSFieldControlHeight;
        Grid.SetColumn(pBox, 1);
        pGrid.Children.Add(pBox);
        return pGrid;
    }

    private void PSOutputSuffixNormalize()
    {
        if (!string.IsNullOrEmpty(psOutputSuffixBox.Text.Trim()))
        {
            return;
        }

        string pMode = PSComboTextRead(psOutputCollisionCombo);
        lsExportSpecificEdit.LPresetSuffixSet(pMode, string.Empty);
        psOutputSuffixBox.Text = lsExportSpecificEdit.LPresetSuffixRead(pMode);
    }

    private static bool PSOutputSuffixCheck(string pPolicy) =>
        string.Equals(pPolicy, "Rename output", StringComparison.Ordinal)
        || string.Equals(pPolicy, "Rename existing", StringComparison.Ordinal);

    private void PSOutputSuffixUpdate()
    {
        string pMode = PSComboTextRead(psOutputCollisionCombo);

        if (psOutputSuffixMode is not null
            && PSOutputSuffixCheck(psOutputSuffixMode)
            && !string.Equals(psOutputSuffixMode, pMode, StringComparison.Ordinal))
        {
            lsExportSpecificEdit.LPresetSuffixSet(psOutputSuffixMode, psOutputSuffixBox.Text.Trim());
        }

        if (PSOutputSuffixCheck(pMode))
        {
            psOutputSuffixBox.Text = lsExportSpecificEdit.LPresetSuffixRead(pMode);
            if (psOutputSuffixLabel is not null)
            {
                psOutputSuffixLabel.Text = LLocalization.LLocalizationTextRead(
                    string.Equals(pMode, "Rename existing", StringComparison.Ordinal)
                        ? "Encoder.Field.Output.SuffixSource"
                        : "Encoder.Field.Output.SuffixOutput");
            }
        }

        psOutputSuffixMode = pMode;

        if (psOutputSuffixRow is not null)
        {
            psOutputSuffixRow.Visibility = PSOutputSuffixCheck(pMode)
                ? Visibility.Visible
                : Visibility.Collapsed;
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
        string pCurrent = PSComboTextRead(psOutputExtensionCombo);
        LLocalizationChoice[] pChoices = PSOutputExtensionRead(PSComboTextRead(psOutputContainerCombo));
        psOutputExtensionCombo.ItemsSource = pChoices;
        psOutputExtensionCombo.SelectedItem = pChoices.FirstOrDefault(
            pChoice => string.Equals(pChoice.LLocalizationChoiceToken, pCurrent, StringComparison.Ordinal))
            ?? pChoices.FirstOrDefault();
    }

    private void PSOutputContainerHandle()
    {
        PSOutputExtensionUpdate();
        PSCodecContainerHandle();
        PSAudioContainerHandle();
    }
}

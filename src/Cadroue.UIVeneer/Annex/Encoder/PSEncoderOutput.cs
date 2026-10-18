using System.Windows;
using System.Windows.Controls;
using Cadroue.Application;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PSEncoderOutput
{
    private readonly LPreset lsEncoderDraft;
    private readonly QSEncoderSuffix lsEncoderSuffix;
    private readonly System.Action psOutputContainerHandler;
    private readonly PSEncoderName psEncoderName;
    private readonly PSEncoderLocation psEncoderLocation;
    private readonly ListBox psOutputContainerCombo;
    private readonly ComboBox psOutputExtensionCombo;
    private readonly ComboBox psOutputCollisionCombo;
    private readonly TextBox psOutputSuffixBox;
    private UIElement? psOutputSuffixRow;
    private TextBlock? psOutputSuffixLabel;

    internal PSEncoderOutput(
        LPreset lsDraft,
        QSEncoderSuffix lsSuffix,
        QSEncoderLocation lsLocation,
        QToken pToken,
        Window pWindow,
        System.Action pContainerHandler)
    {
        lsEncoderDraft = lsDraft;
        lsEncoderSuffix = lsSuffix;
        psOutputContainerHandler = pContainerHandler;
        psEncoderName = new PSEncoderName(pToken, lsDraft.LPresetDisplay, pWindow);
        psOutputContainerCombo = PSList.PSListBuild(lsDraft.LPresetContainer,
            new LLocalizationChoice("Same as source", "Encoder.Location.Source"),
            new LLocalizationChoice("MP4", "Encoder.Container.MP4"),
            new LLocalizationChoice("Matroska", "Encoder.Container.Matroska"),
            new LLocalizationChoice("MOV", "Encoder.Container.MOV"),
            new LLocalizationChoice("WebM", "Encoder.Container.WebM"),
            new LLocalizationChoice("AVI", "Encoder.Container.AVI"),
            new LLocalizationChoice("MPEG-TS", "Encoder.Container.TS"),
            new LLocalizationChoice("FLV", "Encoder.Container.FLV"),
            new LLocalizationChoice("Ogg", "Encoder.Container.Ogg"));
        psOutputExtensionCombo = PSCombo.PSComboBuild(
            lsDraft.LPresetExtension,
            PSOutputExtensionRead(lsDraft.LPresetContainer));
        psOutputCollisionCombo = PSCombo.PSComboBuild(lsDraft.LPresetCollision,
            new LLocalizationChoice("Overwrite", "Encoder.Collision.Overwrite"),
            new LLocalizationChoice("Rename output", "Encoder.Collision.RenameOutput"),
            new LLocalizationChoice("Rename existing", "Encoder.Collision.RenameExisting"));
        psOutputSuffixBox = PSEntry.PSEntryBuild(
            lsDraft.LPresetSuffixRead(lsDraft.LPresetCollision),
            220);
        psEncoderLocation = new PSEncoderLocation(lsLocation, lsDraft);
    }

    internal string PSOutputContainerRead() => PSCombo.PSComboTextRead(psOutputContainerCombo);

    internal void PSOutputDraftSet()
    {
        lsEncoderDraft.LPresetDisplay = psEncoderName.PSNameRead();
        lsEncoderDraft.LPresetContainer = PSCombo.PSComboTextRead(psOutputContainerCombo);
        lsEncoderDraft.LPresetExtension = PSCombo.PSComboTextRead(psOutputExtensionCombo);
        lsEncoderDraft.LPresetCollision = PSCombo.PSComboTextRead(psOutputCollisionCombo);
        lsEncoderDraft.LPresetSuffixSet(
            lsEncoderDraft.LPresetCollision, PSEncoderValue.PSEncoderTrimRead(psOutputSuffixBox));
        lsEncoderDraft.LPresetLocation = psEncoderLocation.PSLocationModeRead();
        lsEncoderDraft.LPresetLocationSet(lsEncoderDraft.LPresetLocation, psEncoderLocation.PSLocationFolderRead());
    }

    internal UIElement PSOutputPlateBuild()
    {
        StackPanel pPanel = PSOutputPanelBuild();
        PSOutputPlateAttach();
        PSOutputSuffixUpdate();
        psEncoderLocation.PSLocationModeUpdate();
        return PSPlate.PSPlateBuild(pPanel);
    }

    private void PSOutputPlateAttach()
    {
        psEncoderLocation.PSLocationAttach();
        psOutputContainerCombo.SelectionChanged += (_, _) => PSOutputContainerHandle();
        psOutputSuffixBox.LostFocus += (_, _) => PSOutputSuffixNormalize();
        psOutputCollisionCombo.SelectionChanged += (_, _) => PSOutputSuffixUpdate();
    }

    private StackPanel PSOutputPanelBuild()
    {
        var pPanel = new StackPanel();
        pPanel.Children.Add(psEncoderName.PSNameFieldBuild());
        pPanel.Children.Add(psEncoderName.PSNameRowBuild());
        psEncoderLocation.PSLocationBuild(pPanel);
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
        psOutputSuffixBox.Text = lsEncoderSuffix.QSEncoderSuffixNormalize(
            PSCombo.PSComboTextRead(psOutputCollisionCombo),
            psOutputSuffixBox.Text);

    private void PSOutputSuffixUpdate()
    {
        string pMode = PSCombo.PSComboTextRead(psOutputCollisionCombo);
        string pSuffix = lsEncoderSuffix.QSEncoderSuffixSelect(pMode, psOutputSuffixBox.Text);
        bool pShown = QSEncoderSuffix.QSEncoderSuffixCheck(pMode);
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
            psOutputSuffixLabel.Text = QLocalizationCatalog.QLocalizationTextRead(
                QSEncoderSuffix.QSEncoderSuffixResolve(pMode));
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
        psOutputContainerHandler();
    }
}

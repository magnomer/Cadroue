using System.Windows;
using System.Windows.Controls;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer.PWing;

internal sealed class PSEncoderName
{
    private readonly RichTextBox psNameBox = new() { MinWidth = 320 };
    private readonly PSEncoderNameChip psEncoderNameChip;

    internal PSEncoderName(QToken pToken, string pDisplay, Window pWindow)
    {
        QTokenControl.QTokenControlAttach(psNameBox, pToken, pDisplay);
        psEncoderNameChip = new PSEncoderNameChip(psNameBox, pWindow);
    }

    internal string PSNameRead() => QTokenControl.QTokenControlRead(psNameBox);

    internal UIElement PSNameFieldBuild()
    {
        PSNameBoxPrepare();
        return PSField.PSFieldBuild(
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Field.Output.Name"), psNameBox);
    }

    private void PSNameBoxPrepare()
    {
        psNameBox.MinWidth = 320;
        psNameBox.Height = PSField.PSFieldControlHeight;
        psNameBox.HorizontalAlignment = HorizontalAlignment.Stretch;
    }

    internal UIElement PSNameRowBuild()
    {
        var pGrid = new Grid { Margin = new Thickness(0, 8, 0, 9) };
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) });
        pGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pGrid.Children.Add(
            PSField.PSFieldLabelBuild(QLocalizationCatalog.QLocalizationTextRead("Encoder.Field.Output.Elements")));

        var pPanel = new WrapPanel();
        pPanel.Children.Add(
            psEncoderNameChip.PSNameTokenBuild(
                QLocalizationCatalog.QLocalizationTextRead("Encoder.Field.Output.Prefix"),
                "{Prefix}"));
        pPanel.Children.Add(
            psEncoderNameChip.PSNameTokenBuild(
                QLocalizationCatalog.QLocalizationTextRead("Encoder.Field.Output.OriginalName"),
                "{OriginalName}"));
        pPanel.Children.Add(
            psEncoderNameChip.PSNameTokenBuild(
                QLocalizationCatalog.QLocalizationTextRead("Encoder.Field.Output.SectionNumber"),
                "{SectionNumber}"));
        pPanel.Children.Add(
            psEncoderNameChip.PSNameTokenBuild(
                QLocalizationCatalog.QLocalizationTextRead("Encoder.Field.Output.SectionName"),
                "{SectionName}"));
        pPanel.Children.Add(
            psEncoderNameChip.PSNameTokenBuild(
                QLocalizationCatalog.QLocalizationTextRead("Encoder.Field.Output.SectionStart"),
                "{SectionStart}"));
        pPanel.Children.Add(
            psEncoderNameChip.PSNameTokenBuild(
                QLocalizationCatalog.QLocalizationTextRead("Encoder.Field.Output.SectionEnd"),
                "{SectionEnd}"));
        pPanel.Children.Add(
            psEncoderNameChip.PSNameTokenBuild(
                QLocalizationCatalog.QLocalizationTextRead("Encoder.Field.Output.SectionDuration"),
                "{SectionDuration}"));
        pPanel.Children.Add(
            psEncoderNameChip.PSNameTokenBuild(
                QLocalizationCatalog.QLocalizationTextRead("Encoder.Field.Output.Date"),
                "{Date}"));
        pPanel.Children.Add(
            psEncoderNameChip.PSNameTokenBuild(
                QLocalizationCatalog.QLocalizationTextRead("Encoder.Field.Output.Time"),
                "{Time}"));
        pPanel.Children.Add(
            psEncoderNameChip.PSNameTokenBuild(
                QLocalizationCatalog.QLocalizationTextRead("Encoder.Field.Output.Suffix"),
                "{Suffix}"));
        pPanel.Children.Add(
            psEncoderNameChip.PSNameOperatorBuild(
                QLocalizationCatalog.QLocalizationTextRead("Encoder.Field.Output.Backspace"),
                "Backspace"));
        pPanel.Children.Add(
            psEncoderNameChip.PSNameOperatorBuild(
                QLocalizationCatalog.QLocalizationTextRead("Encoder.Field.Output.Delete"),
                "Delete"));
        Grid.SetColumn(pPanel, 1);
        pGrid.Children.Add(pPanel);
        return pGrid;
    }
}

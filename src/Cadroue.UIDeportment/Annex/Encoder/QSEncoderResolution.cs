using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

public sealed class QSEncoderResolution
{
    private readonly UserControl qsEncoderResolutionSurface;
    private readonly QSEncoderSize qsEncoderResolutionSize;
    private bool qsEncoderResolutionWriting;

    public QSEncoderResolution(UserControl lSurface, QSEncoderSize lSize, bool lReactive)
    {
        qsEncoderResolutionSurface = lSurface;
        qsEncoderResolutionSize = lSize;
        QSEncoderResolutionLoad();
        QSEncoderResolutionLoad();
        CheckBox lOrientation = QSEncoderResolutionOrientation;
        lOrientation.Content = QLocalizationCatalog.QLocalizationTextRead("Encoder.Video.Orientation");
        lOrientation.IsChecked = lReactive;
        QInteractionClick.QInteractionClickAttach(lOrientation);
        QCheckbox.QCheckboxAttach(lOrientation);
    }

    private static string QSEncoderResolutionSource =>
        QLocalizationCatalog.QLocalizationTextRead("Encoder.Sample.Source");

    private static string QSEncoderResolutionCustom =>
        QLocalizationCatalog.QLocalizationTextRead("Encoder.Value.Custom");

    private TextBlock QSEncoderResolutionCaption =>
        QContract.QContractFind<TextBlock>(qsEncoderResolutionSurface, "psEncoderResolutionCaption");

    private Slider QSEncoderResolutionTier =>
        QContract.QContractFind<Slider>(qsEncoderResolutionSurface, "psEncoderResolutionTier");

    private TextBlock QSEncoderResolutionReadout =>
        QContract.QContractFind<TextBlock>(qsEncoderResolutionSurface, "psEncoderResolutionReadout");

    private StackPanel QSEncoderResolutionRows =>
        QContract.QContractFind<StackPanel>(qsEncoderResolutionSurface, "psEncoderResolutionRows");

    private TextBlock QSEncoderResolutionHint =>
        QContract.QContractFind<TextBlock>(qsEncoderResolutionSurface, "psEncoderResolutionHint");

    private TextBlock QSEncoderResolutionLegend =>
        QContract.QContractFind<TextBlock>(qsEncoderResolutionSurface, "psEncoderResolutionLegend");

    internal CheckBox QSEncoderResolutionOrientation =>
        QContract.QContractFind<CheckBox>(qsEncoderResolutionSurface, "psEncoderResolutionOrientation");

    private Brush QSEncoderResolutionMuted =>
        QContract.QContractSheetFind<Brush>(qsEncoderResolutionSurface, "psEncoderResolutionMuted");

    private Brush QSEncoderResolutionInk =>
        QContract.QContractSheetFind<Brush>(qsEncoderResolutionSurface, "psEncoderResolutionInk");

    public UserControl QSEncoderResolutionBuild()
    {
        Slider lTier = QSEncoderResolutionTier;
        lTier.Maximum = CSEncoderSize.CSEncoderTierMost;
        QSlider.QSliderAttach(lTier);
        foreach (UIElement lRow in QSEncoderResolutionRows.Children)
        {
            QTextbox.QTextboxAttach(QContract.QContractFind<TextBox>(lRow, "psEncoderResolutionValue"));
        }

        foreach (UIElement lRow in QSEncoderResolutionRows.Children)
        {
            Slider lScale = QContract.QContractFind<Slider>(lRow, "psEncoderResolutionScale");
            lScale.Maximum = CSEncoderDimension.CSEncoderDimensionMost;
            QSlider.QSliderAttach(lScale);
        }

        QSEncoderResolutionCaption.Text = QLocalizationCatalog.QLocalizationTextRead("Encoder.Video.Field.Size");
        QSEncoderResolutionHint.Text = QLocalizationCatalog.QLocalizationTextRead("Encoder.Video.Notice.SizeSource");
        QSEncoderResolutionAttach();
        QSEncoderResolutionApply();
        QSEncoderResolutionToggle();
        QSEncoderResolutionLegend.Text = QLocalizationCatalog.QLocalizationTextRead("Encoder.Video.Field.Reactive");
        return qsEncoderResolutionSurface;
    }

    public void QSEncoderResolutionApply()
    {
        CSEncoderDimension lValue = qsEncoderResolutionSize.QSEncoderSizeValue;
        QSEncoderResolutionUpdate(
            lValue.CSEncoderDimensionNative,
            lValue.CSEncoderDimensionResolve(QSEncoderResolutionSource, QSEncoderResolutionCustom),
            lValue.CSEncoderDimensionNotch);
    }

    private void QSEncoderResolutionLoad() =>
        QSEncoderResolutionRows.Children.Add((UIElement)QContract.QContractSheetFind<DataTemplate>(
            qsEncoderResolutionSurface, "psEncoderResolutionDimension").LoadContent());

    private void QSEncoderResolutionAttach()
    {
        QSEncoderResolutionTier.ValueChanged += (_, _) => QSEncoderResolutionSelect();
        QSEncoderResolutionTier.Loaded += (_, _) => QSEncoderResolutionAdjust();
        foreach (UIElement lRow in QSEncoderResolutionRows.Children)
        {
            Slider lScale = QContract.QContractFind<Slider>(lRow, "psEncoderResolutionScale");
            TextBox lValue = QContract.QContractFind<TextBox>(lRow, "psEncoderResolutionValue");
            lScale.Loaded += (_, _) => QSEncoderResolutionAdjust();
            lScale.ValueChanged += (_, _) => QSEncoderResolutionSync(lValue, lScale.Value);
            lValue.TextChanged += (_, _) => QSEncoderResolutionChange();
        }

        QSEncoderResolutionOrientation.Checked += (_, _) => QSEncoderResolutionToggle();
        QSEncoderResolutionOrientation.Unchecked += (_, _) => QSEncoderResolutionToggle();
    }

    private void QSEncoderResolutionSelect() =>
        qsEncoderResolutionSize.QSEncoderTierSelect(QSEncoderResolutionTier.Value);

    private void QSEncoderResolutionSync(TextBox lValue, double lKnob)
    {
        if (qsEncoderResolutionSize.QSEncoderSizeValue.CSEncoderDimensionSync(lValue.Text, lKnob) is string lText)
        {
            lValue.Text = lText;
        }
    }

    private void QSEncoderResolutionChange()
    {
        if (qsEncoderResolutionWriting)
        {
            return;
        }

        qsEncoderResolutionSize.QSEncoderSizeSet(
            QContract.QContractFind<TextBox>(QSEncoderResolutionRows.Children[0], "psEncoderResolutionValue").Text,
            QContract.QContractFind<TextBox>(QSEncoderResolutionRows.Children[1], "psEncoderResolutionValue").Text);
    }

    private void QSEncoderResolutionUpdate(bool lSource, string lTier, int? lNotch)
    {
        qsEncoderResolutionWriting = true;
        try
        {
            string lWidth = qsEncoderResolutionSize.QSEncoderSizeValue.CSEncoderDimensionShown.Item1;
            QSEncoderResolutionSet(QSEncoderResolutionRows.Children[0], lWidth, lSource);
            string lHeight = qsEncoderResolutionSize.QSEncoderSizeValue.CSEncoderDimensionShown.Item2;
            QSEncoderResolutionSet(QSEncoderResolutionRows.Children[1], lHeight, lSource);
        }
        finally
        {
            qsEncoderResolutionWriting = false;
        }

        QSEncoderResolutionReadout.Text = lTier;
        QSEncoderResolutionHint.Visibility = lSource ? Visibility.Visible : Visibility.Collapsed;
        if (lNotch is int lAt)
        {
            QSEncoderResolutionTier.Value = lAt;
        }

        QContract.QContractFind<Slider>(QSEncoderResolutionRows.Children[0], "psEncoderResolutionScale").Value =
            qsEncoderResolutionSize.QSEncoderSizeValue.CSEncoderDimensionKnobs.Item1;
        QContract.QContractFind<Slider>(QSEncoderResolutionRows.Children[1], "psEncoderResolutionScale").Value =
            qsEncoderResolutionSize.QSEncoderSizeValue.CSEncoderDimensionKnobs.Item2;
        QSEncoderResolutionAdjust();
    }

    private void QSEncoderResolutionSet(UIElement lRow, string lShown, bool lSource)
    {
        TextBox lValue = QContract.QContractFind<TextBox>(lRow, "psEncoderResolutionValue");
        lValue.Text = lSource ? QSEncoderResolutionSource : lShown;
        lValue.Foreground = lSource ? QSEncoderResolutionMuted : QSEncoderResolutionInk;
    }

    private void QSEncoderResolutionAdjust()
    {
        QSEncoderResolutionShow(
            QSEncoderResolutionTier, !qsEncoderResolutionSize.QSEncoderSizeValue.CSEncoderDimensionCustom);
        bool lDimension = !qsEncoderResolutionSize.QSEncoderSizeValue.CSEncoderDimensionNative;
        foreach (UIElement lRow in QSEncoderResolutionRows.Children)
        {
            QSEncoderResolutionShow(QContract.QContractFind<Slider>(lRow, "psEncoderResolutionScale"), lDimension);
        }
    }

    private static void QSEncoderResolutionShow(Slider lScale, bool lVisible)
    {
        lScale.ApplyTemplate();
        if (lScale.Template?.FindName("pSliderThumb", lScale) is FrameworkElement lThumb)
        {
            lThumb.Visibility = lVisible ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void QSEncoderResolutionToggle()
    {
        (string lWidth, string lHeight) =
            CSEncoderDimension.CSEncoderDimensionDescribe(QSEncoderResolutionOrientation.IsChecked);
        QContract.QContractFind<TextBlock>(QSEncoderResolutionRows.Children[0], "psEncoderResolutionAxis").Text =
            QLocalizationCatalog.QLocalizationTextRead(lWidth);
        QContract.QContractFind<TextBlock>(QSEncoderResolutionRows.Children[1], "psEncoderResolutionAxis").Text =
            QLocalizationCatalog.QLocalizationTextRead(lHeight);
    }
}

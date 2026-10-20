using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Cadroue.UIDeportment;

public sealed class QSEncoderName
{
    private readonly UserControl qsEncoderNameSurface;
    private readonly QToken qsEncoderNameToken;

    public QSEncoderName(UserControl lSurface, QToken lToken, string lDisplay)
    {
        qsEncoderNameSurface = lSurface;
        qsEncoderNameToken = lToken;
        QTokenControl.QTokenControlAttach(QSEncoderNameBox, lToken, lDisplay);
        QContract.QContractFind<TextBlock>(lSurface, "psNameCaption").Text =
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Field.Output.Name");
        QContract.QContractFind<TextBlock>(lSurface, "psNameElementsCaption").Text =
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Field.Output.Elements");
        QSEncoderNameBuild();
    }

    private RichTextBox QSEncoderNameBox =>
        QContract.QContractFind<RichTextBox>(qsEncoderNameSurface, "psNameBox");

    private WrapPanel QSEncoderNameElements =>
        QContract.QContractFind<WrapPanel>(qsEncoderNameSurface, "psNameElements");

    public string QSEncoderNameRead() => QTokenControl.QTokenControlRead(QSEncoderNameBox);

    private void QSEncoderNameBuild()
    {
        (string QSEncoderNameKey, string QSEncoderNameToken)[] lTokens =
        [
            ("Encoder.Field.Output.Prefix", "{Prefix}"),
            ("Encoder.Field.Output.OriginalName", "{OriginalName}"),
            ("Encoder.Field.Output.SectionNumber", "{SectionNumber}"),
            ("Encoder.Field.Output.SectionName", "{SectionName}"),
            ("Encoder.Field.Output.SectionStart", "{SectionStart}"),
            ("Encoder.Field.Output.SectionEnd", "{SectionEnd}"),
            ("Encoder.Field.Output.SectionDuration", "{SectionDuration}"),
            ("Encoder.Field.Output.Date", "{Date}"),
            ("Encoder.Field.Output.Time", "{Time}"),
            ("Encoder.Field.Output.Suffix", "{Suffix}")
        ];
        foreach ((string lKey, string lToken) in lTokens)
        {
            QSEncoderNameAdd(QLocalizationCatalog.QLocalizationTextRead(lKey), () => lToken, null);
        }

        TextBox lBackspace = QSEncoderNameCreate();
        QSEncoderNameAdd(
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Field.Output.Backspace"),
            () => QSEncoderNameFormat("Backspace", lBackspace),
            lBackspace);
        TextBox lDelete = QSEncoderNameCreate();
        QSEncoderNameAdd(
            QLocalizationCatalog.QLocalizationTextRead("Encoder.Field.Output.Delete"),
            () => QSEncoderNameFormat("Delete", lDelete),
            lDelete);
    }

    private TextBox QSEncoderNameCreate()
    {
        var lCount = new TextBox
        {
            Text = "1",
            Width = 26,
            MaxLength = 3,
            TextAlignment = TextAlignment.Center,
            VerticalContentAlignment = VerticalAlignment.Center,
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            Margin = new Thickness(4, 0, 0, 0)
        };
        lCount.PreviewTextInput += (_, lEvent) => lEvent.Handled = !QSEncoderNameCheck(lEvent.Text);
        lCount.LostFocus += (_, _) => lCount.Text = QSEncoderNameRead(lCount.Text).ToString();
        return lCount;
    }

    private void QSEncoderNameAdd(string lLabel, Func<string> lToken, TextBox? lCount)
    {
        var lPanel = new StackPanel { Orientation = Orientation.Horizontal };
        lPanel.Children.Add(new TextBlock
        {
            Text = lLabel,
            Foreground = Brushes.DarkSlateGray,
            VerticalAlignment = VerticalAlignment.Center
        });
        if (lCount is not null) lPanel.Children.Add(lCount);
        var lChip = new Border
        {
            MinHeight = 28,
            BorderBrush = Brushes.LightGray,
            BorderThickness = new Thickness(1),
            Background = Brushes.White,
            CornerRadius = new CornerRadius(8),
            Padding = new Thickness(10, 0, 8, 0),
            Margin = new Thickness(0, 0, 6, 6),
            Cursor = Cursors.Hand,
            Child = lPanel
        };
        QSEncoderNameAttach(lChip, lToken, lCount);
        QSEncoderNameElements.Children.Add(lChip);
    }

    private void QSEncoderNameAttach(Border lChip, Func<string> lToken, TextBox? lCount)
    {
        Point? lOrigin = null;
        bool lDragged = false;
        lChip.PreviewMouseLeftButtonDown += (_, lEvent) =>
        {
            if (lCount is { IsMouseOver: true }) return;
            lOrigin = lEvent.GetPosition(null);
            lDragged = false;
        };
        lChip.PreviewMouseMove += (_, lEvent) =>
        {
            if (lEvent.LeftButton != MouseButtonState.Pressed || lOrigin is not Point lStart || lDragged) return;
            Point lCurrent = lEvent.GetPosition(null);
            if (Math.Abs(lCurrent.X - lStart.X) < 4 && Math.Abs(lCurrent.Y - lStart.Y) < 4) return;
            lDragged = true;
            QSNameDrag.QSNameDragRun(lChip, lToken());
        };
        lChip.MouseLeftButtonUp += (_, _) =>
        {
            if (!lDragged && lCount is not { IsMouseOver: true }) qsEncoderNameToken.QTokenDropHandle(lToken());
            lOrigin = null;
        };
    }

    private static string QSEncoderNameFormat(string lKind, TextBox lCount) =>
        $"{{{lKind}:{QSEncoderNameRead(lCount.Text)}}}";

    private static bool QSEncoderNameCheck(string lText) => lText.All(char.IsDigit);

    private static int QSEncoderNameRead(string lText) =>
        int.TryParse(lText, out int lCount) && lCount > 0 ? lCount : 1;
}

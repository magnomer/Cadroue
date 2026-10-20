using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

public sealed class QSFader
{
    private readonly CSFader qsFaderPolicy;
    private readonly Slider qsFaderScale;
    private readonly TextBox qsFaderReadout;
    private readonly TextBlock qsFaderNotice;
    private bool qsFaderWriting;

    private QSFader(UserControl lSurface, CSFader lFader, string lLabel, string lNotice)
    {
        qsFaderPolicy = lFader;
        var lRow = (FrameworkElement)QContract.QContractSheetFind<DataTemplate>(lSurface, "psFaderTrack").LoadContent();
        QContract.QContractFind<TextBlock>(lRow, "psFaderCaption").Text = lLabel;
        qsFaderScale = QContract.QContractFind<Slider>(lRow, "psFaderScale");
        qsFaderScale.Minimum = lFader.CSFaderLeast;
        qsFaderScale.Maximum = lFader.CSFaderMost;
        qsFaderScale.SmallChange = lFader.CSFaderStep;
        qsFaderScale.LargeChange = lFader.CSFaderLeap;
        qsFaderScale.TickFrequency = lFader.CSFaderStep;
        if (lFader.CSFaderNotched)
        {
            qsFaderScale.Ticks = new DoubleCollection(lFader.CSFaderTicks);
            qsFaderScale.TickPlacement = TickPlacement.BottomRight;
        }

        qsFaderScale.IsSnapToTickEnabled = lFader.CSFaderSnap;
        qsFaderScale.Value = lFader.CSFaderOrigin;
        QSlider.QSliderAttach(qsFaderScale);
        if (lFader.CSFaderNotched)
        {
            qsFaderScale.IsSnapToTickEnabled = true;
        }

        qsFaderReadout = QContract.QContractFind<TextBox>(lRow, "psFaderReadout");
        QTextbox.QTextboxAttach(qsFaderReadout);
        qsFaderReadout.IsReadOnly = lFader.CSFaderLocked;
        qsFaderReadout.Text = lFader.CSFaderShown;
        qsFaderNotice = QSFaderLoad(lSurface, lRow, lNotice);
        qsFaderNotice.Visibility = lFader.CSFaderNoticeOrigin ? Visibility.Visible : Visibility.Collapsed;
        qsFaderScale.ValueChanged += QSFaderSelect;
        qsFaderScale.ValueChanged += QSFaderShow;
        qsFaderReadout.TextChanged += QSFaderMatch;
        qsFaderReadout.KeyDown += QSFaderKeyHandle;
        qsFaderReadout.LostKeyboardFocus += (_, _) => QSFaderCommit();
    }

    public static TextBox QSFaderBuild(
        UserControl lSurface,
        string lLabel,
        string lText,
        double? lMinimum,
        double? lMaximum,
        double lStep,
        bool lAscending,
        bool lBitrate,
        string lNotice) =>
        CSFader.CSFaderQualityCreate(lText, lMinimum, lMaximum, lStep, lAscending, lBitrate) is { } lFader
            ? new QSFader(lSurface, lFader, lLabel, lNotice).qsFaderReadout
            : QSFaderEntryBuild(lSurface, lLabel, lText, lNotice);

    public static TextBox QSFaderBuild(
        UserControl lSurface,
        string lLabel,
        string lText,
        IReadOnlyList<int> lRates,
        bool lSnap,
        double lMaximum,
        string lZero,
        string lNotice) =>
        new QSFader(
            lSurface, CSFader.CSFaderDetentCreate(lText, lRates, lSnap, lMaximum, lZero), lLabel, lNotice)
            .qsFaderReadout;

    public static Slider QSFaderBuild(
        UserControl lSurface, string lLabel, IReadOnlyList<string> lLabels, int lIndex, string lNotice) =>
        new QSFader(lSurface, CSFader.CSFaderLayoutCreate(lLabels, lIndex), lLabel, lNotice).qsFaderScale;

    public static void QSFaderClear(UserControl lSurface) =>
        QContract.QContractFind<StackPanel>(lSurface, "psFaderRows").Children.Clear();

    public static TextBox? QSFaderRead(UserControl lSurface)
    {
        StackPanel lRows = QContract.QContractFind<StackPanel>(lSurface, "psFaderRows");
        return lRows.Children.Count == 0
            ? null
            : QContract.QContractFind<TextBox>(lRows.Children[0], "psFaderReadout");
    }

    private static TextBox QSFaderEntryBuild(UserControl lSurface, string lLabel, string lText, string lNotice)
    {
        var lRow = (FrameworkElement)QContract.QContractSheetFind<DataTemplate>(lSurface, "psFaderEntry").LoadContent();
        QContract.QContractFind<TextBlock>(lRow, "psFaderCaption").Text = lLabel;
        TextBox lReadout = QContract.QContractFind<TextBox>(lRow, "psFaderReadout");
        lReadout.Text = lText;
        QTextbox.QTextboxAttach(lReadout);
        QSFaderLoad(lSurface, lRow, lNotice);
        return lReadout;
    }

    private static TextBlock QSFaderLoad(UserControl lSurface, FrameworkElement lRow, string lNotice)
    {
        var lLine = (TextBlock)QContract.QContractSheetFind<DataTemplate>(lSurface, "psFaderNotice").LoadContent();
        lLine.Text = lNotice;
        StackPanel lRows = QContract.QContractFind<StackPanel>(lSurface, "psFaderRows");
        lRows.Children.Add(lRow);
        lRows.Children.Add(lLine);
        return lLine;
    }

    private void QSFaderSelect(object lSender, RoutedPropertyChangedEventArgs<double> lEvent)
    {
        if (qsFaderWriting)
        {
            return;
        }

        qsFaderWriting = true;
        try
        {
            qsFaderReadout.Text = qsFaderPolicy.CSFaderFormat(qsFaderScale.Value);
        }
        finally
        {
            qsFaderWriting = false;
        }
    }

    private void QSFaderShow(object lSender, RoutedPropertyChangedEventArgs<double> lEvent) =>
        qsFaderNotice.Visibility =
            qsFaderPolicy.CSFaderNoticeCheck(qsFaderScale.Value) ? Visibility.Visible : Visibility.Collapsed;

    private void QSFaderMatch(object lSender, TextChangedEventArgs lEvent)
    {
        if (qsFaderWriting)
        {
            return;
        }

        if (qsFaderPolicy.CSFaderMatch(qsFaderReadout.Text) is not double lAt)
        {
            return;
        }

        qsFaderWriting = true;
        try
        {
            qsFaderScale.Value = lAt;
        }
        finally
        {
            qsFaderWriting = false;
        }
    }

    private void QSFaderKeyHandle(object lSender, KeyEventArgs lEvent)
    {
        if (lEvent.Key == Key.Return)
        {
            lEvent.Handled = QSFaderCommit();
        }
    }

    private bool QSFaderCommit()
    {
        if (qsFaderPolicy.CSFaderCommit(qsFaderReadout.Text, qsFaderScale.Value) is not { } lResult)
        {
            return false;
        }

        qsFaderWriting = true;
        try
        {
            qsFaderScale.Value = lResult.Item1;
            qsFaderReadout.Text = lResult.Item2;
            qsFaderReadout.CaretIndex = qsFaderReadout.Text.Length;
        }
        finally
        {
            qsFaderWriting = false;
        }

        return true;
    }
}

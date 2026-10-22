using Cadroue.Conduct;
using Cadroue.Infrastructure;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Cadroue.UIDeportment;

public sealed record QSSpectrumRow(
    string QSSpectrumRowName,
    IReadOnlyList<string> QSSpectrumRowColors,
    bool QSSpectrumRowNative,
    string? QSSpectrumRowPath,
    bool QSSpectrumRowSelected);

public sealed record QSSpectrumSnapshot(IReadOnlyList<QSSpectrumRow> QSSpectrumSnapshotRows);

public sealed class QSSpectrum
{
    private const string QSSpectrumRemoveIcon = "/PAsset/PPanel/PSSpectrumRemove.svg";

    private readonly CSSpectrum qsSpectrum;
    private readonly List<(
        Border,
        MouseButtonEventHandler,
        MouseEventHandler,
        Button,
        RoutedEventHandler)> qsSpectrumRows = [];
    private QSSpectrumSnapshot qsSpectrumSnapshot = new([]);
    private FrameworkElement? psSpectrum;
    private StackPanel? psSpectrumGallery;
    private Action<RoutedEventArgs, string>? qsSpectrumRemoval;

    public QSSpectrum(string lName)
        : this(lName, LDepot.LDepotPaletteRead)
    {
    }

    public QSSpectrum(string lName, Func<string> lFolder)
    {
        QSSpectrumFile = new QSSpectrumFile(lFolder);
        qsSpectrum = new CSSpectrum(lName, QSSpectrumFile);
    }

    public QSSpectrumFile QSSpectrumFile { get; }

    public string QSSpectrumName => qsSpectrum.CSSpectrumName;

    public static bool QSSpectrumFixedCheck(string lName) =>
        CSSpectrum.CSSpectrumFixedCheck(lName);

    public void QSSpectrumAttach(
        FrameworkElement lSurface,
        Action<RoutedEventArgs, string> lRemoval)
    {
        QSSpectrumDetach();
        psSpectrum = lSurface;
        psSpectrumGallery = QContract.QContractFind<StackPanel>(lSurface, "PSSpectrumGallery");
        qsSpectrumRemoval = lRemoval;
        QSSpectrumUpdate(qsSpectrumSnapshot);
    }

    public void QSSpectrumDetach()
    {
        foreach (var lRow in qsSpectrumRows)
        {
            lRow.Item1.MouseLeftButtonDown -= lRow.Item2;
            lRow.Item1.MouseEnter -= lRow.Item3;
            lRow.Item1.MouseLeave -= lRow.Item3;
            lRow.Item4.Click -= lRow.Item5;
        }

        qsSpectrumRows.Clear();
        psSpectrumGallery?.Children.Clear();
        psSpectrumGallery = null;
        psSpectrum = null;
        qsSpectrumRemoval = null;
    }

    public string? QSSpectrumImport(string lSourcePath)
    {
        CSSpectrumImportResult lResult =
            qsSpectrum.CSSpectrumImport(lSourcePath, out _, out string? lLoadedName);
        if (lResult != CSSpectrumImportResult.CSSpectrumImportLoaded)
        {
            return lResult switch
            {
                CSSpectrumImportResult.CSSpectrumImportReserved => "Options.Timeline.ReservedPalette",
                CSSpectrumImportResult.CSSpectrumImportFailed => "Options.Timeline.PaletteCopyFailed",
                _ => "Options.Timeline.InvalidPalette"
            };
        }

        QSSpectrumUpdate();
        QSSpectrumSelect(lLoadedName!);
        return null;
    }

    public void QSSpectrumSave(string lTargetPath) => qsSpectrum.CSSpectrumSave(lTargetPath);

    public void QSSpectrumUpdate()
    {
        qsSpectrumSnapshot = QSSpectrumUpdate(qsSpectrum.CSSpectrumUpdate(), qsSpectrum.CSSpectrumName);
        QSSpectrumUpdate(qsSpectrumSnapshot);
    }

    private static QSSpectrumSnapshot QSSpectrumUpdate(CSSpectrumSnapshot lSnapshot, string lSelected) =>
        new(lSnapshot.CSSpectrumSnapshotRows
            .Select(lRow => new QSSpectrumRow(
                lRow.CSSpectrumRowName,
                lRow.CSSpectrumRowColors,
                lRow.CSSpectrumRowNative,
                lRow.CSSpectrumRowPath,
                string.Equals(lRow.CSSpectrumRowName, lSelected, StringComparison.Ordinal)))
            .ToArray());

    private void QSSpectrumUpdate(QSSpectrumSnapshot lSnapshot)
    {
        foreach (var lRow in qsSpectrumRows)
        {
            lRow.Item1.MouseLeftButtonDown -= lRow.Item2;
            lRow.Item1.MouseEnter -= lRow.Item3;
            lRow.Item1.MouseLeave -= lRow.Item3;
            lRow.Item4.Click -= lRow.Item5;
        }

        qsSpectrumRows.Clear();
        psSpectrumGallery?.Children.Clear();
        if (psSpectrum is null || psSpectrumGallery is null || qsSpectrumRemoval is null)
        {
            return;
        }

        DataTemplate lTemplate = (DataTemplate)psSpectrum.FindResource("PSSpectrumLabel");
        foreach (QSSpectrumRow lRow in lSnapshot.QSSpectrumSnapshotRows)
        {
            var lLine = (StackPanel)lTemplate.LoadContent();
            lLine.DataContext = lRow;
            Border lLabel = QContract.QContractFind<Border>(lLine, "PSSpectrumLabel");
            ((TextBlock)((StackPanel)lLabel.Child).Children[0]).Text = lRow.QSSpectrumRowName;
            ItemsControl lBadge = QContract.QContractFind<ItemsControl>(lLine, "PSSpectrumBadge");
            foreach (string lColor in lRow.QSSpectrumRowColors)
            {
                var lSwatch = (Border)lBadge.ItemTemplate.LoadContent();
                lSwatch.Background = (Brush)new BrushConverter().ConvertFromString(lColor)!;
                lBadge.Items.Add(lSwatch);
            }

            if (lRow.QSSpectrumRowSelected)
            {
                lLabel.BorderThickness = new Thickness(2);
                lLabel.Padding = new Thickness(11, 7, 13, 7);
            }

            Action<bool> lLook = lHovered =>
            {
                if (lRow.QSSpectrumRowSelected || lHovered)
                {
                    lLabel.Background = new SolidColorBrush(Color.FromRgb(0xF7, 0xF9, 0xFC));
                    lLabel.BorderBrush = new SolidColorBrush(Color.FromRgb(0x4C, 0x86, 0xF7));
                    return;
                }

                lLabel.ClearValue(Border.BackgroundProperty);
                lLabel.ClearValue(Border.BorderBrushProperty);
            };
            lLook(false);
            MouseEventHandler lHover = (_, lInput) => lLook(lInput.RoutedEvent == UIElement.MouseEnterEvent);
            lLabel.MouseEnter += lHover;
            lLabel.MouseLeave += lHover;
            MouseButtonEventHandler lSelection = (_, _) => QSSpectrumSelect(lRow.QSSpectrumRowName);
            lLabel.MouseLeftButtonDown += lSelection;

            Button lRemover = QContract.QContractFind<Button>(lLine, "PSSpectrumRemover");
            lRemover.Visibility = QSSpectrumFixedCheck(lRow.QSSpectrumRowName)
                ? Visibility.Collapsed
                : Visibility.Visible;
            lRemover.ToolTip = QLocalizationCatalog.QLocalizationFormat(
                "Options.Timeline.RemoveTooltip", lRow.QSSpectrumRowName);
            lRemover.Content = new Image
            {
                Source = QIcon.QIconRead(QSSpectrumRemoveIcon, QTool.QToolIconRead()),
                Width = 18,
                Height = 18,
                Stretch = Stretch.Uniform
            };
            QButton.QButtonAttach(lRemover, QButtonLook.QButtonLookIcon);
            RoutedEventHandler lRemoval = (_, lInput) => qsSpectrumRemoval(lInput, lRow.QSSpectrumRowName);
            lRemover.Click += lRemoval;
            qsSpectrumRows.Add((lLabel, lSelection, lHover, lRemover, lRemoval));
            psSpectrumGallery.Children.Add(lLine);
        }
    }

    public void QSSpectrumSelect(string lName)
    {
        if (!qsSpectrum.CSSpectrumSelect(lName))
        {
            return;
        }

        qsSpectrumSnapshot = QSSpectrumUpdate(qsSpectrum.CSSpectrumSnapshot, qsSpectrum.CSSpectrumName);
        QSSpectrumUpdate(qsSpectrumSnapshot);
    }

    public bool QSSpectrumRemove(string lName, Func<string, bool> lConfirmation)
    {
        QSSpectrumRow lRow = qsSpectrumSnapshot.QSSpectrumSnapshotRows
            .First(lEntry => string.Equals(lEntry.QSSpectrumRowName, lName, StringComparison.Ordinal));
        string lQuestion = QLocalizationCatalog.QLocalizationFormat(
            lRow.QSSpectrumRowNative
                ? "Options.Timeline.RemoveBuiltInConfirm"
                : "Options.Timeline.RemoveWorkspaceConfirm",
            lName);
        if (!lConfirmation(lQuestion)
            || !qsSpectrum.CSSpectrumRemove(lName, lRow.QSSpectrumRowNative, lRow.QSSpectrumRowPath))
        {
            return false;
        }

        QSSpectrumUpdate();
        return true;
    }
}

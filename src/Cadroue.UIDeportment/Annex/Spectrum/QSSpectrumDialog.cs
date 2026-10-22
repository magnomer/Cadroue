using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Cadroue.UIDeportment;

public sealed class QSSpectrumDialog
{
    private const string QSSpectrumLoadIcon = "/PAsset/PPanel/PSSpectrumLoad.svg";
    private const string QSSpectrumSaveIcon = "/PAsset/PPanel/PSSpectrumSave.svg";

    private readonly QSSpectrum qsSpectrum;
    private readonly FrameworkElement psSpectrum;
    private readonly Button psSpectrumLoader;
    private readonly Button psSpectrumSaver;
    private readonly Delegate[] qsSpectrumDialog;

    public QSSpectrumDialog(
        QSSpectrum spectrum,
        FrameworkElement surface,
        Action<string> warning,
        Func<string, bool> confirmation)
        : this(
            spectrum,
            surface,
            warning,
            confirmation,
            () =>
            {
                var dialog = new OpenFileDialog
                {
                    Title = QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.LoadTitle"),
                    Filter = QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.JsonFilter"),
                    InitialDirectory = spectrum.QSSpectrumFile.QSSpectrumFolderRead()
                };
                return dialog.ShowDialog() == true ? dialog.FileName : null;
            },
            name =>
            {
                var dialog = new SaveFileDialog
                {
                    Title = QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.SaveTitle"),
                    Filter = QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.JsonFilter"),
                    FileName = $"{name}.json",
                    InitialDirectory = spectrum.QSSpectrumFile.QSSpectrumFolderRead()
                };
                return dialog.ShowDialog() == true ? dialog.FileName : null;
            })
    {
    }

    public QSSpectrumDialog(
        QSSpectrum spectrum,
        FrameworkElement surface,
        Action<string> warning,
        Func<string, bool> confirmation,
        Func<string?> load,
        Func<string, string?> save)
    {
        qsSpectrum = spectrum;
        psSpectrum = surface;
        psSpectrumLoader = QContract.QContractFind<Button>(surface, "PSSpectrumLoader");
        psSpectrumSaver = QContract.QContractFind<Button>(surface, "PSSpectrumSaver");
        RoutedEventHandler loadRequest = (_, _) => QSSpectrumDialogLoad();
        RoutedEventHandler saveRequest = (_, _) => QSSpectrumDialogSave();
        qsSpectrumDialog = [loadRequest, saveRequest, warning, confirmation, load, save];
    }

    public void QSSpectrumDialogAttach()
    {
        QSSpectrumDialogDetach();
        QContract.QContractFind<TextBlock>(psSpectrum, "PSSpectrumTitle").Text =
            QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.Palette");
        psSpectrumLoader.ToolTip = QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.LoadTooltip");
        psSpectrumSaver.ToolTip = QLocalizationCatalog.QLocalizationTextRead("Options.Timeline.SaveTooltip");
        psSpectrumLoader.Content = new Image
        {
            Source = QIcon.QIconRead(QSSpectrumLoadIcon, QTool.QToolIconRead()),
            Width = 18,
            Height = 18,
            Stretch = Stretch.Uniform
        };
        psSpectrumSaver.Content = new Image
        {
            Source = QIcon.QIconRead(QSSpectrumSaveIcon, QTool.QToolIconRead()),
            Width = 18,
            Height = 18,
            Stretch = Stretch.Uniform
        };
        QButton.QButtonAttach(psSpectrumLoader, QButtonLook.QButtonLookIcon);
        QButton.QButtonAttach(psSpectrumSaver, QButtonLook.QButtonLookIcon);
        psSpectrumLoader.Click += (RoutedEventHandler)qsSpectrumDialog[0];
        psSpectrumSaver.Click += (RoutedEventHandler)qsSpectrumDialog[1];
        qsSpectrum.QSSpectrumAttach(psSpectrum, QSSpectrumDialogRemove);
        qsSpectrum.QSSpectrumUpdate();
    }

    public void QSSpectrumDialogDetach()
    {
        psSpectrumLoader.Click -= (RoutedEventHandler)qsSpectrumDialog[0];
        psSpectrumSaver.Click -= (RoutedEventHandler)qsSpectrumDialog[1];
        qsSpectrum.QSSpectrumDetach();
    }

    public void QSSpectrumDialogLoad()
    {
        string? path = ((Func<string?>)qsSpectrumDialog[4])();
        if (path is null)
        {
            return;
        }

        string? warning = qsSpectrum.QSSpectrumImport(path);
        if (warning is not null)
        {
            ((Action<string>)qsSpectrumDialog[2])(warning);
        }
    }

    public void QSSpectrumDialogSave()
    {
        string? path = ((Func<string, string?>)qsSpectrumDialog[5])(qsSpectrum.QSSpectrumName);
        if (path is not null)
        {
            qsSpectrum.QSSpectrumSave(path);
        }
    }

    public void QSSpectrumDialogRemove(RoutedEventArgs input, string name)
    {
        input.Handled = true;
        qsSpectrum.QSSpectrumRemove(name, (Func<string, bool>)qsSpectrumDialog[3]);
    }
}

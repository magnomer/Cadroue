using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Navigation;
using Cadroue.Conduct;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed class QSAbout
{
    private const string QSAboutPlacementKey = "About";
    private const string QSAboutMarkPath = "/PAsset/PProgram/PProgramIcon.png";
    private const double QSAboutMarkSize = 76;

    private readonly CSAbout qsAboutConduct;
    private readonly Window qsAboutSurface;
    private readonly (Action qsAboutGrabberAttachment, Action qsAboutGrabberDetachment) qsAboutGrabber;
    private readonly (Action<Window, string> qsAboutPlacementRestoration, Action<Window, string> qsAboutPlacementSaving)
        qsAboutPlacement;
    private readonly Action<Window?, string, string> qsAboutWarning;
    private readonly TextBlock qsAboutDeveloper;

    public QSAbout(
        CSAbout about,
        Func<Window> surface,
        Func<Window, string?, FrameworkElement, UIElement> shell,
        Func<Window, (Action qsAboutGrabberAttachment, Action qsAboutGrabberDetachment)> grabber,
        Action<Window?, string, string> warning,
        Action<Window, string> restore,
        Action<Window, string> save)
    {
        qsAboutConduct = about;
        qsAboutSurface = surface();
        QSAboutShellBuild(shell);
        qsAboutGrabber = grabber(qsAboutSurface);
        qsAboutWarning = warning;
        qsAboutPlacement = (restore, save);
        qsAboutDeveloper = QContract.QContractFind<TextBlock>(qsAboutSurface, "PSAboutDeveloper");
    }

    private void QSAboutShellBuild(Func<Window, string?, FrameworkElement, UIElement> shell)
    {
        FrameworkElement body = QContract.QContractFind<FrameworkElement>(qsAboutSurface, "PSAboutBody");
        qsAboutSurface.Content = null;
        qsAboutSurface.Content = shell(qsAboutSurface, null, body);
    }

    public void QSAboutShow(Window owner)
    {
        qsAboutSurface.Title = QLocalizationCatalog.QLocalizationTextRead("About.Window.Title");
        qsAboutSurface.Owner = owner;
        QScrollbar.QScrollbarAttach(qsAboutSurface);
        QSAboutHeaderBuild();
        QSAboutCreditBuild();
        QSAboutButtonBuild();
        qsAboutSurface.Loaded += QSAboutLoadedHandle;
        qsAboutGrabber.qsAboutGrabberAttachment();
        qsAboutSurface.Closed += QSAboutCloseHandle;
        qsAboutSurface.ShowDialog();
    }

    private void QSAboutHeaderBuild()
    {
        Image mark = QContract.QContractFind<Image>(qsAboutSurface, "PSAboutMark");
        mark.Source = QIcon.QIconRead(QSAboutMarkPath, QSAboutMarkSize);
        mark.MouseLeftButtonUp += QSAboutTapHandle;
        qsAboutDeveloper.Text = QLocalizationCatalog.QLocalizationTextRead("About.Developer.Active");
        QSAboutDeveloperShow();
        QContract.QContractFind<TextBlock>(qsAboutSurface, "PSAboutBrand").Text =
            QLocalizationCatalog.QLocalizationTextRead("Terms.Cadroue");
        QContract.QContractFind<TextBlock>(qsAboutSurface, "PSAboutVersion").Text =
            QLocalizationCatalog.QLocalizationFormat("About.Version.Format", LProgramVersion.LProgramVersionRead());
        QSAboutLinkAttach(
            QContract.QContractFind<Hyperlink>(qsAboutSurface, "PSAboutProject"), CSAbout.CSAboutProjectUrl);
    }

    private void QSAboutCreditBuild()
    {
        QContract.QContractFind<TextBlock>(qsAboutSurface, "PSAboutTitle").Text =
            QLocalizationCatalog.QLocalizationTextRead("About.Credits.Title");
        StackPanel gallery = QContract.QContractFind<StackPanel>(qsAboutSurface, "PSAboutGallery");
        DataTemplate template = QContract.QContractSheetFind<DataTemplate>(qsAboutSurface, "PSAboutCredit");
        foreach (CSAboutCredit credit in CSAbout.CSAboutCredits)
        {
            var row = (FrameworkElement)template.LoadContent();
            QContract.QContractFind<TextBlock>(row, "PSAboutLabel").Text = credit.CSAboutCreditName;
            QSAboutLinkAttach(QContract.QContractFind<Hyperlink>(row, "PSAboutLink"), credit.CSAboutCreditUrl);
            gallery.Children.Add(row);
        }

        QContract.QContractFind<TextBlock>(qsAboutSurface, "PSAboutLicense").Text =
            QLocalizationCatalog.QLocalizationTextRead("About.Licenses.Label");
        Hyperlink document = QContract.QContractFind<Hyperlink>(qsAboutSurface, "PSAboutDocument");
        document.Inlines.Add(new Run(QLocalizationCatalog.QLocalizationTextRead("About.Licenses.Link")));
        document.Click += QSAboutNoticeHandle;
    }

    private void QSAboutButtonBuild()
    {
        Button closer = QContract.QContractFind<Button>(qsAboutSurface, "PSAboutCloser");
        closer.Content = QLocalizationCatalog.QLocalizationTextRead("About.Button.Close");
        QButton.QButtonBuild(closer, QButtonLook.QButtonLookWhite);
        closer.Click += (_, _) => qsAboutSurface.Close();
    }

    private void QSAboutLinkAttach(Hyperlink link, string url)
    {
        link.Inlines.Add(new Run(url));
        link.NavigateUri = new Uri(url);
        link.RequestNavigate += QSAboutLinkHandle;
    }

    private void QSAboutTapHandle(object sender, MouseButtonEventArgs input)
    {
        if (qsAboutConduct.CSAboutTapChange(DateTime.UtcNow))
        {
            QSAboutDeveloperShow();
        }
    }

    private void QSAboutDeveloperShow() =>
        qsAboutDeveloper.Visibility = qsAboutConduct.CSAboutDeveloper ? Visibility.Visible : Visibility.Collapsed;

    private void QSAboutLinkHandle(object sender, RequestNavigateEventArgs input)
    {
        input.Handled = true;
        string url = input.Uri.AbsoluteUri;
        QSAboutErrorShow(url, qsAboutConduct.CSAboutLinkOpen(url));
    }

    private void QSAboutNoticeHandle(object sender, RoutedEventArgs input)
    {
        input.Handled = true;
        (string path, string? failure) = qsAboutConduct.CSAboutNoticeOpen();
        QSAboutErrorShow(path, failure);
    }

    private void QSAboutErrorShow(string resource, string? detail)
    {
        if (detail is not null)
        {
            qsAboutWarning(
                qsAboutSurface,
                QLocalizationCatalog.QLocalizationTextRead("About.Window.Title"),
                QLocalizationCatalog.QLocalizationFormat("About.Error.Open", resource, detail));
        }
    }

    private void QSAboutLoadedHandle(object sender, RoutedEventArgs input)
    {
        qsAboutSurface.Loaded -= QSAboutLoadedHandle;
        QSAboutHeightApply();
        qsAboutPlacement.qsAboutPlacementRestoration(qsAboutSurface, QSAboutPlacementKey);
    }

    private void QSAboutHeightApply()
    {
        double height = qsAboutSurface.ActualHeight;
        qsAboutSurface.SizeToContent = SizeToContent.Manual;
        qsAboutSurface.Height = Math.Max(height, qsAboutSurface.MinHeight);
    }

    private void QSAboutCloseHandle(object? sender, EventArgs input)
    {
        qsAboutSurface.Loaded -= QSAboutLoadedHandle;
        qsAboutPlacement.qsAboutPlacementSaving(qsAboutSurface, QSAboutPlacementKey);
        qsAboutGrabber.qsAboutGrabberDetachment();
        qsAboutSurface.Closed -= QSAboutCloseHandle;
    }
}

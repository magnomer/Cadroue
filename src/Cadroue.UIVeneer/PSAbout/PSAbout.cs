using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using Cadroue.UIVeneer.PHouse;

using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PSAbout : Window
{
    internal const string PSAboutPlacementKey = "About";

    private const string PSAboutLogoPath = "/PAsset/PProgram/PProgramIcon.png";

    private const double PSAboutWidthDefault = 460;
    private const double PSAboutWidthMinimum = 400;
    private const double PSAboutHeightMinimum = 400;
    private const double PSAboutLogoSize = 76;
    private const double PSAboutRowGap = 7;

    private readonly QSAbout lsAbout;
    private readonly PSGrabber psAboutGrabber;

    private TextBlock? psAboutDeveloper;

    internal static void PSAboutShow(Window pOwner, QSAbout lsAboutOwner)
    {
        var psAbout = new PSAbout(pOwner, lsAboutOwner);
        psAbout.ShowDialog();
    }

    private PSAbout(Window pOwner, QSAbout lsAboutOwner)
    {
        lsAbout = lsAboutOwner;
        PSAboutLookApply(pOwner);
        QScrollbar.QScrollbarAttach(this);
        lsAbout.QSAboutErrorShown += PSAboutErrorShow;
        PSAboutContentApply();
        PSAboutLoadAttach();
        psAboutGrabber = new PSGrabber(this);
        psAboutGrabber.PSGrabberAttach();
        PSAboutCloseAttach();
    }

    private void PSAboutLookApply(Window pOwner)
    {
        Title = QLocalizationCatalog.QLocalizationTextRead("About.Window.Title");
        Owner = pOwner;
        Width = PSAboutWidthDefault;
        MinWidth = PSAboutWidthMinimum;
        MinHeight = PSAboutHeightMinimum;
        ResizeMode = ResizeMode.NoResize;
        PSDialog.PSDialogApply(this, new SolidColorBrush(Color.FromRgb(0xDC, 0xE8, 0xF7)));
    }

    private void PSAboutContentApply()
    {
        Content = PSAboutBuild();
        SizeToContent = SizeToContent.Height;
    }

    private void PSAboutLoadAttach() => Loaded += PSAboutLoadedHandle;

    private void PSAboutLoadDetach() => Loaded -= PSAboutLoadedHandle;

    private void PSAboutCloseAttach() => Closed += PSAboutCloseHandle;

    private void PSAboutCloseDetach() => Closed -= PSAboutCloseHandle;

    private void PSAboutLoadedHandle(object pSender, RoutedEventArgs pEvent)
    {
        PSAboutLoadDetach();
        PSAboutHeightApply();
        PSGrabber.PSGrabberPlacementRestore(this, PSAboutPlacementKey);
    }

    private void PSAboutHeightApply()
    {
        double pContentHeight = ActualHeight;
        SizeToContent = SizeToContent.Manual;
        Height = PSAboutHeightClamp(pContentHeight);
    }

    private double PSAboutHeightClamp(double pContentHeight) => Math.Max(pContentHeight, MinHeight);

    private UIElement PSAboutBuild() =>
        PSDialog.PSDialogBuild(this, null, PSAboutRootBuild());

    private DockPanel PSAboutRootBuild()
    {
        var pRoot = new DockPanel
        {
            Background = Brushes.White
        };

        var pFooter = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(12)
        };
        Button pClose = PSFooter.PSFooterButtonBuild(QLocalizationCatalog.QLocalizationTextRead("About.Button.Close"));
        PSAboutButtonAttach(pClose);
        pFooter.Children.Add(pClose);
        DockPanel.SetDock(pFooter, Dock.Bottom);
        pRoot.Children.Add(pFooter);

        var pBody = new StackPanel { Margin = new Thickness(18, 14, 18, 0) };
        pBody.Children.Add(PSAboutHeaderBuild());
        pBody.Children.Add(PSAboutCreditBuild());
        pRoot.Children.Add(PSSheet.PSSheetScrollBuild(pBody));
        return pRoot;
    }

    private void PSAboutButtonAttach(Button pClose) => pClose.Click += (_, _) => Close();

    private UIElement PSAboutHeaderBuild()
    {
        var pHeader = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 26)
        };

        var pLogo = new Image
        {
            Source = QIcon.QIconRead(PSAboutLogoPath, PSAboutLogoSize),
            Width = PSAboutLogoSize,
            Height = PSAboutLogoSize,
            Stretch = Stretch.Uniform,
            Margin = new Thickness(0, 0, 0, 12)
        };
        PSAboutLogoAttach(pLogo);
        pHeader.Children.Add(pLogo);

        psAboutDeveloper = new TextBlock
        {
            Text = QLocalizationCatalog.QLocalizationTextRead("About.Developer.Active"),
            FontWeight = FontWeights.SemiBold,
            Foreground = PSField.PSFieldText,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 4),
            Visibility = PSAboutDeveloperRead()
        };
        pHeader.Children.Add(psAboutDeveloper);

        pHeader.Children.Add(new TextBlock
        {
            Text = QLocalizationCatalog.QLocalizationTextRead("Terms.Cadroue"),
            FontSize = 22,
            FontWeight = FontWeights.SemiBold,
            Foreground = PSField.PSFieldText,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        pHeader.Children.Add(new TextBlock
        {
            Text = QLocalizationCatalog.QLocalizationFormat("About.Version.Format", QSAbout.QSAboutVersionRead()),
            Foreground = PSField.PSFieldMuted,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 10)
        });
        pHeader.Children.Add(PSAboutLinkBuild(QSAbout.QSAboutProjectUrl, HorizontalAlignment.Center));
        return pHeader;
    }

    private void PSAboutLogoAttach(Image pLogo) => pLogo.MouseLeftButtonUp += PSAboutTapHandle;

    private Visibility PSAboutDeveloperRead() =>
        lsAbout.QSAboutTap.QSAboutDeveloper ? Visibility.Visible : Visibility.Collapsed;

    private UIElement PSAboutCreditBuild()
    {
        var pRows = new List<UIElement>(QSAbout.QSAboutCredits.Count + 1);
        foreach ((string pName, string pUrl) in QSAbout.QSAboutCredits)
        {
            pRows.Add(PSAboutRowBuild(pName, pUrl));
        }

        pRows.Add(PSAboutLicenseBuild());
        return PSPlate.PSPlateBuild(QLocalizationCatalog.QLocalizationTextRead("About.Credits.Title"), pRows.ToArray());
    }

    private UIElement PSAboutRowBuild(string pName, string pUrl)
    {
        var pRow = new Grid { Margin = new Thickness(0, 0, 0, PSAboutRowGap) };
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PSField.PSFieldLabelWidth) });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pRow.Children.Add(PSField.PSFieldLabelBuild(pName));

        TextBlock pLink = PSAboutLinkBuild(pUrl, HorizontalAlignment.Left);
        Grid.SetColumn(pLink, 1);
        pRow.Children.Add(pLink);
        return pRow;
    }

    private UIElement PSAboutLicenseBuild()
    {
        var pRow = new Grid { Margin = new Thickness(0, 0, 0, PSAboutRowGap) };
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PSField.PSFieldLabelWidth) });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pRow.Children.Add(PSField.PSFieldLabelBuild(QLocalizationCatalog.QLocalizationTextRead(
            "About.Licenses.Label")));

        var pLink = new Hyperlink(new Run(QLocalizationCatalog.QLocalizationTextRead("About.Licenses.Link")));
        PSAboutNoticeAttach(pLink);

        var pText = new TextBlock
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };
        pText.Inlines.Add(pLink);
        Grid.SetColumn(pText, 1);
        pRow.Children.Add(pText);
        return pRow;
    }

    private void PSAboutNoticeAttach(Hyperlink pLink) => pLink.Click += PSAboutNoticeHandle;

    private void PSAboutNoticeHandle(object pSender, RoutedEventArgs pEvent)
    {
        pEvent.Handled = true;
        lsAbout.QSAboutNoticeOpen();
    }

    private TextBlock PSAboutLinkBuild(string pUrl, HorizontalAlignment pAlignment)
    {
        var pLink = new Hyperlink(new Run(pUrl)) { NavigateUri = new Uri(pUrl) };
        PSAboutLinkAttach(pLink);

        var pText = new TextBlock
        {
            HorizontalAlignment = pAlignment,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };
        pText.Inlines.Add(pLink);
        return pText;
    }

    private void PSAboutLinkAttach(Hyperlink pLink) => pLink.RequestNavigate += PSAboutLinkHandle;

    private void PSAboutTapHandle(object pSender, MouseButtonEventArgs pEvent)
    {
        if (lsAbout.QSAboutTap.QSAboutTapChange(DateTime.UtcNow) && psAboutDeveloper is not null)
        {
            PSAboutDeveloperShow(psAboutDeveloper);
        }
    }

    private void PSAboutDeveloperShow(TextBlock pDeveloper) => pDeveloper.Visibility = PSAboutDeveloperRead();

    private void PSAboutLinkHandle(object pSender, RequestNavigateEventArgs pEvent)
    {
        pEvent.Handled = true;
        lsAbout.QSAboutLinkOpen(pEvent.Uri.AbsoluteUri);
    }

    private void PSAboutErrorShow(string pResource, string pDetail) =>
        PSWarning.PSWarningShow(
            this,
            QLocalizationCatalog.QLocalizationTextRead("About.Window.Title"),
            QLocalizationCatalog.QLocalizationFormat("About.Error.Open", pResource, pDetail));

    private void PSAboutCloseHandle(object? pSender, EventArgs pEvent)
    {
        PSAboutLoadDetach();
        PSGrabber.PSGrabberPlacementSave(this, PSAboutPlacementKey);
        psAboutGrabber.PSGrabberDetach();
        PSAboutCloseDetach();
    }

}

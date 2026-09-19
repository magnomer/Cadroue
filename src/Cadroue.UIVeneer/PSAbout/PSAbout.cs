using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PHouse;

using static Cadroue.UIVeneer.PSField;
using static Cadroue.UIVeneer.PSFooter;
using static Cadroue.UIVeneer.PSPlate;

using Cadroue.Infrastructure;
using Cadroue.Application;
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

    private readonly LSAbout lsAbout = new();
    private readonly PSGrabber psAboutGrabber;

    private TextBlock? psAboutDeveloper;

    internal static void PSAboutShow(Window pOwner)
    {
        var psAbout = new PSAbout(pOwner);
        psAbout.ShowDialog();
    }

    private PSAbout(Window pOwner)
    {
        Title = LLocalization.LLocalizationTextRead("About.Window.Title");
        Owner = pOwner;
        Width = PSAboutWidthDefault;
        MinWidth = PSAboutWidthMinimum;
        MinHeight = PSAboutHeightMinimum;
        ResizeMode = ResizeMode.NoResize;
        PSDialog.PSDialogApply(this, new SolidColorBrush(Color.FromRgb(0xDC, 0xE8, 0xF7)));
        PScrollbar.PScrollbarApply(this);
        Content = PSAboutBuild();
        SizeToContent = SizeToContent.Height;
        Loaded += PSAboutLoadedHandle;
        psAboutGrabber = new PSGrabber(this);
        psAboutGrabber.PSGrabberAttach();
        Closed += PSAboutCloseHandle;
    }

    private void PSAboutLoadedHandle(object pSender, RoutedEventArgs pEvent)
    {
        Loaded -= PSAboutLoadedHandle;
        double pContentHeight = ActualHeight;
        SizeToContent = SizeToContent.Manual;
        Height = Math.Max(pContentHeight, MinHeight);
        PSGrabber.PSGrabberPlacementRestore(this, PSAboutPlacementKey);
    }

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
        Button pClose = PSFooterButtonBuild(LLocalization.LLocalizationTextRead("About.Button.Close"));
        pClose.Click += (_, _) => Close();
        pFooter.Children.Add(pClose);
        DockPanel.SetDock(pFooter, Dock.Bottom);
        pRoot.Children.Add(pFooter);

        var pBody = new StackPanel { Margin = new Thickness(18, 14, 18, 0) };
        pBody.Children.Add(PSAboutHeaderBuild());
        pBody.Children.Add(PSAboutCreditBuild());
        pRoot.Children.Add(PSSheet.PSSheetScrollBuild(pBody));
        return pRoot;
    }

    private UIElement PSAboutHeaderBuild()
    {
        var pHeader = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 26)
        };

        var pLogo = new Image
        {
            Source = PIcon.PIconRead(PSAboutLogoPath),
            Width = PSAboutLogoSize,
            Height = PSAboutLogoSize,
            Stretch = Stretch.Uniform,
            Margin = new Thickness(0, 0, 0, 12)
        };
        pLogo.MouseLeftButtonUp += PSAboutTapHandle;
        pHeader.Children.Add(pLogo);

        psAboutDeveloper = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("About.Developer.Active"),
            FontWeight = FontWeights.SemiBold,
            Foreground = PSFieldText,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 4),
            Visibility = lsAbout.LSAboutDeveloper ? Visibility.Visible : Visibility.Collapsed
        };
        pHeader.Children.Add(psAboutDeveloper);

        pHeader.Children.Add(new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Terms.Cadroue"),
            FontSize = 22,
            FontWeight = FontWeights.SemiBold,
            Foreground = PSFieldText,
            HorizontalAlignment = HorizontalAlignment.Center
        });
        pHeader.Children.Add(new TextBlock
        {
            Text = LLocalization.LLocalizationFormat("About.Version.Format", LSAbout.LSAboutVersionRead()),
            Foreground = PSFieldMuted,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 10)
        });
        pHeader.Children.Add(PSAboutLinkBuild(LSAbout.LSAboutProjectUrl, HorizontalAlignment.Center));
        return pHeader;
    }

    private UIElement PSAboutCreditBuild()
    {
        var pRows = new List<UIElement>(LSAbout.LSAboutCredits.Count + 1);
        foreach ((string pName, string pUrl) in LSAbout.LSAboutCredits)
        {
            pRows.Add(PSAboutRowBuild(pName, pUrl));
        }

        pRows.Add(PSAboutLicenseBuild());
        return PSPlateBuild(LLocalization.LLocalizationTextRead("About.Credits.Title"), pRows.ToArray());
    }

    private UIElement PSAboutRowBuild(string pName, string pUrl)
    {
        var pRow = new Grid { Margin = new Thickness(0, 0, 0, PSAboutRowGap) };
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PSFieldLabelWidth) });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pRow.Children.Add(PSFieldLabelBuild(pName));

        TextBlock pLink = PSAboutLinkBuild(pUrl, HorizontalAlignment.Left);
        Grid.SetColumn(pLink, 1);
        pRow.Children.Add(pLink);
        return pRow;
    }

    private UIElement PSAboutLicenseBuild()
    {
        var pRow = new Grid { Margin = new Thickness(0, 0, 0, PSAboutRowGap) };
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(PSFieldLabelWidth) });
        pRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pRow.Children.Add(PSFieldLabelBuild(LLocalization.LLocalizationTextRead("About.Licenses.Label")));

        var pLink = new Hyperlink(new Run(LLocalization.LLocalizationTextRead("About.Licenses.Link")));
        pLink.Click += PSAboutNoticeHandle;

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

    private void PSAboutNoticeHandle(object pSender, RoutedEventArgs pEvent)
    {
        pEvent.Handled = true;
        string pPath = System.IO.Path.Combine(AppContext.BaseDirectory, LSAbout.LSAboutNoticeName);
        if (LUsher.LUsherLinkOpen(pPath) is { } pNoticeError)
        {
            LTraceLog.LTraceErrorRecord($"Notice could not be opened: {pPath}: {pNoticeError}");
            PSAboutErrorShow(pPath, pNoticeError);
        }
    }

    private TextBlock PSAboutLinkBuild(string pUrl, HorizontalAlignment pAlignment)
    {
        var pLink = new Hyperlink(new Run(pUrl)) { NavigateUri = new Uri(pUrl) };
        pLink.RequestNavigate += PSAboutLinkHandle;

        var pText = new TextBlock
        {
            HorizontalAlignment = pAlignment,
            VerticalAlignment = VerticalAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };
        pText.Inlines.Add(pLink);
        return pText;
    }

    private void PSAboutTapHandle(object pSender, MouseButtonEventArgs pEvent)
    {
        if (lsAbout.LSAboutTapChange(DateTime.UtcNow) && psAboutDeveloper is not null)
        {
            psAboutDeveloper.Visibility = lsAbout.LSAboutDeveloper ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void PSAboutLinkHandle(object pSender, RequestNavigateEventArgs pEvent)
    {
        pEvent.Handled = true;
        if (LUsher.LUsherLinkOpen(pEvent.Uri.AbsoluteUri) is { } pLinkError)
        {
            LTraceLog.LTraceErrorRecord($"Link could not be opened: {pEvent.Uri.AbsoluteUri}: {pLinkError}");
            PSAboutErrorShow(pEvent.Uri.AbsoluteUri, pLinkError);
        }
    }

    private void PSAboutErrorShow(string pResource, string pDetail) =>
        PSWarning.PSWarningShow(
            this,
            LLocalization.LLocalizationTextRead("About.Window.Title"),
            LLocalization.LLocalizationFormat("About.Error.Open", pResource, pDetail));

    private void PSAboutCloseHandle(object? pSender, EventArgs pEvent)
    {
        Loaded -= PSAboutLoadedHandle;
        PSGrabber.PSGrabberPlacementSave(this, PSAboutPlacementKey);
        psAboutGrabber.PSGrabberDetach();
        Closed -= PSAboutCloseHandle;
    }

}

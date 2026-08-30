using System.Diagnostics;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using Cadroue.UIShell.PAssets;
using Cadroue.UIShell.PMainWindow;
using Cadroue.UIShell.PSShared;

using static Cadroue.UIShell.PSShared.PSField;
using static Cadroue.UIShell.PSShared.PSFooter;
using static Cadroue.UIShell.PSShared.PSPlate;

using Cadroue.Infrastructure;

namespace Cadroue.UIShell;

internal sealed class PSAbout : Window
{
    internal const string PSAboutPlacementKey = "About";

    private const string PSAboutProjectUrl = "https://github.com/magnomer/Cadroue";
    private const string PSAboutNoticeName = "THIRD-PARTY-NOTICES.md";
    private const string PSAboutLogoPath = "/PAssets/PProgram/PProgramIcon.png";

    private const double PSAboutWidthDefault = 460;
    private const double PSAboutWidthMinimum = 400;
    private const double PSAboutHeightMinimum = 400;
    private const double PSAboutLogoSize = 76;
    private const double PSAboutRowGap = 7;

    private static readonly (string Name, string Url)[] PSAboutCredits =
    {
        ("FFmpeg", "https://ffmpeg.org"),
        ("FlyleafLib", "https://github.com/SuRGeoNix/Flyleaf"),
        ("MPV", "https://mpv.io"),
        ("Phosphor Icons", "https://phosphoricons.com/"),
        ("SharpVectors", "https://github.com/ElinamLLC/SharpVectors")
    };

    private readonly PSGrabber psAboutGrabber;

    private int psAboutTapCount;
    private DateTime psAboutTapLast;

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
            Visibility = Cadroue.Application.LPreference.LPreferenceStateCurrent.LPreferenceDeveloperActive
                ? Visibility.Visible
                : Visibility.Collapsed
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
            Text = LLocalization.LLocalizationFormat("About.Version.Format", PSAboutVersionRead()),
            Foreground = PSFieldMuted,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 4, 0, 10)
        });
        pHeader.Children.Add(PSAboutLinkBuild(PSAboutProjectUrl, HorizontalAlignment.Center));
        return pHeader;
    }

    private UIElement PSAboutCreditBuild()
    {
        var pRows = new List<UIElement>(PSAboutCredits.Length + 1);
        foreach ((string pName, string pUrl) in PSAboutCredits)
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
        string pPath = System.IO.Path.Combine(AppContext.BaseDirectory, PSAboutNoticeName);
        try
        {
            Process.Start(new ProcessStartInfo(pPath) { UseShellExecute = true });
        }
        catch (Exception pException)
        {
            LTraceLog.LTraceErrorRecord($"Notice could not be opened: {pPath}", pException);
            PSAboutErrorShow(pPath, pException.Message);
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
        DateTime pNow = DateTime.UtcNow;
        psAboutTapCount = (pNow - psAboutTapLast).TotalSeconds > 1.5 ? 1 : psAboutTapCount + 1;
        psAboutTapLast = pNow;
        if (psAboutTapCount >= 10)
        {
            psAboutTapCount = 0;
            bool psAboutDeveloperNext = !Cadroue.Application.LPreference.LPreferenceStateCurrent.LPreferenceDeveloperActive;
            Cadroue.Application.LPreference.LPreferenceDeveloperSet(psAboutDeveloperNext);
            if (psAboutDeveloper is not null)
            {
                psAboutDeveloper.Visibility = psAboutDeveloperNext ? Visibility.Visible : Visibility.Collapsed;
            }
        }
    }

    private void PSAboutLinkHandle(object pSender, RequestNavigateEventArgs pEvent)
    {
        pEvent.Handled = true;
        try
        {
            Process.Start(new ProcessStartInfo(pEvent.Uri.AbsoluteUri) { UseShellExecute = true });
        }
        catch (Exception pException)
        {
            LTraceLog.LTraceErrorRecord($"Link could not be opened: {pEvent.Uri.AbsoluteUri}", pException);
            PSAboutErrorShow(pEvent.Uri.AbsoluteUri, pException.Message);
        }
    }

    private void PSAboutErrorShow(string pResource, string pDetail) =>
        PSWarning.PSWarningShow(
            this,
            LLocalization.LLocalizationTextRead("About.Window.Title"),
            LLocalization.LLocalizationFormat("About.Error.Open", pResource, pDetail));

    private static string PSAboutVersionRead()
    {
        Assembly pAssembly = Assembly.GetExecutingAssembly();
        string pVersion = pAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? pAssembly.GetName().Version?.ToString()
            ?? string.Empty;
        int pBuildMark = pVersion.IndexOf('+');
        return pBuildMark < 0 ? pVersion : pVersion[..pBuildMark];
    }

    private void PSAboutCloseHandle(object? pSender, EventArgs pEvent)
    {
        Loaded -= PSAboutLoadedHandle;
        PSGrabber.PSGrabberPlacementSave(this, PSAboutPlacementKey);
        psAboutGrabber.PSGrabberDetach();
        Closed -= PSAboutCloseHandle;
    }

}

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using FlyleafLib.Controls.WPF;
using Cadroue.Core;
using Cadroue.UIShell.PMainWindow;
using Cadroue.UIShell.PPanels;
using Cadroue.UIShell.PSShared;

using static Cadroue.UIShell.PSShared.PSField;

namespace Cadroue.UIShell;

internal sealed partial class PSLoupe : Window
{
    internal const string PSLoupePlacementKey = "Loupe";

    private const double PSLoupeWidthDefault = 720;
    private const double PSLoupeHeightDefault = 480;
    private const double PSLoupeWidthMinimum = 360;
    private const double PSLoupeHeightMinimum = 240;
    private const double PSLoupeBarHeight = 48;
    private const double PSLoupeIconSize = 22;
    private const double PSLoupeButtonSize = 36;
    private const string PSLoupeStartIcon = "/PAssets/PCompass/PCompassPlay.svg";
    private const string PSLoupePauseIcon = "/PAssets/PCompass/PCompassPause.svg";

    private static readonly SolidColorBrush PSLoupePlayBrush = new(Color.FromRgb(0x2F, 0x9E, 0x64));
    private static readonly SolidColorBrush PSLoupeFloatFill = new(Color.FromRgb(0xD3, 0xE1, 0xF2));
    private static readonly SolidColorBrush PSLoupeFloatBorder = new(Color.FromRgb(0xD9, 0xDE, 0xE7));

    private readonly PSGrabber psLoupeGrabber;
    private readonly Window psLoupeOwner;
    private readonly PViewer psLoupeSource;
    private readonly PPlayer psLoupePlayer = new();
    private readonly DispatcherTimer psLoupeClock = new() { Interval = TimeSpan.FromMilliseconds(200) };

    private Border? psLoupeMediaHost;
    private Border? psLoupePlaySlot;
    private Border? psLoupeFloatSlot;

    private FlyleafHost? psLoupeFlyleafHost;
    private PViewerMpvHost? psLoupeMpvHost;
    private Button? psLoupePlayButton;
    private Image? psLoupePlayImage;
    private Button[]? psLoupeFloatButtons;
    private PSLoupeFloat psLoupeFloat = PSLoupeFloat.PSLoupeFloatOwner;
    private bool psLoupePlaying;
    private bool psLoupeEnded;
    private bool psLoupeClosed;

    internal static void PSLoupeShow(Window pOwner, PViewer pSource)
    {
        var psLoupe = new PSLoupe(pOwner, pSource);
        pSource.PViewerLoupeAttach(psLoupe);
        psLoupe.Show();
    }

    private PSLoupe(Window pOwner, PViewer pSource)
    {
        psLoupeOwner = pOwner;
        psLoupeSource = pSource;
        psLoupeFloat = PSLoupeFloatRestore();
        Title = LLocalization.LLocalizationTextRead("Loupe.Window.Title");
        Owner = pOwner;
        Width = PSLoupeWidthDefault;
        Height = PSLoupeHeightDefault;
        MinWidth = PSLoupeWidthMinimum;
        MinHeight = PSLoupeHeightMinimum;
        ResizeMode = ResizeMode.NoResize;
        PSDialog.PSDialogApply(this, PSCasement.PSCasementBandFill);
        PScrollbar.PScrollbarApply(this);
        Content = PSLoupeBuild();
        psLoupeClock.Tick += PSLoupeClockHandle;
        psLoupeSource.PViewerPreviewChange += PSLoupePreviewHandle;
        Loaded += PSLoupeLoadedHandle;
        psLoupeGrabber = new PSGrabber(this);
        psLoupeGrabber.PSGrabberAttach();
        Closed += PSLoupeCloseHandle;
    }

    private void PSLoupeLoadedHandle(object pSender, RoutedEventArgs pEvent)
    {
        Loaded -= PSLoupeLoadedHandle;
        PSGrabber.PSGrabberPlacementRestore(this, PSLoupePlacementKey);
        PSLoupeFloatApply(psLoupeFloat);
        PSLoupePlaybackStart();
    }

    private UIElement PSLoupeBuild() =>
        PSDialog.PSDialogBuild(this, Title, PSLoupeBodyBuild());

    private DockPanel PSLoupeBodyBuild()
    {
        var pBody = new DockPanel
        {
            Background = Brushes.Black
        };

        UIElement pBar = PSLoupeBarBuild();
        DockPanel.SetDock(pBar, Dock.Bottom);
        pBody.Children.Add(pBar);

        psLoupeMediaHost = new Border { Background = Brushes.Black, ClipToBounds = true };
        pBody.Children.Add(psLoupeMediaHost);
        return pBody;
    }

    private UIElement PSLoupeBarBuild()
    {
        var pBar = new Grid { Height = PSLoupeBarHeight, Background = PSCasement.PSCasementBandFill };
        pBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        psLoupePlaySlot = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Child = PSLoupePlayBuild()
        };
        Grid.SetColumn(psLoupePlaySlot, 1);
        pBar.Children.Add(psLoupePlaySlot);

        psLoupeFloatSlot = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 12, 0),
            Child = PSLoupeFloatBuild()
        };
        Grid.SetColumn(psLoupeFloatSlot, 2);
        pBar.Children.Add(psLoupeFloatSlot);
        return pBar;
    }

    private void PSLoupeCloseHandle(object? pSender, EventArgs pEvent)
    {
        psLoupeClosed = true;
        Loaded -= PSLoupeLoadedHandle;
        psLoupeClock.Stop();
        psLoupeClock.Tick -= PSLoupeClockHandle;
        psLoupeSource.PViewerPreviewChange -= PSLoupePreviewHandle;
        PSGrabber.PSGrabberPlacementSave(this, PSLoupePlacementKey);
        psLoupeGrabber.PSGrabberDetach();

        TimeSpan pFinal = psLoupePlayer.PPlayerReady ? psLoupePlayer.PPlayerTimeRead() : TimeSpan.Zero;
        bool pPlaying = psLoupePlaying && !psLoupeEnded;

        PSLoupePlaybackDispose();
        psLoupeSource.PViewerLoupeDetach(pFinal, pPlaying);
        Closed -= PSLoupeCloseHandle;
    }

}

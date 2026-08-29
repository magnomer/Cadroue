using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using FlyleafLib;
using FlyleafLib.Controls.WPF;
using FlyleafLib.MediaPlayer;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIShell.PAssets;
using Cadroue.UIShell.PMainWindow;
using Cadroue.UIShell.PPanels;
using Cadroue.UIShell.PSShared;

using static Cadroue.UIShell.PSShared.PSField;

using Cadroue.Infrastructure;

namespace Cadroue.UIShell;

internal enum PSLoupeFloat
{
    PSLoupeFloatOff,
    PSLoupeFloatOwner,
    PSLoupeFloatTop
}

internal sealed class PSLoupe : Window
{
    internal const string PSLoupePlacementKey = "Loupe";

    private const double PSLoupeWidthDefault = 720;
    private const double PSLoupeHeightDefault = 480;
    private const double PSLoupeWidthMinimum = 360;
    private const double PSLoupeHeightMinimum = 240;
    private const double PSLoupeBandHeight = PSCasement.PSCasementBandHeight * 3 / 4;
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
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        Background = PSCasement.PSCasementBandFill;
        FontSize = PSFieldFontSize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;
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

    private UIElement PSLoupeBuild()
    {
        var pRoot = new Grid { Background = PSCasement.PSCasementBandFill };
        pRoot.Children.Add(PSLoupeBodyBuild());
        pRoot.Children.Add(PSCasement.PSCasementOverlayBuild(this, 0, Title, pCloseOnly: false, PSLoupeBandHeight));
        return pRoot;
    }

    private UIElement PSLoupeBodyBuild()
    {
        var pBody = new DockPanel
        {
            Background = Brushes.Black,
            Margin = new Thickness(0, PSLoupeBandHeight, 0, 0)
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

    private UIElement PSLoupePlayBuild()
    {
        psLoupePlayImage = new Image
        {
            Source = PIcon.PIconRead(PSLoupeStartIcon, PSLoupePlayBrush),
            Width = PSLoupeIconSize,
            Height = PSLoupeIconSize,
            Stretch = Stretch.Uniform,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        psLoupePlayButton = new Button
        {
            Width = PSLoupeButtonSize,
            Height = PSLoupeButtonSize,
            Content = psLoupePlayImage,
            Style = PButton.PButtonPanelCreate(),
            ToolTip = LLocalization.LLocalizationTextRead("Loupe.Play.Tooltip")
        };
        psLoupePlayButton.Click += PSLoupePlayHandle;
        return psLoupePlayButton;
    }

    private async void PSLoupePlaybackStart()
    {
        string? pSource = psLoupeSource.PViewerSourcePath;
        if (psLoupeMediaHost is null || string.IsNullOrWhiteSpace(pSource))
        {
            return;
        }

        bool pLoupeMpv = LRenderer.LRendererEngineRead() == LPreviewEngine.LPreviewEngineMpv
            && LMpv.LMpvInstalledCheck();

        try
        {
            if (pLoupeMpv)
            {
                PSLoupeMpvBuild();
            }
            else
            {
                PSLoupeFlyleafBuild();
            }

            await System.Threading.Tasks.Task.Run(() => psLoupePlayer.PPlayerOpen(pSource));
            if (psLoupeClosed)
            {
                return;
            }

            PSLoupePreviewApply();
            psLoupePlayer.PPlayerVolumeSet(psLoupeSource.PViewerVolumeCurrent);

            TimeSpan pInherit = psLoupeSource.PViewerPositionRead();
            if (pInherit > TimeSpan.Zero)
            {
                psLoupePlayer.PPlayerSeek(pInherit);
            }

            if (psLoupeSource.PViewerPlayingRead())
            {
                psLoupePlayer.PPlayerPlay();
                PSLoupePlayingSet(true);
            }
            else
            {
                psLoupePlayer.PPlayerPause();
                PSLoupePlayingSet(false);
            }
        }
        catch
        {
            PSLoupePlayingSet(false);
        }
    }

    private void PSLoupePreviewHandle()
    {
        if (psLoupeClosed || !psLoupePlayer.PPlayerReady)
        {
            return;
        }

        PSLoupePreviewApply();
    }

    private void PSLoupePreviewApply()
    {
        LPreviewState pState = psLoupeSource.PViewerRenderRead();
        try
        {
            if (psLoupeMpvHost is not null)
            {
                psLoupePlayer.PPlayerFilterSet(LPreview.LPreviewFilterResolve(pState));
                psLoupePlayer.PPlayerAudioSet(psLoupeSource.PViewerAudioRead());
            }
            else
            {
                LPreview.LPreviewApply(psLoupePlayer.PPlayerFlyleafPlayer, pState);
            }
        }
        catch
        {
        }
    }

    private void PSLoupeFlyleafBuild()
    {
        var pLoupeFlyleafPlayer = new Player(new Config());
        pLoupeFlyleafPlayer.Config.Player.KeyBindings.Keys.Clear();
        psLoupeFlyleafHost = new FlyleafHost
        {
            Player = pLoupeFlyleafPlayer,
            VideoBackground = Brushes.Black,
            ToggleFullScreenOnDoubleClick = AvailableWindows.None,
            AttachedDragMove = AttachedDragMoveOptions.None
        };
        psLoupeMediaHost!.Child = psLoupeFlyleafHost;
        psLoupePlayer.PPlayerFlyleafSet(pLoupeFlyleafPlayer);
    }

    private void PSLoupeMpvBuild()
    {
        psLoupeMpvHost = new PViewerMpvHost();
        psLoupeMediaHost!.Child = psLoupeMpvHost;
        if (psLoupeMpvHost.PViewerMpvHwnd == nint.Zero)
        {
            psLoupeMpvHost.UpdateLayout();
        }

        nint pLoupeHandle = psLoupeMpvHost.PViewerMpvHwnd;
        if (pLoupeHandle == nint.Zero)
        {
            throw new InvalidOperationException("mpv host handle not realized");
        }

        psLoupePlayer.PPlayerMpvSet(pLoupeHandle);
    }

    private void PSLoupePlayHandle(object pSender, RoutedEventArgs pEvent)
    {
        if (psLoupePlaying)
        {
            psLoupeSource.PViewerPause();
        }
        else
        {
            psLoupeSource.PViewerPlay();
        }
    }

    internal void PSLoupePlay()
    {
        if (psLoupeClosed || !psLoupePlayer.PPlayerReady)
        {
            return;
        }

        if (psLoupeEnded)
        {
            psLoupePlayer.PPlayerSeek(TimeSpan.Zero);
            psLoupeEnded = false;
        }

        psLoupePlayer.PPlayerPlay();
        PSLoupePlayingSet(true);
    }

    internal void PSLoupePause()
    {
        if (psLoupeClosed || !psLoupePlayer.PPlayerReady)
        {
            return;
        }

        psLoupePlayer.PPlayerPause();
        PSLoupePlayingSet(false);
    }

    internal void PSLoupeSeek(TimeSpan pPosition)
    {
        if (psLoupeClosed || !psLoupePlayer.PPlayerReady)
        {
            return;
        }

        psLoupeEnded = false;
        psLoupePlayer.PPlayerSeek(pPosition);
    }

    internal void PSLoupeVolumeSet(double pVolume)
    {
        if (psLoupeClosed || !psLoupePlayer.PPlayerReady)
        {
            return;
        }

        psLoupePlayer.PPlayerVolumeSet(pVolume);
    }

    private void PSLoupeClockHandle(object? pSender, EventArgs pEvent)
    {
        if (psLoupeClosed || !psLoupePlaying)
        {
            return;
        }

        if (psLoupePlayer.PPlayerEndedRead())
        {
            psLoupeEnded = true;
            PSLoupePlayingSet(false);
            return;
        }

        if (psLoupePlayer.PPlayerReady)
        {
            psLoupeSource.PViewerLoupeSync(psLoupePlayer.PPlayerTimeRead(), true);
        }
    }

    private void PSLoupePlayingSet(bool pPlaying)
    {
        psLoupePlaying = pPlaying;
        if (pPlaying)
        {
            psLoupeEnded = false;
            psLoupeClock.Start();
        }
        else
        {
            psLoupeClock.Stop();
        }

        if (!psLoupeClosed)
        {
            TimeSpan pPosition = psLoupePlayer.PPlayerReady ? psLoupePlayer.PPlayerTimeRead() : TimeSpan.Zero;
            psLoupeSource.PViewerLoupeSync(pPosition, pPlaying);
        }

        if (psLoupePlayImage is not null)
        {
            psLoupePlayImage.Source = pPlaying
                ? PIcon.PIconRead(PSLoupePauseIcon, null)
                : PIcon.PIconRead(PSLoupeStartIcon, PSLoupePlayBrush);
        }

        if (psLoupePlayButton is not null)
        {
            psLoupePlayButton.ToolTip = LLocalization.LLocalizationTextRead(
                pPlaying ? "Loupe.Pause.Tooltip" : "Loupe.Play.Tooltip");
        }
    }

    private UIElement PSLoupeFloatBuild()
    {
        var pStrip = new StackPanel { Orientation = Orientation.Horizontal };
        psLoupeFloatButtons = new[]
        {
            PSLoupeSegmentBuild(PSLoupeFloat.PSLoupeFloatOff, "Loupe.Float.Off"),
            PSLoupeSegmentBuild(PSLoupeFloat.PSLoupeFloatOwner, "Loupe.Float.Owner"),
            PSLoupeSegmentBuild(PSLoupeFloat.PSLoupeFloatTop, "Loupe.Float.Top")
        };

        foreach (Button pSegment in psLoupeFloatButtons)
        {
            pStrip.Children.Add(pSegment);
        }

        PSLoupeFloatShow();
        return new Border
        {
            BorderBrush = PSLoupeFloatBorder,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(6),
            ClipToBounds = true,
            Child = pStrip
        };
    }

    private Button PSLoupeSegmentBuild(PSLoupeFloat pMode, string pLabelKey)
    {
        var pButton = new Button
        {
            Content = LLocalization.LLocalizationTextRead(pLabelKey),
            Height = 24,
            MinWidth = 52,
            Padding = new Thickness(10, 0, 10, 0),
            FontSize = 11,
            Tag = pMode,
            Style = PButton.PButtonPanelCreate()
        };
        pButton.Click += (_, _) => PSLoupeFloatSelect(pMode);
        return pButton;
    }

    private void PSLoupeFloatSelect(PSLoupeFloat pMode)
    {
        psLoupeFloat = pMode;
        PSLoupeFloatShow();
        PSLoupeFloatApply(pMode);
        LPreferenceState pPreference = LPreference.LPreferenceStateCurrent.LPreferenceClone();
        pPreference.LPreferenceLoupeFloat = PSLoupeTokenRead(pMode);
        LPreference.LPreferenceStateSet(pPreference);
    }

    private void PSLoupeFloatShow()
    {
        if (psLoupeFloatButtons is null)
        {
            return;
        }

        foreach (Button pSegment in psLoupeFloatButtons)
        {
            bool pActive = pSegment.Tag is PSLoupeFloat pMode && pMode == psLoupeFloat;
            pSegment.Background = pActive ? PSLoupeFloatFill : Brushes.Transparent;
        }
    }

    private void PSLoupeFloatApply(PSLoupeFloat pMode)
    {
        switch (pMode)
        {
            case PSLoupeFloat.PSLoupeFloatOff:
                Owner = null;
                Topmost = false;
                break;
            case PSLoupeFloat.PSLoupeFloatOwner:
                Owner = psLoupeOwner;
                Topmost = false;
                break;
            case PSLoupeFloat.PSLoupeFloatTop:
                Owner = psLoupeOwner;
                Topmost = true;
                break;
        }
    }

    private static PSLoupeFloat PSLoupeFloatRestore()
    {
        return LPreference.LPreferenceStateCurrent.LPreferenceLoupeFloat switch
        {
            "Off" => PSLoupeFloat.PSLoupeFloatOff,
            "Top" => PSLoupeFloat.PSLoupeFloatTop,
            _ => PSLoupeFloat.PSLoupeFloatOwner
        };
    }

    private static string PSLoupeTokenRead(PSLoupeFloat pMode)
    {
        return pMode switch
        {
            PSLoupeFloat.PSLoupeFloatOff => "Off",
            PSLoupeFloat.PSLoupeFloatTop => "Top",
            _ => "Owner"
        };
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

    private void PSLoupePlaybackDispose()
    {
        psLoupePlayer.PPlayerMpvCancel();
        psLoupePlayer.PPlayerDispose();
        PSLoupeFlyleafDispose();
        if (psLoupeMpvHost is not null)
        {
            psLoupeMpvHost.Dispose();
            psLoupeMpvHost = null;
        }

        if (psLoupeMediaHost is not null)
        {
            psLoupeMediaHost.Child = null;
        }
    }

    private void PSLoupeFlyleafDispose()
    {
        if (psLoupeFlyleafHost is null)
        {
            return;
        }

        try
        {
            psLoupeFlyleafHost.Player = null;
            ((IDisposable)psLoupeFlyleafHost).Dispose();
        }
        catch
        {
        }

        psLoupeFlyleafHost = null;
    }

    protected override void OnSourceInitialized(EventArgs pEvent)
    {
        base.OnSourceInitialized(pEvent);
        PSCasement.PSCasementDwmApply(this);
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using FlyleafLib;
using FlyleafLib.Controls.WPF;
using Cadroue.Application;
using Cadroue.UIVeneer.PAsset;
using Cadroue.UIVeneer.PHouse;
using Cadroue.UIVeneer.PWing;
using Cadroue.UIDeportment;

using static Cadroue.UIVeneer.PSCasement;

namespace Cadroue.UIVeneer;

internal sealed class PSLoupe : Window
{
    internal const string PSLoupePlacementKey = "Loupe";

    private const double PSLoupeWidthDefault = 720;
    private const double PSLoupeHeightDefault = 480;
    private const double PSLoupeWidthMinimum = 360;
    private const double PSLoupeHeightMinimum = 240;
    private const double PSLoupeBarHeight = 48;
    private const double PSLoupeIconSize = 22;
    private const double PSLoupeButtonSize = 36;

    private static readonly SolidColorBrush PSLoupePlayBrush = new(Color.FromRgb(0x2F, 0x9E, 0x64));
    private static readonly SolidColorBrush PSLoupeFloatFill = new(Color.FromRgb(0xD3, 0xE1, 0xF2));
    private static readonly SolidColorBrush PSLoupeFloatBorder = new(Color.FromRgb(0xD9, 0xDE, 0xE7));

    private static readonly IReadOnlyDictionary<bool, Brush?> psLoupePlayTints = new Dictionary<bool, Brush?>
    {
        [true] = PSLoupePlayBrush,
        [false] = null,
    };

    private static readonly IReadOnlyDictionary<bool, Brush> psLoupeSegmentFills = new Dictionary<bool, Brush>
    {
        [true] = PSLoupeFloatFill,
        [false] = Brushes.Transparent,
    };

    private static readonly IReadOnlyDictionary<bool, Action<PSLoupe>> psLoupeEngineCreates =
        new Dictionary<bool, Action<PSLoupe>>
        {
            [true] = psLoupe => psLoupe.PSLoupeMpvCreate(),
            [false] = psLoupe => psLoupe.PSLoupeFlyleafCreate(),
        };

    private static readonly IReadOnlyDictionary<bool, Action<DispatcherTimer>> psLoupeClockRuns =
        new Dictionary<bool, Action<DispatcherTimer>>
        {
            [true] = pClock => pClock.Start(),
            [false] = pClock => pClock.Stop(),
        };

    private readonly PSGrabber psLoupeGrabber;
    private readonly IReadOnlyDictionary<bool, Window?> psLoupeOwners;
    private readonly DispatcherTimer psLoupeClock = new() { Interval = TimeSpan.FromMilliseconds(200) };
    private readonly FlyleafHost psLoupeFlyleafHost;
    private readonly PViewerMpvHost psLoupeMpvHost;
    private readonly Image psLoupePlayImage;
    private readonly Button psLoupePlayButton;
    private readonly IReadOnlyDictionary<LSLoupeFloat, Button> psLoupeSegments;

    public LSLoupe LSLoupe { get; } = new();

    internal static void PSLoupeShow(Window pOwner, LViewer lViewer)
    {
        var psLoupe = new PSLoupe(pOwner);
        psLoupe.LSLoupe.LSLoupeViewerAttach(lViewer);
        psLoupe.Show();
    }

    private PSLoupe(Window pOwner)
    {
        psLoupeOwners = new Dictionary<bool, Window?>
        {
            [true] = pOwner,
            [false] = null,
        };
        LSLoupe.LSLoupeFloatRestore();
        Title = LLocalization.LLocalizationTextRead("Loupe.Window.Title");
        Owner = pOwner;
        Width = PSLoupeWidthDefault;
        Height = PSLoupeHeightDefault;
        MinWidth = PSLoupeWidthMinimum;
        MinHeight = PSLoupeHeightMinimum;
        ResizeMode = ResizeMode.NoResize;
        PSDialog.PSDialogApply(this, PSCasementBandFill);
        PScrollbar.PScrollbarApply(this);

        psLoupeFlyleafHost = new FlyleafHost
        {
            VideoBackground = Brushes.Black,
            ToggleFullScreenOnDoubleClick = AvailableWindows.None,
            AttachedDragMove = AttachedDragMoveOptions.None,
            Visibility = Visibility.Collapsed
        };
        psLoupeMpvHost = new PViewerMpvHost { Visibility = Visibility.Collapsed };
        psLoupePlayImage = new Image
        {
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
            Style = PButton.PButtonPanelCreate()
        };
        psLoupePlayButton.Click += PSLoupePlayHandle;
        psLoupeSegments = LSLoupe.LSLoupeButtonsRead().ToDictionary(
            lButton => lButton.LSLoupeButtonKind, PSLoupeSegmentBuild);
        Content = PSDialog.PSDialogBuild(this, Title, PSLoupeBodyBuild());
        PSLoupePlayApply(LSLoupe.LSLoupePlaying);
        PSLoupeFloatApply();

        psLoupeClock.Tick += PSLoupeClockHandle;
        LSLoupe.LSLoupePlayingChange += PSLoupePlayApply;
        LSLoupe.LSLoupeFloatChange += PSLoupeFloatApply;
        LSLoupe.LSLoupeEngineCreate += PSLoupeEngineCreate;
        LSLoupe.LSLoupeCloseApply += Close;
        Loaded += PSLoupeLoadedHandle;
        psLoupeGrabber = new PSGrabber(this);
        psLoupeGrabber.PSGrabberAttach();
        Closed += PSLoupeCloseHandle;
    }

    private DockPanel PSLoupeBodyBuild()
    {
        var pBody = new DockPanel { Background = Brushes.Black };
        UIElement pBar = PSLoupeBarBuild();
        DockPanel.SetDock(pBar, Dock.Bottom);
        pBody.Children.Add(pBar);

        var pMediaHost = new Grid { Background = Brushes.Black, ClipToBounds = true };
        pMediaHost.Children.Add(psLoupeFlyleafHost);
        pMediaHost.Children.Add(psLoupeMpvHost);
        pBody.Children.Add(pMediaHost);
        return pBody;
    }

    private UIElement PSLoupeBarBuild()
    {
        var pBar = new Grid { Height = PSLoupeBarHeight, Background = PSCasementBandFill };
        pBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pBar.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

        var pPlaySlot = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Child = psLoupePlayButton
        };
        Grid.SetColumn(pPlaySlot, 1);
        pBar.Children.Add(pPlaySlot);

        var pStrip = new StackPanel { Orientation = Orientation.Horizontal };
        psLoupeSegments.Values.ToList().ForEach(pSegment => pStrip.Children.Add(pSegment));
        var pFloatSlot = new Border
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 12, 0),
            Child = new Border
            {
                BorderBrush = PSLoupeFloatBorder,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                ClipToBounds = true,
                Child = pStrip
            }
        };
        Grid.SetColumn(pFloatSlot, 2);
        pBar.Children.Add(pFloatSlot);
        return pBar;
    }

    private Button PSLoupeSegmentBuild(LSLoupeButton lButton)
    {
        var pButton = new Button
        {
            Content = lButton.LSLoupeButtonText,
            Height = 24,
            MinWidth = 52,
            Padding = new Thickness(10, 0, 10, 0),
            FontSize = 11,
            Style = PButton.PButtonPanelCreate()
        };
        pButton.Click += (_, _) => LSLoupe.LSLoupeFloatSet(lButton.LSLoupeButtonKind);
        return pButton;
    }

    private void PSLoupeSegmentApply(LSLoupeButton lButton) =>
        psLoupeSegments[lButton.LSLoupeButtonKind].Background = psLoupeSegmentFills[lButton.LSLoupeButtonSelected];

    private void PSLoupeFloatApply()
    {
        LSLoupe.LSLoupeButtonsRead().ToList().ForEach(PSLoupeSegmentApply);
        Owner = psLoupeOwners[LSLoupe.LSLoupeOwned];
        Topmost = LSLoupe.LSLoupeTopmost;
    }

    private void PSLoupePlayApply(bool lPlaying)
    {
        psLoupeClockRuns[lPlaying](psLoupeClock);
        psLoupePlayImage.Source = PIcon.PIconRead(LSLoupe.LSLoupePlayIcon, psLoupePlayTints[LSLoupe.LSLoupePlayTinted]);
        psLoupePlayButton.ToolTip = LSLoupe.LSLoupePlayTip;
    }

    private void PSLoupeEngineCreate()
    {
        psLoupeMpvHost.Visibility = PLook.PLookVisible[LSLoupe.LSLoupeMpvActive];
        psLoupeFlyleafHost.Visibility = PLook.PLookVisible[LSLoupe.LSLoupeFlyleafActive];
        psLoupeEngineCreates[LSLoupe.LSLoupeMpvActive](this);
    }

    private void PSLoupeFlyleafCreate()
    {
        var pConfig = new Config();
        pConfig.Player.KeyBindings.Keys.Clear();
        LSLoupe.LPlayer.LPlayerEngineSet(
            new PPlayerFlyleaf(psLoupeFlyleafHost, LSLoupe.LPlayer, pConfig).PPlayerSeamRead());
    }

    private void PSLoupeMpvCreate() =>
        LSLoupe.LPlayer.LPlayerEngineSet(new LPlayerMpv(psLoupeMpvHost.PViewerHandleRead()).LPlayerSeamRead());

    private void PSLoupeLoadedHandle(object pSender, RoutedEventArgs pEvent)
    {
        Loaded -= PSLoupeLoadedHandle;
        PSGrabber.PSGrabberPlacementRestore(this, PSLoupePlacementKey);
        PSLoupeFloatApply();
        _ = LSLoupe.LSLoupeStart();
    }

    private void PSLoupePlayHandle(object pSender, RoutedEventArgs pEvent) => LSLoupe.LSLoupePlayToggle();

    private void PSLoupeClockHandle(object? pSender, EventArgs pEvent) => LSLoupe.LSLoupeTick();

    private void PSLoupeCloseHandle(object? pSender, EventArgs pEvent)
    {
        LSLoupe.LSLoupeClose();
        Loaded -= PSLoupeLoadedHandle;
        psLoupeClock.Stop();
        psLoupeClock.Tick -= PSLoupeClockHandle;
        LSLoupe.LSLoupePlayingChange -= PSLoupePlayApply;
        LSLoupe.LSLoupeFloatChange -= PSLoupeFloatApply;
        LSLoupe.LSLoupeEngineCreate -= PSLoupeEngineCreate;
        LSLoupe.LSLoupeCloseApply -= Close;
        PSGrabber.PSGrabberPlacementSave(this, PSLoupePlacementKey);
        psLoupeGrabber.PSGrabberDetach();
        PSLoupeStageRun("flyleaf host", PSLoupeFlyleafDispose);
        PSLoupeStageRun("mpv host", psLoupeMpvHost.Dispose);
        LSLoupe.LSLoupeDetach();
        Closed -= PSLoupeCloseHandle;
    }

    private void PSLoupeFlyleafDispose()
    {
        psLoupeFlyleafHost.Player = null;
        ((IDisposable)psLoupeFlyleafHost).Dispose();
    }

    private static void PSLoupeStageRun(string pStage, Action pAction)
    {
        try
        {
            pAction();
        }
        catch (Exception pException)
        {
            Cadroue.Infrastructure.LTraceLog.LTraceErrorRecord($"Loupe close stage '{pStage}' failed", pException);
        }
    }
}

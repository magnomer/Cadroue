using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using FlyleafLib;
using FlyleafLib.Controls.WPF;
using Cadroue.UIVeneer.PHouse;
using Cadroue.UIVeneer.PWing;
using Cadroue.UIDeportment;

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
    private readonly IReadOnlyDictionary<QSLoupeFloat, Button> psLoupeSegments;

    public QSLoupe QSLoupe { get; }

    internal static void PSLoupeShow(Window pOwner, QSLoupe lLoupe)
    {
        var psLoupe = new PSLoupe(pOwner, lLoupe);
        lLoupe.QSLoupeViewerAttach();
        psLoupe.Show();
    }

    private PSLoupe(Window pOwner, QSLoupe lLoupe)
    {
        QSLoupe = lLoupe;
        psLoupeOwners = PSLoupeOwnerCreate(pOwner);
        QSLoupe.QSLoupeFloatRestore();
        PSLoupeWindowApply(pOwner);
        QScrollbar.QScrollbarAttach(this);
        psLoupeFlyleafHost = PSLoupeFlyleafBuild();
        psLoupeMpvHost = PSLoupeViewerBuild();
        psLoupePlayImage = PSLoupeImageBuild();
        psLoupePlayButton = PSLoupePlayBuild(psLoupePlayImage);
        PSLoupePlayAttach();
        QButton.QButtonAttach(psLoupePlayButton, QButtonLook.QButtonLookPanel);
        psLoupeSegments = QSLoupe.QSLoupeButtonsRead().ToDictionary(
            lButton => lButton.QSLoupeButtonKind, PSLoupeSegmentBuild);
        Content = PSDialog.PSDialogBuild(this, Title, PSLoupeBodyBuild());
        PSLoupePlayApply(QSLoupe.QSLoupePlaying);
        PSLoupeFloatApply();
        PSLoupeHandlerAttach();
        PSLoupeLoadedAttach();
        psLoupeGrabber = new PSGrabber(this);
        psLoupeGrabber.PSGrabberAttach();
        PSLoupeWindowAttach();
    }

    private static IReadOnlyDictionary<bool, Window?> PSLoupeOwnerCreate(Window pOwner) =>
        new Dictionary<bool, Window?>
        {
            [true] = pOwner,
            [false] = null,
        };

    private void PSLoupeWindowApply(Window pOwner)
    {
        Title = QLocalizationCatalog.QLocalizationTextRead("Loupe.Window.Title");
        Owner = pOwner;
        Width = PSLoupeWidthDefault;
        Height = PSLoupeHeightDefault;
        MinWidth = PSLoupeWidthMinimum;
        MinHeight = PSLoupeHeightMinimum;
        ResizeMode = ResizeMode.NoResize;
        PSDialog.PSDialogApply(this, PSCasement.PSCasementBandFill);
    }

    private static FlyleafHost PSLoupeFlyleafBuild() => new()
    {
        VideoBackground = Brushes.Black,
        ToggleFullScreenOnDoubleClick = AvailableWindows.None,
        AttachedDragMove = AttachedDragMoveOptions.None,
        Visibility = Visibility.Collapsed
    };

    private static PViewerMpvHost PSLoupeViewerBuild() => new() { Visibility = Visibility.Collapsed };

    private static Image PSLoupeImageBuild() => new()
    {
        Width = PSLoupeIconSize,
        Height = PSLoupeIconSize,
        Stretch = Stretch.Uniform,
        HorizontalAlignment = HorizontalAlignment.Center,
        VerticalAlignment = VerticalAlignment.Center
    };

    private static Button PSLoupePlayBuild(Image pPlayImage) => new()
    {
        Width = PSLoupeButtonSize,
        Height = PSLoupeButtonSize,
        Content = pPlayImage
    };

    private void PSLoupePlayAttach() => psLoupePlayButton.Click += PSLoupePlayHandle;

    private void PSLoupeHandlerAttach()
    {
        psLoupeClock.Tick += PSLoupeClockHandle;
        QSLoupe.QSLoupePlayingChanged += PSLoupePlayApply;
        QSLoupe.QSLoupeFloatChanged += PSLoupeFloatApply;
        QSLoupe.QSLoupeEngineCreated += PSLoupeEngineCreate;
        QSLoupe.QSLoupeCloseApplied += Close;
    }

    private void PSLoupeHandlerDetach()
    {
        psLoupeClock.Tick -= PSLoupeClockHandle;
        QSLoupe.QSLoupePlayingChanged -= PSLoupePlayApply;
        QSLoupe.QSLoupeFloatChanged -= PSLoupeFloatApply;
        QSLoupe.QSLoupeEngineCreated -= PSLoupeEngineCreate;
        QSLoupe.QSLoupeCloseApplied -= Close;
    }

    private void PSLoupeLoadedAttach() => Loaded += PSLoupeLoadedHandle;

    private void PSLoupeLoadedDetach() => Loaded -= PSLoupeLoadedHandle;

    private void PSLoupeWindowAttach() => Closed += PSLoupeCloseHandle;

    private void PSLoupeWindowDetach() => Closed -= PSLoupeCloseHandle;

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
        var pBar = new Grid { Height = PSLoupeBarHeight, Background = PSCasement.PSCasementBandFill };
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

    private Button PSLoupeSegmentBuild(QSLoupeButton lButton)
    {
        Button pButton = PSLoupeSegmentCreate(lButton);
        PSLoupeSegmentAttach(pButton, lButton);
        QButton.QButtonAttach(pButton, QButtonLook.QButtonLookPanel);
        return pButton;
    }

    private static Button PSLoupeSegmentCreate(QSLoupeButton lButton) => new()
    {
        Content = lButton.QSLoupeButtonText,
        Height = 24,
        MinWidth = 52,
        Padding = new Thickness(10, 0, 10, 0),
        FontSize = 11
    };

    private void PSLoupeSegmentAttach(Button pButton, QSLoupeButton lButton) =>
        pButton.Click += (_, _) => QSLoupe.QSLoupeFloatSet(lButton.QSLoupeButtonKind);

    private void PSLoupeSegmentApply(QSLoupeButton lButton) =>
        psLoupeSegments[lButton.QSLoupeButtonKind].Background = psLoupeSegmentFills[lButton.QSLoupeButtonSelected];

    private void PSLoupeFloatApply()
    {
        QSLoupe.QSLoupeButtonsRead().ToList().ForEach(PSLoupeSegmentApply);
        PSLoupeOwnerApply();
    }

    private void PSLoupeOwnerApply()
    {
        Owner = psLoupeOwners[QSLoupe.QSLoupeOwned];
        Topmost = QSLoupe.QSLoupeTopmost;
    }

    private void PSLoupePlayApply(bool lPlaying)
    {
        psLoupeClockRuns[lPlaying](psLoupeClock);
        PSLoupeIconApply();
        PSLoupeTipShow();
    }

    private void PSLoupeIconApply() =>
        psLoupePlayImage.Source = QIcon.QIconRead(QSLoupe.QSLoupePlayIcon, psLoupePlayTints[QSLoupe.QSLoupePlayTinted]);

    private void PSLoupeTipShow() => psLoupePlayButton.ToolTip = QSLoupe.QSLoupePlayTip;

    private void PSLoupeEngineCreate()
    {
        PSLoupeHostApply();
        psLoupeEngineCreates[QSLoupe.QSLoupeMpvActive](this);
    }

    private void PSLoupeHostApply()
    {
        psLoupeMpvHost.Visibility = PLook.PLookVisible[QSLoupe.QSLoupeMpvActive];
        psLoupeFlyleafHost.Visibility = PLook.PLookVisible[QSLoupe.QSLoupeFlyleafActive];
    }

    private void PSLoupeFlyleafCreate()
    {
        var pConfig = new Config();
        pConfig.Player.KeyBindings.Keys.Clear();
        QSLoupe.QPlayer.QPlayerEngineSet(
            new PPlayerFlyleaf(psLoupeFlyleafHost, QSLoupe.QPlayer, pConfig).PPlayerSeamRead());
    }

    private void PSLoupeMpvCreate() =>
        QSLoupe.QPlayer.QPlayerMpvCreate(psLoupeMpvHost.PViewerHandleRead());

    private void PSLoupeLoadedHandle(object pSender, RoutedEventArgs pEvent)
    {
        PSLoupeLoadedDetach();
        PSGrabber.PSGrabberPlacementRestore(this, PSLoupePlacementKey);
        PSLoupeFloatApply();
        _ = QSLoupe.QSLoupeStart();
    }

    private void PSLoupePlayHandle(object pSender, RoutedEventArgs pEvent) => QSLoupe.QSLoupePlayToggle();

    private void PSLoupeClockHandle(object? pSender, EventArgs pEvent) => QSLoupe.QSLoupeTick();

    private void PSLoupeCloseHandle(object? pSender, EventArgs pEvent)
    {
        QSLoupe.QSLoupeClose();
        PSLoupeLoadedDetach();
        psLoupeClock.Stop();
        PSLoupeHandlerDetach();
        PSGrabber.PSGrabberPlacementSave(this, PSLoupePlacementKey);
        psLoupeGrabber.PSGrabberDetach();
        PSLoupeStageRun("flyleaf host", PSLoupeFlyleafDispose);
        PSLoupeStageRun("mpv host", psLoupeMpvHost.Dispose);
        QSLoupe.QSLoupeDetach();
        PSLoupeWindowDetach();
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

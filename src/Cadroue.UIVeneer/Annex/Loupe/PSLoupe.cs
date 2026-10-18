using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
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

    private static readonly IReadOnlyDictionary<bool, Action<DispatcherTimer>> psLoupeClockRuns =
        new Dictionary<bool, Action<DispatcherTimer>>
        {
            [true] = pClock => pClock.Start(),
            [false] = pClock => pClock.Stop(),
        };

    private readonly PSGrabber psLoupeGrabber;
    private readonly IReadOnlyDictionary<bool, Window?> psLoupeOwners;
    private readonly DispatcherTimer psLoupeClock = new() { Interval = TimeSpan.FromMilliseconds(200) };
    private readonly PSLoupeEngine psLoupeEngine;
    private readonly PSLoupePlayback psLoupePlayback;
    private readonly PSLoupeFloat psLoupeFloat;

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
        QSLoupe.QSLoupeFloatChoice.QSLoupeFloatRestore();
        PSLoupeWindowApply(pOwner);
        QScrollbar.QScrollbarAttach(this);
        psLoupeEngine = new PSLoupeEngine(QSLoupe);
        psLoupePlayback = new PSLoupePlayback(QSLoupe);
        psLoupeFloat = new PSLoupeFloat(QSLoupe.QSLoupeFloatChoice);
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

    private void PSLoupeHandlerAttach()
    {
        psLoupeClock.Tick += PSLoupeClockHandle;
        QSLoupe.QSLoupePlayingChanged += PSLoupePlayApply;
        QSLoupe.QSLoupeFloatChoice.QSLoupeFloatChanged += PSLoupeFloatApply;
        QSLoupe.QSLoupeEngineCreated += psLoupeEngine.PSLoupeEngineCreate;
        QSLoupe.QSLoupeCloseApplied += Close;
    }

    private void PSLoupeHandlerDetach()
    {
        psLoupeClock.Tick -= PSLoupeClockHandle;
        QSLoupe.QSLoupePlayingChanged -= PSLoupePlayApply;
        QSLoupe.QSLoupeFloatChoice.QSLoupeFloatChanged -= PSLoupeFloatApply;
        QSLoupe.QSLoupeEngineCreated -= psLoupeEngine.PSLoupeEngineCreate;
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
        pBody.Children.Add(psLoupeEngine.PSLoupeMediaBuild());
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
            Child = psLoupePlayback.PSLoupePlayButton
        };
        Grid.SetColumn(pPlaySlot, 1);
        pBar.Children.Add(pPlaySlot);

        UIElement pFloatSlot = psLoupeFloat.PSLoupeStripBuild();
        Grid.SetColumn(pFloatSlot, 2);
        pBar.Children.Add(pFloatSlot);
        return pBar;
    }

    private void PSLoupeFloatApply()
    {
        psLoupeFloat.PSLoupeSegmentsApply();
        PSLoupeOwnerApply();
    }

    private void PSLoupeOwnerApply()
    {
        Owner = psLoupeOwners[QSLoupe.QSLoupeFloatChoice.QSLoupeOwned];
        Topmost = QSLoupe.QSLoupeFloatChoice.QSLoupeTopmost;
    }

    private void PSLoupePlayApply(bool lPlaying)
    {
        psLoupeClockRuns[lPlaying](psLoupeClock);
        psLoupePlayback.PSLoupeIconApply();
        psLoupePlayback.PSLoupeTipShow();
    }

    private void PSLoupeLoadedHandle(object pSender, RoutedEventArgs pEvent)
    {
        PSLoupeLoadedDetach();
        PSGrabberPlacement.PSGrabberPlacementRestore(this, PSLoupePlacementKey);
        PSLoupeFloatApply();
        _ = QSLoupe.QSLoupeStart();
    }

    private void PSLoupeClockHandle(object? pSender, EventArgs pEvent) => QSLoupe.QSLoupeTick();

    private void PSLoupeCloseHandle(object? pSender, EventArgs pEvent)
    {
        QSLoupe.QSLoupeClose();
        PSLoupeLoadedDetach();
        psLoupeClock.Stop();
        PSLoupeHandlerDetach();
        PSGrabberPlacement.PSGrabberPlacementSave(this, PSLoupePlacementKey);
        psLoupeGrabber.PSGrabberDetach();
        PSLoupeStageRun("flyleaf host", psLoupeEngine.PSLoupeFlyleafDispose);
        PSLoupeStageRun("mpv host", psLoupeEngine.PSLoupeMpvDispose);
        QSLoupe.QSLoupeDetach();
        PSLoupeWindowDetach();
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

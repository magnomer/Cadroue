using Cadroue.Core;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using Cadroue.UIShell.PMainWindow;
using Cadroue.UIShell.PSShared;

using Cadroue.Infrastructure;

namespace Cadroue.UIShell;

public sealed partial class PLogWindow : Window
{
    private const string PLogPlacementKey = "Log";
    private const int PLogRowMaximum = 5000;
    private const int PLogFlushMilliseconds = 200;

    private static PLogWindow? pLogWindowCurrent;

    private readonly ObservableCollection<PLogRow> pLogRowsShown = new();
    private readonly List<PLogRow> pLogRowsAll = new();
    private readonly List<LTraceEntry> pLogPending = new();
    private readonly object pLogPendingLock = new();

    private readonly ListBox pLogFeed;
    private readonly ComboBox pLogFileCombo;
    private readonly ComboBox pLogCategoryCombo;
    private readonly CheckBox pLogVerboseBox;
    private readonly DispatcherTimer pLogFlushTimer;
    private readonly PSGrabber pLogGrabber;

    private Window? pLogOwnerWindow;
    private string pLogSourceText = string.Empty;
    private string pLogFilePath = string.Empty;
    private bool pLogFileLive = true;

    private PLogWindow()
    {
        pLogFileCombo = PLogComboBuild(320);
        pLogCategoryCombo = PLogComboBuild(200);
        pLogVerboseBox = PLogVerboseBuild();
        pLogFeed = PLogFeedBuild();

        Title = LLocalization.LLocalizationTextRead("Log.Window.Title");
        Width = 860;
        Height = 560;
        MinWidth = 640;
        MinHeight = 380;
        WindowStyle = WindowStyle.None;
        ResizeMode = ResizeMode.NoResize;
        Background = new SolidColorBrush(Color.FromRgb(0xDC, 0xE8, 0xF7));
        FontSize = PSField.PSFieldFontSize;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        UseLayoutRounding = true;
        SnapsToDevicePixels = true;
        PScrollbar.PScrollbarApply(this);
        Content = PLogContentBuild();

        PLogCategoryBuild();
        PLogFilesBuild();

        pLogFlushTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(PLogFlushMilliseconds)
        };
        pLogFlushTimer.Tick += PLogFlushHandle;
        pLogFlushTimer.Start();

        PSGrabber.PSGrabberPlacementRestore(this, PLogPlacementKey);
        pLogGrabber = new PSGrabber(this);
        pLogGrabber.PSGrabberAttach();
        LTrace.LTraceAppend += PLogAppendHandle;
        Closed += PLogCloseHandle;
    }

    public static void PLogWindowShow(Window? pOwner)
    {
        if (pLogWindowCurrent is not null)
        {
            pLogWindowCurrent.Activate();
            return;
        }

        pLogWindowCurrent = new PLogWindow();
        pLogWindowCurrent.PLogOwnerAttach(pOwner);
        pLogWindowCurrent.Show();
    }

    private void PLogOwnerAttach(Window? pLogOwner)
    {
        if (pLogOwner is null)
        {
            return;
        }

        pLogOwnerWindow = pLogOwner;
        pLogOwner.Closed += PLogOwnerHandle;
        if (WindowStartupLocation == WindowStartupLocation.Manual)
        {
            return;
        }

        WindowStartupLocation = WindowStartupLocation.Manual;
        Left = pLogOwner.Left + ((pLogOwner.ActualWidth - Width) / 2);
        Top = pLogOwner.Top + ((pLogOwner.ActualHeight - Height) / 2);
    }

    private void PLogOwnerHandle(object? sender, EventArgs e) => Close();

    private void PLogVerboseSet(bool pLogVerboseOn)
    {
        PLogCategoryApply();
        if (LTrace.LTraceVerbose == pLogVerboseOn)
        {
            return;
        }

        LTrace.LTraceVerbose = pLogVerboseOn;
        LPreferenceState pLogPreferenceNext = LPreference.LPreferenceStateCurrent.LPreferenceClone();
        pLogPreferenceNext.LPreferenceLogVerbose = pLogVerboseOn;
        LPreference.LPreferenceStateSet(pLogPreferenceNext);
    }

    private void PLogCloseHandle(object? sender, EventArgs e)
    {
        pLogFlushTimer.Stop();
        pLogFlushTimer.Tick -= PLogFlushHandle;
        PSGrabber.PSGrabberPlacementSave(this, PLogPlacementKey);
        pLogGrabber.PSGrabberDetach();
        LTrace.LTraceAppend -= PLogAppendHandle;
        Closed -= PLogCloseHandle;
        if (pLogOwnerWindow is not null)
        {
            pLogOwnerWindow.Closed -= PLogOwnerHandle;
            pLogOwnerWindow = null;
        }

        pLogWindowCurrent = null;
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        PSCasement.PSCasementDwmApply(this);
    }
}

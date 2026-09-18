using Cadroue.Core;
using Cadroue.UIVeneer.PSCasement;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Cadroue.UIVeneer.PHouse;
using Microsoft.Win32;

using Cadroue.Infrastructure;
using Cadroue.Application;

namespace Cadroue.UIVeneer.PDeck;

public sealed partial class PConsole
{
    private PWindow? pConsoleSceneWindow;
    private DispatcherTimer? pConsoleSceneTimer;

    private void PConsoleSceneAttach()
    {
        PDropdown.PDropdownActionApply(
            pConsoleRelayCombo,
            LLocalization.LLocalizationTextRead("Console.Scene.DeleteTooltip"));
        pConsoleRelayCombo.AddHandler(ButtonBase.ClickEvent, new RoutedEventHandler(PConsoleDeleteHandle));
        pConsoleRelayCombo.ToolTip = LLocalization.LLocalizationTextRead("Console.Scene.ComboTooltip");
        pConsoleSaveButton.ToolTip = LLocalization.LLocalizationTextRead("Console.Scene.SaveTooltip");
        pConsoleExportButton.ToolTip = LLocalization.LLocalizationTextRead("Console.Scene.ExportTooltip");
        pConsoleImportButton.ToolTip = LLocalization.LLocalizationTextRead("Console.Scene.ImportTooltip");
        pConsoleSaveButton.Click += PConsoleSaveHandle;
        pConsoleExportButton.Click += PConsoleExportHandle;
        pConsoleImportButton.Click += PConsoleImportHandle;
        pConsoleRelayCombo.SelectionChanged += PConsoleSelectHandle;
        pConsoleRelayCombo.DropDownOpened += PConsoleOpenHandle;
        pConsoleRelayCombo.DropDownClosed += PConsoleCloseHandle;
        pConsoleRelayCombo.MouseEnter += PConsoleDropHandle;
        pConsoleRelayCombo.ItemContainerGenerator.StatusChanged += PConsoleRowsHandle;
        Loaded += PConsoleLoadHandle;
        Unloaded += PConsoleSceneClose;
        PConsoleSceneRebuild();
    }

    private void PConsoleLoadHandle(object pSender, RoutedEventArgs pArguments)
    {
        if (LConsole.LConsoleCaretSet())
        {
            PConsoleCaretAttach();
        }

        LConsole.LConsoleSceneSet(LScene.LSceneActiveName);
        PConsoleSceneUpdate();

        if (pConsoleSceneWindow is null && PConsoleWindowRead() is { } pWindow)
        {
            pConsoleSceneWindow = pWindow;
            pWindow.PreviewMouseDown += PConsolePressHandle;
            pWindow.Deactivated += PConsoleDeactivateHandle;
        }

        pConsoleSceneTimer ??= PConsoleTimerCreate();
        pConsoleSceneTimer.Start();
    }

    private void PConsoleSceneClose(object pSender, RoutedEventArgs pArguments)
    {
        pConsoleSceneTimer?.Stop();
        if (pConsoleSceneWindow is { } pWindow)
        {
            pWindow.PreviewMouseDown -= PConsolePressHandle;
            pWindow.Deactivated -= PConsoleDeactivateHandle;
            pConsoleSceneWindow = null;
        }
    }

    private DispatcherTimer PConsoleTimerCreate()
    {
        var pTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        pTimer.Tick += PConsoleTickHandle;
        return pTimer;
    }

    private void PConsoleTickHandle(object? pSender, EventArgs pArguments)
    {
        if (LConsole.LConsoleSceneName.Length > 0 && !pConsoleRelayCombo.IsKeyboardFocusWithin)
        {
            PConsoleMarkUpdate();
        }
    }

    private void PConsoleDropHandle(object? pSender, EventArgs pArguments) => PConsoleMarkUpdate();

    private void PConsoleOpenHandle(object? pSender, EventArgs pArguments)
    {
        LConsole.LConsoleReloadSet(null);
        PConsoleCaretSet();
        PConsoleMarkUpdate();
    }

    private void PConsoleRowsHandle(object? pSender, EventArgs pArguments)
    {
        if (pConsoleRelayCombo.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
        {
            return;
        }

        foreach (object pItem in pConsoleRelayCombo.Items)
        {
            if (pConsoleRelayCombo.ItemContainerGenerator.ContainerFromItem(pItem) is ComboBoxItem pRow)
            {
                pRow.PreviewMouseLeftButtonUp -= PConsoleRowHandle;
                pRow.PreviewMouseLeftButtonUp += PConsoleRowHandle;
            }
        }
    }

    private void PConsoleRowHandle(object pSender, MouseButtonEventArgs pArguments)
    {
        if (pSender is ComboBoxItem { Content: string lSceneName })
        {
            LConsole.LConsoleReloadSet(lSceneName);
        }
    }

    private void PConsoleDeleteHandle(object pSender, RoutedEventArgs pArguments)
    {
        if (pArguments.OriginalSource is not Button { DataContext: string lSceneName })
        {
            return;
        }

        pArguments.Handled = true;
        LConsole.LConsoleReloadSet(null);
        if (!LScene.LSceneDelete(lSceneName))
        {
            return;
        }

        if (LConsole.LConsoleSceneCheck(lSceneName))
        {
            LConsole.LConsoleSceneSet(string.Empty);
            LScene.LSceneActiveSet(string.Empty);
        }

        PConsoleSceneRebuild();
        LTraceLog.LTraceInfoRecord($"Scene deleted '{lSceneName}'");
    }

    private void PConsoleCloseHandle(object? pSender, EventArgs pArguments)
    {
        string? lSceneName = LConsole.LConsoleReloadRead();
        if (lSceneName is null)
        {
            PConsoleSceneUpdate();
            return;
        }

        PConsoleSceneLoad(lSceneName);
    }

    private void PConsoleSceneLoad(string lSceneName)
    {
        if (LScene.LSceneRead(lSceneName) is not { } lScene)
        {
            PConsoleSceneUpdate();
            return;
        }

        if (!PConsoleSceneConfirm(lSceneName) || PConsoleWindowRead() is not { } pWindow)
        {
            PConsoleSceneUpdate();
            return;
        }

        if (pWindow.PWindowSceneApply(lScene))
        {
            PConsoleSceneSet(lSceneName);
            return;
        }

        PConsoleSceneUpdate();
    }

    private void PConsoleSceneRebuild()
    {
        LScene.LSceneCatalogueLoad();
        pConsoleRelayCombo.ItemsSource = LScene.LSceneNames;
        PConsoleSceneUpdate();
    }

    private void PConsoleSceneUpdate()
    {
        string lSceneName = LConsole.LConsoleSceneName;
        pConsoleRelayCombo.SelectedItem = LScene.LSceneRead(lSceneName) is not null ? lSceneName : null;
        pConsoleRelayCombo.Text = lSceneName;
        PConsoleMarkUpdate();
    }

    private void PConsoleSceneSet(string lSceneName)
    {
        LConsole.LConsoleSceneSet(lSceneName);
        LScene.LSceneActiveSet(lSceneName);
        PConsoleSceneUpdate();
    }

    private void PConsoleMarkUpdate()
    {
        FontStyle pStyle = PConsoleDirtyCheck() ? FontStyles.Italic : FontStyles.Normal;
        pConsoleRelayCombo.ApplyTemplate();
        if (pConsoleRelayCombo.Template?.FindName("PART_EditableTextBox", pConsoleRelayCombo) is TextBox pEditableBox)
        {
            pEditableBox.FontStyle = pStyle;
        }
    }

    private bool PConsoleDirtyCheck()
    {
        string lSceneName = LConsole.LConsoleSceneName;
        if (lSceneName.Length == 0
            || LScene.LSceneRead(lSceneName) is not { } lSceneStored
            || PConsoleWindowRead() is not { } pWindow)
        {
            return false;
        }

        return !LScene.LSceneMatch(lSceneStored, pWindow.PWindowSceneRead(lSceneName));
    }

    private void PConsoleSaveHandle(object pSender, RoutedEventArgs pArguments)
    {
        string lSceneName = (pConsoleRelayCombo.Text ?? string.Empty).Trim();
        if (lSceneName.Length == 0)
        {
            PSAnnouncement.PSAnnouncementShow(
                PConsoleWindowRead(),
                LLocalization.LLocalizationTextRead("Console.Scene.SaveTitle"),
                LLocalization.LLocalizationTextRead("Console.Scene.NameRequired"));
            return;
        }

        if (PConsoleWindowRead() is not { } pWindow)
        {
            return;
        }

        LScene.LSceneSave(pWindow.PWindowSceneRead(lSceneName));
        PConsoleSceneRebuild();
        PConsoleSceneSet(lSceneName);
        LTraceLog.LTraceInfoRecord($"Scene saved '{lSceneName}'");
    }

    private void PConsoleSelectHandle(object pSender, SelectionChangedEventArgs pArguments)
    {
        if (pConsoleRelayCombo.SelectedItem is not string lSceneName || LConsole.LConsoleSceneCheck(lSceneName))
        {
            return;
        }

        if (pConsoleRelayCombo.IsDropDownOpen)
        {
            LConsole.LConsoleReloadSet(lSceneName);
            return;
        }

        LConsole.LConsoleReloadSet(null);
        PConsoleSceneLoad(lSceneName);
    }

    private static bool PConsoleSceneConfirm(string lSceneName) =>
        PSAlert.PSAlertConfirm(
            null,
            LLocalization.LLocalizationTextRead("Console.Scene.LoadTitle"),
            LLocalization.LLocalizationFormat("Console.Scene.LoadConfirm", lSceneName),
            LLocalization.LLocalizationTextRead("Terms.Load"));

    private PWindow? PConsoleWindowRead() => Window.GetWindow(this) as PWindow;
}

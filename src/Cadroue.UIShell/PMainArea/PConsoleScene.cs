using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Cadroue.UIShell.PMainWindow;
using Microsoft.Win32;

using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PMainArea;

public sealed partial class PConsole
{
    private bool pConsoleSceneApplying;
    private bool pConsoleCaretReady;
    private string? pConsoleReloadName;
    private string pConsoleSceneName = string.Empty;
    private PWindow? pConsoleSceneWindow;
    private DispatcherTimer? pConsoleSceneTimer;

    private void PConsoleSceneAttach()
    {
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
        if (!pConsoleCaretReady)
        {
            pConsoleCaretReady = true;
            PConsoleCaretAttach();
        }

        pConsoleSceneName = PProgram.LPreferenceStateCurrent.LPreferenceSceneName;
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
        if (pConsoleSceneName.Length > 0 && !pConsoleRelayCombo.IsKeyboardFocusWithin)
        {
            PConsoleMarkUpdate();
        }
    }

    private void PConsoleDropHandle(object? pSender, EventArgs pArguments) => PConsoleMarkUpdate();

    private void PConsoleOpenHandle(object? pSender, EventArgs pArguments)
    {
        pConsoleReloadName = null;
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
            pConsoleReloadName = lSceneName;
        }
    }

    private void PConsoleCloseHandle(object? pSender, EventArgs pArguments)
    {
        string? lSceneName = pConsoleReloadName;
        pConsoleReloadName = null;
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

        pWindow.PWindowSceneApply(lScene);
        PConsoleSceneSet(lSceneName);
        LTraceLog.LTraceInfoRecord($"Scene loaded '{lSceneName}'");
    }

    private void PConsoleSceneRebuild()
    {
        pConsoleSceneApplying = true;
        pConsoleRelayCombo.ItemsSource = LScene.LSceneNames;
        pConsoleSceneApplying = false;
        PConsoleSceneUpdate();
    }

    private void PConsoleSceneUpdate()
    {
        pConsoleSceneApplying = true;
        pConsoleRelayCombo.SelectedItem = null;
        pConsoleRelayCombo.Text = pConsoleSceneName;
        pConsoleSceneApplying = false;
        PConsoleMarkUpdate();
    }

    private void PConsoleSceneSet(string lSceneName)
    {
        pConsoleSceneName = lSceneName;
        PProgram.LPreferenceSceneSet(lSceneName);
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
        if (pConsoleSceneName.Length == 0
            || LScene.LSceneRead(pConsoleSceneName) is not { } lSceneStored
            || PConsoleWindowRead() is not { } pWindow)
        {
            return false;
        }

        return !LScene.LSceneMatch(lSceneStored, pWindow.PWindowSceneRead(pConsoleSceneName));
    }

    private void PConsoleSaveHandle(object pSender, RoutedEventArgs pArguments)
    {
        string lSceneName = (pConsoleRelayCombo.Text ?? string.Empty).Trim();
        if (lSceneName.Length == 0)
        {
            MessageBox.Show(
                LLocalization.LLocalizationTextRead("Console.Scene.NameRequired"),
                LLocalization.LLocalizationTextRead("Console.Scene.SaveTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
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
        if (!pConsoleSceneApplying && pConsoleRelayCombo.SelectedItem is string lSceneName)
        {
            pConsoleReloadName = lSceneName;
        }
    }

    private void PConsoleExportHandle(object pSender, RoutedEventArgs pArguments)
    {
        string lSceneName = (pConsoleRelayCombo.Text ?? string.Empty).Trim();
        LSceneRecord? lScene = lSceneName.Length > 0 ? LScene.LSceneRead(lSceneName) : null;
        if (lScene is null)
        {
            if (PConsoleWindowRead() is not { } pWindow)
            {
                return;
            }

            lScene = pWindow.PWindowSceneRead(
                lSceneName.Length > 0 ? lSceneName : LLocalization.LLocalizationTextRead("Console.Scene.DefaultName"));
        }

        var pDialog = new SaveFileDialog
        {
            Title = LLocalization.LLocalizationTextRead("Console.Scene.Dialog.Export"),
            Filter = LLocalization.LLocalizationTextRead("Console.Scene.Dialog.Filter"),
            DefaultExt = "json",
            AddExtension = true,
            FileName = PConsoleFileResolve(lScene.LSceneName)
        };

        if (pDialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            LScene.LSceneFileSave(lScene, pDialog.FileName);
        }
        catch (Exception pError)
        {
            PConsoleErrorShow("Console.Scene.Error.Write", pError.Message, "Console.Scene.Dialog.Export");
        }
    }

    private void PConsoleImportHandle(object pSender, RoutedEventArgs pArguments)
    {
        var pDialog = new OpenFileDialog
        {
            Title = LLocalization.LLocalizationTextRead("Console.Scene.Dialog.Import"),
            Filter = LLocalization.LLocalizationTextRead("Console.Scene.Dialog.Filter"),
            DefaultExt = "json",
            CheckFileExists = true
        };

        if (pDialog.ShowDialog() != true)
        {
            return;
        }

        LSceneRecord? lScene;
        try
        {
            lScene = LScene.LSceneFileLoad(pDialog.FileName);
        }
        catch (Exception pError)
        {
            PConsoleErrorShow("Console.Scene.Error.Read", pError.Message, "Console.Scene.Dialog.Import");
            return;
        }

        if (lScene is null)
        {
            PConsoleErrorShow("Console.Scene.Error.Invalid", string.Empty, "Console.Scene.Dialog.Import");
            return;
        }

        string lSceneName = lScene.LSceneName.Trim();
        if (lSceneName.Length == 0)
        {
            lSceneName = Path.GetFileNameWithoutExtension(pDialog.FileName).Trim();
        }

        lScene.LSceneName = PConsoleNameCreate(
            lSceneName.Length > 0 ? lSceneName : LLocalization.LLocalizationTextRead("Console.Scene.ImportedName"));
        LScene.LSceneSave(lScene);
        PConsoleSceneRebuild();
        LTraceLog.LTraceInfoRecord($"Scene imported '{lScene.LSceneName}'");
    }

    private static bool PConsoleSceneConfirm(string lSceneName) =>
        MessageBox.Show(
            LLocalization.LLocalizationFormat("Console.Scene.LoadConfirm", lSceneName),
            LLocalization.LLocalizationTextRead("Console.Scene.LoadTitle"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning,
            MessageBoxResult.No) == MessageBoxResult.Yes;

    private static void PConsoleErrorShow(string lSceneMessageKey, string lSceneDetail, string lSceneTitleKey) =>
        MessageBox.Show(
            lSceneDetail.Length > 0
                ? LLocalization.LLocalizationFormat(lSceneMessageKey, lSceneDetail)
                : LLocalization.LLocalizationTextRead(lSceneMessageKey),
            LLocalization.LLocalizationTextRead(lSceneTitleKey),
            MessageBoxButton.OK,
            MessageBoxImage.Warning);

    private PWindow? PConsoleWindowRead() => Window.GetWindow(this) as PWindow;

    private static string PConsoleFileResolve(string lSceneName)
    {
        char[] pInvalid = Path.GetInvalidFileNameChars();
        string pClean = new string(lSceneName.Trim()
            .Select(pCharacter => pInvalid.Contains(pCharacter) ? '_' : pCharacter)
            .ToArray());
        return string.IsNullOrWhiteSpace(pClean)
            ? $"{LLocalization.LLocalizationTextRead("Console.Scene.DefaultName")}.json"
            : $"{pClean}.json";
    }

    private static string PConsoleNameCreate(string lSceneBaseName)
    {
        if (!LScene.LSceneNames.Any(lName => string.Equals(lName, lSceneBaseName, StringComparison.OrdinalIgnoreCase)))
        {
            return lSceneBaseName;
        }

        for (int lIndex = 2; ; lIndex++)
        {
            string lCandidate = $"{lSceneBaseName} {lIndex}";
            if (!LScene.LSceneNames.Any(lName => string.Equals(lName, lCandidate, StringComparison.OrdinalIgnoreCase)))
            {
                return lCandidate;
            }
        }
    }
}

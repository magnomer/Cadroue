using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;

using Cadroue.Infrastructure;

namespace Cadroue.UIShell;

public sealed partial class PLogWindow
{
    private void PLogCategoryBuild()
    {
        pLogCategoryCombo.Items.Add(new ComboBoxItem
        {
            Content = LLocalization.LLocalizationTextRead("Log.Category.All"),
            Tag = null
        });

        foreach (LTraceKind pLogKind in Enum.GetValues<LTraceKind>())
        {
            pLogCategoryCombo.Items.Add(new ComboBoxItem
            {
                Content = LTraceEntry.LTraceKindRead(pLogKind),
                Tag = pLogKind
            });
        }

        pLogCategoryCombo.SelectedIndex = 0;
        pLogCategoryCombo.SelectionChanged += (_, _) => PLogRowsApply();
        PLogCategoryApply();
    }

    private void PLogCategoryApply()
    {
        foreach (object pLogItem in pLogCategoryCombo.Items)
        {
            if (pLogItem is not ComboBoxItem pLogEntry || pLogEntry.Tag is not LTraceKind pLogKind)
            {
                continue;
            }

            bool pLogAllowed = LTrace.LTraceCheck(pLogKind);
            pLogEntry.IsEnabled = pLogAllowed;
            pLogEntry.ToolTip = pLogAllowed
                ? null
                : LLocalization.LLocalizationTextRead("Log.Category.VerboseNotice");
        }
    }

    private LTraceKind? PLogCategoryRead() =>
        pLogCategoryCombo.SelectedItem is ComboBoxItem pLogItem && pLogItem.Tag is LTraceKind pLogKind
            ? pLogKind
            : null;

    private void PLogFilesBuild()
    {
        string pLogCurrentPath = LTraceWriter.LTracePathRead();
        List<string> pLogFiles = LTraceWriter.LTraceFilesRead();
        if (!pLogFiles.Contains(pLogCurrentPath, StringComparer.OrdinalIgnoreCase))
        {
            pLogFiles.Insert(0, pLogCurrentPath);
        }

        int pLogCurrentIndex = 0;
        foreach (string pLogFile in pLogFiles)
        {
            bool pLogCurrent = string.Equals(pLogFile, pLogCurrentPath, StringComparison.OrdinalIgnoreCase);
            if (pLogCurrent)
            {
                pLogCurrentIndex = pLogFileCombo.Items.Count;
            }

            pLogFileCombo.Items.Add(new ComboBoxItem
            {
                Content = pLogCurrent
                    ? LLocalization.LLocalizationTextRead("Log.File.Current")
                    : Path.GetFileNameWithoutExtension(pLogFile),
                Tag = pLogFile
            });
        }

        pLogFileCombo.SelectedIndex = pLogCurrentIndex;
        pLogFileCombo.SelectionChanged += (_, _) => PLogFileLoad();
        PLogFileLoad();
    }

    private void PLogFileLoad()
    {
        if (pLogFileCombo.SelectedItem is not ComboBoxItem pLogItem || pLogItem.Tag is not string pLogPath)
        {
            return;
        }

        pLogFilePath = pLogPath;
        pLogFileLive = string.Equals(pLogPath, LTraceWriter.LTracePathRead(), StringComparison.OrdinalIgnoreCase);
        pLogSourceText = LTraceWriter.LTraceFileRead(pLogPath);

        pLogRowsAll.Clear();
        foreach (LTraceEntry pLogEntry in LTraceEntry.LTraceEntryParse(pLogSourceText))
        {
            pLogRowsAll.Add(new PLogRow(pLogEntry));
        }

        PLogRowsRemove();
        PLogRowsApply();
    }

    private void PLogRowsApply()
    {
        LTraceKind? pLogCategory = PLogCategoryRead();
        pLogRowsShown.Clear();
        foreach (PLogRow pLogRow in pLogRowsAll)
        {
            if (pLogCategory is null || pLogRow.PLogRowCategory == pLogCategory)
            {
                pLogRowsShown.Add(pLogRow);
            }
        }

        PLogFeedScroll();
    }

    private void PLogRowsRemove()
    {
        int pLogExcess = pLogRowsAll.Count - PLogRowMaximum;
        if (pLogExcess > 0)
        {
            pLogRowsAll.RemoveRange(0, pLogExcess);
        }
    }

    private void PLogFeedScroll()
    {
        if (pLogRowsShown.Count > 0)
        {
            pLogFeed.ScrollIntoView(pLogRowsShown[^1]);
        }
    }

    private void PLogAppendHandle(LTraceEntry pLogEntry)
    {
        lock (pLogPendingLock)
        {
            pLogPending.Add(pLogEntry);
        }
    }

    private void PLogFlushHandle(object? sender, EventArgs e)
    {
        List<LTraceEntry> pLogBatch;
        lock (pLogPendingLock)
        {
            if (pLogPending.Count == 0)
            {
                return;
            }

            pLogBatch = new List<LTraceEntry>(pLogPending);
            pLogPending.Clear();
        }

        if (!pLogFileLive)
        {
            return;
        }

        LTraceKind? pLogCategory = PLogCategoryRead();
        foreach (LTraceEntry pLogEntry in pLogBatch)
        {
            var pLogRow = new PLogRow(pLogEntry);
            pLogRowsAll.Add(pLogRow);
            pLogSourceText += LTraceEntry.LTraceEntryFormat(pLogEntry);
            if (pLogCategory is null || pLogRow.PLogRowCategory == pLogCategory)
            {
                pLogRowsShown.Add(pLogRow);
            }
        }

        if (pLogRowsAll.Count > PLogRowMaximum)
        {
            PLogRowsRemove();
            PLogRowsApply();
            return;
        }

        PLogFeedScroll();
    }

    private void PLogDetailToggle(object sender, RoutedEventArgs e)
    {
        if (e.OriginalSource is FrameworkElement pLogElement && pLogElement.DataContext is PLogRow pLogRow)
        {
            pLogRow.PLogRowExpanded = !pLogRow.PLogRowExpanded;
            e.Handled = true;
        }
    }

    private void PLogTextCopy()
    {
        try
        {
            Clipboard.SetText(pLogSourceText);
        }
        catch (COMException lLogException)
        {
            LTraceLog.LTraceErrorRecord("Log copy failed", lLogException);
        }
    }

    private void PLogFolderOpen()
    {
        try
        {
            string pLogFolder = LTraceWriter.LTraceFolderRead();
            Directory.CreateDirectory(pLogFolder);
            Process.Start(new ProcessStartInfo
            {
                FileName = File.Exists(pLogFilePath) ? "explorer.exe" : pLogFolder,
                Arguments = File.Exists(pLogFilePath) ? $"/select,\"{pLogFilePath}\"" : string.Empty,
                UseShellExecute = true
            });
        }
        catch (Exception pLogException) when (pLogException is IOException or UnauthorizedAccessException)
        {
        }
    }
}

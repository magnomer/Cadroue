using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using Cadroue.UIShell.PMainWindow;
using Cadroue.UIShell.PSShared;

using Cadroue.Infrastructure;

namespace Cadroue.UIShell;

public sealed partial class PLogWindow
{
    private PPicker PLogCategoryBuild()
    {
        string[] pLogTokens = Enum.GetValues<LTraceKind>()
            .Select(LTraceEntry.LTraceKindRead)
            .ToArray();

        var pLogPicker = new PPicker(
            pLogTokens,
            Array.Empty<string>(),
            LLocalization.LLocalizationTextRead("Log.Category.All"))
        {
            Width = 200,
            Height = PSField.PSFieldControlHeight,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        };
        pLogPicker.PPickerChange += PLogRowsApply;
        return pLogPicker;
    }

    private void PLogCategoryApply()
    {
        foreach (LTraceKind pLogKind in Enum.GetValues<LTraceKind>())
        {
            bool pLogAllowed = LTrace.LTraceCheck(pLogKind);
            pLogCategoryPicker.PPickerEnableSet(
                LTraceEntry.LTraceKindRead(pLogKind),
                pLogAllowed,
                pLogAllowed ? null : LLocalization.LLocalizationTextRead("Log.Category.VerboseNotice"));
        }
    }

    private HashSet<LTraceKind> PLogCategoryRead()
    {
        var pLogKinds = new HashSet<LTraceKind>();
        foreach (string pLogToken in pLogCategoryPicker.PPickerSelectionRead())
        {
            pLogKinds.Add(LTraceEntry.LTraceKindFind(pLogToken));
        }

        return pLogKinds;
    }

    private void PLogFilesBuild()
    {
        PLogFilesUpdate();
        pLogFileCombo.SelectionChanged += (_, _) => PLogFileLoad();
        pLogFileCombo.DropDownOpened += (_, _) => PLogFilesUpdate();
        PLogFileLoad();
    }

    private void PLogFilesUpdate()
    {
        string pLogCurrentPath = LTraceWriter.LTracePathRead();
        List<string> pLogFiles = LTraceWriter.LTraceFilesRead();
        if (!pLogFiles.Contains(pLogCurrentPath, StringComparer.OrdinalIgnoreCase))
        {
            pLogFiles.Insert(0, pLogCurrentPath);
        }

        string pLogSelectedPath = pLogFileCombo.SelectedItem is ComboBoxItem pLogSelectedItem
            && pLogSelectedItem.Tag is string pLogSelected
                ? pLogSelected
                : pLogCurrentPath;
        if (!pLogFiles.Contains(pLogSelectedPath, StringComparer.OrdinalIgnoreCase))
        {
            pLogSelectedPath = pLogCurrentPath;
        }

        string[] pLogExisting = pLogFileCombo.Items
            .OfType<ComboBoxItem>()
            .Select(pLogItem => pLogItem.Tag as string ?? string.Empty)
            .ToArray();
        if (pLogExisting.SequenceEqual(pLogFiles, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        pLogFileCombo.Items.Clear();
        int pLogSelectedIndex = 0;
        foreach (string pLogFile in pLogFiles)
        {
            bool pLogCurrent = string.Equals(pLogFile, pLogCurrentPath, StringComparison.OrdinalIgnoreCase);
            if (string.Equals(pLogFile, pLogSelectedPath, StringComparison.OrdinalIgnoreCase))
            {
                pLogSelectedIndex = pLogFileCombo.Items.Count;
            }

            pLogFileCombo.Items.Add(new ComboBoxItem
            {
                Content = pLogCurrent
                    ? LLocalization.LLocalizationTextRead("Log.File.Current")
                    : Path.GetFileNameWithoutExtension(pLogFile),
                Tag = pLogFile
            });
        }

        pLogFileCombo.SelectedIndex = pLogSelectedIndex;
    }

    private void PLogFileLoad()
    {
        if (pLogFileCombo.SelectedItem is not ComboBoxItem pLogItem || pLogItem.Tag is not string pLogPath)
        {
            return;
        }

        pLogFilePath = pLogPath;
        pLogFileLive = string.Equals(pLogPath, LTraceWriter.LTracePathRead(), StringComparison.OrdinalIgnoreCase);
        string pLogText;
        if (pLogFileLive)
        {
            pLogText = LTraceWriter.LTraceWriterRead(out long pLogCommitted);
            pLogSnapshotSequence = pLogCommitted;
            lock (pLogPendingLock)
            {
                pLogPending.RemoveAll(pLogItem => pLogItem.Sequence <= pLogSnapshotSequence);
            }
        }
        else
        {
            pLogText = LTraceWriter.LTraceFileRead(pLogPath);
        }

        pLogRowsAll.Clear();
        foreach (LTraceEntry pLogEntry in LTraceEntry.LTraceEntryParse(pLogText))
        {
            pLogRowsAll.Add(new PLogRow(pLogEntry));
        }

        PLogRowsRemove();
        PLogRowsApply();
    }

    private void PLogRowsApply()
    {
        HashSet<LTraceKind> pLogCategories = PLogCategoryRead();
        pLogRowsShown.Clear();
        foreach (PLogRow pLogRow in pLogRowsAll)
        {
            if (pLogCategories.Count == 0 || pLogCategories.Contains(pLogRow.PLogRowCategory))
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

    private void PLogAppendHandle(long pLogSequence, LTraceEntry pLogEntry)
    {
        lock (pLogPendingLock)
        {
            pLogPending.Add((pLogSequence, pLogEntry));
        }
    }

    private void PLogFlushHandle(object? sender, EventArgs e)
    {
        List<(long Sequence, LTraceEntry Entry)> pLogBatch;
        lock (pLogPendingLock)
        {
            if (pLogPending.Count == 0)
            {
                return;
            }

            pLogBatch = new List<(long Sequence, LTraceEntry Entry)>(pLogPending);
            pLogPending.Clear();
        }

        pLogFileLive = string.Equals(
            pLogFilePath,
            LTraceWriter.LTracePathRead(),
            StringComparison.OrdinalIgnoreCase);
        if (!pLogFileLive)
        {
            return;
        }

        HashSet<LTraceKind> pLogCategories = PLogCategoryRead();
        foreach ((long pLogSequence, LTraceEntry pLogEntry) in pLogBatch)
        {
            if (pLogSequence <= pLogSnapshotSequence)
            {
                continue;
            }

            var pLogRow = new PLogRow(pLogEntry);
            pLogRowsAll.Add(pLogRow);
            if (pLogCategories.Count == 0 || pLogCategories.Contains(pLogRow.PLogRowCategory))
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
            Clipboard.SetText(LTraceWriter.LTraceFileRead(pLogFilePath));
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

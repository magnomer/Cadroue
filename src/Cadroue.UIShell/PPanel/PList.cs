using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.UIShell.PHouse;

using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PList : PPanel
{
    private static readonly FontFamily pListFontFamily = new("Segoe UI");
    private static readonly Brush pListSelectBrush = new SolidColorBrush(Color.FromRgb(0xEE, 0xF4, 0xFB));
    private static readonly Brush pListLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush pListRowBrush = new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27));
    private static readonly Brush pListMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));
    private static readonly Brush pListLockedBrush = new SolidColorBrush(Color.FromRgb(0xF3, 0xF5, 0xF8));
    private static readonly Brush pListLockedAccent = new SolidColorBrush(Color.FromRgb(0xE4, 0xEB, 0xF3));

    public const double PListStripWidth = 48;
    public const string PListDragKind = "CadrouePaths";

    private readonly LDocket pListDocket;
    private readonly StackPanel pListRowPanel;
    private readonly TextBlock pListEmptyNotice;
    private readonly Dictionary<string, Border> pListRows = new(StringComparer.OrdinalIgnoreCase);
    private readonly UIElement pListFullBody;
    private readonly UIElement pListStripBody;
    private string? pListPathCurrent;
    private bool pListMinimized;
    private Point? pListDragOrigin;
    private Point pListDragOffset;
    private string? pListDragPath;

    public event Action<string?>? PListPathChange;
    public event Action<bool>? PListMinimizeChange;
    public event Action<IReadOnlyList<string>>? PListClearChange;
    public event Action<IReadOnlyList<LDocketEntry>>? PListItemsAdd;
    public event Action<bool>? PListLockChange;

    public PList(LDocket pListOwner) : base("")
    {
        pListDocket = pListOwner;
        pListRowPanel = new StackPanel();

        pListEmptyNotice = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("List.Empty.Notice"),
            FontSize = 12,
            FontFamily = pListFontFamily,
            Foreground = pListMutedBrush,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(16, 24, 16, 16)
        };

        var pBody = new Grid();
        pBody.Children.Add(pListEmptyNotice);
        pBody.Children.Add(pListRowPanel);

        var pScroll = new ScrollViewer
        {
            Content = pBody,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };

        var pRoot = new DockPanel { LastChildFill = true };
        UIElement pHeader = PListHeaderBuild();
        DockPanel.SetDock(pHeader, Dock.Top);
        UIElement pActionBar = PListActionBuild();
        DockPanel.SetDock(pActionBar, Dock.Bottom);
        pRoot.Children.Add(pHeader);
        pRoot.Children.Add(pActionBar);
        pRoot.Children.Add(pScroll);

        pListFullBody = pRoot;
        pListStripBody = PListStripBuild();
        pListStripBody.Visibility = Visibility.Collapsed;

        var pBodyHost = new Grid();
        pBodyHost.Children.Add(pListFullBody);
        pBodyHost.Children.Add(pListStripBody);

        FocusVisualStyle = null;
        Focusable = true;
        KeyDown += PListKeyHandle;
        Content = PPanelBorderBuild(pBodyHost);
        pListDocket.LDocketChange += PListDocketHandle;
        pListDocket.LDocketAdded += PListAddHandle;
        pListDocket.LDocketRemoved += PListRemoveHandle;
        PListEmptyUpdate();
    }

    private void PListDocketHandle(IReadOnlyList<LDocketEntry> pListEntries)
    {
        PListRowsRebuild();
        PListSelectionUpdate();
        PListLockChange?.Invoke(PListLockCheck());
    }

    private void PListAddHandle(IReadOnlyList<LDocketEntry> pListAdded)
    {
        LTraceLog.LTraceInfoRecord(
            $"List add handled: {pListAdded.Count} entry(ies), "
            + $"selecting '{System.IO.Path.GetFileName(pListAdded[0].LDocketEntryPath)}' and notifying subscribers");
        PListSelectApply(pListAdded[0].LDocketEntryPath);
        PListItemsAdd?.Invoke(pListAdded);
        LTraceLog.LTraceInfoRecord("List add subscribers notified");
    }

    private void PListRemoveHandle(IReadOnlyList<string> pListRemoved)
    {
        if (pListPathCurrent is null || pListDocket.LDocketItemFind(pListPathCurrent) is null)
        {
            PListSelectApply(pListDocket.LDocketPathsRead().FirstOrDefault());
        }

        PListClearChange?.Invoke(pListRemoved);
    }

    public bool PListMinimizedCheck() => pListMinimized;

    public void PListMinimizeSet(bool pListMinimizeRequest)
    {
        if (pListMinimized == pListMinimizeRequest)
        {
            return;
        }

        pListMinimized = pListMinimizeRequest;
        pListFullBody.Visibility = pListMinimized ? Visibility.Collapsed : Visibility.Visible;
        pListStripBody.Visibility = pListMinimized ? Visibility.Visible : Visibility.Collapsed;
        PListMinimizeChange?.Invoke(pListMinimized);
    }

    public LDocket PListDocketRead() => pListDocket;

    public IReadOnlyList<string> PListPathsRead() => pListDocket.LDocketPathsRead();

    public IReadOnlyList<LDocketEntry> PListItemsRead() => pListDocket.LDocketItemsRead();

    public IReadOnlyList<LDocketEntry> PListUnlockedRead() => pListDocket.LDocketUnlockedRead();

    public string? PListCurrentRead() => pListPathCurrent;

    public LDocketEntry? PListItemRead() =>
        pListPathCurrent is { } pListCurrentPath ? pListDocket.LDocketItemFind(pListCurrentPath) : null;

    public LDocketEntry? PListEditableRead() =>
        PListItemRead() is { LDocketEntryLocked: false } pListItem ? pListItem : null;

    public bool PListLockCheck() => PListItemRead()?.LDocketEntryLocked == true;

    public bool PListLockCheck(string pListPath) => pListDocket.LDocketLockCheck(pListPath);

    public int PListPathsAdd(IEnumerable<string> pAddPaths)
    {
        IReadOnlyList<string> pRequested = pAddPaths as IReadOnlyList<string> ?? pAddPaths.ToArray();
        LTraceLog.LTraceInfoRecord(
            $"List add requested: {pRequested.Count} path(s)",
            string.Join(", ", pRequested.Select(System.IO.Path.GetFileName)));
        try
        {
            IReadOnlyList<string> pScannedPaths = PListMediaScan(pRequested);
            LTraceLog.LTraceInfoRecord($"List scan resolved {pScannedPaths.Count} media path(s); adding to docket");
            int pAdded = pScannedPaths.Count == 0 ? 0 : pListDocket.LDocketPathsAdd(pScannedPaths);
            LTraceLog.LTraceInfoRecord($"List add committed: {pAdded} entry(ies)");
            return pAdded;
        }
        catch (Exception pAddException)
        {
            LTraceLog.LTraceErrorRecord("List add failed", pAddException);
            throw;
        }
    }

    public static bool PListMediaCheck(string pMediaPath) =>
        Cadroue.Media.LMedia.LMediaCheck(pMediaPath);

    public static IReadOnlyList<string> PListMediaScan(IEnumerable<string> pScanPaths)
    {
        var pScanned = new List<string>();
        foreach (string pScanPath in pScanPaths)
        {
            if (File.Exists(pScanPath) && PListMediaCheck(pScanPath))
            {
                pScanned.Add(pScanPath);
                continue;
            }

            if (!Directory.Exists(pScanPath))
            {
                continue;
            }

            try
            {
                pScanned.AddRange(Directory
                    .EnumerateFiles(pScanPath, "*", SearchOption.AllDirectories)
                    .Where(PListMediaCheck)
                    .OrderBy(pFilePath => pFilePath, StringComparer.OrdinalIgnoreCase));
            }
            catch (Exception pScanError) when (pScanError is IOException or UnauthorizedAccessException)
            {
                LTraceLog.LTraceErrorRecord($"List skipped folder '{pScanPath}': {pScanError.Message}");
            }
        }

        return pScanned;
    }

    private void PListRemove()
    {
        IReadOnlyList<string> pRemovedPaths = PListSelectionRead()
            .Where(pListPath => !PListLockCheck(pListPath))
            .ToArray();
        if (pRemovedPaths.Count == 0)
        {
            return;
        }

        int pRemovedIndex = PListIndexRead(pRemovedPaths[0]);
        pListDocket.LDocketPathsRemove(pRemovedPaths);
        IReadOnlyList<string> pRemainingPaths = pListDocket.LDocketPathsRead();
        PListSelectApply(pRemainingPaths.Count == 0
            ? null
            : pRemainingPaths[Math.Clamp(pRemovedIndex, 0, pRemainingPaths.Count - 1)]);
    }

    public void PListClear()
    {
        string[] pListRemovedPaths = pListDocket.LDocketUnlockedRead()
            .Select(pListItem => pListItem.LDocketEntryPath)
            .ToArray();
        if (pListRemovedPaths.Length > 0)
        {
            pListDocket.LDocketPathsRemove(pListRemovedPaths);
        }
    }

    public int PListStaleClear(IReadOnlySet<Guid> pListActiveBatches)
    {
        string[] pListRemovedPaths = pListDocket.LDocketStaleRead(pListActiveBatches)
            .Select(pListItem => pListItem.LDocketEntryPath)
            .ToArray();
        if (pListRemovedPaths.Length > 0)
        {
            pListDocket.LDocketPathsRemove(pListRemovedPaths);
        }

        return pListRemovedPaths.Length;
    }

    public IReadOnlySet<string> PListProtectedRead(IReadOnlySet<Guid> pListActiveBatches) =>
        pListDocket.LDocketProtectedRead(pListActiveBatches)
            .Select(pListItem => pListItem.LDocketEntryPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
}

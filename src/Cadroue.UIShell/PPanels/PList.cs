using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.UIShell.PAssets;
using Cadroue.UIShell.PMainWindow;
using Microsoft.Win32;

using Cadroue.Infrastructure;

namespace Cadroue.UIShell.PPanels;

public sealed partial class PList : PPanel
{
    private static readonly FontFamily pListFontFamily = new("Segoe UI");
    private static readonly Brush pListSelectBrush = new SolidColorBrush(Color.FromRgb(0xEE, 0xF4, 0xFB));
    private const double PListActionGap = 16;

    private static readonly Brush pListIconBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly Brush pListLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush pListTitleBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));
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
            $"List add handled: {pListAdded.Count} entry(ies), selecting '{System.IO.Path.GetFileName(pListAdded[0].LDocketEntryPath)}' and notifying subscribers");
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

    private UIElement PListStripBuild()
    {
        Button pMaximizeButton = PListButtonBuild(
            "/PAssets/PPanels/PListMaximize.svg", LLocalization.LLocalizationTextRead("List.Show.Tooltip"), () => PListMinimizeSet(false));
        pMaximizeButton.Margin = new Thickness(0, 6, 0, 0);
        pMaximizeButton.HorizontalAlignment = HorizontalAlignment.Center;

        var pStrip = new StackPanel { Background = Brushes.White };
        pStrip.Children.Add(pMaximizeButton);
        return pStrip;
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

    private UIElement PListHeaderBuild()
    {
        var pTitleLabel = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("List.Header.Files"),
            FontSize = 12,
            FontFamily = pListFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = pListTitleBrush,
            VerticalAlignment = VerticalAlignment.Center
        };

        Button pMinimizeButton = PListButtonBuild(
            "/PAssets/PPanels/PListMinimize.svg", LLocalization.LLocalizationTextRead("List.Hide.Tooltip"), () => PListMinimizeSet(true));
        pMinimizeButton.Margin = new Thickness(0);
        pMinimizeButton.HorizontalAlignment = HorizontalAlignment.Right;

        var pHeaderGrid = new Grid();
        pHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pMinimizeButton, 1);
        pHeaderGrid.Children.Add(pTitleLabel);
        pHeaderGrid.Children.Add(pMinimizeButton);

        return new Border
        {
            Padding = new Thickness(12, 5, 6, 5),
            BorderBrush = pListLineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Background = Brushes.White,
            Child = pHeaderGrid
        };
    }

    private UIElement PListActionBuild()
    {
        Button pAddFolderButton = PListButtonBuild("/PAssets/PPanels/PFolder.svg", LLocalization.LLocalizationTextRead("List.Button.AddFolder"), PListFolderOpen);
        pAddFolderButton.Margin = new Thickness(PListActionGap, 0, 2, 0);
        var pLeftPanel = new StackPanel { Orientation = Orientation.Horizontal };
        pLeftPanel.Children.Add(PListButtonBuild("/PAssets/PPanels/PExportPlus.svg", LLocalization.LLocalizationTextRead("List.Button.AddFiles"), PListFilesOpen));
        pLeftPanel.Children.Add(pAddFolderButton);

        Button pRemoveAllButton = PListButtonBuild("/PAssets/PPanels/PListRemoveAll.svg", LLocalization.LLocalizationTextRead("List.Button.RemoveAll"), PListClear);
        pRemoveAllButton.Margin = new Thickness(PListActionGap, 0, 2, 0);
        var pRightPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        pRightPanel.Children.Add(PListButtonBuild("/PAssets/PPanels/PExportMinus.svg", LLocalization.LLocalizationTextRead("List.Button.RemoveFile"), PListRemove));
        pRightPanel.Children.Add(pRemoveAllButton);

        var pActionGrid = new Grid { Margin = new Thickness(10, 4, 10, 6) };
        pActionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        pActionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pActionGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pLeftPanel, 0);
        Grid.SetColumn(pRightPanel, 2);
        pActionGrid.Children.Add(pLeftPanel);
        pActionGrid.Children.Add(pRightPanel);

        return new Border
        {
            BorderBrush = pListLineBrush,
            BorderThickness = new Thickness(0, 1, 0, 0),
            Background = Brushes.White,
            Child = pActionGrid
        };
    }

    private static Button PListButtonBuild(string pIconPath, string pTooltip, Action pClick)
    {
        var pButton = new Button
        {
            Content = new Image
            {
                Width = 14,
                Height = 14,
                Source = PIcon.PIconRead(pIconPath, pListIconBrush),
                Stretch = Stretch.Uniform
            },
            ToolTip = pTooltip,
            Width = 28,
            Height = 26,
            Margin = new Thickness(0, 0, 2, 0),
            Style = PButton.PButtonPanelCreate()
        };
        pButton.Click += (_, _) => pClick();
        return pButton;
    }

    private void PListFilesOpen()
    {
        var pDialog = new OpenFileDialog
        {
            Title = LLocalization.LLocalizationTextRead("List.Dialog.AddFiles"),
            Multiselect = true,
            Filter = LLocalization.LLocalizationTextRead("List.Dialog.MediaFilter")
        };

        if (pDialog.ShowDialog() == true)
        {
            LTraceLog.LTraceInfoRecord($"List manual file dialog confirmed: {pDialog.FileNames.Length} file(s)");
            PListPathsAdd(pDialog.FileNames);
        }
    }

    private void PListFolderOpen()
    {
        var pDialog = new OpenFolderDialog { Title = LLocalization.LLocalizationTextRead("List.Dialog.AddFolder"), Multiselect = true };
        if (pDialog.ShowDialog() == true)
        {
            LTraceLog.LTraceInfoRecord($"List manual folder dialog confirmed: {pDialog.FolderNames.Length} folder(s)");
            PListPathsAdd(pDialog.FolderNames);
        }
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

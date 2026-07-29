using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.UIShell.PAssets;
using Cadroue.UIShell.PMainWindow;
using Microsoft.Win32;

namespace Cadroue.UIShell.PPanels;

public sealed class PList : PPanel
{
    private static readonly FontFamily pListFontFamily = new("Segoe UI");
    private static readonly Brush pListSelectBrush = new SolidColorBrush(Color.FromRgb(0xEE, 0xF4, 0xFB));
    private static readonly Brush pListIconBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
    private static readonly Brush pListLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush pListTitleBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));
    private static readonly Brush pListRowBrush = new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27));
    private static readonly Brush pListMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));

    private static readonly string[] pListMediaExtensions =
    [
        ".mp4", ".mkv", ".avi", ".mov", ".wmv", ".flv", ".webm", ".m4v", ".ts", ".mts", ".m2ts",
        ".mp3", ".aac", ".flac", ".wav", ".ogg"
    ];

    public const double PListStripWidth = 48;

    private readonly StackPanel pListRowPanel;
    private readonly TextBlock pListEmptyNotice;
    private readonly List<string> pListPaths = [];
    private readonly UIElement pListFullBody;
    private readonly UIElement pListStripBody;
    private string? pListPathCurrent;
    private bool pListMinimized;

    public event Action<string?>? PListPathChange;
    public event Action<bool>? PListMinimizeChange;

    public PList() : base("")
    {
        pListRowPanel = new StackPanel();

        pListEmptyNotice = new TextBlock
        {
            Text = "Drop media files or folders here, or use the buttons above.",
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
        Content = PPanelBorderBuild(pBodyHost);
        PListEmptyUpdate();
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
            "/PAssets/PPanels/PListMaximize.svg", "Show the Files panel", () => PListMinimizeSet(false));
        pMaximizeButton.Margin = new Thickness(0, 6, 0, 0);
        pMaximizeButton.HorizontalAlignment = HorizontalAlignment.Center;

        var pStrip = new StackPanel { Background = Brushes.White };
        pStrip.Children.Add(pMaximizeButton);
        return pStrip;
    }

    public IReadOnlyList<string> PListPathsRead() => pListPaths.ToArray();

    public string? PListPathCurrentRead() => pListPathCurrent;

    public int PListPathsAdd(IEnumerable<string> pAddPaths)
    {
        int pAddedCount = 0;
        string? pAddedFirst = null;
        foreach (string pMediaPath in PListMediaScan(pAddPaths))
        {
            if (pListPaths.Any(pExisting => string.Equals(pExisting, pMediaPath, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            pListPaths.Add(pMediaPath);
            pAddedFirst ??= pMediaPath;
            pAddedCount++;
        }

        if (pAddedCount == 0)
        {
            return 0;
        }

        PListRowsRebuild();
        PListSelectApply(pAddedFirst);
        return pAddedCount;
    }

    public static bool PListMediaCheck(string pMediaPath) =>
        pListMediaExtensions.Contains(Path.GetExtension(pMediaPath), StringComparer.OrdinalIgnoreCase);

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
                LAppLog.LError($"List skipped folder '{pScanPath}': {pScanError.Message}");
            }
        }

        return pScanned;
    }

    private UIElement PListHeaderBuild()
    {
        var pTitleLabel = new TextBlock
        {
            Text = "Files",
            FontSize = 12,
            FontFamily = pListFontFamily,
            FontWeight = FontWeights.SemiBold,
            Foreground = pListTitleBrush,
            VerticalAlignment = VerticalAlignment.Center
        };

        Button pMinimizeButton = PListButtonBuild(
            "/PAssets/PPanels/PListMinimize.svg", "Hide the Files panel", () => PListMinimizeSet(true));
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
        var pLeftPanel = new StackPanel { Orientation = Orientation.Horizontal };
        pLeftPanel.Children.Add(PListButtonBuild("/PAssets/PPanels/PExportPlus.svg", "Add media files", PListFilesOpen));
        pLeftPanel.Children.Add(PListButtonBuild("/PAssets/PPanels/PExportMinus.svg", "Remove the selected file", PListRemove));

        var pRightPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        pRightPanel.Children.Add(PListButtonBuild("/PAssets/PPanels/PBrowse.svg", "Add every media file in a folder", PListFolderOpen));
        pRightPanel.Children.Add(PListButtonBuild("/PAssets/PPanels/PListRemoveAll.svg", "Remove every file", PListClear));

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
            Title = "Add media files",
            Multiselect = true,
            Filter = "Media files|*.mp4;*.mkv;*.avi;*.mov;*.wmv;*.flv;*.webm;*.m4v;*.ts;*.mts;*.m2ts;*.mp3;*.aac;*.flac;*.wav;*.ogg|All files|*.*"
        };

        if (pDialog.ShowDialog() == true)
        {
            PListPathsAdd(pDialog.FileNames);
        }
    }

    private void PListFolderOpen()
    {
        var pDialog = new OpenFolderDialog { Title = "Add every media file in a folder", Multiselect = true };
        if (pDialog.ShowDialog() == true)
        {
            PListPathsAdd(pDialog.FolderNames);
        }
    }

    private void PListRemove()
    {
        if (pListPathCurrent is null)
        {
            return;
        }

        int pRemovedIndex = pListPaths.IndexOf(pListPathCurrent);
        pListPaths.Remove(pListPathCurrent);
        PListRowsRebuild();
        PListSelectApply(pListPaths.Count == 0
            ? null
            : pListPaths[Math.Clamp(pRemovedIndex, 0, pListPaths.Count - 1)]);
    }

    public void PListClear()
    {
        pListPaths.Clear();
        PListRowsRebuild();
        PListSelectApply(null);
    }

    private void PListRowsRebuild()
    {
        pListRowPanel.Children.Clear();
        foreach (string pRowPath in pListPaths)
        {
            pListRowPanel.Children.Add(PListRowBuild(pRowPath));
        }

        PListEmptyUpdate();
    }

    private Border PListRowBuild(string pRowPath)
    {
        var pRowContent = new StackPanel { Orientation = Orientation.Horizontal };
        pRowContent.Children.Add(new Image
        {
            Width = 14,
            Height = 14,
            Source = PIcon.PIconRead("/PAssets/PPanels/PVideo.svg", pListIconBrush),
            Stretch = Stretch.Uniform,
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(0, 0, 8, 0)
        });
        pRowContent.Children.Add(new TextBlock
        {
            Text = Path.GetFileName(pRowPath),
            FontSize = 12,
            FontFamily = pListFontFamily,
            Foreground = pListRowBrush,
            VerticalAlignment = VerticalAlignment.Center,
            TextTrimming = TextTrimming.CharacterEllipsis
        });

        var pRowBorder = new Border
        {
            Padding = new Thickness(12, 7, 12, 7),
            Background = string.Equals(pRowPath, pListPathCurrent, StringComparison.OrdinalIgnoreCase)
                ? pListSelectBrush
                : Brushes.White,
            BorderBrush = pListLineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Cursor = Cursors.Hand,
            ToolTip = pRowPath,
            Child = pRowContent,
            Tag = pRowPath
        };
        pRowBorder.MouseLeftButtonDown += (_, pRowEvent) =>
        {
            PListSelectApply(pRowPath);
            pRowEvent.Handled = true;
        };
        return pRowBorder;
    }

    private void PListSelectApply(string? pSelectPath)
    {
        pListPathCurrent = pSelectPath;
        foreach (UIElement pRow in pListRowPanel.Children)
        {
            if (pRow is Border { Tag: string pRowPath } pRowBorder)
            {
                pRowBorder.Background = string.Equals(pRowPath, pListPathCurrent, StringComparison.OrdinalIgnoreCase)
                    ? pListSelectBrush
                    : Brushes.White;
            }
        }

        PListPathChange?.Invoke(pListPathCurrent);
    }

    private void PListEmptyUpdate()
    {
        pListEmptyNotice.Visibility = pListPaths.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
}

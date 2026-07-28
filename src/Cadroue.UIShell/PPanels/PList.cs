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

    private readonly StackPanel pListRowPanel;
    private readonly TextBlock pListEmptyNotice;
    private readonly List<string> pListPaths = [];
    private string? pListPathCurrent;

    public event Action<string?>? PListPathChange;

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
        pRoot.Children.Add(pHeader);
        pRoot.Children.Add(pScroll);

        FocusVisualStyle = null;
        Content = PPanelBorderBuild(pRoot);
        PListEmptyUpdate();
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

        var pButtonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        pButtonPanel.Children.Add(PListButtonBuild("/PAssets/PPanels/PExportPlus.svg", "Add media files", PListFilesOpen));
        pButtonPanel.Children.Add(PListButtonBuild("/PAssets/PPanels/PBrowse.svg", "Add every media file in a folder", PListFolderOpen));
        pButtonPanel.Children.Add(PListButtonBuild("/PAssets/PPanels/PExportMinus.svg", "Remove the selected file", PListRemove));
        pButtonPanel.Children.Add(PListButtonBuild("/PAssets/PPanels/PExportCancel.svg", "Remove every file", PListClear));

        var pHeaderGrid = new Grid();
        pHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        pHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(pButtonPanel, 1);
        pHeaderGrid.Children.Add(pTitleLabel);
        pHeaderGrid.Children.Add(pButtonPanel);

        return new Border
        {
            Padding = new Thickness(12, 6, 8, 6),
            BorderBrush = pListLineBrush,
            BorderThickness = new Thickness(0, 0, 0, 1),
            Background = Brushes.White,
            Child = pHeaderGrid
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
            Margin = new Thickness(2, 0, 0, 0),
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

    private void PListClear()
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

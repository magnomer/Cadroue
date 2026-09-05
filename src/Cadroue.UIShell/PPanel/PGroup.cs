using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Cadroue.Application;

namespace Cadroue.UIShell.PPanel;

public sealed partial class PGroup : PPanel
{
    private static readonly FontFamily pGroupFontFamily = new("Segoe UI");
    private static readonly Brush pGroupLineBrush = new SolidColorBrush(Color.FromRgb(0xD9, 0xDE, 0xE7));
    private static readonly Brush pGroupTitleBrush = new SolidColorBrush(Color.FromRgb(0x26, 0x36, 0x4A));
    private static readonly Brush pGroupRowBrush = new SolidColorBrush(Color.FromRgb(0x11, 0x18, 0x27));
    private static readonly Brush pGroupMutedBrush = new SolidColorBrush(Color.FromRgb(0x8A, 0x93, 0x9E));
    private static readonly Brush pGroupCardBrush = new SolidColorBrush(Color.FromRgb(0xF6, 0xF8, 0xFB));
    private static readonly Brush pGroupIconBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));

    private readonly List<PGroupRecord> pGroupRecords = [];
    private readonly LGroupSelection lGroupOwner;
    private readonly StackPanel pGroupRowPanel;
    private readonly TextBlock pGroupEmptyNotice;
    private readonly UIElement pGroupFullBody;
    private readonly UIElement pGroupStripBody;
    private bool pGroupMinimized;

    public Func<IReadOnlyList<string>, IReadOnlyList<string>>? PGroupFileRequest { get; set; }

    public Func<IReadOnlyList<string>>? PGroupSourceFiles { get; set; }

    public event Action<string>? PGroupItemOpen;

    public PGroup(LGroupSelection lGroupOwner) : base("")
    {
        this.lGroupOwner = lGroupOwner;
        UIElement pHeader = PGroupHeaderBuild();

        pGroupRowPanel = new StackPanel();

        pGroupEmptyNotice = new TextBlock
        {
            Text = LLocalization.LLocalizationTextRead("Group.Empty.Notice"),
            FontSize = 12,
            FontFamily = pGroupFontFamily,
            Foreground = pGroupMutedBrush,
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            TextAlignment = TextAlignment.Center,
            Margin = new Thickness(16, 24, 16, 16),
            IsHitTestVisible = false
        };

        var pBody = new Grid();
        pBody.Children.Add(pGroupEmptyNotice);
        pBody.Children.Add(pGroupRowPanel);

        var pScroll = new ScrollViewer
        {
            Content = pBody,
            Background = Brushes.Transparent,
            AllowDrop = true,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled
        };
        pScroll.DragOver += PGroupOverHandle;
        pScroll.Drop += PGroupDropHandle;

        var pRoot = new DockPanel { LastChildFill = true };
        UIElement pActionBar = PGroupActionBuild();
        DockPanel.SetDock(pHeader, Dock.Top);
        DockPanel.SetDock(pActionBar, Dock.Bottom);
        pRoot.Children.Add(pHeader);
        pRoot.Children.Add(pActionBar);
        pRoot.Children.Add(pScroll);

        pGroupFullBody = pRoot;
        pGroupStripBody = PGroupStripBuild();
        pGroupStripBody.Visibility = Visibility.Collapsed;

        var pBodyHost = new Grid();
        pBodyHost.Children.Add(pGroupFullBody);
        pBodyHost.Children.Add(pGroupStripBody);

        Content = PPanelBorderBuild(pBodyHost);
        PGroupRebuild();

        Loaded += (_, _) =>
        {
            lGroupOwner.LGroupSelectionChange += PGroupSelectionUpdate;
            PGroupSelectionUpdate();
        };
        Unloaded += (_, _) => lGroupOwner.LGroupSelectionChange -= PGroupSelectionUpdate;
    }

    private void PGroupSort()
    {
        if (pGroupRecords.Count == 0)
        {
            return;
        }

        foreach (PGroupRecord pRecord in pGroupRecords)
        {
            pRecord.PGroupRecordPaths.Sort((pLeft, pRight) =>
                string.Compare(Path.GetFileName(pLeft), Path.GetFileName(pRight), StringComparison.OrdinalIgnoreCase));
        }

        PGroupRebuild();
    }

    public void PGroupClear(IReadOnlySet<string> pGroupProtectedPaths)
    {
        if (pGroupRecords.Count == 0)
        {
            return;
        }

        int pGroupRemoved = pGroupRecords.RemoveAll(pRecord =>
            !(pRecord.PGroupRecordPaths.Count > 0
                && pRecord.PGroupRecordPaths.All(pRecordPath => pGroupProtectedPaths.Contains(pRecordPath))));
        if (pGroupRemoved == 0)
        {
            return;
        }

        PGroupRebuild();
    }

    public bool PGroupPathsRemove(IReadOnlyList<string> pGroupPaths)
    {
        var pGroupTargetSet = new HashSet<string>(pGroupPaths, StringComparer.OrdinalIgnoreCase);
        int pGroupRemovedCount = pGroupRecords.RemoveAll(pRecord =>
            pRecord.PGroupRecordPaths.Count > 0
            && pRecord.PGroupRecordPaths.All(pRecordPath => pGroupTargetSet.Contains(pRecordPath)));
        if (pGroupRemovedCount == 0)
        {
            return false;
        }

        PGroupRebuild();
        return true;
    }

    public IReadOnlyList<PGroupSelection> PGroupGroupsRead() =>
        pGroupRecords
            .Select(pRecord => new PGroupSelection(pRecord.PGroupRecordName, pRecord.PGroupRecordPaths.ToArray()))
            .ToArray();

    private void PGroupMinimizeSet(bool pGroupMinimizeRequest)
    {
        if (pGroupMinimized == pGroupMinimizeRequest)
        {
            return;
        }

        pGroupMinimized = pGroupMinimizeRequest;
        pGroupFullBody.Visibility = pGroupMinimized ? Visibility.Collapsed : Visibility.Visible;
        pGroupStripBody.Visibility = pGroupMinimized ? Visibility.Visible : Visibility.Collapsed;
    }

    private sealed class PGroupRecord
    {
        public string PGroupRecordName { get; set; } = string.Empty;

        public List<string> PGroupRecordPaths { get; } = [];
    }

    public sealed record PGroupSelection(string PGroupSelectionName, IReadOnlyList<string> PGroupSelectionPaths);
}

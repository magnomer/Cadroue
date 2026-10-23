using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Tests.Interface;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer;
using Xunit;
namespace Cadroue.Tests.Windows;
[Collection("Preset")]
public sealed class TExportList
{
    [Fact]
    public void Read_PreservesOrderTransitionsAndFallbacks() {
        string[] names = ["Loose", "One", "Two", "Three", "Four", "User A", "User B"];
        var groups = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["Loose"] = null,
            ["One"] = "First",
            ["Two"] = "first",
            ["Three"] = "Second",
            ["Four"] = "FIRST"
        };
        var reads = new List<(string, bool)>();
        var list = new QExportList(
            () => names,
            name => groups.ContainsKey(name),
            name => groups[name],
            (name, fallback) =>
            {
                reads.Add((name, fallback));
                return fallback;
            },
            (_, _, _) => { });
        IReadOnlyList<QExportEntry> entries = list.QExportListRead();
        Assert.Equal(names, entries.Select(entry => entry.QExportEntryName));
        Assert.Equal([false, true, false, true, true, true, false],
            entries.Select(entry => entry.QExportEntryHeader));
        Assert.Equal([false, false, false, false, false, true, true],
            entries.Select(entry => entry.QExportEntryUser));
        Assert.Equal([false, true, true, true, true, false, false],
            entries.Select(entry => entry.QExportEntryCollapsed));
        Assert.Equal(
            [
                ("$User", false),
                ("First", true),
                ("first", true),
                ("Second", true),
                ("FIRST", true),
                ("$User", false),
                ("$User", false)
            ],
            reads);
    }
    [Fact]
    public void Toggle_RebuildsAfterRestoredPersistenceFailure() {
        var writes = new List<(string, bool, bool)>();
        var list = new QExportList(
            () => [],
            _ => true,
            _ => "Native",
            (name, fallback) => string.Equals(name, "Native", StringComparison.Ordinal) || fallback,
            (name, folded, fallback) => writes.Add((name, folded, fallback)));
        var native = new QExportEntry("One", "Native", true, false, true);
        var user = new QExportEntry("User", null, true, true, false);
        int rebuilds = 0;
        list.QExportListToggle(native, () => rebuilds++);
        list.QExportListToggle(native, () => rebuilds++);
        list.QExportListToggle(user, () => rebuilds++);
        Assert.Equal(
            [("Native", false, true), ("Native", false, true), ("$User", true, false)],
            writes);
        Assert.Equal(3, rebuilds);
    }
    [Fact]
    public void Show_RevealsHiddenGroupBeforeSelection() {
        var folded = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase)
        {
            ["Native"] = true,
            ["$User"] = true
        };
        var writes = new List<(string, bool, bool)>();
        var list = new QExportList(
            () => [],
            name => name.StartsWith("Native", StringComparison.Ordinal),
            name => name == "Native loose" ? null : "Native",
            (name, fallback) => folded.TryGetValue(name, out bool value) ? value : fallback,
            (name, value, fallback) =>
            {
                writes.Add((name, value, fallback));
                folded[name] = value;
            });
        int rebuilds = 0;
        Assert.True(list.QExportListShow("Native row", () => rebuilds++));
        Assert.False(list.QExportListShow("Native row", () => rebuilds++));
        Assert.False(list.QExportListShow("Native loose", () => rebuilds++));
        Assert.True(list.QExportListShow("User row", () => rebuilds++));
        Assert.False(list.QExportListShow("User row", () => rebuilds++));
        Assert.Equal([("Native", false, true), ("$User", false, false)], writes);
        Assert.Equal(2, rebuilds);
    }
    [Fact]
    public void Surface_RendersPassiveHeadersAndPreservesDragContract() {
        System.Windows.Application? program = System.Windows.Application.Current;
        ExceptionDispatchInfo? failure = null;
        Thread thread = new(() => {
            try {
                using TPreset presets = new();
                presets.TPresetSeedCreate("Alpha", "Beta");
                object[] preferenceState = TInterface.TInterfaceExportRead("$User", true);
                LPresetSelection selection = TInterface.TPresetSelectionCreate("Alpha");
                QExport export = TInterface.TExportCreate(selection, "Convert");
                PExport? surface = null;
                object[]? iconState = null;
                try {
                    (surface, iconState) = TInterface.TExportSurfaceRead(export);
                    TInterface.TInterfaceExportRead(surface, true);
                    var scroll = Assert.IsType<ScrollViewer>(TInterface.TInterfaceExportRead(surface)[0]);
                    var panel = Assert.IsType<StackPanel>(scroll.Content);
                    Assert.Equal(3, panel.Children.Count);
                    var header = Assert.IsAssignableFrom<Border>(panel.Children[0]);
                    Assert.Equal("Cadroue.UIVeneer.PExportPresetGroup", header.GetType().FullName);
                    var alpha = Assert.IsType<Border>(panel.Children[1]);
                    var beta = Assert.IsType<Border>(panel.Children[2]);
                    object[] face = TInterface.TInterfaceExportRead(header, true);
                    Assert.Equal("Header", header.Tag);
                    Assert.Equal(new Thickness(12, 5, 8, 5), header.Padding);
                    Assert.Equal(new Thickness(0, 0, 0, 1), header.BorderThickness);
                    Assert.Equal(Colors.White, Assert.IsType<SolidColorBrush>(header.Background).Color);
                    Assert.Equal(Color.FromRgb(0xD9, 0xDE, 0xE7),
                        Assert.IsType<SolidColorBrush>(header.BorderBrush).Color);
                    Assert.Equal(TInterface.TLocalizationCatalogRead("ExportPreset.Group.User"),
                        Assert.IsType<TextBlock>(face[0]).Text);
                    var button = Assert.IsType<Button>(face[1]);
                    var image = Assert.IsType<Image>(face[2]);
                    Assert.Equal(22, button.Width);
                    Assert.Equal(20, button.Height);
                    Assert.Equal(16, image.Width);
                    Assert.Equal(16, image.Height);
                    Assert.Contains("/PAsset/PPanel/PExportExpand.svg", Assert.IsType<string>(face[3]));
                    Assert.Equal(TInterface.TLocalizationCatalogRead("ExportPreset.Group.Expand"), button.ToolTip);
                    Assert.Equal(Visibility.Collapsed, alpha.Visibility);
                    Assert.Equal(Visibility.Collapsed, beta.Visibility);
                    button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    Assert.False(TInterface.TInterfaceExportRead("$User"));
                    Assert.DoesNotContain(alpha, panel.Children.Cast<UIElement>());
                    header = Assert.IsAssignableFrom<Border>(panel.Children[0]);
                    face = TInterface.TInterfaceExportRead(header, true);
                    Assert.Contains("/PAsset/PPanel/PExportCollapse.svg", Assert.IsType<string>(face[3]));
                    Assert.Equal(Visibility.Visible, Assert.IsType<Border>(panel.Children[1]).Visibility);
                    Border beforeFailure = Assert.IsType<Border>(panel.Children[1]);
                    TInterface.TInterfaceExportRead(false, "$User");
                    Assert.IsType<Button>(face[1]).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    Assert.False(TInterface.TInterfaceExportRead("$User"));
                    Assert.DoesNotContain(beforeFailure, panel.Children.Cast<UIElement>());
                    TInterface.TInterfaceExportRead(true, "$User");
                    header = Assert.IsAssignableFrom<Border>(panel.Children[0]);
                    face = TInterface.TInterfaceExportRead(header, true);
                    Assert.IsType<Button>(face[1]).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    beta = Assert.IsType<Border>(panel.Children[2]);
                    int repaints = 0;
                    export.QExportPresetsChanged += () => repaints++;
                    var up = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                    {
                        RoutedEvent = UIElement.MouseLeftButtonUpEvent
                    };
                    beta.RaiseEvent(up);
                    Assert.True(up.Handled);
                    Assert.Equal("Beta", export.QExportSelected);
                    Assert.Equal(1, repaints);
                    Assert.False(TInterface.TInterfaceExportRead("$User"));
                    Assert.DoesNotContain(beta, panel.Children.Cast<UIElement>());
                    Assert.Equal("Header", Assert.IsAssignableFrom<Border>(panel.Children[0]).Tag);
                }
                finally
                {
                    if (surface is not null && iconState is not null)
                    {
                        TInterface.TInterfaceExportRead(surface, false, iconState);
                    }
                    TInterface.TPresetSelectionClose(selection);
                    TInterface.TInterfaceExportRead(preferenceState, true);
                }
            }
            catch (Exception exception)
            {
                failure = ExceptionDispatchInfo.Capture(exception);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        failure?.Throw();
        Assert.True(program is null || ReferenceEquals(program, System.Windows.Application.Current));
    }
    [Fact]
    public void Drag_ThresholdMoveAndRelease_ReordersOnceAndClearsSurfaceState() {
        ExceptionDispatchInfo? failure = null;
        Thread thread = new(() => {
            try { using TPreset presets = new(); presets.TPresetSeedCreate("Alpha", "Beta");
                object[] preference = TInterface.TInterfaceExportRead("$User", false);
                LPresetSelection selection = TInterface.TPresetSelectionCreate("Beta");
                var persist = LPresetPersistence.LPresetSaveSeam; LPresetPersistence.LPresetSaveSeam = null;
                int storeChanges = 0; Action storeChanged = () => storeChanges++;
                LPresetTable.LPresetStoreChanged += storeChanged;
                QExport export = TInterface.TExportCreate(selection, "Convert");
                (PExport surface, object[] icons) = TInterface.TExportSurfaceRead(export);
                ResourceDictionary resources = System.Windows.Application.Current!.Resources;
                bool hadGhostResource = resources.Contains("PGhostImage");
                object? ghostResource = resources["PGhostImage"];
                resources["PGhostImage"] = new System.Windows.Shapes.Rectangle { Fill = new ImageBrush() };
                try { var decorator = new System.Windows.Documents.AdornerDecorator { Child = surface };
                    using var source = new System.Windows.Interop.HwndSource(
                        new System.Windows.Interop.HwndSourceParameters());
                    source.RootVisual = decorator; decorator.Measure(new Size(400, 800));
                    decorator.Arrange(new Rect(0, 0, 400, 800));
                    var scroll = Assert.IsType<ScrollViewer>(TInterface.TInterfaceExportRead(surface)[0]);
                    var panel = Assert.IsType<StackPanel>(scroll.Content);
                    UIElement[] items = panel.Children.Cast<UIElement>().ToArray();
                    Border row = Assert.IsType<Border>(items[1]); int moverCalls = 0;
                    var samePosition = new QExportDrag(export, (_, _) => { moverCalls++; return false; });
                    samePosition.QExportDragAttach(panel, _ => { }); Point samePoint = items[0].TranslatePoint(
                        new Point(0, items[0].RenderSize.Height), panel);
                    samePosition.QExportDragStart("Alpha", 0, 0); samePosition.QExportDragSet(row);
                    samePosition.QExportDragSet(new Point(samePoint.X + 100, samePoint.Y + 100));
                    Assert.True(samePosition.QExportDragMove(samePoint, true, false)); Assert.Equal(0, moverCalls);
                    Assert.True(samePosition.QExportDragClear()); items = panel.Children.Cast<UIElement>().ToArray();
                    row = Assert.IsType<Border>(items[1]); QExportDrag drag = export.QExportDrag;
                    Point far = new(0, 900);
                    MouseDevice mouse = Mouse.PrimaryDevice;
                    RoutedEvent releaseEvent = UIElement.MouseLeftButtonUpEvent;
                    int repaints = 0; int selections = 0; export.QExportPresetsChanged += () => repaints++;
                    selection.LPresetSelectionChanged += () => selections++;
                    AdornerLayer tGhostLayer = ((AdornerDecorator)QWalk.QWalkParentFind(
                        panel, node => node is AdornerDecorator)!).AdornerLayer;
                    drag.QExportDragStart("Alpha", 0, 0);
                    drag.QExportDragSet(row); drag.QExportDragSet(new Point(0, 0));
                    Assert.True(drag.QExportDragMove(far, true, true));
                    Assert.Equal(1, VisualTreeHelper.GetChildrenCount(tGhostLayer));
                    Assert.IsType<QGhostAdorner>(VisualTreeHelper.GetChild(tGhostLayer, 0));
                    Assert.Equal([items[0], items[2], row], panel.Children.Cast<UIElement>());
                    MouseButtonEventArgs up = new(mouse, 0, MouseButton.Left) { RoutedEvent = releaseEvent };
                    panel.RaiseEvent(up); Assert.True(up.Handled);
                    Assert.Equal(["Beta", "Alpha"], LPresetTable.LPresetNames); Assert.Equal(1, storeChanges);
                    Assert.Equal((1, 0, "Beta"), (repaints, selections, export.QExportSelected));
                    Assert.False(drag.QExportDragMove()); Assert.False(drag.QExportDragActive);
                    Assert.Equal(1, row.Opacity);
                    Assert.Equal(0, VisualTreeHelper.GetChildrenCount(tGhostLayer));
                    items = panel.Children.Cast<UIElement>().ToArray();
                    resources["PGhostImage"] = new System.Windows.Shapes.Rectangle { Fill = new ImageBrush() };
                    row = Assert.IsType<Border>(items[2]); drag.QExportDragStart("Missing", 0, 0);
                    drag.QExportDragSet(row); drag.QExportDragSet(new Point(0, 0));
                    Assert.False(drag.QExportDragMove(new Point(1, 1), true, true));
                    Assert.True(drag.QExportDragMove(far, true, true));
                    Assert.Equal(items, panel.Children.Cast<UIElement>());
                    Assert.Equal(1, VisualTreeHelper.GetChildrenCount(tGhostLayer));
                    Assert.IsType<QGhostAdorner>(VisualTreeHelper.GetChild(tGhostLayer, 0));
                    panel.RaiseEvent(new MouseEventArgs(mouse, 0) { RoutedEvent = Mouse.LostMouseCaptureEvent });
                    Assert.False(drag.QExportDragMove()); Assert.False(drag.QExportDragActive);
                    Assert.Equal(1, row.Opacity);
                    Assert.Equal(0, VisualTreeHelper.GetChildrenCount(tGhostLayer));
                    row = Assert.IsType<Border>(panel.Children[2]);
                    drag.QExportDragStart("Alpha", 0, 0);
                    drag.QExportDragSet(row); drag.QExportDragSet(new Point(0, 0));
                    int beforeClick = repaints;
                    MouseButtonEventArgs click = new(mouse, 0, MouseButton.Left) { RoutedEvent = releaseEvent };
                    panel.RaiseEvent(click); Assert.True(click.Handled); Assert.Equal("Alpha", export.QExportSelected);
                    Assert.Equal(1, selections); Assert.Equal(beforeClick + 1, repaints); Assert.Equal(1, storeChanges);
                } finally {
                    if (hadGhostResource) resources["PGhostImage"] = ghostResource;
                    else resources.Remove("PGhostImage");
                    LPresetTable.LPresetStoreChanged -= storeChanged; LPresetPersistence.LPresetSaveSeam = persist;
                    TInterface.TInterfaceExportRead(surface, false, icons);
                    TInterface.TPresetSelectionClose(selection); TInterface.TInterfaceExportRead(preference, true);
                }
            } catch (Exception exception) { failure = ExceptionDispatchInfo.Capture(exception); }
        });
        thread.SetApartmentState(ApartmentState.STA); thread.Start(); thread.Join(); failure?.Throw();
    }
    [Fact]
    public void Rebuild_DetachesStaleHeaderActions() {
        ExceptionDispatchInfo? failure = null;
        Thread thread = new(() => {
            try {
                using TPreset presets = new();
                presets.TPresetSeedCreate("Alpha", "Beta");
                object[] preferenceState = TInterface.TInterfaceExportRead("$User", false);
                LPresetSelection selection = TInterface.TPresetSelectionCreate("Alpha");
                QExport export = TInterface.TExportCreate(selection, "Convert");
                PExport? surface = null;
                object[]? iconState = null;
                try
                {
                    (surface, iconState) = TInterface.TExportSurfaceRead(export);
                    TInterface.TInterfaceExportRead(surface, true);
                    var panel = Assert.IsType<StackPanel>(
                        Assert.IsType<ScrollViewer>(TInterface.TInterfaceExportRead(surface)[0]).Content);
                    Border staleHeader = Assert.IsAssignableFrom<Border>(panel.Children[0]);
                    Button staleAction = Assert.IsType<Button>(TInterface.TInterfaceExportRead(staleHeader, true)[1]);
                    staleAction.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    Assert.True(TInterface.TInterfaceExportRead("$User"));
                    UIElement[] rebuilt = panel.Children.Cast<UIElement>().ToArray();
                    staleAction.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

                    Assert.True(TInterface.TInterfaceExportRead("$User"));
                    Assert.Equal(rebuilt, panel.Children.Cast<UIElement>());
                }
                finally
                {
                    if (surface is not null && iconState is not null)
                    {
                        TInterface.TInterfaceExportRead(surface, false, iconState);
                    }
                    TInterface.TPresetSelectionClose(selection);
                    TInterface.TInterfaceExportRead(preferenceState, true);
                }
            }
            catch (Exception exception)
            {
                failure = ExceptionDispatchInfo.Capture(exception);
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        failure?.Throw();
    }
}

using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer;
using Cadroue.Tests.Interface;
using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Preset")]
public sealed class TExportRow
{
    [Fact]
    public void RowsRead_CapturesWorkingWhileSelectionAndEditingRemainLive()
    {
        LPresetDocument first = TInterface.TPresetDocumentCreate("First", "First");
        LPresetDocument second = TInterface.TPresetDocumentCreate("Second", "Second");
        LPresetDocument current = first;
        LPresetDocument? matched = null;
        LPresetDocument? supported = null;
        bool editing = false;
        QExportRows rows = TInterface.TInterfaceExportRead(
            _ => false,
            (_, working) =>
            {
                matched = working;
                return false;
            },
            (_, working, _) =>
            {
                supported = working;
                return false;
            },
            () => current,
            () => "preset",
            _ => editing,
            name => $"Display:{name}",
            "Notice");

        current = second;
        QExportFace face = TInterface.TInterfaceExportRead(rows, "Preset");
        Assert.Same(first, matched);
        Assert.Same(first, supported);
        Assert.Equal("Display:Preset", face.QExportFaceName);
        Assert.True(face.QExportFaceSelected);
        Assert.True(face.QExportFaceModified);
        Assert.False(face.QExportFaceSupported);
        Assert.False(face.QExportFaceEditing);

        editing = true;
        Assert.True(TInterface.TInterfaceExportRead(rows, "Preset").QExportFaceEditing);
    }

    [Fact]
    public void RowFace_NativeModifiedUnsupportedAndEditing_PreservesPassiveSurfaceAndRoutes()
    {
        System.Windows.Application? program = System.Windows.Application.Current;
        ExceptionDispatchInfo? failure = null;
        Thread thread = new(() =>
        {
            try
            {
                using TPreset presets = new();
                presets.TPresetSeedCreate();
                TInterface.TInterfaceExportRead(presets.TPresetNativeName, ["Alpha", "Beta"]);
                LPresetSelection selection = TInterface.TInterfaceExportRead("Alpha", "Edit");
                LPresetDocument alpha = TInterface.TPresetDocumentCreate("Alpha", "Draft");
                alpha.LPresetVideo.LPresetMode = "Copy";
                selection.LPresetSelectionValue = alpha;
                QExport export = TInterface.TExportCreate(selection, "Edit");
                Func<string, LPresetDocument, bool>? save = null;
                PExport? surface = null;
                object[]? iconState = null;
                Window? owner = null;
                try
                {
                    save = TInterface.TInterfaceExportRead();
                    TInterface.TInterfaceExportRead(false);
                    (surface, iconState) = TInterface.TInterfaceExportRead(export, selection);
                    owner = new Window { Content = surface };
                    owner.Show();
                    owner.Hide();
                    var scroll = Assert.IsType<ScrollViewer>(TInterface.TInterfaceExportRead(surface)[0]);
                    Assert.Equal(Colors.White, Assert.IsAssignableFrom<SolidColorBrush>(scroll.Background).Color);
                    Assert.Equal(ScrollBarVisibility.Auto, scroll.VerticalScrollBarVisibility);
                    Assert.Equal(ScrollBarVisibility.Disabled, scroll.HorizontalScrollBarVisibility);
                    Assert.Null(scroll.FocusVisualStyle);
                    var panel = Assert.IsType<StackPanel>(scroll.Content);
                    Func<Border, string?> labelRead = row => row.Child is Grid grid
                        && grid.Children[0] is StackPanel name
                        && name.Children[0] is TextBlock label
                            ? label.Text
                            : null;
                    Border root = panel.Children.OfType<Border>().Single(row =>
                        labelRead(row)?.StartsWith("Alpha", StringComparison.Ordinal) == true);
                    Border nativeRoot = panel.Children.OfType<Border>().Single(row =>
                        labelRead(row)?.StartsWith("Split", StringComparison.Ordinal) == true);
                    object[] face = TInterface.TInterfaceExportRead(root);
                    TextBlock label = Assert.IsType<TextBlock>(face[0]);
                    Border badge = Assert.IsType<Border>(face[1]);
                    StackPanel actions = Assert.IsType<StackPanel>(face[2]);
                    Button accept = Assert.IsType<Button>(face[3]);
                    Button revert = Assert.IsType<Button>(face[4]);
                    Assert.Equal(
                        $"Alpha (Modified) {TInterface.TLocalizationCatalogRead("ExportPreset.Incompatible")}",
                        label.Text);
                    Assert.Equal(FontStyles.Italic, label.FontStyle);
                    Assert.Equal(Visibility.Collapsed, badge.Visibility);
                    Assert.Equal(Visibility.Visible, actions.Visibility);
                    Assert.Equal(0.42, root.Opacity);
                    Assert.Equal(TInterface.TLocalizationCatalogRead("ExportPreset.DisabledTooltip"), root.ToolTip);
                    Assert.Equal(Color.FromRgb(0xEE, 0xF4, 0xFB), ((SolidColorBrush)root.Background).Color);
                    Assert.Equal(TInterface.TLocalizationCatalogRead("ExportPreset.ApplyTooltip"), accept.ToolTip);
                    Assert.Equal(TInterface.TLocalizationCatalogRead("ExportPreset.DiscardTooltip"), revert.ToolTip);
                    Assert.Equal(22, accept.Width);
                    Assert.Equal(20, accept.Height);
                    Assert.Equal(new(2, 0, 0, 0), accept.Margin);
                    Assert.Contains("/PAsset/PPanel/PExportCheck.svg", Assert.IsType<string>(face[5]));
                    Assert.Contains("/PAsset/PPanel/PExportCancel.svg", Assert.IsType<string>(face[6]));
                    object[] nativeFace = TInterface.TInterfaceExportRead(nativeRoot);
                    Assert.Equal(Visibility.Visible, Assert.IsType<Border>(nativeFace[1]).Visibility);
                    Assert.Equal(Visibility.Collapsed, Assert.IsType<StackPanel>(nativeFace[2]).Visibility);
                    Assert.Equal(TInterface.TLocalizationCatalogRead("ExportPreset.Native"),
                        Assert.IsType<TextBlock>(Assert.IsType<Border>(nativeFace[1]).Child).Text);

                    int repaints = 0;
                    TInterface.TExportAttach(export, () => repaints++);
                    Dispatcher.CurrentDispatcher.BeginInvoke(
                        DispatcherPriority.ApplicationIdle,
                        new Action(() => owner.OwnedWindows
                            .OfType<Window>()
                            .Single()
                            .Close()));
                    var failed = new RoutedEventArgs(Button.ClickEvent);
                    accept.RaiseEvent(failed);
                    Assert.True(failed.Handled);
                    Assert.Equal(0, repaints);
                    Assert.Contains(root, panel.Children.Cast<UIElement>());

                    TInterface.TInterfaceExportRead(true);
                    var accepted = new RoutedEventArgs(Button.ClickEvent);
                    accept.RaiseEvent(accepted);
                    Assert.True(accepted.Handled);
                    Assert.Equal(1, repaints);
                    Assert.DoesNotContain(root, panel.Children.Cast<UIElement>());
                    var staleAccepted = new RoutedEventArgs(Button.ClickEvent);
                    repaints = 0;
                    accept.RaiseEvent(staleAccepted);
                    Assert.False(staleAccepted.Handled);
                    Assert.Equal(0, repaints);
                    root = panel.Children.OfType<Border>().Single(row =>
                        labelRead(row)?.StartsWith("Alpha", StringComparison.Ordinal) == true);
                    Assert.Equal(Visibility.Collapsed,
                        Assert.IsType<StackPanel>(TInterface.TInterfaceExportRead(root)[2]).Visibility);

                    LPresetDocument changed = TInterface.TPresetDocumentCreate("Alpha", "Changed again");
                    changed.LPresetVideo.LPresetMode = "Encode";
                    repaints = 0;
                    selection.LPresetSelectionValue = changed;
                    Assert.Equal(1, repaints);
                    root = panel.Children.OfType<Border>().Single(row =>
                        labelRead(row)?.StartsWith("Alpha", StringComparison.Ordinal) == true);
                    Button liveRevert = Assert.IsType<Button>(TInterface.TInterfaceExportRead(root)[4]);
                    repaints = 0;
                    var reverted = new RoutedEventArgs(Button.ClickEvent);
                    liveRevert.RaiseEvent(reverted);
                    Assert.True(reverted.Handled);
                    Assert.Equal(1, repaints);
                    Assert.DoesNotContain(root, panel.Children.Cast<UIElement>());
                    var staleReverted = new RoutedEventArgs(Button.ClickEvent);
                    repaints = 0;
                    liveRevert.RaiseEvent(staleReverted);
                    Assert.False(staleReverted.Handled);
                    Assert.Equal(0, repaints);

                    Border beta = panel.Children.OfType<Border>().Single(row =>
                        labelRead(row)?.StartsWith("Beta", StringComparison.Ordinal) == true);
                    var down = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                    {
                        RoutedEvent = Mouse.PreviewMouseDownEvent
                    };
                    beta.RaiseEvent(down);
                    Assert.False(down.Handled);
                    var up = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                    {
                        RoutedEvent = UIElement.MouseLeftButtonUpEvent
                    };
                    repaints = 0;
                    beta.RaiseEvent(up);
                    Assert.True(up.Handled);
                    Assert.Equal("Beta", TInterface.TInterfaceExportRead(export).Item1);
                    Assert.Equal(1, repaints);
                    Assert.DoesNotContain(beta, panel.Children.Cast<UIElement>());
                    var staleSelected = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                    {
                        RoutedEvent = UIElement.MouseLeftButtonUpEvent
                    };
                    repaints = 0;
                    beta.RaiseEvent(staleSelected);
                    Assert.False(staleSelected.Handled);
                    Assert.Equal(0, repaints);

                    root = panel.Children.OfType<Border>().Single(row =>
                        labelRead(row)?.StartsWith("Alpha", StringComparison.Ordinal) == true);
                    var doubleClick = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                    {
                        RoutedEvent = Mouse.PreviewMouseDownEvent
                    };
                    typeof(MouseButtonEventArgs).GetProperty(nameof(MouseButtonEventArgs.ClickCount))!
                        .SetValue(doubleClick, 2);
                    repaints = 0;
                    root.RaiseEvent(doubleClick);
                    Assert.True(doubleClick.Handled);
                    Assert.Equal("Alpha", TInterface.TInterfaceExportRead(export).Item2);
                    Assert.Equal(2, repaints);
                    Border editingRoot = panel.Children.OfType<Border>().Single(row => row.Child is TextBox);
                    TextBox box = Assert.IsType<TextBox>(editingRoot.Child);
                    Assert.Equal("Alpha", box.Text);

                    beta = panel.Children.OfType<Border>().Single(row =>
                        labelRead(row)?.StartsWith("Beta", StringComparison.Ordinal) == true);
                    var commit = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                    {
                        RoutedEvent = UIElement.MouseLeftButtonUpEvent
                    };
                    repaints = 0;
                    beta.RaiseEvent(commit);
                    Assert.True(commit.Handled);
                    Assert.Null(TInterface.TInterfaceExportRead(export).Item2);
                    Assert.Equal("Beta", TInterface.TInterfaceExportRead(export).Item1);
                    Assert.Equal(2, repaints);
                    Assert.DoesNotContain(editingRoot, panel.Children.Cast<UIElement>());

                    beta = panel.Children.OfType<Border>().Single(row =>
                        labelRead(row)?.StartsWith("Beta", StringComparison.Ordinal) == true);
                    Button inline = Assert.IsType<Button>(TInterface.TInterfaceExportRead(beta)[3]);
                    Image inlineImage = Assert.IsType<Image>(inline.Content);
                    TInterface.TExportDragStart(export, "Beta");
                    var inlineDown = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                    {
                        RoutedEvent = Mouse.PreviewMouseDownEvent
                    };
                    repaints = 0;
                    inline.RaiseEvent(inlineDown);
                    Assert.False(inlineDown.Handled);
                    inlineDown.RoutedEvent = Mouse.MouseDownEvent;
                    inline.RaiseEvent(inlineDown);
                    Assert.True(inlineDown.Handled);
                    Assert.Null(TInterface.TInterfaceExportRead(export).Item2);
                    Assert.False(TInterface.TExportDragMove(export));
                    Assert.Equal(0, repaints);
                    TInterface.TExportDragStart(export, "Beta");
                    var imageDown = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                    {
                        RoutedEvent = Mouse.PreviewMouseDownEvent
                    };
                    inlineImage.RaiseEvent(imageDown);
                    Assert.False(imageDown.Handled);
                    imageDown.RoutedEvent = Mouse.MouseDownEvent;
                    inlineImage.RaiseEvent(imageDown);
                    Assert.True(imageDown.Handled);
                    Assert.Null(TInterface.TInterfaceExportRead(export).Item2);
                    Assert.False(TInterface.TExportDragMove(export));
                    Assert.Equal(0, repaints);

                    repaints = 0;
                    Border beforeReload = beta;
                    Button beforeReloadInline = inline;
                    Image beforeReloadImage = inlineImage;
                    TInterface.TInterfaceExportRead(surface, false);
                    LPresetDocument detached = TInterface.TPresetDocumentCreate("Beta", "Detached");
                    selection.LPresetSelectionValue = detached;
                    Assert.Equal(0, repaints);
                    TInterface.TInterfaceExportRead(surface, true);
                    Assert.Equal(1, repaints);
                    Assert.DoesNotContain(beforeReload, panel.Children.Cast<UIElement>());
                    var staleReloaded = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                    {
                        RoutedEvent = UIElement.MouseLeftButtonUpEvent
                    };
                    repaints = 0;
                    beforeReload.RaiseEvent(staleReloaded);
                    Assert.False(staleReloaded.Handled);
                    Assert.Equal(0, repaints);
                    var staleInlineDown = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                    {
                        RoutedEvent = Mouse.PreviewMouseDownEvent
                    };
                    beforeReloadInline.RaiseEvent(staleInlineDown);
                    Assert.False(staleInlineDown.Handled);
                    Assert.Equal(0, repaints);
                    var staleImageDown = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                    {
                        RoutedEvent = Mouse.PreviewMouseDownEvent
                    };
                    beforeReloadImage.RaiseEvent(staleImageDown);
                    Assert.False(staleImageDown.Handled);
                    Assert.Equal(0, repaints);
                    root = panel.Children.OfType<Border>().Single(row =>
                        labelRead(row)?.StartsWith("Alpha", StringComparison.Ordinal) == true);
                    var reloaded = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                    {
                        RoutedEvent = UIElement.MouseLeftButtonUpEvent
                    };
                    repaints = 0;
                    root.RaiseEvent(reloaded);
                    Assert.True(reloaded.Handled);
                    Assert.Equal("Alpha", TInterface.TInterfaceExportRead(export).Item1);
                    Assert.Equal(1, repaints);
                    root = panel.Children.OfType<Border>().Single(row =>
                        labelRead(row)?.StartsWith("Alpha", StringComparison.Ordinal) == true);
                    inline = Assert.IsType<Button>(TInterface.TInterfaceExportRead(root)[3]);
                    inlineImage = Assert.IsType<Image>(inline.Content);
                    TInterface.TExportDragStart(export, "Alpha");
                    var liveInlineDown = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                    {
                        RoutedEvent = Mouse.PreviewMouseDownEvent
                    };
                    repaints = 0;
                    inline.RaiseEvent(liveInlineDown);
                    Assert.False(liveInlineDown.Handled);
                    liveInlineDown.RoutedEvent = Mouse.MouseDownEvent;
                    inline.RaiseEvent(liveInlineDown);
                    Assert.True(liveInlineDown.Handled);
                    Assert.Null(TInterface.TInterfaceExportRead(export).Item2);
                    Assert.False(TInterface.TExportDragMove(export));
                    Assert.Equal(0, repaints);
                    TInterface.TExportDragStart(export, "Alpha");
                    var liveImageDown = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                    {
                        RoutedEvent = Mouse.PreviewMouseDownEvent
                    };
                    inlineImage.RaiseEvent(liveImageDown);
                    Assert.False(liveImageDown.Handled);
                    liveImageDown.RoutedEvent = Mouse.MouseDownEvent;
                    inlineImage.RaiseEvent(liveImageDown);
                    Assert.True(liveImageDown.Handled);
                    Assert.Null(TInterface.TInterfaceExportRead(export).Item2);
                    Assert.False(TInterface.TExportDragMove(export));
                    Assert.Equal(0, repaints);
                }
                finally
                {
                    try
                    {
                        TInterface.TInterfaceExportRead(save);
                    }
                    finally
                    {
                        try
                        {
                            if (surface is not null && iconState is not null)
                            {
                                TInterface.TInterfaceExportRead(surface, false, iconState);
                            }
                        }
                        finally
                        {
                            try
                            {
                                if (owner is not null)
                                {
                                    owner.Content = null;
                                    owner.Close();
                                }
                            }
                            finally
                            {
                                TInterface.TPresetSelectionClose(selection);
                            }
                        }
                    }
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
        if (program is null)
        {
            Assert.True(System.Windows.Application.Current?.Dispatcher.Thread.IsAlive);
        }
        else
        {
            Assert.Same(program, System.Windows.Application.Current);
        }
    }
}

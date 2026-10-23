using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Tests.Interface;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer;
using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Preset")]
public sealed class TExportLifecycle
{
    [Fact]
    public void Unload_DetachesHeaderAndRowHandlers()
    {
        System.Windows.Application? program = System.Windows.Application.Current;
        ExceptionDispatchInfo? failure = null;
        Thread thread = new(() =>
        {
            try
            {
                using TPreset presets = new();
                presets.TPresetSeedCreate("Alpha", "Beta");
                LPresetSelection selection = TInterface.TPresetSelectionCreate("Alpha");
                QExport export = TInterface.TExportCreate(selection, "Convert");
                int[] rebuilds = [0];
                PExport? surface = null;
                object[]? iconState = null;
                try
                {
                    (surface, iconState) = TInterface.TExportSurfaceRead(export, rebuildCount: rebuilds);
                    Assert.Equal(2, rebuilds[0]);
                    var panel = Assert.IsType<StackPanel>(
                        Assert.IsType<ScrollViewer>(TInterface.TInterfaceExportRead(surface)[0]).Content);
                    Border header = Assert.IsAssignableFrom<Border>(panel.Children[0]);
                    Border row = Assert.IsType<Border>(panel.Children[1]);
                    Button headerAction = Assert.IsType<Button>(TInterface.TInterfaceExportRead(header, true)[1]);
                    Button accept = Assert.IsType<Button>(TInterface.TInterfaceExportRead(row)[3]);
                    QExportDrag drag = export.QExportDrag;
                    drag.QExportDragStart("Alpha", 0, 0);
                    Assert.True(drag.QExportDragMove());
                    panel.RaiseEvent(new MouseEventArgs(Mouse.PrimaryDevice, 0)
                    {
                        RoutedEvent = Mouse.LostMouseCaptureEvent
                    });
                    Assert.False(drag.QExportDragActive);
                    header = Assert.IsAssignableFrom<Border>(panel.Children[0]);
                    row = Assert.IsType<Border>(panel.Children[1]);
                    headerAction = Assert.IsType<Button>(TInterface.TInterfaceExportRead(header, true)[1]);
                    accept = Assert.IsType<Button>(TInterface.TInterfaceExportRead(row)[3]);
                    object[] controls = TInterface.TInterfaceExportRead(surface);

                    TInterface.TInterfaceExportRead(surface, false);
                    rebuilds[0] = 0;

                    var click = new RoutedEventArgs(Button.ClickEvent);
                    accept.RaiseEvent(click);
                    Assert.False(click.Handled);
                    headerAction.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                    var up = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                    {
                        RoutedEvent = UIElement.MouseLeftButtonUpEvent
                    };
                    row.RaiseEvent(up);
                    Assert.False(up.Handled);
                    Assert.Equal("Alpha", TInterface.TInterfaceExportRead(export).Item1);
                    Assert.False(TInterface.TInterfaceExportRead("$User"));
                    drag.QExportDragStart("Beta", 0, 0);
                    drag.QExportDragSet(row);
                    var detachedClick = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                    {
                        RoutedEvent = UIElement.MouseLeftButtonUpEvent
                    };
                    panel.RaiseEvent(detachedClick);
                    Assert.False(detachedClick.Handled);
                    Assert.Equal("Alpha", export.QExportSelected);
                    Assert.False(drag.QExportDragClear());
                    selection.LPresetSelectionValue = TInterface.TPresetDocumentCreate("Alpha", "Changed");
                    export.QExportUpdate();
                    Assert.Equal(0, rebuilds[0]);
                    Assert.Contains(header, panel.Children.Cast<UIElement>());
                    Assert.Contains(row, panel.Children.Cast<UIElement>());

                    TInterface.TInterfaceExportRead(surface, true);
                    Assert.Equal(2, rebuilds[0]);
                    export.QExportUpdate();
                    Assert.Equal(3, rebuilds[0]);

                    Assert.DoesNotContain(header, panel.Children.Cast<UIElement>());
                    Assert.DoesNotContain(row, panel.Children.Cast<UIElement>());
                    object[] reloadedControls = TInterface.TInterfaceExportRead(surface);
                    Assert.Same(controls[0], reloadedControls[0]);
                    Assert.Same(controls[1], reloadedControls[1]);
                    int selections = 0;
                    selection.LPresetSelectionChanged += () => selections++;
                    row = Assert.IsType<Border>(panel.Children[2]);
                    drag.QExportDragStart("Beta", 0, 0);
                    drag.QExportDragSet(row);
                    var reloadedClick = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                    {
                        RoutedEvent = UIElement.MouseLeftButtonUpEvent
                    };
                    panel.RaiseEvent(reloadedClick);
                    Assert.True(reloadedClick.Handled);
                    Assert.Equal("Beta", export.QExportSelected);
                    Assert.Equal(1, selections);

                    rebuilds[0] = 0;
                    TInterface.TInterfaceExportRead(surface, false);
                    export.QExportUpdate();
                    Assert.Equal(0, rebuilds[0]);
                    TInterface.TInterfaceExportRead(surface, true);
                    Assert.Equal(2, rebuilds[0]);
                    export.QExportUpdate();
                    Assert.Equal(3, rebuilds[0]);

                    row = Assert.IsType<Border>(panel.Children[1]);
                    drag.QExportDragStart("Alpha", 0, 0);
                    drag.QExportDragSet(row);
                    var repeatedClick = new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                    {
                        RoutedEvent = UIElement.MouseLeftButtonUpEvent
                    };
                    panel.RaiseEvent(repeatedClick);
                    Assert.True(repeatedClick.Handled);
                    Assert.Equal("Alpha", export.QExportSelected);
                    Assert.Equal(2, selections);
                }
                finally
                {
                    if (surface is not null && iconState is not null)
                    {
                        TInterface.TInterfaceExportRead(surface, false, iconState);
                    }
                    TInterface.TPresetSelectionClose(selection);
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
}

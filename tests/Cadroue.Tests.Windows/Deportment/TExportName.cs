using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using Cadroue.UIDeportment;
using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TExportName
{
    [Fact]
    public void Editor_ReturnCommits_EscapeCancels_AndOutsideClickCommits()
    {
        ExceptionDispatchInfo? failure = null;
        Thread thread = new(() =>
        {
            try
            {
                string selected = "Alpha";
                var commits = new List<(string, string)>();
                int repaints = 0;
                int syncs = 0;
                var name = new QExportName(
                    () => selected,
                    value => selected = value,
                    (oldValue, newValue) => commits.Add((oldValue, newValue)),
                    () => repaints++,
                    () => syncs++);
                using var source = new HwndSource(new HwndSourceParameters());
                var owner = new Window();
                try
                {
                    name.QExportNameStart("Alpha");
                    var returned = new TextBox { Text = "Beta" };
                    name.QExportNameAttach(returned, "Alpha");
                    owner.Content = returned;
                    owner.Show();
                    var returnEvent = new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Return)
                    {
                        RoutedEvent = Keyboard.KeyDownEvent
                    };
                    returned.RaiseEvent(returnEvent);
                    Assert.True(returnEvent.Handled);
                    Assert.Equal([("Alpha", "Beta")], commits);
                    Assert.Null(name.QExportNameEditing);
                    Assert.Equal(1, syncs);

                    name.QExportNameStart("Alpha");
                    var escaped = new TextBox { Text = "Gamma" };
                    name.QExportNameAttach(escaped, "Alpha");
                    owner.Content = escaped;
                    var escapeEvent = new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Escape)
                    {
                        RoutedEvent = Keyboard.KeyDownEvent
                    };
                    escaped.RaiseEvent(escapeEvent);
                    Assert.True(escapeEvent.Handled);
                    Assert.Equal([("Alpha", "Beta")], commits);
                    Assert.Null(name.QExportNameEditing);
                    Assert.Equal(2, syncs);

                    name.QExportNameStart("Alpha");
                    var outside = new TextBox { Text = "Delta" };
                    var button = new Button();
                    var panel = new StackPanel();
                    panel.Children.Add(outside);
                    panel.Children.Add(button);
                    name.QExportNameAttach(outside, "Alpha");
                    owner.Content = panel;
                    outside.RaiseEvent(new RoutedEventArgs(FrameworkElement.LoadedEvent));
                    button.RaiseEvent(new MouseButtonEventArgs(Mouse.PrimaryDevice, 0, MouseButton.Left)
                    {
                        RoutedEvent = Mouse.PreviewMouseDownEvent
                    });
                    Assert.Equal([("Alpha", "Beta"), ("Alpha", "Delta")], commits);
                    Assert.Null(name.QExportNameEditing);
                    Assert.Equal(3, syncs);
                    Assert.Equal(3, repaints);
                }
                finally
                {
                    owner.Content = null;
                    owner.Close();
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

    [Fact]
    public void Editor_FocusLossCommits_AndInvalidNameEndsWithoutRename()
    {
        ExceptionDispatchInfo? failure = null;
        Thread thread = new(() =>
        {
            try
            {
                string selected = "Alpha";
                int requests = 0;
                int syncs = 0;
                var name = new QExportName(
                    () => selected,
                    value => selected = value,
                    (_, _) => requests++,
                    () => { },
                    () => syncs++);
                name.QExportNameStart("Alpha");
                var editor = new TextBox { Text = "" };
                name.QExportNameAttach(editor, "Alpha");

                editor.RaiseEvent(new KeyboardFocusChangedEventArgs(
                    Keyboard.PrimaryDevice,
                    0,
                    editor,
                    null)
                {
                    RoutedEvent = Keyboard.LostKeyboardFocusEvent
                });

                Assert.Equal("Alpha", selected);
                Assert.Equal(1, requests);
                Assert.Equal(1, syncs);
                Assert.Null(name.QExportNameEditing);
                Assert.False(name.QExportNameCommit("Alpha", "Beta"));
                Assert.Equal(1, requests);
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

    [Fact]
    public void RebuildAndUnload_DetachStaleEditorHandlers()
    {
        ExceptionDispatchInfo? failure = null;
        Thread thread = new(() =>
        {
            try
            {
                string selected = "Alpha";
                int commits = 0;
                int syncs = 0;
                var name = new QExportName(
                    () => selected,
                    value => selected = value,
                    (_, _) => commits++,
                    () => { },
                    () => syncs++);
                using var source = new HwndSource(new HwndSourceParameters());
                name.QExportNameStart("Alpha");
                var rebuilt = new TextBox { Text = "Stale" };
                name.QExportNameAttach(rebuilt, "Alpha");
                name.QExportNameDetach(rebuilt);
                name.QExportNameStart("Alpha");
                var live = new TextBox { Text = "Live" };
                name.QExportNameAttach(live, "Alpha");

                var staleReturn = new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Return)
                {
                    RoutedEvent = Keyboard.KeyDownEvent
                };
                rebuilt.RaiseEvent(staleReturn);
                Assert.False(staleReturn.Handled);
                Assert.Equal(0, commits);
                Assert.Equal("Alpha", name.QExportNameEditing);

                var liveReturn = new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Return)
                {
                    RoutedEvent = Keyboard.KeyDownEvent
                };
                live.RaiseEvent(liveReturn);
                Assert.True(liveReturn.Handled);
                Assert.Equal(1, commits);
                Assert.Equal(1, syncs);

                name.QExportNameStart("Alpha");
                var unloaded = new TextBox();
                name.QExportNameAttach(unloaded, "Alpha");
                unloaded.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent));
                name.QExportNameStart("Alpha");
                var replacement = new TextBox();
                name.QExportNameAttach(replacement, "Alpha");
                var staleEscape = new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Escape)
                {
                    RoutedEvent = Keyboard.KeyDownEvent
                };
                unloaded.RaiseEvent(staleEscape);
                Assert.False(staleEscape.Handled);
                Assert.Equal("Alpha", name.QExportNameEditing);
                var liveEscape = new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Escape)
                {
                    RoutedEvent = Keyboard.KeyDownEvent
                };
                replacement.RaiseEvent(liveEscape);
                Assert.True(liveEscape.Handled);
                Assert.Null(name.QExportNameEditing);
                Assert.Equal(1, commits);
                Assert.Equal(2, syncs);
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

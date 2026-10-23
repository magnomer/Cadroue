using Cadroue.Core;
using Cadroue.UIDeportment;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;
using Xunit;
namespace Cadroue.Tests.Windows;
[Collection("Preference")]
public class TOptionsWorkspace
{
    private readonly Delegate[] tOptionsPending;
    public TOptionsWorkspace() {
        tOptionsPending =
        [
            new Action<Action>(assertion =>
            {
            Exception? failure = null;
            var thread = new Thread(() =>
                {
                    try
                    {
                        Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
                        SynchronizationContext.SetSynchronizationContext(
                            new DispatcherSynchronizationContext(dispatcher));
                        System.Windows.Application program = System.Windows.Application.Current
                            ?? new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                        program.Resources["PIconRoot"] = "pack://application:,,,/";
                        var icons =
                            (System.Collections.Concurrent.ConcurrentDictionary<string, ImageSource>)typeof(QIcon)
                            .GetField("qIconCache", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
                        Brush icon = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
                        icons[$"pack://application:,,,/PAsset/PPanel/PBrowse.svg|{icon}"] = new DrawingImage();
                        icons[$"pack://application:,,,/PAsset/PPanel/POpen.svg|{icon}"] = new DrawingImage();
                        var resolvers = (Dictionary<bool, Func<string, double, string?>>)typeof(QIcon)
                            .GetField("qIconResolvers", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
                        Func<string, double, string?> resolver = resolvers[true]; resolvers[true] = (_, _) => null;
                        if (!program.Resources.Contains("PButtonWhite"))
                        {
                            string resource = Path.GetFullPath(Path.Combine(
                                AppContext.BaseDirectory,
                                "../../../../../src/Cadroue.UIVeneer/Kit/Button/PButton.xaml"));
                            using var reader = System.Xml.XmlReader.Create(
                                new StringReader(File.ReadAllText(resource)));
                            program.Resources.MergedDictionaries.Add((ResourceDictionary)XamlReader.Load(reader));
                        }
                        try { assertion(); } finally { resolvers[true] = resolver; }
                    }
                    catch (Exception exception)
                    {
                        failure = exception;
                    }
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                thread.Join();
                Assert.Null(failure);
            })
        ];
    }
    [Fact] public void DoneClear_Unapplied_DoesNotRequest() {
        ((Action<Action>)tOptionsPending[0])(() =>
        {
            var requests = new List<string>();
            (QSWorkspace driver, FrameworkElement surface) = TOptionsPendingAttach(
                "C:\\Other",
                TInterface.TWorkspaceAnswerCreate((_, _, _) => { },
                    (_, _, _, continuation) => continuation(), (_, _) => { }, (_, _) => { }),
                request =>
                {
                    if (request != "done") return TOptionsPendingRead(request);
                    requests.Add(request);
                    return TInterface.TOptionsFactCreate(string.Empty, 0, 0, false);
                });
            Button done = (Button)LogicalTreeHelper.FindLogicalNode(surface, "PSSystemDone");
            Assert.False(done.IsEnabled);
            done.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, done));
            Assert.Empty(requests);
            TInterface.TWorkspaceDetach(driver);
        });
    }
    [Fact] public void DoneClear_Rejected_DoesNotRequest() {
        ((Action<Action>)tOptionsPending[0])(() =>
        {
            int requests = 0;
            (QSWorkspace driver, FrameworkElement surface) = TOptionsPendingAttach(
                "C:\\Workspace",
                TInterface.TWorkspaceAnswerCreate((_, _, _) => { },
                    (_, _, _, _) => { }, (_, _) => { }, (_, _) => { }),
                request =>
                {
                    if (request != "done") return TOptionsPendingRead(request);
                    requests++;
                    return TInterface.TOptionsFactCreate(string.Empty, 0, 0, false);
                });
            Button done = (Button)LogicalTreeHelper.FindLogicalNode(surface, "PSSystemDone");
            done.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, done));
            Assert.Equal(0, requests);
            Assert.Equal("0 B", ((TextBlock)LogicalTreeHelper.FindLogicalNode(surface, "PSWorkspaceSize")).Text);
            TInterface.TWorkspaceDetach(driver);
        });
    }
    [Fact] public void DoneClear_Accepted_PreservesEffectOrder() {
        ((Action<Action>)tOptionsPending[0])(() =>
        {
            string[] cleared = ["scheduled", "done", "failed", "cancelled"];
            string root = Path.Combine(Path.GetTempPath(), "Cadroue.Tests", "Workspace", Guid.NewGuid().ToString("N"));
            foreach (string folder in cleared.Append("running"))
            {
                Directory.CreateDirectory(Path.Combine(root, folder));
                File.WriteAllText(Path.Combine(root, folder, "work.json"), "{}");
            }
            string previous = TInterface.TDepotRootRead();
            TInterface.TDepotRootSet(root);
            try
            {
                var order = new List<string>();
                TextBlock? size = null;
                QSOptions options = TInterface.TOptionsCreate(
                    Interface.TInterface.TPreferenceClone(TInterface.TPreferenceCurrentRead()), false);
                (QSWorkspace driver, FrameworkElement surface) = TOptionsPendingAttach(
                    options,
                    root,
                    TInterface.TWorkspaceAnswerCreate((_, _, _) => { },
                        (_, _, _, continuation) =>
                        {
                            order.Add(cleared.All(folder => File.Exists(Path.Combine(root, folder, "work.json")))
                                ? "confirm" : "late");
                            continuation();
                        }, (_, _) => { },
                        (title, message) =>
                        {
                            Assert.Equal(Interface.TInterface.TLocalizationTextRead(
                                "Options.System.ClearDoneTitle"), title);
                            Assert.Equal(string.Format(Interface.TInterface.TLocalizationTextRead(
                                "Options.System.WorkRecordsRemoved"), 4), message);
                            order.Add(cleared.Any(folder => Directory.EnumerateFiles(Path.Combine(root, folder)).Any())
                                ? "early" : "cleared");
                            long bytes = Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                                .Sum(file => new FileInfo(file).Length);
                            string shown = bytes < 1024 ? $"{bytes} B" : $"{bytes / 1024.0:0.##} KB";
                            order.Add(size!.Text == shown ? "size" : "stale");
                            order.Add("announce");
                        }));
                size = (TextBlock)LogicalTreeHelper.FindLogicalNode(surface, "PSWorkspaceSize");
                Button done = (Button)LogicalTreeHelper.FindLogicalNode(surface, "PSSystemDone");
                done.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, done));
                Assert.Equal(new[] { "confirm", "cleared", "size", "announce" }, order);
                Assert.True(File.Exists(Path.Combine(root, "running", "work.json")));
                Assert.True(File.Exists(Path.Combine(root, "work.db")));
                TInterface.TWorkspaceDetach(driver);
            }
            finally
            {
                TInterface.TDepotRootSet(previous);
                try { Directory.Delete(root, true); } catch (IOException) { }
            }
        });
    }
    [Fact] public void Workspace_AppliedRefresh_KeepsSize() {
        ((Action<Action>)tOptionsPending[0])(() =>
        {
            (QSOptions options, FrameworkElement surface, string root, string previous) = TOptionsPendingAttach();
            try
            {
                var size = (TextBlock)LogicalTreeHelper.FindLogicalNode(surface, "PSWorkspaceSize");
                var done = (Button)LogicalTreeHelper.FindLogicalNode(surface, "PSSystemDone");
                Assert.Equal(("2 B", true), (size.Text, done.IsEnabled));
                TInterface.TDepotRootSet(Path.Combine(root, "elsewhere"));
                options.QSOptionsSystem.QSOptionsSystemUpdate();
                Assert.Equal(("2 B", false), (size.Text, done.IsEnabled));
                TInterface.TWorkspaceDetach(options.QSWorkspace);
            }
            finally
            {
                TInterface.TDepotRootSet(previous);
                try { Directory.Delete(root, true); } catch (IOException) { }
            }
        });
    }
    [Fact] public void Workspace_SidecarRefresh_KeepsState() {
        ((Action<Action>)tOptionsPending[0])(() =>
        {
            (QSOptions options, FrameworkElement surface, string root, string previous) = TOptionsPendingAttach();
            try
            {
                var size = (TextBlock)LogicalTreeHelper.FindLogicalNode(surface, "PSWorkspaceSize");
                var done = (Button)LogicalTreeHelper.FindLogicalNode(surface, "PSSystemDone");
                Assert.Equal(("2 B", true), (size.Text, done.IsEnabled));
                var workspace = (QSWorkspace)typeof(QSOptionsSystem)
                    .GetField("qsWorkspace", BindingFlags.Instance | BindingFlags.NonPublic)!
                    .GetValue(options.QSOptionsSystem)!;
                Action refresh = workspace.QSWorkspaceSizeUpdate;
                TInterface.TDepotRootSet(Path.Combine(root, "elsewhere"));
                refresh();
                Assert.Equal(("0 B", true), (size.Text, done.IsEnabled));
                TInterface.TWorkspaceDetach(options.QSWorkspace);
            }
            finally
            {
                TInterface.TDepotRootSet(previous);
                try { Directory.Delete(root, true); } catch (IOException) { }
            }
        });
    }
    [Fact] public void WorkspaceClear_Running_ShowsWarningOnly() {
        ((Action<Action>)tOptionsPending[0])(() =>
        {
            int confirmations = 0;
            var warnings = new List<(string, string)>();
            (QSWorkspace driver, FrameworkElement surface) = TOptionsPendingAttach(
                "C:\\Workspace",
                TInterface.TWorkspaceAnswerCreate((_, _, _) => { },
                    (_, _, _, _) => confirmations++, (title, message) => warnings.Add((title, message)), (_, _) => { }),
                request => TOptionsPendingRead(request, true));
            Button clear = (Button)LogicalTreeHelper.FindLogicalNode(surface, "PSWorkspaceCleaner");
            clear.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, clear));
            Assert.Equal(0, confirmations);
            Assert.Equal((Interface.TInterface.TLocalizationTextRead("Options.System.ClearWorkspaceTitle"),
                Interface.TInterface.TLocalizationTextRead("Options.System.WorkspaceRunning")), warnings.Single());
            TInterface.TWorkspaceDetach(driver);
        });
    }
    [Fact] public void WorkspaceClear_Accepted_PreservesEffectOrder() {
        ((Action<Action>)tOptionsPending[0])(() =>
        {
            var order = new List<string>();
            (QSWorkspace driver, FrameworkElement surface) = TOptionsPendingAttach(
                "C:\\Workspace",
                TInterface.TWorkspaceAnswerCreate((_, _, _) => { },
                    (_, _, _, continuation) => { order.Add("confirm"); continuation(); }, (_, _) => { },
                    (title, message) =>
                    {
                        Assert.Equal(Interface.TInterface.TLocalizationTextRead(
                            "Options.System.ClearWorkspaceTitle"), title);
                        Assert.Equal(Interface.TInterface.TLocalizationTextRead(
                            "Options.System.WorkspaceReset"), message);
                        order.Add("announce");
                    }),
                request =>
                {
                    if (request == "size" && order.Contains("compact")) { order.Add("size"); return 1048576L; }
                    if (request != "workspace") return TOptionsPendingRead(request);
                    order.AddRange(["reset", "rebuild", "compact"]);
                    return TInterface.TOptionsFactCreate(string.Empty, 0, 0, false);
                });
            Button clear = (Button)LogicalTreeHelper.FindLogicalNode(surface, "PSWorkspaceCleaner");
            clear.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, clear));
            Assert.Equal(new[] { "confirm", "reset", "rebuild", "compact", "size", "announce" }, order);
            Assert.Equal("1 MB", ((TextBlock)LogicalTreeHelper.FindLogicalNode(surface, "PSWorkspaceSize")).Text);
            TInterface.TWorkspaceDetach(driver);
        });
    }
    [Theory] [InlineData(true)] [InlineData(false)] public void Commit_Occupied_KeepsCurrentAndWarns(bool occupied) {
        ((Action<Action>)tOptionsPending[0])(() =>
        {
            var warnings = new List<(string, string)>();
            (QSWorkspace driver, FrameworkElement surface) = TOptionsPendingAttach("C:\\Occupied",
                TInterface.TWorkspaceAnswerCreate((_, _, _) => { }, (_, _, _, _) => { },
                    (title, message) => warnings.Add((title, message)), (_, _) => { }),
                request => request == "occupied" ? occupied : TOptionsPendingRead(request));
            driver.QSWorkspaceCommit();
            string current = TInterface.TPreferenceCurrentRead().LPreferenceWorkspaceFolder;
            Assert.Equal(occupied ? current : "C:\\Occupied",
                ((TextBox)LogicalTreeHelper.FindLogicalNode(surface, "PSWorkspaceBox")).Text);
            Assert.Equal(occupied ? new[] { (Interface.TInterface.TLocalizationTextRead("Options.System.Workspace"),
                QLocalizationCatalog.QLocalizationFormat("Options.System.WorkspaceOccupied", "C:\\Occupied")) }
                : Array.Empty<(string, string)>(), warnings);
            TInterface.TWorkspaceDetach(driver);
        });
    }
    [Fact] public void Browse_Cancelled_PreservesPath()
    {
        ((Action<Action>)tOptionsPending[0])(() =>
        {
            (QSWorkspace driver, FrameworkElement surface) = TOptionsPendingAttach(
                "C:\\Workspace",
                TInterface.TWorkspaceAnswerCreate((title, initial, continuation) =>
                {
                    Assert.Equal(Interface.TInterface.TLocalizationTextRead("Options.System.ChooseWorkspace"), title);
                    Assert.Equal("C:\\Workspace", initial);
                }, (_, _, _, _) => { }, (_, _) => { }, (_, _) => { }),
                request => TOptionsPendingRead(request));
            Button browse = (Button)LogicalTreeHelper.FindLogicalNode(surface, "PSWorkspaceBrowse");
            browse.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, browse));
            Assert.Equal("C:\\Workspace", ((TextBox)LogicalTreeHelper.FindLogicalNode(surface, "PSWorkspaceBox")).Text);
            TInterface.TWorkspaceDetach(driver);
        });
    }
    [Fact] public void Detach_PreventsSurfaceCallbacks()
    {
        ((Action<Action>)tOptionsPending[0])(() =>
        {
            var requests = new List<string>();
            Action<string>? folder = null;
            Action? confirmation = null;
            int presentations = 0;
            (QSWorkspace driver, FrameworkElement surface) = TOptionsPendingAttach(
                "C:\\Workspace",
                TInterface.TWorkspaceAnswerCreate((_, _, continuation) => folder = continuation,
                    (_, _, _, continuation) => confirmation = continuation,
                    (_, _) => presentations++, (_, _) => presentations++),
                request =>
                {
                    if (request != "done") return TOptionsPendingRead(request);
                    requests.Add(request);
                    return TInterface.TOptionsFactCreate(string.Empty, 1, 0, false);
                });
            Button browse = (Button)LogicalTreeHelper.FindLogicalNode(surface, "PSWorkspaceBrowse");
            Button done = (Button)LogicalTreeHelper.FindLogicalNode(surface, "PSSystemDone");
            browse.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, browse));
            done.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, done));
            TInterface.TWorkspaceDetach(driver);
            requests.Clear();
            folder!("C:\\Replacement");
            confirmation!();
            browse.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, browse));
            done.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, done));
            Assert.Equal("C:\\Workspace", ((TextBox)LogicalTreeHelper.FindLogicalNode(surface, "PSWorkspaceBox")).Text);
            Assert.Empty(requests);
            Assert.Equal(0, presentations);
        });
    }
    private static (QSWorkspace, FrameworkElement) TOptionsPendingAttach(string path, QSWorkspaceAnswer answer,
        Func<string, object?> request)
    {
        string assemblyPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/Cadroue.UIVeneer/bin/Debug/net10.0-windows10.0.19041.0/Cadroue.UIVeneer.dll"));
        Type passiveType = Assembly.LoadFrom(assemblyPath).GetType(
            "Cadroue.UIVeneer.PSOptionsSystemWorkspace",
            true)!;
        var workspace = (FrameworkElement)Activator.CreateInstance(passiveType, true)!;
        (QSWorkspace driver, Button done) = TInterface.TWorkspaceAttach(
            fact => fact.LOptionsRequestKind switch
            {
                LOptionsRequestKind.LOptionsRequestRoot => TInterface.TOptionsFactCreate("C:\\Workspace", 0, 0, false),
                LOptionsRequestKind.LOptionsRequestResolution =>
                    TInterface.TOptionsFactCreate(fact.LOptionsRequestPath ?? string.Empty, 0, 0, false),
                LOptionsRequestKind.LOptionsRequestDefault =>
                    TInterface.TOptionsFactCreate((string)request("default")!, 0, 0, false),
                LOptionsRequestKind.LOptionsRequestSize =>
                    TInterface.TOptionsFactCreate(string.Empty, 0, (long)request("size")!, false),
                LOptionsRequestKind.LOptionsRequestRunning =>
                    TInterface.TOptionsFactCreate(string.Empty, 0, 0, (bool)request("running")!),
                LOptionsRequestKind.LOptionsRequestOccupancy =>
                    TInterface.TOptionsFactCreate(string.Empty, 0, 0, (bool)request("occupied")!),
                _ => throw new NotSupportedException(fact.LOptionsRequestKind.ToString())
            },
            fact => fact.LOptionsRequestKind switch
            {
                LOptionsRequestKind.LOptionsRequestDone => (LOptionsFact)request("done")!,
                LOptionsRequestKind.LOptionsRequestWorkspace => (LOptionsFact)request("workspace")!,
                _ => throw new NotSupportedException(fact.LOptionsRequestKind.ToString())
            },
            workspace,
            path,
            answer);
        done.Name = "PSSystemDone";
        var surface = new StackPanel { Children = { workspace, done } };
        return (driver, surface);
    }
    private static (QSWorkspace, FrameworkElement) TOptionsPendingAttach(QSOptions options, string path,
        QSWorkspaceAnswer answer)
    {
        var workspace = (FrameworkElement)Activator.CreateInstance(
            TOptionsPendingRead().GetType("Cadroue.UIVeneer.PSOptionsSystemWorkspace", true)!, true)!;
        (QSWorkspace driver, Button done) = TInterface.TWorkspaceAttach(options, workspace, path, answer);
        done.Name = "PSSystemDone";
        var surface = new StackPanel { Children = { workspace, done } };
        return (driver, surface);
    }
    private static (QSOptions, FrameworkElement, string, string) TOptionsPendingAttach()
    {
        string root = Path.Combine(Path.GetTempPath(), "Cadroue.Tests", "Workspace", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "done"));
        File.WriteAllText(Path.Combine(root, "done", "work.json"), "{}");
        string previous = TInterface.TDepotRootRead();
        TInterface.TDepotRootSet(root);
        QSOptions options = TInterface.TOptionsCreate(
            Interface.TInterface.TPreferenceClone(TInterface.TPreferenceCurrentRead()), false);
        (_, FrameworkElement surface) = TOptionsPendingAttach(options, root,
            TInterface.TWorkspaceAnswerCreate((_, _, _) => { }, (_, _, _, _) => { }, (_, _) => { }, (_, _) => { }));
        return (options, surface, root, previous);
    }
    private static Assembly TOptionsPendingRead() => Assembly.LoadFrom(Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory,
        "../../../../../src/Cadroue.UIVeneer/bin/Debug/net10.0-windows10.0.19041.0/Cadroue.UIVeneer.dll")));
    private static object TOptionsPendingRead(string request, bool running = false) => request switch
    {
        "default" => "C:\\Default",
        "size" => 0L,
        "running" => running,
        _ => throw new NotSupportedException(request)
    };
}

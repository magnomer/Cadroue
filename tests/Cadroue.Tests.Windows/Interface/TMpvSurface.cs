using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;
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
public sealed class TMpvSurface : CSMpvPort, CSFlyleafPort, LUsherPort
{
    private readonly Action<Action> _tMpvSurface;
    private bool tMpvSurfaceInstalled;
    private Exception? tMpvSurfaceFailure;
    private TaskCompletionSource<(bool, string, bool)>? tMpvSurfaceResult;
    private TaskCompletionSource<bool>? tMpvSurfaceAvailability;
    private IProgress<double>? tMpvSurfaceProgress;
    private string? tMpvSurfaceOpened;
    private int tMpvSurfaceOpenings;
    private readonly List<string> tMpvSurfaceOrder = [];
    private readonly List<(bool, string, string)> tMpvSurfaceNotices = [];
    private QSMpv tMpvSurfaceDriver = null!;
    private UserControl tMpvSurfaceControl = null!;
    private Button tMpvSurfaceInstaller = null!;
    private Button tMpvSurfaceOpener = null!;
    private ProgressBar tMpvSurfaceBar = null!;

    public TMpvSurface()
    {
        _tMpvSurface = assertion =>
        {
            Exception? failure = null;
            var thread = new Thread(() =>
            {
                try
                {
                    Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
                    SynchronizationContext.SetSynchronizationContext(new DispatcherSynchronizationContext(dispatcher));
                    System.Windows.Application program = System.Windows.Application.Current
                        ?? new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                    program.Resources["PIconRoot"] = "pack://application:,,,/";
                    var resolvers = (Dictionary<bool, Func<string, double, string?>>)typeof(QIcon)
                        .GetField("qIconResolvers", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
                    Func<string, double, string?> resolver = resolvers[true];
                    resolvers[true] = (_, _) => null;
                    if (!program.Resources.Contains("PButtonWhite"))
                    {
                        string resource = Path.GetFullPath(Path.Combine(
                            AppContext.BaseDirectory,
                            "../../../../../src/Cadroue.UIVeneer/Kit/Button/PButton.xaml"));
                        using var reader = System.Xml.XmlReader.Create(new StringReader(File.ReadAllText(resource)));
                        program.Resources.MergedDictionaries.Add((ResourceDictionary)XamlReader.Load(reader));
                    }

                    var icons = (System.Collections.Concurrent.ConcurrentDictionary<string, ImageSource>)typeof(QIcon)
                        .GetField("qIconCache", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
                    Brush icon = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
                    icons[$"pack://application:,,,/PAsset/PPanel/POpen.svg|{icon}"] = new DrawingImage();

                    string path = Path.GetFullPath(Path.Combine(
                        AppContext.BaseDirectory,
                        "../../../../../src/Cadroue.UIVeneer/Annex/Options/System/PSMpv.xaml"));
                    string contract = File.ReadAllText(path)
                        .Replace(" x:Class=\"Cadroue.UIVeneer.PSMpv\"", string.Empty, StringComparison.Ordinal);
                    tMpvSurfaceControl = (UserControl)XamlReader.Parse(contract);
                    tMpvSurfaceInstaller = QContract.QContractFind<Button>(tMpvSurfaceControl, "PSMpvInstaller");
                    tMpvSurfaceOpener = QContract.QContractFind<Button>(tMpvSurfaceControl, "PSMpvOpener");
                    tMpvSurfaceBar = QContract.QContractFind<ProgressBar>(tMpvSurfaceControl, "PSMpvProgress");

                    LPreferenceState draft = Interface.TInterface.TPreferenceClone(TInterface.TPreferenceCurrentRead());
                    QSOptions options = TInterface.TOptionsCreate(draft, false, this, this, this);
                    tMpvSurfaceDriver = options.QSMpv;
                    tMpvSurfaceDriver.QSMpvAttach(
                        tMpvSurfaceControl,
                        new QSMpvAnswer(
                            options,
                            (title, message) =>
                            {
                                tMpvSurfaceOrder.Add("notice");
                                tMpvSurfaceNotices.Add((true, title, message));
                            },
                            (title, message) =>
                            {
                                tMpvSurfaceOrder.Add("notice");
                                tMpvSurfaceNotices.Add((false, title, message));
                            }));
                    try
                    {
                        assertion();
                    }
                    finally
                    {
                        tMpvSurfaceDriver.QSMpvDetach();
                        resolvers[true] = resolver;
                    }
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
        };
    }

    [Fact]
    public void Attach_ProjectsInitialLabelsAndOpenRoots()
    {
        _tMpvSurface(() =>
        {
            Assert.Equal(
                QLocalizationCatalog.QLocalizationTextRead("Options.System.DownloadMpv"),
                tMpvSurfaceInstaller.Content);
            tMpvSurfaceOpener.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, tMpvSurfaceOpener));
            Assert.Equal(LMpvInstall.LMpvRootRead(), tMpvSurfaceOpened);
            Assert.Equal(1, tMpvSurfaceOpenings);

            var answer = new QSMpvAnswer(
                () => tMpvSurfaceOpenings++,
                (_, _) => { },
                (_, _) => { });
            tMpvSurfaceInstalled = true;
            tMpvSurfaceDriver.QSMpvAttach(tMpvSurfaceControl, answer);
            tMpvSurfaceDriver.QSMpvAttach(tMpvSurfaceControl, answer);
            Assert.Equal(
                QLocalizationCatalog.QLocalizationTextRead("Options.System.ReinstallMpv"),
                tMpvSurfaceInstaller.Content);
            tMpvSurfaceOpener.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, tMpvSurfaceOpener));
            Assert.Equal(2, tMpvSurfaceOpenings);

            tMpvSurfaceDriver.QSMpvDetach();
            tMpvSurfaceOpener.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, tMpvSurfaceOpener));
            Assert.Equal(2, tMpvSurfaceOpenings);
        });
    }

    [Fact]
    public void Mpv_RestoresIdleBeforeAvailability()
    {
        _tMpvSurface(() =>
        {
            tMpvSurfaceDriver.QSMpvChanged += _ => tMpvSurfaceOrder.Add("availability");
            tMpvSurfaceResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
            tMpvSurfaceAvailability = new(TaskCreationOptions.RunContinuationsAsynchronously);
            Task success = tMpvSurfaceDriver.QSMpvStart();
            Assert.False(tMpvSurfaceInstaller.IsEnabled);
            Assert.Equal(0, tMpvSurfaceBar.Value);
            Assert.Equal(Visibility.Visible, tMpvSurfaceBar.Visibility);

            tMpvSurfaceInstalled = true;
            tMpvSurfaceResult.SetResult((true, string.Empty, true));
            while (!tMpvSurfaceOrder.Contains("check"))
            {
                var frame = new DispatcherFrame();
                Dispatcher.CurrentDispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() => frame.Continue = false));
                Dispatcher.PushFrame(frame);
            }
            Assert.True(tMpvSurfaceInstaller.IsEnabled);
            Assert.Equal(Visibility.Collapsed, tMpvSurfaceBar.Visibility);
            Assert.Equal(
                QLocalizationCatalog.QLocalizationTextRead("Options.System.ReinstallMpv"),
                tMpvSurfaceInstaller.Content);
            Assert.Empty(tMpvSurfaceNotices);
            tMpvSurfaceAvailability.SetResult(true);
            while (!success.IsCompleted)
            {
                var frame = new DispatcherFrame();
                Dispatcher.CurrentDispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() => frame.Continue = false));
                Dispatcher.PushFrame(frame);
            }
            success.GetAwaiter().GetResult();
            Assert.True(tMpvSurfaceInstaller.IsEnabled);
            Assert.Equal(Visibility.Collapsed, tMpvSurfaceBar.Visibility);
            Assert.True(tMpvSurfaceDriver.QSMpvEnabled);
            Assert.Equal(new[] { "check", "availability", "notice" }, tMpvSurfaceOrder);
            Assert.Equal((
                true,
                QLocalizationCatalog.QLocalizationTextRead("Options.System.LocalMpv"),
                QLocalizationCatalog.QLocalizationTextRead("Mpv.Local.Install.Completed")),
                tMpvSurfaceNotices.Single());

            tMpvSurfaceOrder.Clear();
            tMpvSurfaceNotices.Clear();
            tMpvSurfaceInstalled = false;
            tMpvSurfaceResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
            tMpvSurfaceAvailability = new(TaskCreationOptions.RunContinuationsAsynchronously);
            Task returned = tMpvSurfaceDriver.QSMpvStart();
            tMpvSurfaceResult.SetResult((false, "forced failure", false));
            while (!tMpvSurfaceOrder.Contains("check"))
            {
                var frame = new DispatcherFrame();
                Dispatcher.CurrentDispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() => frame.Continue = false));
                Dispatcher.PushFrame(frame);
            }
            Assert.Empty(tMpvSurfaceNotices);
            tMpvSurfaceAvailability.SetResult(false);
            while (!returned.IsCompleted)
            {
                var frame = new DispatcherFrame();
                Dispatcher.CurrentDispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() => frame.Continue = false));
                Dispatcher.PushFrame(frame);
            }
            returned.GetAwaiter().GetResult();
            Assert.False(tMpvSurfaceDriver.QSMpvEnabled);
            Assert.Equal(new[] { "check", "availability", "notice" }, tMpvSurfaceOrder);
            Assert.Equal((
                false,
                QLocalizationCatalog.QLocalizationTextRead("Options.System.LocalMpv"),
                QLocalizationCatalog.QLocalizationFormat("Mpv.Local.Install.Failed", "forced failure")),
                tMpvSurfaceNotices.Single());
        });
    }

    [Fact]
    public void Close_DetachesMpvProjection()
    {
        string draftPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/Cadroue.UIVeneer/Annex/Options/PSOptionsDraft.cs"));
        string optionsPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/Cadroue.UIVeneer/Annex/Options/PSOptions.cs"));
        string draft = File.ReadAllText(draftPath);
        string options = File.ReadAllText(optionsPath);

        Assert.Contains("lsOptions.QSMpv.QSMpvChanged += psOptionsDraftMpv;", draft, StringComparison.Ordinal);
        Assert.Contains("lsOptions.QSMpv.QSMpvChanged -= psOptionsDraftMpv;", draft, StringComparison.Ordinal);
        Assert.Contains("psOptionsDraft.PSOptionsDraftDetach();", options, StringComparison.Ordinal);
    }

    [Fact]
    public void Start_DetachBeforeCompletion_DoesNotUpdateSurface()
    {
        _tMpvSurface(() =>
        {
            tMpvSurfaceResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
            tMpvSurfaceAvailability = new(TaskCreationOptions.RunContinuationsAsynchronously);
            Task operation = tMpvSurfaceDriver.QSMpvStart();
            tMpvSurfaceProgress!.Report(0.7);
            tMpvSurfaceDriver.QSMpvDetach();
            tMpvSurfaceProgress!.Report(0.9);
            tMpvSurfaceResult.SetResult((true, string.Empty, true));
            tMpvSurfaceAvailability.SetResult(true);
            while (!operation.IsCompleted)
            {
                var frame = new DispatcherFrame();
                Dispatcher.CurrentDispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() => frame.Continue = false));
                Dispatcher.PushFrame(frame);
            }
            operation.GetAwaiter().GetResult();

            Assert.Equal(0, tMpvSurfaceBar.Value);
            Assert.Equal(Visibility.Visible, tMpvSurfaceBar.Visibility);
            Assert.False(tMpvSurfaceDriver.QSMpvEnabled);
            Assert.Empty(tMpvSurfaceNotices);
        });
    }

    [Fact]
    public void Start_ReattachBeforeCompletion_DoesNotUpdateReplacementSurface()
    {
        _tMpvSurface(() =>
        {
            tMpvSurfaceResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
            tMpvSurfaceAvailability = new(TaskCreationOptions.RunContinuationsAsynchronously);
            UserControl replacement = (UserControl)XamlReader.Parse(XamlWriter.Save(tMpvSurfaceControl));
            Assert.NotSame(tMpvSurfaceControl, replacement);
            Button replacementInstaller = QContract.QContractFind<Button>(replacement, "PSMpvInstaller");
            ProgressBar replacementProgress = QContract.QContractFind<ProgressBar>(replacement, "PSMpvProgress");
            int replacementNotices = 0;
            Task operation = tMpvSurfaceDriver.QSMpvStart();
            tMpvSurfaceProgress!.Report(0.7);
            tMpvSurfaceDriver.QSMpvAttach(
                replacement,
                new QSMpvAnswer(() => { }, (_, _) => replacementNotices++, (_, _) => replacementNotices++));
            tMpvSurfaceProgress!.Report(0.9);
            tMpvSurfaceResult.SetResult((true, string.Empty, true));
            tMpvSurfaceAvailability.SetResult(true);
            while (!operation.IsCompleted)
            {
                var frame = new DispatcherFrame();
                Dispatcher.CurrentDispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() => frame.Continue = false));
                Dispatcher.PushFrame(frame);
            }
            operation.GetAwaiter().GetResult();

            Assert.Equal(QLocalizationCatalog.QLocalizationTextRead("Options.System.DownloadMpv"),
                tMpvSurfaceInstaller.Content);
            Assert.Equal(0, tMpvSurfaceBar.Value);
            Assert.True(replacementInstaller.IsEnabled);
            Assert.Equal(Visibility.Collapsed, replacementProgress.Visibility);
            Assert.Equal(QLocalizationCatalog.QLocalizationTextRead("Options.System.DownloadMpv"),
                replacementInstaller.Content);
            Assert.Equal(0, replacementProgress.Value);
            Assert.False(tMpvSurfaceDriver.QSMpvEnabled);
            Assert.Empty(tMpvSurfaceNotices);
            Assert.Equal(0, replacementNotices);
        });
    }

    [Fact]
    public void Start_AttachedFailure_PreservesException()
    {
        _tMpvSurface(() =>
        {
            try
            {
                throw new IOException("forced throw");
            }
            catch (IOException failure)
            {
                tMpvSurfaceFailure = failure;
            }
            string stack = tMpvSurfaceFailure.StackTrace!;
            Task operation = tMpvSurfaceDriver.QSMpvStart();
            while (!operation.IsCompleted)
            {
                var frame = new DispatcherFrame();
                Dispatcher.CurrentDispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() => frame.Continue = false));
                Dispatcher.PushFrame(frame);
            }

            IOException exception = Assert.Throws<IOException>(() => operation.GetAwaiter().GetResult());
            Assert.Same(tMpvSurfaceFailure, exception);
            Assert.Contains(stack, exception.StackTrace, StringComparison.Ordinal);
            Assert.Equal(Visibility.Collapsed, tMpvSurfaceBar.Visibility);
            Assert.Empty(tMpvSurfaceNotices);
        });
    }

    bool CSMpvPort.CSMpvInstalledCheck() => tMpvSurfaceInstalled;

    async Task<bool> CSMpvPort.CSMpvAvailabilityCheck()
    {
        tMpvSurfaceOrder.Add("check");
        return await tMpvSurfaceAvailability!.Task;
    }

    Task<(bool, string, bool)> CSMpvPort.CSMpvStart(IProgress<double> progress)
    {
        tMpvSurfaceProgress = progress;
        return tMpvSurfaceFailure is null
            ? tMpvSurfaceResult!.Task
            : Task.FromException<(bool, string, bool)>(tMpvSurfaceFailure);
    }

    bool CSFlyleafPort.CSFlyleafInstalledCheck() => false;

    Task<(bool, string, bool)> CSFlyleafPort.CSFlyleafStart(IProgress<double> progress) =>
        throw new NotSupportedException();

    bool LUsherPort.LUsherFileExist(string? lPath) => throw new NotSupportedException();

    bool LUsherPort.LUsherFolderExist(string? lFolder) => throw new NotSupportedException();

    string LUsherPort.LUsherNameRead(string lPath) => throw new NotSupportedException();

    string LUsherPort.LUsherStemRead(string lPath) => throw new NotSupportedException();

    string LUsherPort.LUsherProgramResolve(string lName) => throw new NotSupportedException();

    string? LUsherPort.LUsherPathOpen(string lPath, string? lFallback, string? lFailure) =>
        throw new NotSupportedException();

    string? LUsherPort.LUsherFolderOpen(string lFolder)
    {
        tMpvSurfaceOpened = lFolder;
        tMpvSurfaceOpenings++;
        return null;
    }

    string? LUsherPort.LUsherLinkOpen(string lTarget, string? lFailure) => throw new NotSupportedException();
}

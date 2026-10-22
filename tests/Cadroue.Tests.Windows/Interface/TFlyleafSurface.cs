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
public sealed class TFlyleafSurface : CSFlyleafPort, CSMpvPort, LUsherPort
{
    private readonly Action<Action> _tFlyleafSurface;
    private bool tFlyleafSurfaceInstalled;
    private Exception? tFlyleafSurfaceFailure;
    private TaskCompletionSource<(bool, string, bool)>? tFlyleafSurfaceResult;
    private IProgress<double>? tFlyleafSurfaceProgress;
    private string? tFlyleafSurfaceOpened;
    private int tFlyleafSurfaceOpenings;
    private readonly List<(bool, string, string)> tFlyleafSurfaceNotices = [];
    private QSFlyleaf tFlyleafSurfaceDriver = null!;
    private UserControl tFlyleafSurfaceControl = null!;
    private Button tFlyleafSurfaceInstaller = null!;
    private Button tFlyleafSurfaceOpener = null!;
    private ProgressBar tFlyleafSurfaceBar = null!;

    public TFlyleafSurface()
    {
        _tFlyleafSurface = assertion =>
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
                        "../../../../../src/Cadroue.UIVeneer/Annex/Options/System/PSFlyleaf.xaml"));
                    string contract = File.ReadAllText(path)
                        .Replace(" x:Class=\"Cadroue.UIVeneer.PSFlyleaf\"", string.Empty, StringComparison.Ordinal);
                    tFlyleafSurfaceControl = (UserControl)XamlReader.Parse(contract);
                    tFlyleafSurfaceInstaller =
                        QContract.QContractFind<Button>(tFlyleafSurfaceControl, "PSFlyleafInstaller");
                    tFlyleafSurfaceOpener =
                        QContract.QContractFind<Button>(tFlyleafSurfaceControl, "PSFlyleafOpener");
                    tFlyleafSurfaceBar =
                        QContract.QContractFind<ProgressBar>(tFlyleafSurfaceControl, "PSFlyleafProgress");

                    LPreferenceState draft = Interface.TInterface.TPreferenceClone(TInterface.TPreferenceCurrentRead());
                    QSOptions options = TInterface.TOptionsCreate(draft, false, this, this, this);
                    tFlyleafSurfaceDriver = options.QSFlyleaf;
                    tFlyleafSurfaceDriver.QSFlyleafAttach(
                        tFlyleafSurfaceControl,
                        new QSFlyleafAnswer(
                            options,
                            (title, message) => tFlyleafSurfaceNotices.Add((true, title, message)),
                            (title, message) => tFlyleafSurfaceNotices.Add((false, title, message))));
                    try
                    {
                        assertion();
                    }
                    finally
                    {
                        tFlyleafSurfaceDriver.QSFlyleafDetach();
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
        _tFlyleafSurface(() =>
        {
            Assert.Equal(
                QLocalizationCatalog.QLocalizationTextRead("Options.System.InstallFlyleaf"),
                tFlyleafSurfaceInstaller.Content);
            tFlyleafSurfaceOpener.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, tFlyleafSurfaceOpener));
            Assert.Equal(LFlyleafFolder.LFlyleafRootRead(), tFlyleafSurfaceOpened);
            Assert.Equal(1, tFlyleafSurfaceOpenings);

            tFlyleafSurfaceInstalled = true;
            tFlyleafSurfaceDriver.QSFlyleafDetach();
            LPreferenceState draft = Interface.TInterface.TPreferenceClone(TInterface.TPreferenceCurrentRead());
            QSOptions options = TInterface.TOptionsCreate(draft, false, this, this, this);
            tFlyleafSurfaceDriver.QSFlyleafAttach(
                tFlyleafSurfaceControl,
                new QSFlyleafAnswer(options, (_, _) => { }, (_, _) => { }));
            Assert.Equal(
                QLocalizationCatalog.QLocalizationTextRead("Options.System.ReinstallFlyleaf"),
                tFlyleafSurfaceInstaller.Content);
        });
    }

    [Fact]
    public void Install_ProjectsProgressAndSuccess()
    {
        _tFlyleafSurface(() =>
        {
            tFlyleafSurfaceResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
            Task install = tFlyleafSurfaceDriver.QSFlyleafStart();
            Assert.False(tFlyleafSurfaceInstaller.IsEnabled);
            Assert.Equal(0, tFlyleafSurfaceBar.Value);
            Assert.Equal(Visibility.Visible, tFlyleafSurfaceBar.Visibility);

            tFlyleafSurfaceProgress!.Report(0.6);
            var progressFrame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() => progressFrame.Continue = false));
            Dispatcher.PushFrame(progressFrame);
            Assert.Equal(0.6, tFlyleafSurfaceBar.Value);

            tFlyleafSurfaceInstalled = true;
            tFlyleafSurfaceResult.SetResult((true, string.Empty, true));
            while (!install.IsCompleted)
            {
                var frame = new DispatcherFrame();
                Dispatcher.CurrentDispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() => frame.Continue = false));
                Dispatcher.PushFrame(frame);
            }
            install.GetAwaiter().GetResult();

            Assert.True(tFlyleafSurfaceInstaller.IsEnabled);
            Assert.Equal(Visibility.Collapsed, tFlyleafSurfaceBar.Visibility);
            Assert.Equal(
                QLocalizationCatalog.QLocalizationTextRead("Options.System.ReinstallFlyleaf"),
                tFlyleafSurfaceInstaller.Content);
            Assert.Equal((
                true,
                QLocalizationCatalog.QLocalizationTextRead("Options.System.LocalFlyleaf"),
                QLocalizationCatalog.QLocalizationTextRead("Flyleaf.Local.Install.Completed")),
                tFlyleafSurfaceNotices.Single());
        });
    }

    [Fact]
    public void Start_AttachedFailure_PreservesException()
    {
        _tFlyleafSurface(() =>
        {
            tFlyleafSurfaceResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
            Task returned = tFlyleafSurfaceDriver.QSFlyleafStart();
            tFlyleafSurfaceResult.SetResult((false, "forced failure", false));
            while (!returned.IsCompleted)
            {
                var frame = new DispatcherFrame();
                Dispatcher.CurrentDispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() => frame.Continue = false));
                Dispatcher.PushFrame(frame);
            }
            returned.GetAwaiter().GetResult();
            Assert.True(tFlyleafSurfaceInstaller.IsEnabled);
            Assert.Equal(Visibility.Collapsed, tFlyleafSurfaceBar.Visibility);
            Assert.Equal((
                false,
                QLocalizationCatalog.QLocalizationTextRead("Options.System.LocalFlyleaf"),
                QLocalizationCatalog.QLocalizationFormat("Flyleaf.Local.Install.Failed", "forced failure")),
                tFlyleafSurfaceNotices.Single());

            try
            {
                throw new IOException("forced throw");
            }
            catch (IOException failure)
            {
                tFlyleafSurfaceFailure = failure;
            }
            string stack = tFlyleafSurfaceFailure.StackTrace!;
            Task thrown = tFlyleafSurfaceDriver.QSFlyleafStart();
            while (!thrown.IsCompleted)
            {
                var frame = new DispatcherFrame();
                Dispatcher.CurrentDispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() => frame.Continue = false));
                Dispatcher.PushFrame(frame);
            }
            IOException exception = Assert.Throws<IOException>(() => thrown.GetAwaiter().GetResult());
            Assert.Same(tFlyleafSurfaceFailure, exception);
            Assert.Equal("forced throw", exception.Message);
            Assert.Contains(stack, exception.StackTrace, StringComparison.Ordinal);
            Assert.False(tFlyleafSurfaceInstaller.IsEnabled);
            Assert.Equal(Visibility.Collapsed, tFlyleafSurfaceBar.Visibility);
            Assert.Single(tFlyleafSurfaceNotices);
        });
    }

    [Fact]
    public void Failure_RestoresSurfaceAndPreservesThrow() => Start_AttachedFailure_PreservesException();

    [Fact]
    public void Reattach_DoesNotDuplicateHandlers()
    {
        _tFlyleafSurface(() =>
        {
            var answer = new QSFlyleafAnswer(
                () => tFlyleafSurfaceOpenings++,
                (_, _) => { },
                (_, _) => { });
            tFlyleafSurfaceDriver.QSFlyleafAttach(tFlyleafSurfaceControl, answer);
            tFlyleafSurfaceDriver.QSFlyleafAttach(tFlyleafSurfaceControl, answer);
            tFlyleafSurfaceOpener.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, tFlyleafSurfaceOpener));
            Assert.Equal(1, tFlyleafSurfaceOpenings);

            tFlyleafSurfaceDriver.QSFlyleafDetach();
            tFlyleafSurfaceOpener.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, tFlyleafSurfaceOpener));
            Assert.Equal(1, tFlyleafSurfaceOpenings);
        });
    }

    [Fact]
    public void Start_DetachBeforeCompletion_DoesNotUpdateSurface()
    {
        _tFlyleafSurface(() =>
        {
            tFlyleafSurfaceResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
            Task operation = tFlyleafSurfaceDriver.QSFlyleafStart();
            tFlyleafSurfaceProgress!.Report(0.7);
            tFlyleafSurfaceDriver.QSFlyleafDetach();
            tFlyleafSurfaceProgress!.Report(0.9);
            tFlyleafSurfaceResult.SetResult((true, string.Empty, true));

            while (!operation.IsCompleted)
            {
                var frame = new DispatcherFrame();
                Dispatcher.CurrentDispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() => frame.Continue = false));
                Dispatcher.PushFrame(frame);
            }
            operation.GetAwaiter().GetResult();
            var completionFrame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(
                DispatcherPriority.Background,
                new Action(() => completionFrame.Continue = false));
            Dispatcher.PushFrame(completionFrame);

            Assert.False(tFlyleafSurfaceInstaller.IsEnabled);
            Assert.Equal(Visibility.Visible, tFlyleafSurfaceBar.Visibility);
            Assert.Equal(QLocalizationCatalog.QLocalizationTextRead("Options.System.InstallFlyleaf"),
                tFlyleafSurfaceInstaller.Content);
            Assert.Equal(0, tFlyleafSurfaceBar.Value);
            Assert.Empty(tFlyleafSurfaceNotices);
        });
    }

    [Fact]
    public void Detach_SilencesQueuedProgressAndCompletion() =>
        Start_DetachBeforeCompletion_DoesNotUpdateSurface();

    [Fact]
    public void Reattach_DoesNotRedirectActiveOperation()
    {
        _tFlyleafSurface(() =>
        {
            tFlyleafSurfaceResult = new(TaskCreationOptions.RunContinuationsAsynchronously);
            Task operation = tFlyleafSurfaceDriver.QSFlyleafStart();
            tFlyleafSurfaceDriver.QSFlyleafDetach();
            tFlyleafSurfaceInstalled = false;
            tFlyleafSurfaceDriver.QSFlyleafAttach(
                tFlyleafSurfaceControl,
                new QSFlyleafAnswer(
                    () => { },
                    (title, message) => tFlyleafSurfaceNotices.Add((true, title, message)),
                    (title, message) => tFlyleafSurfaceNotices.Add((false, title, message))));
            tFlyleafSurfaceResult.SetResult((true, string.Empty, true));
            while (!operation.IsCompleted)
            {
                var frame = new DispatcherFrame();
                Dispatcher.CurrentDispatcher.BeginInvoke(
                    DispatcherPriority.Background,
                    new Action(() => frame.Continue = false));
                Dispatcher.PushFrame(frame);
            }
            operation.GetAwaiter().GetResult();

            Assert.Equal(
                QLocalizationCatalog.QLocalizationTextRead("Options.System.InstallFlyleaf"),
                tFlyleafSurfaceInstaller.Content);
            Assert.Empty(tFlyleafSurfaceNotices);
        });
    }

    bool CSFlyleafPort.CSFlyleafInstalledCheck() => tFlyleafSurfaceInstalled;

    Task<(bool, string, bool)> CSFlyleafPort.CSFlyleafStart(IProgress<double> progress)
    {
        tFlyleafSurfaceProgress = progress;
        return tFlyleafSurfaceFailure is null
            ? tFlyleafSurfaceResult!.Task
            : Task.FromException<(bool, string, bool)>(tFlyleafSurfaceFailure);
    }

    bool CSMpvPort.CSMpvInstalledCheck() => false;

    Task<bool> CSMpvPort.CSMpvAvailabilityCheck() => Task.FromResult(false);

    Task<(bool, string, bool)> CSMpvPort.CSMpvStart(IProgress<double> progress) =>
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
        tFlyleafSurfaceOpened = lFolder;
        tFlyleafSurfaceOpenings++;
        return null;
    }

    string? LUsherPort.LUsherLinkOpen(string lTarget, string? lFailure) => throw new NotSupportedException();
}

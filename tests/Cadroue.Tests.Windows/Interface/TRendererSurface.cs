using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.ShellEngine;
using Cadroue.UIDeportment;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;

using Xunit;

namespace Cadroue.Tests.Windows;

[Collection("Preference")]
public sealed class TRendererSurface : CSRendererPort, LUsherPort
{
    private (bool, bool) tRendererSurfaceFact = (false, false);
    private int tRendererSurfaceReads;

    [Fact]
    public void QSRendererAttach()
    {
        Exception? failure = null;
        var thread = new Thread(() =>
        {
            Dictionary<bool, Func<string, double, string?>>? resolvers = null;
            Func<string, double, string?>? resolver = null;
            Window? host = null;
            try
            {
                System.Windows.Application program = System.Windows.Application.Current
                    ?? new System.Windows.Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
                program.Resources["PToolIcon"] = Brushes.Black;
                program.Resources["PIconRoot"] = "pack://application:,,,/";
                foreach (string sheet in new[] { "Kit/Button/PButton", "Kit/PTextbox" })
                {
                    string resource = Path.GetFullPath(Path.Combine(
                        AppContext.BaseDirectory, $"../../../../../src/Cadroue.UIVeneer/{sheet}.xaml"));
                    using var reader = System.Xml.XmlReader.Create(new StringReader(File.ReadAllText(resource)));
                    program.Resources.MergedDictionaries.Add((ResourceDictionary)XamlReader.Load(reader));
                }

                resolvers = (Dictionary<bool, Func<string, double, string?>>)typeof(QIcon)
                    .GetField("qIconResolvers", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
                resolver = resolvers[true];
                resolvers[true] = (_, _) => null;
                var icons = (System.Collections.Concurrent.ConcurrentDictionary<string, ImageSource>)typeof(QIcon)
                    .GetField("qIconCache", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
                Brush iconBrush = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
                foreach (string icon in new[] { "PBrowse.svg", "POpen.svg", "PDiagnosis.svg" })
                {
                    icons[$"pack://application:,,,/PAsset/PPanel/{icon}|{iconBrush}"] =
                        new DrawingImage();
                }

                string path = Path.GetFullPath(Path.Combine(
                    AppContext.BaseDirectory,
                    "../../../../../src/Cadroue.UIVeneer/Annex/Options/System/PSRenderer.xaml"));
                string contract = File.ReadAllText(path)
                    .Replace(" x:Class=\"Cadroue.UIVeneer.PSRenderer\"", string.Empty, StringComparison.Ordinal);
                var surface = (UserControl)XamlReader.Parse(contract);
                host = new Window
                {
                    Content = surface,
                    Width = 700,
                    Height = 240,
                    ShowActivated = false,
                    ShowInTaskbar = false
                };
                host.Show();

                string? folder = null;
                string? opened = null;
                int folders = 0;
                int diagnoses = 0;
                var answer = new QSRendererAnswer(
                    _ => { folders++; return folder; },
                    value => opened = value,
                    () => diagnoses++);
                var renderer = new QSRenderer("Original", this);

                renderer.QSRendererAttach(surface, answer);
                renderer.QSRendererAttach(surface, answer);
                var root = (StackPanel)surface.Content;
                var row = (Grid)root.Children[1];
                var controls = (StackPanel)row.Children[1];
                TextBox pathBox = QContract.QContractFind<TextBox>(surface, "PSRendererPath");
                TextBlock status = QContract.QContractFind<TextBlock>(surface, "PSRendererStatus");
                Button finder = QContract.QContractFind<Button>(surface, "PSRendererFinder");
                Button opener = QContract.QContractFind<Button>(surface, "PSRendererOpener");
                Button examiner = QContract.QContractFind<Button>(surface, "PSRendererExaminer");
                Assert.Equal(
                    QLocalizationCatalog.QLocalizationTextRead("Options.System.FFmpeg"),
                    ((TextBlock)root.Children[0]).Text);
                Assert.Equal(
                    QLocalizationCatalog.QLocalizationTextRead("Options.System.Location"),
                    ((TextBlock)row.Children[0]).Text);
                Assert.Equal("Original", pathBox.Text);
                Assert.Equal(320, pathBox.Width);
                Assert.Equal(32, pathBox.Height);
                Assert.Equal(new Thickness(130, -7, 0, 9), status.Margin);
                Assert.Equal(
                    QLocalizationCatalog.QLocalizationTextRead("Options.System.FFmpegMissing"), status.Text);
                Assert.Equal(4, controls.Children.Count);
                Assert.Equal(40, finder.Width);
                Assert.Equal(32, finder.Height);
                Assert.Equal(new Thickness(8, 0, 0, 0), finder.Margin);
                Assert.Equal(new Thickness(6, 0, 0, 0), opener.Margin);
                Assert.Equal(new Thickness(6, 0, 0, 0), examiner.Margin);
                Assert.Equal(QLocalizationCatalog.QLocalizationTextRead("Options.System.Browse"), finder.ToolTip);
                Assert.Equal(QLocalizationCatalog.QLocalizationTextRead("Options.System.Open"), opener.ToolTip);
                Assert.Equal(QLocalizationCatalog.QLocalizationTextRead("Options.System.Diagnosis"), examiner.ToolTip);
                Assert.Same(
                    icons[$"pack://application:,,,/PAsset/PPanel/PBrowse.svg|{iconBrush}"],
                    ((Image)finder.Content).Source);
                Assert.Same(
                    icons[$"pack://application:,,,/PAsset/PPanel/POpen.svg|{iconBrush}"],
                    ((Image)opener.Content).Source);
                Assert.Same(
                    icons[$"pack://application:,,,/PAsset/PPanel/PDiagnosis.svg|{iconBrush}"],
                    ((Image)examiner.Content).Source);

                int reads = tRendererSurfaceReads;
                pathBox.Text = "  ";
                Assert.Equal(reads, tRendererSurfaceReads);
                Assert.Equal(
                    QLocalizationCatalog.QLocalizationTextRead("Options.System.FFmpegBlank"), status.Text);

                ((bool, bool), CSRendererState, string)[] states =
                [
                    ((true, true), CSRendererState.CSRendererStateReady,
                        "Options.System.FFmpegReady"),
                    ((true, false), CSRendererState.CSRendererStateProgram,
                        "Options.System.FFmpegProgramOnly"),
                    ((false, true), CSRendererState.CSRendererStateLibrary,
                        "Options.System.FFmpegLibraryOnly"),
                    ((false, false), CSRendererState.CSRendererStateMissing,
                        "Options.System.FFmpegMissing")
                ];
                foreach (var state in states)
                {
                    tRendererSurfaceFact = state.Item1;
                    int before = tRendererSurfaceReads;
                    pathBox.Text = Guid.NewGuid().ToString("N");
                    Assert.Equal(before + 1, tRendererSurfaceReads);
                    Assert.Equal(QLocalizationCatalog.QLocalizationTextRead(state.Item3), status.Text);
                }

                LPreferenceState draft = Interface.TInterface.TPreferenceClone(TInterface.TPreferenceCurrentRead());
                draft.LPreferenceFfmpegFolder = "Original";
                pathBox.Text = " raw ";
                Assert.Equal("Original", draft.LPreferenceFfmpegFolder);
                Assert.Equal(" raw ", renderer.QSRendererApply());
                Assert.Equal("Original", draft.LPreferenceFfmpegFolder);

                int readsBeforeCancel = tRendererSurfaceReads;
                finder.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, finder));
                Assert.Equal(1, folders);
                Assert.Equal(" raw ", pathBox.Text);
                Assert.Equal(readsBeforeCancel, tRendererSurfaceReads);

                folder = "accepted";
                finder.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, finder));
                Assert.Equal(2, folders);
                Assert.Equal("accepted", pathBox.Text);
                Assert.Equal(readsBeforeCancel + 1, tRendererSurfaceReads);

                opener.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, opener));
                examiner.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, examiner));
                Assert.Equal("accepted", opened);
                Assert.Equal(1, diagnoses);

                string statusBeforeFailure = status.Text;
                IOException readinessFailure = Assert.Throws<IOException>(() => pathBox.Text = "new raw");
                Assert.Equal("forced readiness failure", readinessFailure.Message);
                Assert.Equal("new raw", pathBox.Text);
                Assert.Equal("new raw", renderer.QSRendererApply());
                Assert.Equal("Original", draft.LPreferenceFfmpegFolder);
                Assert.Equal(statusBeforeFailure, status.Text);

                renderer.QSRendererDetach();
                int readsAfterDetach = tRendererSurfaceReads;
                int foldersAfterDetach = folders;
                pathBox.Text = "detached";
                finder.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, finder));
                opener.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, opener));
                examiner.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, examiner));
                Assert.Equal(readsAfterDetach, tRendererSurfaceReads);
                Assert.Equal(foldersAfterDetach, folders);
                Assert.Equal("accepted", opened);
                Assert.Equal(1, diagnoses);

                Window? diagnosisOwner = null;
                QSDiagnosis? diagnosisResult = null;
                QSOptions options = TInterface.TOptionsCreate(draft, false, this);
                var productionAnswer = new QSRendererAnswer(
                    options,
                    host,
                    (owner, factory) =>
                    {
                        diagnosisOwner = owner;
                        diagnosisResult = factory();
                    });
                var productionRenderer = new QSRenderer("  ");
                productionRenderer.QSRendererAttach(surface, productionAnswer);
                opener.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, opener));
                examiner.RaiseEvent(new RoutedEventArgs(Button.ClickEvent, examiner));
                Assert.Same(host, diagnosisOwner);
                Assert.IsType<QSDiagnosis>(diagnosisResult);
                productionRenderer.QSRendererDetach();
            }
            catch (Exception exception)
            {
                failure = exception;
            }
            finally
            {
                if (resolvers is not null && resolver is not null)
                {
                    resolvers[true] = resolver;
                }

                host?.Close();
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();
        Assert.Null(failure);
    }

    bool CSRendererPort.CSRendererProgramCheck(string path)
    {
        tRendererSurfaceReads++;
        if (path == "new raw")
        {
            throw new IOException("forced readiness failure");
        }

        return tRendererSurfaceFact.Item1;
    }

    bool CSRendererPort.CSRendererLibraryCheck(string path) => tRendererSurfaceFact.Item2;

    bool LUsherPort.LUsherFileExist(string? lPath) => throw new NotSupportedException();

    bool LUsherPort.LUsherFolderExist(string? lFolder) => throw new NotSupportedException();

    string LUsherPort.LUsherNameRead(string lPath) => throw new NotSupportedException();

    string LUsherPort.LUsherStemRead(string lPath) => throw new NotSupportedException();

    string LUsherPort.LUsherProgramResolve(string lName) => throw new NotSupportedException();

    string? LUsherPort.LUsherPathOpen(string lPath, string? lFallback, string? lFailure) =>
        throw new NotSupportedException();

    string? LUsherPort.LUsherFolderOpen(string lFolder)
    {
        Assert.Equal(string.Empty, lFolder);
        return "forced opener failure";
    }

    string? LUsherPort.LUsherLinkOpen(string lTarget, string? lFailure) => throw new NotSupportedException();
}

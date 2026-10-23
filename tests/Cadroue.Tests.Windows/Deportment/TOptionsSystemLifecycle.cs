using Cadroue.Application;
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
public sealed class TOptionsSystemLifecycle
{
    [Fact]
    public void FileRecordClear_DelayedAnswerAfterDetach_IsDisarmed() => TOptionsSystemRun(() =>
    {
        using var sidecar = new Interface.TSidecar();
        string source = sidecar.TSourceCreate("source.mp4", "source");
        sidecar.TSidecarTextSave(source, "{}");
        string record = sidecar.TSidecarPathRead(source);
        Action? confirmation = null;
        int announcements = 0;
        QSOptions options = TInterface.TOptionsCreate(
            Interface.TInterface.TPreferenceClone(TInterface.TPreferenceCurrentRead()), false);
        string assemblyPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "../../../../../src/Cadroue.UIVeneer/bin/Debug/net10.0-windows10.0.19041.0/Cadroue.UIVeneer.dll"));
        var surface = (FrameworkElement)Activator.CreateInstance(
            Assembly.LoadFrom(assemblyPath).GetType("Cadroue.UIVeneer.PSOptionsSystem", true)!, true)!;
        options.QSOptionsSystem.QSOptionsSystemAttach(
            surface,
            TInterface.TWorkspaceAnswerCreate(
                (_, _, _) => { },
                (_, _, _, continuation) => confirmation = continuation,
                (_, _) => { },
                (_, _) => announcements++),
            new QSRendererAnswer(_ => null, _ => { }, () => { }),
            (string selected, Action _, out Action<string, bool> enable, LLocalizationChoice[] _) =>
            {
                enable = (_, _) => { };
                return new Border { Tag = selected };
            },
            border => (string)border.Tag);

        QContract.QContractFind<Button>(surface, "PSSystemEraser")
            .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        Assert.NotNull(confirmation);
        options.QSOptionsDetach();

        confirmation!();

        Assert.True(File.Exists(record));
        Assert.Equal(0, announcements);
    });

    private static void TOptionsSystemRun(Action assertion)
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
                foreach (string sheet in new[]
                         {
                             "Kit/Button/PButton", "Kit/PCheckbox", "Kit/PSlider", "Kit/PTextbox"
                         })
                {
                    string resource = Path.GetFullPath(Path.Combine(
                        AppContext.BaseDirectory, $"../../../../../src/Cadroue.UIVeneer/{sheet}.xaml"));
                    using var reader = System.Xml.XmlReader.Create(new StringReader(File.ReadAllText(resource)));
                    program.Resources.MergedDictionaries.Add((ResourceDictionary)XamlReader.Load(reader));
                }
                var icons = (System.Collections.Concurrent.ConcurrentDictionary<string, ImageSource>)typeof(QIcon)
                    .GetField("qIconCache", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
                Brush icon = new SolidColorBrush(Color.FromRgb(0x1D, 0x2A, 0x3D));
                foreach (string name in new[] { "PBrowse.svg", "POpen.svg", "PDiagnosis.svg" })
                {
                    icons[$"pack://application:,,,/PAsset/PPanel/{name}|{icon}"] = new DrawingImage();
                }
                var resolvers = (Dictionary<bool, Func<string, double, string?>>)typeof(QIcon)
                    .GetField("qIconResolvers", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
                Func<string, double, string?> resolver = resolvers[true];
                resolvers[true] = (_, _) => null;
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
    }
}

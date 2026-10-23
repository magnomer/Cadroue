using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.UIVeneer;

namespace Cadroue.Tests.Windows;

internal static partial class TInterface
{
    internal static QExportRows TInterfaceExportRead(
        Func<string, bool> native,
        Func<string, LPresetDocument, bool> matched,
        Func<string, LPresetDocument, bool, bool> supported,
        Func<LPresetDocument> working,
        Func<string?> selected,
        Func<string, bool> editing,
        Func<string, string> display,
        string notice)
    {
        LPresetDocument document = working();
        CExportRow row = new(native, matched, supported);
        return new(row, document, selected, editing, display, notice);
    }

    internal static QExportFace TInterfaceExportRead(QExportRows rows, string name) => rows.QExportRowsRead(name);

    internal static LPresetSelection TInterfaceExportRead(string name, string key) => new(name, key);

    internal static void TInterfaceExportRead(string native, string[] stored)
    {
        MethodInfo storedAdd = typeof(LPresetTable).GetMethod(
            "LPresetStoredAdd",
            BindingFlags.Static | BindingFlags.NonPublic)!;
        foreach (string name in stored)
        {
            storedAdd.Invoke(null, [LPreset.LPresetStateCreate(new LPresetDocument
            {
                LPresetName = name,
                LPresetDisplay = name
            })]);
        }

        typeof(LPresetTable).GetMethod(
            "LPresetNativeAdd",
            BindingFlags.Static | BindingFlags.NonPublic)!.Invoke(
                null,
                [new LPresetDocument { LPresetName = native, LPresetDisplay = native }, "Native"]);
    }

    internal static Func<string, LPresetDocument, bool>? TInterfaceExportRead() =>
        LPresetSelection.LPresetSaveSeam;

    internal static void TInterfaceExportRead(Func<string, LPresetDocument, bool>? save) =>
        LPresetSelection.LPresetSaveSeam = save;

    internal static void TInterfaceExportRead(bool save) =>
        LPresetSelection.LPresetSaveSeam = save ? LPresetTable.LPresetSelectionSave : (_, _) => false;

    internal static (string?, string?) TInterfaceExportRead(QExport export) =>
        (export.QExportSelected, export.QExportEditing);

    internal static object[] TInterfaceExportRead(string group, bool folded)
    {
        LPreferenceState original = LPreference.LPreferenceStateCurrent.LPreferenceClone();
        Func<LPreferenceState, bool>? save = LPreference.LPreferenceSaveSeam;
        LPreferenceState configured = original.LPreferenceClone();
        configured.LPreferenceFold[group] = folded;
        LPreference.LPreferenceSaveSeam = _ => true;
        _ = LPreference.LPreferenceStateSet(configured);
        return [original, save!];
    }

    internal static bool TInterfaceExportRead(string group) =>
        LPreference.LPreferenceStateCurrent.LPreferenceFoldRead(group, false);

    internal static void TInterfaceExportRead(bool save, string preference) =>
        LPreference.LPreferenceSaveSeam = _ => save;

    internal static void TInterfaceExportRead(object[] state, bool preference)
    {
        LPreference.LPreferenceSaveSeam = null;
        _ = LPreference.LPreferenceStateSet((LPreferenceState)state[0]);
        LPreference.LPreferenceSaveSeam = (Func<LPreferenceState, bool>?)state[1];
    }

    internal static int TInterfaceExportRead(object[] state, int cache) =>
        ((ConcurrentDictionary<string, ImageSource>)state[5]).Keys.Count(
            key => !((HashSet<string>)state[6]).Contains(key));

    internal static object[] TInterfaceExportRead(Border root)
    {
        var grid = (Grid)root.Child;
        var name = (StackPanel)grid.Children[0];
        var actions = (StackPanel)grid.Children[1];
        var accept = (Button)actions.Children[0];
        var revert = (Button)actions.Children[1];
        var cache = (ConcurrentDictionary<string, ImageSource>)typeof(QIcon)
            .GetField("qIconCache", BindingFlags.Static | BindingFlags.NonPublic)!
            .GetValue(null)!;
        Func<Button, string> icon = button => cache.Single(pair =>
            ReferenceEquals(pair.Value, ((Image)button.Content).Source)).Key;
        return [name.Children[0], name.Children[1], actions, accept, revert, icon(accept), icon(revert)];
    }

    internal static object[] TInterfaceExportRead(Border header, bool group)
    {
        var grid = (Grid)header.Child;
        var label = (TextBlock)grid.Children[0];
        var button = (Button)grid.Children[1];
        var image = (Image)button.Content;
        var cache = (ConcurrentDictionary<string, ImageSource>)typeof(QIcon)
            .GetField("qIconCache", BindingFlags.Static | BindingFlags.NonPublic)!
            .GetValue(null)!;
        string icon = cache.Single(pair => ReferenceEquals(pair.Value, image.Source)).Key;
        return [label, button, image, icon];
    }

    internal static QExportTransfer TInterfaceExportRead(
        Func<LPresetDocument> value,
        Func<string?> selected,
        Func<string, bool> catalogue,
        Func<LPresetDocument, string, bool> fileSave,
        Func<string, LPresetDocument?> fileLoad,
        Func<string, string, string> nameResolve,
        Func<string, string> nameCreate,
        Func<string, LPresetDocument, bool> presetSave,
        Action<string> select,
        Func<string, string> format) =>
        new(new CExportTransfer(
            value,
            selected,
            catalogue,
            fileSave,
            fileLoad,
            nameResolve,
            nameCreate,
            presetSave,
            select,
            format));

    internal static void TInterfaceExportRead(
        QExportTransfer transfer,
        bool? confirmed,
        string path,
        Action<string> failure,
        Action<string, string?> refusal,
        Action? modal = null) =>
        transfer.QExportTransferSave(
            _ =>
            {
                modal?.Invoke();
                return (confirmed, path);
            },
            failure,
            refusal);

    internal static void TInterfaceExportRead(
        QExportTransfer transfer,
        bool? confirmed,
        string path,
        string fallback,
        Action<string> failure,
        Action<string, string?> refusal) =>
        transfer.QExportTransferImport(confirmed, path, fallback, failure, refusal);

    internal static (LPreset, QExportDigest) TExportSummaryRead(QExport export) =>
        (export.QExportWorkingRead(), export.QExportSummaryRead());

    internal static (PExport, object[]) TExportSurfaceRead(
        QExport export,
        int[]? routes = null,
        int[]? rebuildCount = null)
    {
        bool program = System.Windows.Application.Current is null;
        var resolvers = (Dictionary<bool, Func<string, double, string?>>)typeof(QIcon)
            .GetField("qIconResolvers", BindingFlags.Static | BindingFlags.NonPublic)!
            .GetValue(null)!;
        var readers = (Dictionary<bool, Func<Uri, Brush?, ImageSource>>)typeof(QIcon)
            .GetField("qIconReaders", BindingFlags.Static | BindingFlags.NonPublic)!
            .GetValue(null)!;
        Func<string, double, string?> resolver = resolvers[true];
        Func<Uri, Brush?, ImageSource> reader = readers[true];
        var cache = (ConcurrentDictionary<string, ImageSource>)typeof(QIcon)
            .GetField("qIconCache", BindingFlags.Static | BindingFlags.NonPublic)!
            .GetValue(null)!;
        HashSet<string> cacheKeys = cache.Keys.ToHashSet(StringComparer.Ordinal);
        try
        {
            if (System.Windows.Application.Current is null)
            {
                var ready = new ManualResetEventSlim();
                ExceptionDispatchInfo? startup = null;
                Thread thread = new(() =>
                {
                    try
                    {
                        _ = new PProgram();
                    }
                    catch (Exception exception)
                    {
                        startup = ExceptionDispatchInfo.Capture(exception);
                    }
                    finally
                    {
                        ready.Set();
                    }

                    if (startup is null)
                    {
                        System.Windows.Threading.Dispatcher.Run();
                    }
                }) { IsBackground = true };
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
                ready.Wait();
                startup?.Throw();
                System.Windows.Application application = System.Windows.Application.Current!;
                AppDomain.CurrentDomain.ProcessExit += (_, _) =>
                {
                    application.Dispatcher.BeginInvokeShutdown(
                        System.Windows.Threading.DispatcherPriority.Background);
                };
            }
            resolvers[true] = (_, _) => null;
            readers[true] = (_, _) =>
            {
                var image = new DrawingImage();
                image.Freeze();
                return image;
            };
            Func<string, object> shellCreate = name => Activator.CreateInstance(
                typeof(PExport).Assembly.GetType($"Cadroue.UIVeneer.{name}")!, true)!;
            PExport surface = QExportSurface.QExportSurfaceBuild(
                export,
                new PExport(),
                () => new PSEncoder(),
                (_, _) => { },
                (_, _, _) => { },
                () =>
                {
                    if (rebuildCount is not null)
                    {
                        rebuildCount[0]++;
                    }

                    return (Border)shellCreate("PExportPresetGroup");
                },
                () => (ResourceDictionary)shellCreate("PExportRow"),
                () => (ResourceDictionary)shellCreate("PExportPreset"),
                () => (ResourceDictionary)shellCreate("PExportCommands"),
                routes is null ? null : new RoutedEventHandler((_, _) => routes[0]++),
                routes is null ? null : new RoutedEventHandler((_, _) => routes[1]++),
                routes is null ? null : new RoutedEventHandler((_, _) => routes[2]++),
                routes is null ? null : new RoutedEventHandler((_, _) => routes[3]++),
                routes is null ? null : new RoutedEventHandler((_, _) => routes[4]++));
            return (surface, [resolvers, readers, resolver, reader, program, cache, cacheKeys]);
        }
        catch
        {
            resolvers[true] = resolver;
            readers[true] = reader;
            foreach (string key in cache.Keys.Where(key => !cacheKeys.Contains(key)))
            {
                _ = cache.TryRemove(key, out _);
            }
            throw;
        }
    }

    internal static object[] TInterfaceExportRead(PExport surface)
    {
        var body = (ContentControl)surface.FindName("PExportBody");
        var commands = (ContentControl)surface.FindName("PExportCommands");
        var digest = (FrameworkElement)surface.FindName("PExportDigest");
        var wrapper = (TextBlock)digest.FindName("PExportWrapper");
        var picture = (TextBlock)digest.FindName("PExportPicture");
        var sound = (TextBlock)digest.FindName("PExportSound");
        var destination = (TextBlock)digest.FindName("PExportDestination");
        return
        [
            body.Content!,
            commands.Content!,
            digest,
            Grid.GetRow(body),
            Grid.GetRow(commands),
            Grid.GetRow(digest),
            surface.FindName("PExportTitle"),
            ((StackPanel)((Border)digest).Child).Children[0],
            ((Grid)wrapper.Parent).Children[0],
            ((Grid)picture.Parent).Children[0],
            ((Grid)sound.Parent).Children[0],
            ((Grid)destination.Parent).Children[0],
            wrapper,
            picture,
            sound,
            destination,
        ];
    }

    internal static object[] TInterfaceExportRead(PExport surface, int marker)
    {
        var commands = (Grid)((ContentControl)surface.FindName("PExportCommands")).Content;
        var left = (StackPanel)commands.Children[0];
        var right = (StackPanel)commands.Children[1];
        Button[] buttons = left.Children.Cast<Button>().Concat(right.Children.Cast<Button>()).ToArray();
        var cache = (ConcurrentDictionary<string, ImageSource>)typeof(QIcon)
            .GetField("qIconCache", BindingFlags.Static | BindingFlags.NonPublic)!
            .GetValue(null)!;

        Func<Button, int> handlerCount = button =>
        {
            object store = typeof(UIElement)
                .GetProperty("EventHandlersStore", BindingFlags.Instance | BindingFlags.NonPublic)!
                .GetValue(button)!;
            var handlers = (Array)store.GetType()
                .GetMethod(
                    "GetRoutedEventHandlers",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
                .Invoke(store, [Button.ClickEvent])!;
            return handlers.Length;
        };

        Func<Button, string> iconKey = button => cache.Single(pair =>
            ReferenceEquals(pair.Value, ((Image)button.Content).Source)).Key;

        return
        [
            commands,
            ..buttons,
            ..buttons.Select(iconKey),
            ..buttons.Select(handlerCount).Cast<object>(),
        ];
    }

    internal static void TInterfaceExportRead(PExport surface, bool loaded) =>
        surface.RaiseEvent(new RoutedEventArgs(loaded ? FrameworkElement.LoadedEvent : FrameworkElement.UnloadedEvent));

    internal static void TInterfaceExportRead(PExport surface, bool loaded, object[] state)
    {
        try
        {
            TInterfaceExportRead(surface, loaded);
        }
        finally
        {
            TInterfaceExportRead(state);
        }
    }

    internal static void TInterfaceExportRead(object[] state)
    {
        ((Dictionary<bool, Func<string, double, string?>>)state[0])[true] =
            (Func<string, double, string?>)state[2];
        ((Dictionary<bool, Func<Uri, Brush?, ImageSource>>)state[1])[true] =
            (Func<Uri, Brush?, ImageSource>)state[3];
        var cache = (ConcurrentDictionary<string, ImageSource>)state[5];
        var cacheKeys = (HashSet<string>)state[6];
        foreach (string key in cache.Keys.Where(key => !cacheKeys.Contains(key)))
        {
            _ = cache.TryRemove(key, out _);
        }
    }
}

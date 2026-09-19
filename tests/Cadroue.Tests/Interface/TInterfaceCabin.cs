using Cadroue.Core;
using Cadroue.UIDeportment;

namespace Cadroue.Tests;

internal static partial class TInterface
{
    internal static LConsole TConsoleCreate() => new();
    internal static bool TConsoleSpinSet(LConsole console, bool spinning) => console.LConsoleSpinSet(spinning);
    internal static bool TConsoleProgressSet(LConsole console, double target) => console.LConsoleProgressSet(target);
    internal static void TConsoleProgressAttach(LConsole console, Action<double, bool> handler) =>
        console.LConsoleProgressApply += handler;
    internal static void TConsoleSpinAttach(LConsole console, Action<bool> handler) =>
        console.LConsoleSpinApply += handler;
    internal static IReadOnlyList<LConsoleRun> TConsoleRunsResolve(string line, string? accent) =>
        LConsole.LConsoleRunsResolve(line, accent);
    internal static string? TConsoleRemovalFormat(IReadOnlyDictionary<Guid, LScheduleRemoval> outcomes) =>
        LConsole.LConsoleRemovalFormat(outcomes);
    internal static int TConsoleIndexResolve(int index, int step, int count) =>
        LConsoleStation.LConsoleIndexResolve(index, step, count);

    internal static LConsoleScene TConsoleSceneCreate() => new();
    internal static void TConsoleReloadSet(LConsoleScene scene, string? name) => scene.LConsoleReloadSet(name);
    internal static string? TConsoleReloadRead(LConsoleScene scene) => scene.LConsoleReloadRead();
    internal static void TConsoleRowHandle(LConsoleScene scene, string? name) => scene.LConsoleRowHandle(name);
    internal static void TConsoleSceneSet(LConsoleScene scene, string name) => scene.LConsoleSceneSet(name);
    internal static bool TConsoleSceneCheck(LConsoleScene scene, string name) => scene.LConsoleSceneCheck(name);
    internal static bool TConsoleDirtyCheck(LConsoleScene scene) => scene.LConsoleDirtyCheck();
    internal static bool TConsoleDeleteHandle(LConsoleScene scene, string? name) => scene.LConsoleDeleteHandle(name);
    internal static void TConsoleFocusAttach(LConsoleScene scene, Action handler) =>
        scene.LConsoleFocusClear += handler;
    internal static void TConsolePressHandle(LConsoleScene scene, bool dropOpen, bool focusWithin, bool inside) =>
        scene.LConsolePressHandle(dropOpen, focusWithin, inside);
    internal static string TConsoleStemResolve(string name, string path) =>
        LConsoleScene.LConsoleStemResolve(name, path);
    internal static string TConsoleNameCreate(string baseName, IReadOnlyList<string> names) =>
        LConsoleScene.LConsoleNameCreate(baseName, names);
}

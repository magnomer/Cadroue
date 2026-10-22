using System.Runtime.CompilerServices;
using Cadroue.Infrastructure;

namespace Cadroue.Tests.Engine;

internal static class TInterfaceEngine
{
    [ModuleInitializer]
    internal static void TInterfaceEngineStart() =>
        RuntimeHelpers.RunModuleConstructor(typeof(Interface.TInterface).Module.ModuleHandle);

    internal static (bool, bool) TRendererReadinessRead(string path)
    {
        LRendererReadiness lReadiness = LRendererReadiness.LRendererReadinessRead(path);
        return (lReadiness.LRendererReadinessProgram, lReadiness.LRendererReadinessLibrary);
    }

    internal static (bool, bool) TRendererReadinessRead(
        string path,
        Func<string, bool> program,
        Func<string, bool> library)
    {
        LRendererReadiness lReadiness = LRendererReadiness.LRendererReadinessRead(path, program, library);
        return (lReadiness.LRendererReadinessProgram, lReadiness.LRendererReadinessLibrary);
    }
}

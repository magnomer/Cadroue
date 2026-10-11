using System.Runtime.CompilerServices;

namespace Cadroue.Tests.Engine;

internal static class TInterfaceEngine
{
    [ModuleInitializer]
    internal static void TInterfaceEngineStart() =>
        RuntimeHelpers.RunModuleConstructor(typeof(Interface.TInterface).Module.ModuleHandle);
}

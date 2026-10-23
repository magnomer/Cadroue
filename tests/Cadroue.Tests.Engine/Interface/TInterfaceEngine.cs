using System.Runtime.CompilerServices;
using Cadroue.Infrastructure;

namespace Cadroue.Tests.Engine;

internal static class TInterfaceEngine
{
    [ModuleInitializer]
    internal static void TInterfaceEngineStart() =>
        RuntimeHelpers.RunModuleConstructor(typeof(Interface.TInterface).Module.ModuleHandle);

    internal static (bool, bool) TRendererReadinessRead(string path) =>
        (LRendererLibrary.LRendererProgramExist(path), LRendererLibrary.LRendererFolderValidate(path));
}

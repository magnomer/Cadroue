using Cadroue.ShellEngine;

namespace Cadroue.Tests.Conduct;

internal static partial class TInterface
{
    internal static LVideoPort TVideoOutletCreate() => new LVideoOutlet(Interface.TInterface.TInterfaceEngine);
}

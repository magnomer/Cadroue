using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.Tests.Conduct;

internal static partial class TInterface
{
    internal static LRigList TRigListCreate(
        Func<IReadOnlyList<string>, CancellationToken,
            Task<(IReadOnlyList<string>, IReadOnlyList<(string, string)>)>> scanner,
        Action<string, string?> info,
        Action<string, string?> warning,
        Action<string, Exception?> error) => new(scanner, info, warning, error);
    internal static LListPort TListOutletCreate(LRigList rig, Func<string, string> name) =>
        LListOutlet.LListOutletCreate(rig)(name);
    internal static Task<int> TListPathsAdd(LListPort port, IReadOnlyList<string> paths) => port.LListPathsAdd(paths);
    internal static IReadOnlyList<(bool TListLocked, IReadOnlyList<string> TListPaths)> TListCardsRead(
        LListPort port) => port.LListCardsRead();
}

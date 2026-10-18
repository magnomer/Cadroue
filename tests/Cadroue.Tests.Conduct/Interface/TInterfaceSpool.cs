using System;
using Cadroue.Core;

namespace Cadroue.Tests.Conduct;

internal static partial class TInterface
{
    internal static void TSpoolRangeSet(LSpool spool, TimeSpan origin, TimeSpan limit) =>
        spool.LSpoolRangeSet(origin, limit);
}

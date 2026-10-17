using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static class LLineageEngine
{
    public static Func<string, Guid> LLineageFileSeam { get; set; } = _ => Guid.Empty;

    public static Func<LWorkItem, Guid> LLineageWorkSeam { get; set; } = _ => Guid.Empty;
}

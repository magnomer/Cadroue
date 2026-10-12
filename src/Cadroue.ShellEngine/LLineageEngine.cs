namespace Cadroue.ShellEngine;

public static class LLineageEngine
{
    public static Func<string, Guid> LLineageFileSeam { get; set; } = _ => Guid.Empty;
}

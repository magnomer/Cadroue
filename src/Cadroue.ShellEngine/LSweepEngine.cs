namespace Cadroue.ShellEngine;

public static class LSweepEngine
{
    public static Func<CancellationToken, Task> LSweepClaimSeam { get; set; } = _ => Task.CompletedTask;

    public static Action LSweepReleaseSeam { get; set; } = () => { };
}

using Cadroue.Core;

namespace Cadroue.ShellEngine;

public static class LEmployerEngine
{
    private const string LEmployerDetachedError = "No process employer is attached.";

    public static Func<string, string, LCustodyFamily?, int?, TimeSpan?, string, CancellationToken,
        Guid?, Action<string>, Action<string>, Task<LEmployerResult>>
        LEmployerRunSeam { get; set; } =
        (_, _, _, _, _, _, _, _, _, _) => Task.FromResult(new LEmployerResult(-1, LEmployerDetachedError));

    public static Func<string, LCustodyFamily?, int?, TimeSpan?, IReadOnlyList<string>, CancellationToken,
        Action<string>, Task<LEmployerResult>> LEmployerListSeam { get; set; } =
        (_, _, _, _, _, _, _) => Task.FromResult(new LEmployerResult(-1, LEmployerDetachedError));

    public static Func<Guid, bool> LEmployerSuspendSeam { get; set; } = _ => true;

    public static Func<Guid, bool> LEmployerResumeSeam { get; set; } = _ => true;

    public static Func<Guid, bool> LEmployerLiveSeam { get; set; } = _ => true;
}

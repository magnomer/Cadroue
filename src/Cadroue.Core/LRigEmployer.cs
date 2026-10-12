namespace Cadroue.Core;

public sealed record LRigEmployer(
    Func<string, string, LCustodyFamily?, int?, TimeSpan?, string, CancellationToken,
        Guid?, Action<string>, Action<string>, Task<LEmployerResult>> LRigEmployerCommand,
    Func<string, LCustodyFamily?, int?, TimeSpan?, IReadOnlyList<string>, CancellationToken,
        Action<string>, Task<LEmployerResult>> LRigEmployerList,
    Func<Guid, bool> LRigEmployerSuspension,
    Func<Guid, bool> LRigEmployerResumption,
    Func<Guid, bool> LRigEmployerLive);

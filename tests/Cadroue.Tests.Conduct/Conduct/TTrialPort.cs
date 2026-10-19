using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

// A trial that runs nothing: every token fails with no message, the available set keeps nothing, every repertoire
// entry passes the check, and the trace is dropped.
internal sealed class TTrialPort : LSTrialPort
{
    public Task<(bool LSTrialSuccess, string LSTrialMessage)> LSTrialRun(string lEncoder, CancellationToken lToken) =>
        Task.FromResult((false, string.Empty));

    public void LSTrialApply(IEnumerable<string> lAvailable)
    {
    }

    public bool LSTrialCheck(LRepertoireEncoder lCandidate) => true;

    public void LSTrialRecord(string lMessage, string lDetail)
    {
    }
}

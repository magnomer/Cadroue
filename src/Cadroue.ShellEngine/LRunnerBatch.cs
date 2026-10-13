namespace Cadroue.ShellEngine;

internal sealed class LRunnerBatch
{
    public readonly CancellationTokenSource LRunnerBatchSource = new();
    public int LRunnerBatchActive;
}

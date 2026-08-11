using System.Diagnostics;
using System.Collections.Concurrent;

using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

internal enum TRunnerWorkState
{
    Pending,
    Running,
    Done,
    Failed,
    Cancelled
}

internal sealed record TRunnerWork(
    Guid Id,
    string Name,
    TRunnerWorkState State,
    double Progress,
    int Attempts,
    bool OutputExists,
    string Message);

internal sealed class TRunner : IDisposable
{
    private const int WaitMilliseconds = 8_000;
    private readonly object tRunnerScheduleGate = new();
    private readonly string tRunnerRoot;
    private readonly string tRunnerControlRoot;
    private readonly LSchedule tRunnerSchedule;
    private readonly LRunner tRunner;
    private readonly Dictionary<Guid, string> tRunnerNames = new();
    private readonly HashSet<string> tRunnerOutputPaths = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentQueue<string> tRunnerDiagnostics = new();
    private int tRunnerSequence;
    private bool tRunnerDisposed;

    internal TRunner(int parallelMaximum = 1)
    {
        tRunnerRoot = Path.Combine(Path.GetTempPath(), "cadroue-runner-" + Guid.NewGuid().ToString("N"));
        tRunnerControlRoot = Path.Combine(tRunnerRoot, "control");
        Directory.CreateDirectory(tRunnerControlRoot);

        string scriptPath = Path.Combine(tRunnerControlRoot, "runner.ps1");
        File.WriteAllText(scriptPath, RunnerScript);

        LDepotIndex.LDepotIndexRelease();
        LDepot.LDepotRootSet(tRunnerRoot);
        tRunnerSchedule = new LSchedule();
        tRunnerSchedule.LScheduleLoad();
        tRunner = new LRunner(tRunnerSchedule, action =>
        {
            lock (tRunnerScheduleGate)
            {
                action();
            }
        })
        {
            LRunnerParallelMaximum = parallelMaximum,
            LRunnerProgramPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.System),
                "WindowsPowerShell", "v1.0", "powershell.exe"),
            LRunnerProgramArgumentPrefix = "-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass",
            LRunnerProgramArgumentsTransform = arguments =>
            {
                string outputPath = tRunnerOutputPaths.Single(path =>
                    arguments.Contains(path, StringComparison.OrdinalIgnoreCase));
                return $"-File \"{scriptPath}\" \"{tRunnerControlRoot}\" \"{outputPath}\"";
            }
        };
        LRunner.LRunnerVerboseSource = () => true;
        LRunner.LRunnerReport = (message, exception) =>
            tRunnerDiagnostics.Enqueue(exception is null ? message : message + ": " + exception.Message);
        LRunner.LRunnerFfmpegReport = (summary, detail) =>
            tRunnerDiagnostics.Enqueue(detail is null ? summary : summary + ": " + detail);
    }

    internal Guid WorkAdd(string name, int steps = 40, int delayMilliseconds = 50)
    {
        string uniqueName = $"{++tRunnerSequence:D2}-{name}.mp4";
        string sourcePath = Path.Combine(tRunnerRoot, uniqueName + ".source");
        string outputPath = Path.Combine(tRunnerRoot, uniqueName);
        File.WriteAllText(sourcePath, "controlled runner source");
        File.WriteAllLines(
            Path.Combine(tRunnerControlRoot, uniqueName + ".plan"),
            [steps.ToString(), delayMilliseconds.ToString()]);

        var workItem = new LWorkItem(
            Guid.NewGuid(),
            LWorkKind.LWorkKindEdit,
            LWorkPriority.LWorkPriorityNormal,
            sourcePath,
            TimeSpan.Zero,
            TimeSpan.FromSeconds(10),
            uniqueName,
            outputPath,
            WorkCreationOutput.Create(),
            lWorkCreateTime: DateTimeOffset.UnixEpoch.AddTicks(tRunnerSequence))
        {
            LWorkSourceBytes = new FileInfo(sourcePath).Length,
            LWorkSourceMedia = new LWorkMedia(1920, 1080, 30, 10_000, true)
        };

        lock (tRunnerScheduleGate)
        {
            if (tRunnerSchedule.LScheduleAdd([workItem]) != 1)
            {
                throw new InvalidOperationException($"Controlled work '{name}' was not admitted to the schedule.");
            }
        }

        tRunnerNames[workItem.LWorkId] = uniqueName;
        tRunnerOutputPaths.Add(outputPath);
        return workItem.LWorkId;
    }

    internal void Start() => tRunner.LRunnerStart();

    internal void Pause() => tRunner.LRunnerPause();

    internal void Stop() => tRunner.LRunnerCancel();

    internal void CancelWork(Guid workId) => tRunner.LRunnerJobCancel(workId);

    internal bool Suspended => tRunner.LRunnerSuspended;

    internal bool Running => tRunner.LRunnerRunning;

    internal bool Remove(Guid workId)
    {
        lock (tRunnerScheduleGate)
        {
            return tRunnerSchedule.LScheduleRemove(workId);
        }
    }

    internal TRunnerWork Read(Guid workId)
    {
        lock (tRunnerScheduleGate)
        {
            LWorkItem item = tRunnerSchedule.LScheduleRecords.Single(item => item.LWorkId == workId);
            return Snapshot(item);
        }
    }

    internal int ExecutionCount(Guid workId)
    {
        string name = tRunnerNames[workId];
        string path = Path.Combine(tRunnerControlRoot, name + ".executions");
        for (int attempt = 0; attempt < 20; attempt++)
        {
            if (!File.Exists(path))
            {
                return 0;
            }

            try
            {
                return File.ReadAllLines(path).Length;
            }
            catch (IOException) when (attempt < 19)
            {
                Thread.Sleep(5);
            }
        }

        return File.ReadAllLines(path).Length;
    }

    internal TRunnerWork WaitForState(Guid workId, TRunnerWorkState state, int milliseconds = WaitMilliseconds) =>
        WaitFor(workId, item => item.State == state, $"state {state}", milliseconds);

    internal TRunnerWork WaitForProgress(Guid workId, double minimum, int milliseconds = WaitMilliseconds) =>
        WaitFor(workId, item => item.Progress >= minimum, $"progress >= {minimum:0.###}", milliseconds);

    internal TRunnerWork WaitForOutput(Guid workId, int milliseconds = WaitMilliseconds) =>
        WaitFor(workId, item => item.OutputExists, "a spawned partial output", milliseconds);

    internal TRunnerWork WaitForOutputRemoved(Guid workId, int milliseconds = WaitMilliseconds) =>
        WaitFor(workId, item => !item.OutputExists, "partial output cleanup", milliseconds);

    internal void WaitForExecutionCount(Guid workId, int count, int milliseconds = WaitMilliseconds)
    {
        var clock = Stopwatch.StartNew();
        while (clock.ElapsedMilliseconds < milliseconds)
        {
            if (ExecutionCount(workId) >= count)
            {
                return;
            }

            Thread.Sleep(15);
        }

        throw new TimeoutException(
            $"Runner did not execute work {workId} {count} time(s) within {milliseconds} ms. " +
            $"Observed {ExecutionCount(workId)} execution(s).");
    }

    internal double WaitForPausedProgress(Guid workId, int milliseconds = WaitMilliseconds)
    {
        var clock = Stopwatch.StartNew();
        double previous = Read(workId).Progress;
        long stableSince = clock.ElapsedMilliseconds;
        while (clock.ElapsedMilliseconds < milliseconds)
        {
            Thread.Sleep(25);
            double current = Read(workId).Progress;
            if (Math.Abs(current - previous) > 0.000001)
            {
                previous = current;
                stableSince = clock.ElapsedMilliseconds;
            }
            else if (clock.ElapsedMilliseconds - stableSince >= 400)
            {
                return current;
            }
        }

        throw new TimeoutException(
            $"Runner progress for work {workId} did not settle at the production pause point within {milliseconds} ms.");
    }

    public void Dispose()
    {
        if (tRunnerDisposed)
        {
            return;
        }

        tRunnerDisposed = true;
        tRunner.LRunnerCancel();
        tRunner.LRunnerDispose();
        LRunner.LRunnerVerboseSource = null;
        LRunner.LRunnerReport = null;
        LRunner.LRunnerFfmpegReport = null;
        lock (tRunnerScheduleGate)
        {
            tRunnerSchedule.LScheduleBatchRemove(
                tRunnerSchedule.LScheduleRecords.Select(item => item.LWorkId).ToArray());
        }

        LDepotIndex.LDepotIndexRelease();
        LDepot.LDepotRootSet(null);
        try
        {
            Directory.Delete(tRunnerRoot, true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }

    private TRunnerWork WaitFor(
        Guid workId,
        Func<TRunnerWork, bool> predicate,
        string expectation,
        int milliseconds)
    {
        var clock = Stopwatch.StartNew();
        TRunnerWork current = Read(workId);
        while (clock.ElapsedMilliseconds < milliseconds)
        {
            current = Read(workId);
            if (predicate(current))
            {
                return current;
            }

            Thread.Sleep(15);
        }

        throw new TimeoutException(
            $"Runner work {workId} did not reach {expectation} within {milliseconds} ms. " +
            $"Observed state {current.State}, progress {current.Progress:0.###}, attempts {current.Attempts}, " +
            $"message '{current.Message}'. Helper diagnostic: {HelperDiagnosticRead()}");
    }

    private string HelperDiagnosticRead()
    {
        string path = Path.Combine(tRunnerControlRoot, "error.log");
        return File.Exists(path)
            ? File.ReadAllText(path)
            : string.Join(" | ", tRunnerDiagnostics.TakeLast(4));
    }

    private static TRunnerWork Snapshot(LWorkItem item) => new(
        item.LWorkId,
        item.LWorkOutputName,
        item.LWorkStateCurrent switch
        {
            LWorkState.LWorkStateRunning => TRunnerWorkState.Running,
            LWorkState.LWorkStateDone => TRunnerWorkState.Done,
            LWorkState.LWorkStateFailed => TRunnerWorkState.Failed,
            LWorkState.LWorkStateCancelled => TRunnerWorkState.Cancelled,
            _ => TRunnerWorkState.Pending
        },
        item.LWorkProgress,
        item.LWorkAttemptCount,
        File.Exists(item.LWorkOutputPath),
        item.LWorkMessage);

    private const string RunnerScript = """
        param([string]$controlRoot, [string]$outputPath)
        try {
            $outputName = [IO.Path]::GetFileName($outputPath)
            $plan = Get-Content -LiteralPath (Join-Path $controlRoot ($outputName + '.plan'))
            $steps = [int]$plan[0]
            $delay = [int]$plan[1]
            Add-Content -LiteralPath (Join-Path $controlRoot ($outputName + '.executions')) -Value 'started'
            Set-Content -LiteralPath $outputPath -Value 'partial controlled output'
            for ($index = 1; $index -le $steps; $index++) {
                $microseconds = [long](10000000 * $index / $steps)
                [Console]::Out.WriteLine('out_time_us=' + $microseconds)
                [Console]::Out.WriteLine('progress=' + $(if ($index -eq $steps) { 'end' } else { 'continue' }))
                [Console]::Out.Flush()
                if ($index -lt $steps) { Start-Sleep -Milliseconds $delay }
            }
        } catch {
            ($_ | Out-String) | Set-Content -LiteralPath (Join-Path $controlRoot 'error.log')
            exit 65
        }
        exit 0
        """;
}

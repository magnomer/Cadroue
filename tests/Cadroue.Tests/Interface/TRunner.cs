using System.Diagnostics;
using System.Collections.Concurrent;

using Cadroue.Core;
using Cadroue.Infrastructure;
using Cadroue.ShellEngine;

namespace Cadroue.Tests;

internal enum TRunnerWorkState
{
    TRunnerPending,
    TRunnerRunning,
    TRunnerDone,
    TRunnerFailed,
    TRunnerCancelled
}

internal sealed record TRunnerWork(
    Guid TRunnerId,
    string TRunnerName,
    TRunnerWorkState TRunnerState,
    double TRunnerProgress,
    int TRunnerAttempts,
    bool TRunnerOutputFlag,
    string TRunnerMessage);

internal sealed class TRunner : IDisposable
{
    private const int TRunnerWaitMilliseconds = 8_000;
    private readonly object tRunnerScheduleGate = new();
    private readonly string tRunnerRoot;
    private readonly string tRunnerControlRoot;
    private readonly LSchedule tRunnerSchedule;
    private readonly List<LRunner> tRunnerList = new();
    private readonly Dictionary<Guid, string> tRunnerNames = new();
    private readonly HashSet<string> tRunnerOutputPaths = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentQueue<string> tRunnerDiagnostics = new();
    private int tRunnerSequence;
    private bool tRunnerDisposed;

    internal TRunner(int workerCount = 1)
    {
        tRunnerRoot = Path.Combine(Path.GetTempPath(), "cadroue-runner-" + Guid.NewGuid().ToString("N"));
        tRunnerControlRoot = Path.Combine(tRunnerRoot, "control");
        Directory.CreateDirectory(tRunnerControlRoot);

        string scriptPath = Path.Combine(tRunnerControlRoot, "runner.ps1");
        File.WriteAllText(scriptPath, TRunnerScript);

        LDepotIndex.LDepotIndexRelease();
        LDepot.LDepotRootSet(tRunnerRoot);
        tRunnerSchedule = new LSchedule();
        tRunnerSchedule.LScheduleLoad();
        for (int worker = 0; worker < Math.Max(1, workerCount); worker++)
        {
            tRunnerList.Add(new LRunner(tRunnerSchedule, action =>
            {
                lock (tRunnerScheduleGate)
                {
                    action();
                }
            })
            {
                LRunnerProgramPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.System),
                    "WindowsPowerShell", "v1.0", "powershell.exe"),
                LRunnerArgumentPrefix = "-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass",
                LRunnerArgumentTransform = arguments =>
                {
                    string outputPath = tRunnerOutputPaths.Single(path =>
                        arguments.Contains(path, StringComparison.OrdinalIgnoreCase));
                    return $"-File \"{scriptPath}\" \"{tRunnerControlRoot}\" \"{outputPath}\"";
                }
            });
        }
        LRunner.LRunnerVerboseSource = () => true;
        LRunner.LRunnerReport = (message, exception) =>
            tRunnerDiagnostics.Enqueue(exception is null ? message : message + ": " + exception.Message);
        LRunner.LRunnerFfmpegReport = (summary, detail) =>
            tRunnerDiagnostics.Enqueue(detail is null ? summary : summary + ": " + detail);
    }

    internal Guid TWorkAdd(string name, int steps = 40, int delayMilliseconds = 50)
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
            TWorkOutput.TWorkOutputCreate(),
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

    internal void TRunnerStart() => tRunnerList.ForEach(runner => runner.LRunnerStart());

    internal void TRunnerPause() => tRunnerList.ForEach(runner => runner.LRunnerPause());

    internal void TRunnerStop() => tRunnerList.ForEach(runner => runner.LRunnerCancel());

    internal void TRunnerWorkCancel(Guid workId) => tRunnerList.ForEach(runner => runner.LRunnerJobCancel(workId));

    internal bool TRunnerSuspended => tRunnerList.All(runner => runner.LRunnerSuspended);

    internal bool TRunnerRunning => tRunnerList.Any(runner => runner.LRunnerRunning);

    internal bool TRunnerRemove(Guid workId)
    {
        lock (tRunnerScheduleGate)
        {
            return tRunnerSchedule.LScheduleRemove(workId);
        }
    }

    internal TRunnerWork TRunnerRead(Guid workId)
    {
        lock (tRunnerScheduleGate)
        {
            LWorkItem item = tRunnerSchedule.LScheduleRecords.Single(item => item.LWorkId == workId);
            return TRunnerSnapshotCreate(item);
        }
    }

    internal int TRunnerExecutionRead(Guid workId)
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

    internal TRunnerWork TRunnerStateRead(Guid workId, TRunnerWorkState state, int milliseconds = TRunnerWaitMilliseconds) =>
        TRunnerWaitRead(workId, item => item.TRunnerState == state, $"state {state}", milliseconds);

    internal TRunnerWork TRunnerProgressRead(Guid workId, double minimum, int milliseconds = TRunnerWaitMilliseconds) =>
        TRunnerWaitRead(workId, item => item.TRunnerProgress >= minimum, $"progress >= {minimum:0.###}", milliseconds);

    internal TRunnerWork TRunnerOutputRead(Guid workId, int milliseconds = TRunnerWaitMilliseconds) =>
        TRunnerWaitRead(workId, item => item.TRunnerOutputFlag, "a spawned partial output", milliseconds);

    internal TRunnerWork TRunnerRemovedRead(Guid workId, int milliseconds = TRunnerWaitMilliseconds) =>
        TRunnerWaitRead(workId, item => !item.TRunnerOutputFlag, "partial output cleanup", milliseconds);

    internal void TRunnerCountRead(Guid workId, int count, int milliseconds = TRunnerWaitMilliseconds)
    {
        var clock = Stopwatch.StartNew();
        while (clock.ElapsedMilliseconds < milliseconds)
        {
            if (TRunnerExecutionRead(workId) >= count)
            {
                return;
            }

            Thread.Sleep(15);
        }

        throw new TimeoutException(
            $"Runner did not execute work {workId} {count} time(s) within {milliseconds} ms. " +
            $"Observed {TRunnerExecutionRead(workId)} execution(s).");
    }

    internal double TRunnerPausedRead(Guid workId, int milliseconds = TRunnerWaitMilliseconds)
    {
        var clock = Stopwatch.StartNew();
        double previous = TRunnerRead(workId).TRunnerProgress;
        long stableSince = clock.ElapsedMilliseconds;
        while (clock.ElapsedMilliseconds < milliseconds)
        {
            Thread.Sleep(25);
            double current = TRunnerRead(workId).TRunnerProgress;
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
        foreach (LRunner runner in tRunnerList)
        {
            runner.LRunnerCancel();
            runner.LRunnerDispose();
        }
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

    private TRunnerWork TRunnerWaitRead(
        Guid workId,
        Func<TRunnerWork, bool> predicate,
        string expectation,
        int milliseconds)
    {
        var clock = Stopwatch.StartNew();
        TRunnerWork current = TRunnerRead(workId);
        while (clock.ElapsedMilliseconds < milliseconds)
        {
            current = TRunnerRead(workId);
            if (predicate(current))
            {
                return current;
            }

            Thread.Sleep(15);
        }

        throw new TimeoutException(
            $"Runner work {workId} did not reach {expectation} within {milliseconds} ms. " +
            $"Observed state {current.TRunnerState}, progress {current.TRunnerProgress:0.###}, attempts {current.TRunnerAttempts}, " +
            $"message '{current.TRunnerMessage}'. Helper diagnostic: {TRunnerDiagnosticRead()}");
    }

    private string TRunnerDiagnosticRead()
    {
        string path = Path.Combine(tRunnerControlRoot, "error.log");
        return File.Exists(path)
            ? File.ReadAllText(path)
            : string.Join(" | ", tRunnerDiagnostics.TakeLast(4));
    }

    private static TRunnerWork TRunnerSnapshotCreate(LWorkItem item) => new(
        item.LWorkId,
        item.LWorkOutputName,
        item.LWorkStateCurrent switch
        {
            LWorkState.LWorkStateRunning => TRunnerWorkState.TRunnerRunning,
            LWorkState.LWorkStateDone => TRunnerWorkState.TRunnerDone,
            LWorkState.LWorkStateFailed => TRunnerWorkState.TRunnerFailed,
            LWorkState.LWorkStateCancelled => TRunnerWorkState.TRunnerCancelled,
            _ => TRunnerWorkState.TRunnerPending
        },
        item.LWorkProgress,
        item.LWorkAttemptCount,
        File.Exists(item.LWorkOutputPath),
        item.LWorkMessage);

    private const string TRunnerScript = """
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

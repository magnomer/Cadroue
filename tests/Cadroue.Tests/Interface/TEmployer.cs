using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

using Cadroue.ShellEngine;

namespace Cadroue.Tests;

internal enum TEmployerStatus
{
    TEmployerSucceeded,
    TEmployerFailed,
    TEmployerCancelled
}

internal sealed record TEmployerResult(
    TEmployerStatus TEmployerState,
    int? TEmployerExitCode,
    string TEmployerError,
    IReadOnlyList<string> TEmployerOutput,
    IReadOnlyList<string> TEmployerErrorOutput,
    Exception? TEmployerException);

internal sealed class TEmployerExecution : IDisposable
{
    private readonly CancellationTokenSource tEmployerCancellation;
    private readonly TaskCompletionSource<Process> tEmployerAttached =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
    private Process? tEmployerProcess;
    private int tEmployerDisposed;

    internal TEmployerExecution(Func<CancellationToken, Action<Process>, Task<TEmployerResult>> run)
    {
        tEmployerCancellation = new CancellationTokenSource();
        TEmployerCompletion = run(tEmployerCancellation.Token, TEmployerAttach);
    }

    internal Task<TEmployerResult> TEmployerCompletion { get; }

    internal async Task<int> TEmployerProcessRead()
    {
        Process process = await tEmployerAttached.Task.WaitAsync(TimeSpan.FromSeconds(10));
        return process.Id;
    }

    internal bool TEmployerChildAlive
    {
        get
        {
            Process? process = Volatile.Read(ref tEmployerProcess);
            if (process is null)
            {
                return false;
            }

            try
            {
                return !process.HasExited;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }
    }

    internal void TEmployerCancel()
    {
        tEmployerCancellation.Cancel();
        TEmployerProcessStop(Volatile.Read(ref tEmployerProcess));
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref tEmployerDisposed, 1) != 0)
        {
            return;
        }

        TEmployerCancel();
        tEmployerCancellation.Dispose();
    }

    private void TEmployerAttach(Process process)
    {
        Volatile.Write(ref tEmployerProcess, process);
        tEmployerAttached.TrySetResult(process);
        if (tEmployerCancellation.IsCancellationRequested)
        {
            TEmployerProcessStop(process);
        }
    }

    private static void TEmployerProcessStop(Process? process)
    {
        if (process is null)
        {
            return;
        }

        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }

            process.WaitForExit(5_000);
        }
        catch (Exception exception)
            when (exception is InvalidOperationException
                or System.ComponentModel.Win32Exception
                or SystemException)
        {
        }
    }
}

internal sealed class TEmployer : IDisposable
{
    private readonly string tEmployerRoot;
    private readonly string tEmployerScriptPath;
    private readonly ConcurrentBag<TEmployerExecution> tEmployerExecutions = new();
    private int tEmployerDisposed;

    internal TEmployer()
    {
        tEmployerRoot = Path.Combine(Path.GetTempPath(), "Cadroue-EmployerTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tEmployerRoot);
        tEmployerScriptPath = Path.Combine(tEmployerRoot, "child.ps1");
        File.WriteAllText(tEmployerScriptPath, TEmployerChildScript, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    internal TEmployerExecution TEmployerStart(params string[] arguments) =>
        TEmployerProgramStart(TEmployerShellRead(), TEmployerPrefixRead(), arguments);

    internal TEmployerExecution TEmployerMissingStart()
    {
        string missing = Path.Combine(tEmployerRoot, "missing-" + Guid.NewGuid().ToString("N") + ".exe");
        return TEmployerProgramStart(missing, string.Empty, ["success"]);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref tEmployerDisposed, 1) != 0)
        {
            return;
        }

        foreach (TEmployerExecution execution in tEmployerExecutions)
        {
            execution.Dispose();
        }

        for (int attempt = 0; attempt < 5 && Directory.Exists(tEmployerRoot); attempt++)
        {
            try
            {
                Directory.Delete(tEmployerRoot, recursive: true);
            }
            catch (IOException) when (attempt < 4)
            {
                Thread.Sleep(50);
            }
            catch (UnauthorizedAccessException) when (attempt < 4)
            {
                Thread.Sleep(50);
            }
        }
    }

    private TEmployerExecution TEmployerProgramStart(string program, string prefix, IReadOnlyList<string> arguments)
    {
        ObjectDisposedException.ThrowIf(tEmployerDisposed != 0, this);
        var execution = new TEmployerExecution(
            (token, attach) => TEmployerRun(program, prefix, TEmployerArgumentFormat(arguments), token, attach));
        tEmployerExecutions.Add(execution);
        return execution;
    }

    private static async Task<TEmployerResult> TEmployerRun(
        string program,
        string prefix,
        string arguments,
        CancellationToken token,
        Action<Process> attach)
    {
        var output = new ConcurrentQueue<string>();
        var errorOutput = new ConcurrentQueue<string>();

        try
        {
            var employer = new LEmployer(program, prefix);
            LEmployerResult result = await employer.LEmployerRun(
                arguments,
                token,
                attach,
                output.Enqueue,
                errorOutput.Enqueue);
            return new TEmployerResult(
                result.LEmployerExit == 0 ? TEmployerStatus.TEmployerSucceeded : TEmployerStatus.TEmployerFailed,
                result.LEmployerExit,
                result.LEmployerError,
                output.ToArray(),
                errorOutput.ToArray(),
                null);
        }
        catch (OperationCanceledException exception) when (token.IsCancellationRequested)
        {
            return new TEmployerResult(
                TEmployerStatus.TEmployerCancelled,
                null,
                string.Join(Environment.NewLine, errorOutput),
                output.ToArray(),
                errorOutput.ToArray(),
                exception);
        }
        catch (Exception exception)
        {
            return new TEmployerResult(
                token.IsCancellationRequested ? TEmployerStatus.TEmployerCancelled : TEmployerStatus.TEmployerFailed,
                null,
                string.Join(Environment.NewLine, errorOutput),
                output.ToArray(),
                errorOutput.ToArray(),
                exception);
        }
    }

    private string TEmployerPrefixRead() =>
        $"-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File {TEmployerQuoteFormat(tEmployerScriptPath)}";

    private static string TEmployerShellRead() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.System),
        "WindowsPowerShell",
        "v1.0",
        "powershell.exe");

    private static string TEmployerArgumentFormat(IReadOnlyList<string> arguments) =>
        string.Join(" ", arguments.Select(TEmployerQuoteFormat));

    private static string TEmployerQuoteFormat(string argument)
    {
        if (argument.Length > 0 && !argument.Any(character => char.IsWhiteSpace(character) || character == '"'))
        {
            return argument;
        }

        var quoted = new StringBuilder(argument.Length + 2).Append('"');
        int backslashes = 0;
        foreach (char character in argument)
        {
            if (character == '\\')
            {
                backslashes++;
                continue;
            }

            if (character == '"')
            {
                quoted.Append('\\', backslashes * 2 + 1).Append('"');
                backslashes = 0;
                continue;
            }

            quoted.Append('\\', backslashes).Append(character);
            backslashes = 0;
        }

        return quoted.Append('\\', backslashes * 2).Append('"').ToString();
    }

    private const string TEmployerChildScript = """
        param([Parameter(ValueFromRemainingArguments = $true)][string[]]$ChildArguments)
        $mode = $ChildArguments[0]
        switch ($mode) {
            'success' { exit 0 }
            'failure' {
                [Console]::Error.WriteLine('controlled failure')
                exit 23
            }
            'stderr' {
                [Console]::Error.WriteLine($ChildArguments[1])
                exit 0
            }
            'stdout' {
                $count = [int]$ChildArguments[1]
                for ($index = 0; $index -lt $count; $index++) {
                    [Console]::Out.WriteLine('progress=' + $index)
                }
                exit 0
            }
            'large-stderr' {
                $count = [int]$ChildArguments[1]
                $line = 'e' * [int]$ChildArguments[2]
                for ($index = 0; $index -lt $count; $index++) {
                    [Console]::Error.WriteLine($line)
                }
                exit 0
            }
            'echo' {
                for ($index = 1; $index -lt $ChildArguments.Count; $index++) {
                    $bytes = [Text.Encoding]::UTF8.GetBytes($ChildArguments[$index])
                    [Console]::Out.WriteLine('ARG:' + [Convert]::ToBase64String($bytes))
                }
                exit 0
            }
            'wait' {
                [Console]::Out.WriteLine('ready')
                [Console]::Out.Flush()
                while ($true) { Start-Sleep -Milliseconds 100 }
            }
            default {
                [Console]::Error.WriteLine('unknown mode: ' + $mode)
                exit 64
            }
        }
        """;
}

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;

using Cadroue.ShellEngine;

namespace Cadroue.Tests;

internal enum TEmployerStatus
{
    Succeeded,
    Failed,
    Cancelled
}

internal sealed record TEmployerResult(
    TEmployerStatus Status,
    int? ExitCode,
    string Error,
    IReadOnlyList<string> Output,
    IReadOnlyList<string> ErrorOutput,
    Exception? Exception);

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
        Completion = run(tEmployerCancellation.Token, AttachAsync);
    }

    internal Task<TEmployerResult> Completion { get; }

    internal async Task<int> ProcessIdRead()
    {
        Process process = await tEmployerAttached.Task.WaitAsync(TimeSpan.FromSeconds(10));
        return process.Id;
    }

    internal bool ChildAlive
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

    internal void Cancel()
    {
        tEmployerCancellation.Cancel();
        ProcessKill(Volatile.Read(ref tEmployerProcess));
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref tEmployerDisposed, 1) != 0)
        {
            return;
        }

        Cancel();
        tEmployerCancellation.Dispose();
    }

    private void AttachAsync(Process process)
    {
        Volatile.Write(ref tEmployerProcess, process);
        tEmployerAttached.TrySetResult(process);
        if (tEmployerCancellation.IsCancellationRequested)
        {
            ProcessKill(process);
        }
    }

    private static void ProcessKill(Process? process)
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
        File.WriteAllText(tEmployerScriptPath, ChildScript, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
    }

    internal TEmployerExecution Execute(params string[] arguments) =>
        ExecuteProgram(PowerShellPathRead(), ScriptPrefixRead(), arguments);

    internal TEmployerExecution ExecuteMissingProgram()
    {
        string missing = Path.Combine(tEmployerRoot, "missing-" + Guid.NewGuid().ToString("N") + ".exe");
        return ExecuteProgram(missing, string.Empty, ["success"]);
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

    private TEmployerExecution ExecuteProgram(string program, string prefix, IReadOnlyList<string> arguments)
    {
        ObjectDisposedException.ThrowIf(tEmployerDisposed != 0, this);
        var execution = new TEmployerExecution(
            (token, attach) => Run(program, prefix, ArgumentsJoin(arguments), token, attach));
        tEmployerExecutions.Add(execution);
        return execution;
    }

    private static async Task<TEmployerResult> Run(
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
                result.LEmployerExit == 0 ? TEmployerStatus.Succeeded : TEmployerStatus.Failed,
                result.LEmployerExit,
                result.LEmployerError,
                output.ToArray(),
                errorOutput.ToArray(),
                null);
        }
        catch (OperationCanceledException exception) when (token.IsCancellationRequested)
        {
            return new TEmployerResult(
                TEmployerStatus.Cancelled,
                null,
                string.Join(Environment.NewLine, errorOutput),
                output.ToArray(),
                errorOutput.ToArray(),
                exception);
        }
        catch (Exception exception)
        {
            return new TEmployerResult(
                token.IsCancellationRequested ? TEmployerStatus.Cancelled : TEmployerStatus.Failed,
                null,
                string.Join(Environment.NewLine, errorOutput),
                output.ToArray(),
                errorOutput.ToArray(),
                exception);
        }
    }

    private string ScriptPrefixRead() =>
        $"-NoLogo -NoProfile -NonInteractive -ExecutionPolicy Bypass -File {ArgumentQuote(tEmployerScriptPath)}";

    private static string PowerShellPathRead() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.System),
        "WindowsPowerShell",
        "v1.0",
        "powershell.exe");

    private static string ArgumentsJoin(IReadOnlyList<string> arguments) =>
        string.Join(" ", arguments.Select(ArgumentQuote));

    private static string ArgumentQuote(string argument)
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

    private const string ChildScript = """
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

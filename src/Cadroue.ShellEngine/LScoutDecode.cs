using Cadroue.Core;

namespace Cadroue.ShellEngine;

internal static class LScoutDecode
{
    internal static async Task<bool> LScoutDecodeCheck(
        LRunner lScoutRunner, string lScoutOutputPath, CancellationToken lScoutToken = default)
    {
        if (string.IsNullOrWhiteSpace(lScoutOutputPath) || !File.Exists(lScoutOutputPath))
        {
            return false;
        }

        string lScoutBaseArguments = "-hide_banner -nostdin -v error -xerror "
            + $"-i {LEncode.LEncodeFormat(lScoutOutputPath)} -f null -";
        string lScoutArguments = lScoutRunner.LRunnerArgumentTransform?.Invoke(lScoutBaseArguments)
            ?? lScoutBaseArguments;

        try
        {
            LEmployerResult lScoutResult = await LEmployerEngine.LEmployerRunSeam(
                lScoutRunner.LRunnerProgramPath,
                lScoutRunner.LRunnerArgumentPrefix,
                LCustodyFamily.LCustodyFamilyEncode,
                lScoutRunner.LRunnerLevel,
                null,
                lScoutArguments,
                lScoutToken,
                null,
                static _ => { },
                static _ => { }).ConfigureAwait(false);
            lScoutToken.ThrowIfCancellationRequested();
            return lScoutResult.LEmployerExit == 0
                && string.IsNullOrWhiteSpace(lScoutResult.LEmployerError);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            lScoutToken.ThrowIfCancellationRequested();
            return false;
        }
    }
}

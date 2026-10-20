using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// One trial row: the repertoire entry, the encoder token tried, whether it passed, and its message.
public sealed record CSEncoderRow(
    string CSEncoderRowFamily,
    string CSEncoderRowEncoder,
    bool CSEncoderRowSuccess,
    string CSEncoderRowMessage);

// The encoder window's video trial: it tries every encoder token, keeps the available set and this window's rows,
// and traces the verification.
public sealed class CSEncoderTrial
{
    private readonly LSTrialPort _csEncoderTrialPort;
    private readonly LSRepertoirePort _csEncoderTrialRepertoire;
    private readonly CSEncoderVideo _csEncoderTrialVideo;
    private IReadOnlyList<CSEncoderRow> _csEncoderTrialRows = [];
    private CancellationTokenSource? _csEncoderTrialSource;

    internal CSEncoderTrial(LSTrialPort port, LSRepertoirePort repertoire, CSEncoderVideo video)
    {
        _csEncoderTrialPort = port;
        _csEncoderTrialRepertoire = repertoire;
        _csEncoderTrialVideo = video;
    }

    public IReadOnlyList<CSEncoderRow> CSEncoderTrialRows => _csEncoderTrialRows;

    public IReadOnlyList<CSEncoderTrialEntry> CSEncoderTrialRead<CSEncoderTrialEntry>(
        Func<string, string, bool, string, CSEncoderTrialEntry> compose)
    {
        ArgumentNullException.ThrowIfNull(compose);
        return _csEncoderTrialRows
            .Select(row => compose(
                row.CSEncoderRowFamily, row.CSEncoderRowEncoder, row.CSEncoderRowSuccess, row.CSEncoderRowMessage))
            .ToArray();
    }

    // The available repertoire entries, with the chosen encoder first when it is a repertoire entry that failed.
    // A cancelled trial throws and keeps nothing.
    public async Task<(IReadOnlyList<string>, int)> CSEncoderTrialRun(IProgress<double> feed)
    {
        CancellationToken token = CSEncoderTrialStart();
        IReadOnlyList<LRepertoireEncoder> candidates = _csEncoderTrialRepertoire.LSRepertoireEncodersRead();
        (List<string> available, HashSet<string> names, List<CSEncoderRow> rows) =
            await CSEncoderTrialScan(candidates, feed, token);
        token.ThrowIfCancellationRequested();
        _csEncoderTrialPort.LSTrialApply(names);
        CSEncoderTrialInsert(available, candidates);
        _csEncoderTrialRows = rows;
        CSEncoderTrialRecord(_csEncoderTrialPort, "video", rows);
        return CSEncoderVideo.CSEncoderChoiceResolve(
            available, _csEncoderTrialVideo.CSEncoderVideoState.CSEncoderStateEncoder);
    }

    // A new trial cancels the one still running; closing the window cancels the last one.
    private CancellationToken CSEncoderTrialStart()
    {
        _csEncoderTrialSource?.Cancel();
        _csEncoderTrialSource = new CancellationTokenSource();
        return _csEncoderTrialSource.Token;
    }

    public void CSEncoderTrialCancel() => _csEncoderTrialSource?.Cancel();

    private async Task<(List<string>, HashSet<string>, List<CSEncoderRow>)> CSEncoderTrialScan(
        IReadOnlyList<LRepertoireEncoder> candidates, IProgress<double> feed, CancellationToken token)
    {
        var available = new List<string>();
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var rows = new List<CSEncoderRow>();
        int total = candidates.Sum(candidate => candidate.LRepertoireTokens.Count);
        int done = 0;
        foreach (LRepertoireEncoder candidate in candidates)
        {
            bool candidateAvailable = false;
            foreach (string encoder in candidate.LRepertoireTokens)
            {
                (bool success, string message) = await _csEncoderTrialPort.LSTrialRun(encoder, token);
                candidateAvailable |= success;
                if (success)
                {
                    names.Add(encoder);
                }

                rows.Add(new CSEncoderRow(candidate.LRepertoireText, encoder, success, message));
                done++;
                feed.Report(CSEncoder.CSEncoderProgressResolve(done, total));
            }

            if (candidateAvailable)
            {
                available.Add(candidate.LRepertoireText);
            }
        }

        return (available, names, rows);
    }

    private void CSEncoderTrialInsert(List<string> available, IReadOnlyList<LRepertoireEncoder> candidates)
    {
        string encoder = _csEncoderTrialVideo.CSEncoderVideoState.CSEncoderStateEncoder;
        if (!available.Contains(encoder)
            && candidates.Any(candidate => string.Equals(
                candidate.LRepertoireText, encoder, StringComparison.Ordinal)))
        {
            available.Insert(0, encoder);
        }
    }

    // How many encoders passed, with one detail line per row.
    internal static void CSEncoderTrialRecord(LSTrialPort port, string kind, IReadOnlyList<CSEncoderRow> rows)
    {
        int passed = rows.Count(row => row.CSEncoderRowSuccess);
        port.LSTrialRecord(
            $"Encoder verification ({kind}): {passed} of {rows.Count} encoder(s) available",
            CSEncoderTrialFormat(rows));
    }

    private static string CSEncoderTrialFormat(IReadOnlyList<CSEncoderRow> rows)
    {
        var detail = new StringBuilder();
        foreach (CSEncoderRow row in rows)
        {
            detail.AppendLine(
                $"{row.CSEncoderRowFamily} / {row.CSEncoderRowEncoder}: {(row.CSEncoderRowSuccess ? "OK" : "FAIL")} - "
                + $"{row.CSEncoderRowMessage}");
        }

        return detail.ToString().TrimEnd();
    }
}

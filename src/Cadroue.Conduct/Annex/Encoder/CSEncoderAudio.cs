using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// One kept audio choice: the encoder, its rate resolved against the codec's modes, and the modes the rate row offers.
public sealed record CSEncoderAudioState(
    string CSEncoderAudioEncoder,
    string CSEncoderAudioRate,
    IReadOnlyList<string> CSEncoderAudioModes);

// The encoder window's audio codec: the chosen encoder and rate, the encoders a container offers, and whether an
// encoder is available.
public sealed class CSEncoderAudio
{
    private readonly LSTrialPort _csEncoderAudioPort;
    private readonly LSRepertoirePort _csEncoderAudioRepertoire;
    private CSEncoderAudioState _csEncoderAudioState;
    private IReadOnlyList<CSEncoderRow> _csEncoderAudioRows = [];
    private CancellationTokenSource? _csEncoderAudioSource;

    internal CSEncoderAudio(string encoder, string rate, LSTrialPort port, LSRepertoirePort repertoire)
    {
        _csEncoderAudioPort = port;
        _csEncoderAudioRepertoire = repertoire;
        _csEncoderAudioState = CSEncoderAudioResolve(encoder, rate);
    }

    // Raised once per kept change of encoder or rate, with the whole new choice.
    public event Action<CSEncoderAudioState>? CSEncoderAudioChanged;

    public CSEncoderAudioState CSEncoderAudioState => _csEncoderAudioState;

    public IReadOnlyList<CSEncoderRow> CSEncoderAudioRows => _csEncoderAudioRows;

    // Keeps the encoder and its rate resolved against the encoder's codec; answers whether the choice changed.
    public bool CSEncoderAudioSet(string encoder, string rate)
    {
        CSEncoderAudioState kept = _csEncoderAudioState;
        bool encoderChanged = !string.Equals(kept.CSEncoderAudioEncoder, encoder, StringComparison.Ordinal);
        CSEncoderAudioState state = CSEncoderAudioResolve(encoder, rate);
        if (!encoderChanged
            && string.Equals(kept.CSEncoderAudioRate, state.CSEncoderAudioRate, StringComparison.Ordinal))
        {
            return false;
        }

        _csEncoderAudioState = state;
        CSEncoderAudioChanged?.Invoke(state);
        return true;
    }

    // The available audio encoders a container takes, with the kept one first when it fits the container yet is not
    // offered; an unknown container takes every available encoder.
    public string[] CSEncoderAudioRead(string container, string keep)
    {
        LSRepertoirePort repertoire = _csEncoderAudioRepertoire;
        bool known = repertoire.LSRepertoireContainerNames.Contains(container);
        string[] items = repertoire.LSRepertoireAudioCandidates
            .Where(candidate => CSEncoderAudioCheck(candidate.LRepertoireText)
                && (!known || repertoire.LSRepertoireAudioCheck(candidate.LRepertoireName, container)))
            .Select(candidate => candidate.LRepertoireText)
            .ToArray();
        if (string.IsNullOrEmpty(keep) || items.Contains(keep))
        {
            return items;
        }

        string? name = repertoire.LSRepertoireAudioResolve(keep);
        bool fits = name is not null
            && (!known || repertoire.LSRepertoireAudioCheck(name, container));
        return fits ? [keep, .. items] : items;
    }

    // Whether an audio encoder text is available: a repertoire entry asks the audio trial set, any other text is.
    public bool CSEncoderAudioCheck(string text)
    {
        if (_csEncoderAudioRepertoire.LSRepertoireAudioResolve(text) is not { } name)
        {
            return true;
        }

        return _csEncoderAudioPort.LSTrialAudioCheck(name);
    }

    public async Task<IReadOnlyList<string>> CSEncoderAudioRun(IProgress<double> feed)
    {
        CancellationToken token = CSEncoderAudioStart();
        IReadOnlyList<LRepertoireAudio> candidates = _csEncoderAudioRepertoire.LSRepertoireAudioCandidates;
        (List<string> available, List<string> names, List<CSEncoderRow> rows) =
            await CSEncoderAudioScan(candidates, feed, token);
        token.ThrowIfCancellationRequested();
        _csEncoderAudioPort.LSTrialAudioApply(names);
        CSEncoderAudioInsert(available);
        _csEncoderAudioRows = rows;
        CSEncoderTrial.CSEncoderTrialRecord(_csEncoderAudioPort, "audio", rows);
        return available;
    }

    private CancellationToken CSEncoderAudioStart()
    {
        _csEncoderAudioSource?.Cancel();
        _csEncoderAudioSource = new CancellationTokenSource();
        return _csEncoderAudioSource.Token;
    }

    public void CSEncoderAudioCancel() => _csEncoderAudioSource?.Cancel();

    private async Task<(List<string>, List<string>, List<CSEncoderRow>)> CSEncoderAudioScan(
        IReadOnlyList<LRepertoireAudio> candidates, IProgress<double> feed, CancellationToken token)
    {
        var available = new List<string>();
        var names = new List<string>();
        var rows = new List<CSEncoderRow>();
        int done = 0;
        foreach (LRepertoireAudio candidate in candidates)
        {
            (bool success, string message) =
                await _csEncoderAudioPort.LSTrialAudioRun(candidate.LRepertoireName, token);
            rows.Add(new CSEncoderRow(candidate.LRepertoireText, candidate.LRepertoireName, success, message));
            if (success)
            {
                available.Add(candidate.LRepertoireText);
                names.Add(candidate.LRepertoireName);
            }

            done++;
            feed.Report(CSEncoder.CSEncoderProgressResolve(done, candidates.Count));
        }

        return (available, names, rows);
    }

    private void CSEncoderAudioInsert(List<string> available)
    {
        string encoder = _csEncoderAudioState.CSEncoderAudioEncoder;
        if (!available.Contains(encoder)
            && _csEncoderAudioRepertoire.LSRepertoireAudioResolve(encoder) is not null)
        {
            available.Insert(0, encoder);
        }
    }

    // The choice an encoder and an asked rate make: the rate resolved against the modes the encoder's codec offers.
    private CSEncoderAudioState CSEncoderAudioResolve(string encoder, string rate)
    {
        string[] modes = _csEncoderAudioRepertoire.LSRepertoireAudioRead(encoder).LCapabilityModeLabels;
        return new CSEncoderAudioState(encoder, CSEncoder.CSEncoderRateResolve(modes, rate), modes);
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
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

public sealed record CSEncoderSound(
    CSEncoderCodec CSEncoderSoundCodec,
    string CSEncoderSoundOption,
    (IReadOnlyList<int>, bool, double, string) CSEncoderSoundSample,
    (IReadOnlyList<string>, int) CSEncoderSoundChannel);

// The encoder window's audio codec: the chosen encoder and rate, the encoders a container offers, and whether an
// encoder is available.
public sealed class CSEncoderAudio
{
    private readonly LSTrialPort _csEncoderAudioPort;
    private readonly LSRepertoirePort _csEncoderAudioRepertoire;
    private readonly LSEncoderPort _csEncoderAudioDraft;
    private readonly CSEncoderChannel _csEncoderAudioChannel = new();
    private CSEncoderAudioState _csEncoderAudioState;
    private IReadOnlyList<CSEncoderRow> _csEncoderAudioRows = [];
    private CancellationTokenSource? _csEncoderAudioSource;
    private CSEncoderCodec? _csEncoderAudioCodec;

    internal CSEncoderAudio(
        string encoder, string rate, LSTrialPort port, LSRepertoirePort repertoire, LSEncoderPort draft)
    {
        _csEncoderAudioPort = port;
        _csEncoderAudioRepertoire = repertoire;
        _csEncoderAudioDraft = draft;
        _csEncoderAudioState = CSEncoderAudioResolve(encoder, rate);
    }

    // Raised once per kept change of encoder or rate, with the whole new choice.
    public event Action<CSEncoderAudioState>? CSEncoderAudioChanged;

    public CSEncoderAudioState CSEncoderAudioState => _csEncoderAudioState;

    public (CSEncoderAudioState, string, string, string[], bool) CSEncoderAudioOpen()
    {
        CSEncoderAudioState state = _csEncoderAudioState;
        (_, _, string stream, string mode, _, _, _, _, _) = _csEncoderAudioDraft.LSEncoderAudioRead();
        (string[] offer, _, bool available) = CSEncoderAudioRead(
            _csEncoderAudioDraft.LSEncoderVideoRead().Item5, state.CSEncoderAudioEncoder);
        return (state, stream, mode, offer, available);
    }

    public CSEncoderSound CSEncoderAudioRead(string? sample, double? channel)
    {
        CSEncoderAudioState state = _csEncoderAudioState;
        (LCapabilityCodec codec, LCapabilityMode mode) = _csEncoderAudioRepertoire.LSRepertoireAudioRead(
            state.CSEncoderAudioEncoder, state.CSEncoderAudioRate);
        (_, string rate, _, _, string quality, string speed, IReadOnlyDictionary<string, string> extras,
            string keptSample, string keptChannel) = _csEncoderAudioDraft.LSEncoderAudioRead();
        (IReadOnlyList<string> layouts, IReadOnlyList<int> samples) =
            _csEncoderAudioDraft.LSEncoderHelpRead(state.CSEncoderAudioEncoder);
        _csEncoderAudioCodec = CSEncoderCodec.CSEncoderCodecRead(
            codec,
            mode,
            () => (rate, quality, speed, extras),
            mode.LCapabilityModeLabel);
        (IReadOnlyList<int> ticks, bool discrete, double maximum, string kept) =
            CSEncoderSample.CSEncoderSampleResolve(sample, keptSample, samples);
        (IReadOnlyList<string> channels, int index) =
            _csEncoderAudioChannel.CSEncoderChannelResolve(channel, keptChannel, layouts);
        string optionKey = string.IsNullOrEmpty(_csEncoderAudioCodec.CSEncoderCodecQuality?.CSEncoderQualityRange)
            ? "Encoder.Audio.FFmpegOption"
            : "Encoder.Audio.FFmpegOptionRange";
        return new CSEncoderSound(
            _csEncoderAudioCodec, optionKey, (ticks, discrete, maximum, kept), (channels, index));
    }

    public void CSEncoderAudioSave(
        string stream,
        string mode,
        string? quality,
        int speed,
        IReadOnlyList<int> extras,
        string? sample,
        double? channel)
    {
        CSEncoderCodec? codec = _csEncoderAudioCodec;
        _csEncoderAudioDraft.LSEncoderAudioSet(
            stream,
            mode,
            quality?.Trim() ?? string.Empty,
            codec?.CSEncoderCodecSpeed?.CSEncoderSpeedResolve(speed) ?? string.Empty,
            codec?.CSEncoderCodecResolve(extras) ?? new Dictionary<string, string>(StringComparer.Ordinal),
            CSEncoderSample.CSEncoderSampleRead(sample),
            _csEncoderAudioChannel.CSEncoderChannelRead(channel));
    }

    public IReadOnlyList<CSEncoderRow> CSEncoderAudioRows => _csEncoderAudioRows;

    // Keeps the encoder and its rate resolved against the encoder's codec; answers whether the choice changed.
    public bool CSEncoderAudioSet(string encoder, string rate)
    {
        if (encoder.Length == 0 || rate.Length == 0)
        {
            return false;
        }

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
    public (string[], string, bool) CSEncoderAudioRead(string container, string keep)
    {
        LSRepertoirePort repertoire = _csEncoderAudioRepertoire;
        bool known = repertoire.LSRepertoireContainerNames.Contains(container);
        string[] items = repertoire.LSRepertoireAudioCandidates
            .Where(candidate => CSEncoderAudioCheck(candidate.LRepertoireText)
                && (!known || repertoire.LSRepertoireAudioCheck(candidate.LRepertoireName, container)))
            .Select(candidate => candidate.LRepertoireText)
            .ToArray();
        string[] offer = items;
        if (!string.IsNullOrEmpty(keep) && !items.Contains(keep))
        {
            string? name = repertoire.LSRepertoireAudioResolve(keep);
            bool fits = name is not null
                && (!known || repertoire.LSRepertoireAudioCheck(name, container));
            offer = fits ? [keep, .. items] : items;
        }

        string selected = offer.Contains(keep) ? keep : offer.FirstOrDefault() ?? string.Empty;
        return (offer, selected, CSEncoderAudioCheck(selected));
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

    public (bool, string) CSEncoderAudioCheck(string stream, string mode)
    {
        bool excluded = stream == "Exclude" || mode == "Exclude";
        return (
            !excluded && mode != "Copy",
            excluded ? "Encoder.Audio.Notice.Excluded" : "Encoder.Audio.Notice.Copied");
    }

    public async Task<(IReadOnlyList<string>, string, bool)> CSEncoderAudioRun(IProgress<double> feed)
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
        string selected = _csEncoderAudioState.CSEncoderAudioEncoder;
        return (available, selected, CSEncoderAudioCheck(selected));
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

internal sealed class CSEncoderSample
{
    private static readonly int[] _csEncoderSampleStandard =
        [8000, 11025, 16000, 22050, 24000, 32000, 44100, 48000, 88200, 96000, 176400, 192000];

    internal static (IReadOnlyList<int>, bool, double, string) CSEncoderSampleResolve(
        string? shown, string kept, IReadOnlyList<int> rates)
    {
        kept = shown is null ? kept : CSEncoderSampleRead(shown);
        bool discrete = rates.Count > 0;
        IReadOnlyList<int> ticks = discrete ? rates : _csEncoderSampleStandard;
        return (ticks, discrete, ticks.Count > 0 ? ticks[^1] : 48000, kept);
    }

    internal static string CSEncoderSampleRead(string? shown) =>
        int.TryParse(shown?.Trim() ?? string.Empty, NumberStyles.Integer, CultureInfo.CurrentCulture, out int hertz)
        && hertz > 0
            ? hertz.ToString(CultureInfo.CurrentCulture)
            : CSEncoderDraft.CSEncoderDraftSource;
}

internal sealed class CSEncoderChannel
{
    private static readonly string[] _csEncoderChannelStandard =
        ["mono", "stereo", "2.1", "3.0", "4.0", "5.0", "5.1", "6.1", "7.1"];

    private IReadOnlyList<string> _csEncoderChannelLayouts = [];

    internal (IReadOnlyList<string>, int) CSEncoderChannelResolve(
        double? position, string kept, IReadOnlyList<string> layouts)
    {
        kept = position is null ? kept : CSEncoderChannelRead(position);
        _csEncoderChannelLayouts = layouts.Count == 0 ? _csEncoderChannelStandard : layouts;
        int index = 0;
        for (int at = 0; at < _csEncoderChannelLayouts.Count; at++)
        {
            if (string.Equals(_csEncoderChannelLayouts[at], kept, StringComparison.OrdinalIgnoreCase))
            {
                index = at + 1;
                break;
            }
        }

        return (_csEncoderChannelLayouts, index);
    }

    internal string CSEncoderChannelRead(double? position)
    {
        if (position is not { } value)
        {
            return CSEncoderDraft.CSEncoderDraftSource;
        }

        int index = Math.Clamp((int)Math.Round(value), 0, _csEncoderChannelLayouts.Count);
        return index <= 0 ? CSEncoderDraft.CSEncoderDraftSource : _csEncoderChannelLayouts[index - 1];
    }
}

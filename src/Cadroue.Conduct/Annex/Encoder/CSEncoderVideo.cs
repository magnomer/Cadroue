using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// One kept video choice: the encoder, its rate resolved against the codec's modes, and what the rate rows read.
public sealed record CSEncoderState(
    string CSEncoderStateEncoder,
    string CSEncoderStateRate,
    CSEncoderCodec CSEncoderStateCodec)
{
    // The kept mode is the codec's mode the resolved rate labels.
    internal static CSEncoderState CSEncoderStateRead(
        LSRepertoirePort repertoire, LSEncoderPort draft, string encoder, string rate)
    {
        LCapabilityCodec codec =
            repertoire.LSRepertoireCodecRead(repertoire.LSRepertoireTokenResolve(encoder) ?? string.Empty);
        string[] labels = codec.LCapabilityModeLabels;
        string resolved = CSEncoder.CSEncoderRateResolve(labels, rate);
        LCapabilityMode mode = codec.LCapabilityModes[Array.IndexOf(labels, resolved)];
        return new CSEncoderState(encoder, resolved, CSEncoderCodec.CSEncoderCodecRead(codec, mode, draft, resolved));
    }
}

// The encoder window's video codec: the chosen encoder and rate, the notice each mode shows, the encoders a
// container offers, and whether an encoder is available.
public sealed class CSEncoderVideo
{
    private readonly LSTrialPort _csEncoderVideoPort;
    private readonly LSRepertoirePort _csEncoderVideoRepertoire;
    private readonly bool _csEncoderVideoSmart;
    private readonly LSEncoderPort _csEncoderVideoDraft;
    private CSEncoderState _csEncoderVideoState;
    private string _csEncoderVideoMode;

    internal CSEncoderVideo(
        string encoder, string rate, LSTrialPort port, LSRepertoirePort repertoire, LSEncoderPort draft, bool smart)
    {
        _csEncoderVideoPort = port;
        _csEncoderVideoRepertoire = repertoire;
        _csEncoderVideoSmart = smart;
        _csEncoderVideoDraft = draft;
        _csEncoderVideoState = CSEncoderState.CSEncoderStateRead(repertoire, draft, encoder, rate);
        string stored = draft.LSEncoderVideoRead().Item1;
        _csEncoderVideoMode = CSEncoderVideoModes.Any(mode => mode.Item1 == stored)
            ? stored
            : CSEncoderVideoModes[0].Item1;
    }

    // Raised once per kept change of encoder or rate, with the whole new choice.
    public event Action<CSEncoderState>? CSEncoderVideoChanged;

    internal static IReadOnlyList<(string, string)> CSEncoderVideoModes { get; } =
    [
        ("Copy", "Encoder.Codec.Copy"),
        ("Smart", "Encoder.Codec.Smart"),
        ("Encode", "Encoder.Codec.Encode")
    ];

    private static IReadOnlyList<(string, string)> CSEncoderVideoLayouts { get; } =
    [
        ("Auto", "Encoder.Codec.Auto"),
        ("yuv420p", "Encoder.Pixel.Yuv420"),
        ("yuv422p", "Encoder.Pixel.Yuv422"),
        ("yuv444p", "Encoder.Pixel.Yuv444"),
        ("yuv420p10le", "Encoder.Pixel.Yuv420Ten"),
        ("yuv422p10le", "Encoder.Pixel.Yuv422Ten"),
        ("yuv444p10le", "Encoder.Pixel.Yuv444Ten")
    ];

    public CSEncoderState CSEncoderVideoState => _csEncoderVideoState;

    public bool CSEncoderVideoEncoded => _csEncoderVideoMode == "Encode";

    public (IReadOnlyList<string>, int) CSEncoderVideoOffer
    {
        get
        {
            string container = _csEncoderVideoDraft.LSEncoderVideoRead().Item5;
            return CSEncoderVideoRead(container, _csEncoderVideoState.CSEncoderStateEncoder);
        }
    }

    internal string CSEncoderVideoMode => _csEncoderVideoMode;

    public static IReadOnlyList<(string, double, double, double)> CSEncoderFacetRead(Func<string, string> translate)
    {
        ArgumentNullException.ThrowIfNull(translate);
        int last = CSEncoderVideoModes.Count - 1;
        return CSEncoderVideoModes
            .Select((mode, index) => (
                translate(mode.Item2),
                index == 0 ? 0.0 : 1.0,
                index == 0 ? 5.0 : 0.0,
                index == last ? 5.0 : 0.0))
            .ToArray();
    }

    internal static (IReadOnlyList<string>, int) CSEncoderChoiceResolve(IReadOnlyList<string> items, string current)
    {
        ArgumentNullException.ThrowIfNull(items);
        int kept = items.ToList().IndexOf(current);
        return (items, kept >= 0 || items.Count == 0 ? kept : 0);
    }

    public bool CSEncoderModeCheck(int index) =>
        index >= 0 && index < CSEncoderVideoModes.Count && CSEncoderVideoModes[index].Item1 == _csEncoderVideoMode;

    public void CSEncoderModeSet(int index)
    {
        if (index >= 0 && index < CSEncoderVideoModes.Count)
        {
            _csEncoderVideoMode = CSEncoderVideoModes[index].Item1;
        }
    }

    public (IReadOnlyList<string>, int) CSEncoderLayoutRead(Func<string, string> translate)
    {
        ArgumentNullException.ThrowIfNull(translate);
        string stored = _csEncoderVideoDraft.LSEncoderVideoRead().Item4;
        (_, int kept) = CSEncoderChoiceResolve(CSEncoderVideoLayouts.Select(layout => layout.Item1).ToArray(), stored);
        return (CSEncoderVideoLayouts.Select(layout => translate(layout.Item2)).ToArray(), kept);
    }

    internal static string CSEncoderLayoutRead(int index) =>
        index >= 0 && index < CSEncoderVideoLayouts.Count ? CSEncoderVideoLayouts[index].Item1 : string.Empty;

    public string CSEncoderVideoResolve() => CSEncoderVideoResolve(_csEncoderVideoMode);

    public string[]? CSEncoderVideoSync(object? shown)
    {
        string[] modes = _csEncoderVideoState.CSEncoderStateCodec.CSEncoderCodecModes.ToArray();
        return shown is string[] kept && kept.SequenceEqual(modes) ? null : modes;
    }

    // The notice key a video mode shows: copied streams, or smart cutting as this window's work allows it.
    public string CSEncoderVideoResolve(string mode)
    {
        if (mode != "Smart")
        {
            return "Encoder.Video.Notice.Copied";
        }

        return _csEncoderVideoSmart ? "Encoder.Video.Notice.Smart" : "Encoder.Video.Notice.SmartFull";
    }

    // Keeps the encoder and its rate resolved against the encoder's codec; answers whether the choice changed.
    public bool CSEncoderVideoSet(string encoder, string rate)
    {
        CSEncoderState kept = _csEncoderVideoState;
        bool encoderChanged = !string.Equals(kept.CSEncoderStateEncoder, encoder, StringComparison.Ordinal);
        CSEncoderState state =
            CSEncoderState.CSEncoderStateRead(_csEncoderVideoRepertoire, _csEncoderVideoDraft, encoder, rate);
        if (!encoderChanged
            && string.Equals(kept.CSEncoderStateRate, state.CSEncoderStateRate, StringComparison.Ordinal))
        {
            return false;
        }

        _csEncoderVideoState = state;
        CSEncoderVideoChanged?.Invoke(state);
        return true;
    }

    // The available encoders a container takes, with the kept one first when it fits the container yet is not
    // offered; an unknown container takes every available encoder.
    public (IReadOnlyList<string>, int) CSEncoderVideoRead(string container, string keep)
    {
        LSRepertoirePort repertoire = _csEncoderVideoRepertoire;
        bool known = repertoire.LSRepertoireContainerNames.Contains(container);
        string[] items = repertoire.LSRepertoireEncodersRead()
            .Where(candidate =>
                (!known || repertoire.LSRepertoireContainerCheck(candidate.LRepertoireText, container))
                && _csEncoderVideoPort.LSTrialCheck(candidate))
            .Select(candidate => candidate.LRepertoireText)
            .ToArray();
        if (string.IsNullOrEmpty(keep) || items.Contains(keep))
        {
            return CSEncoderChoiceResolve(items, keep);
        }

        bool fits = repertoire.LSRepertoireEncodersRead()
                .Any(candidate => string.Equals(candidate.LRepertoireText, keep, StringComparison.Ordinal))
            && (!known || repertoire.LSRepertoireContainerCheck(keep, container));
        return CSEncoderChoiceResolve(fits ? [keep, .. items] : items, keep);
    }

    // Whether an encoder text is available: a repertoire entry asks the trial set, any other text is.
    public bool CSEncoderVideoCheck(string text)
    {
        foreach (LRepertoireEncoder candidate in _csEncoderVideoRepertoire.LSRepertoireEncodersRead())
        {
            if (string.Equals(candidate.LRepertoireText, text, StringComparison.Ordinal))
            {
                return _csEncoderVideoPort.LSTrialCheck(candidate);
            }
        }

        return true;
    }
}

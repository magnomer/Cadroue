using System;
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
    internal static CSEncoderState CSEncoderStateRead(LSRepertoirePort repertoire, string encoder, string rate)
    {
        LCapabilityCodec codec =
            repertoire.LSRepertoireCodecRead(repertoire.LSRepertoireTokenResolve(encoder) ?? string.Empty);
        string[] labels = codec.LCapabilityModeLabels;
        string resolved = CSEncoder.CSEncoderRateResolve(labels, rate);
        LCapabilityMode mode = codec.LCapabilityModes[Array.IndexOf(labels, resolved)];
        return new CSEncoderState(encoder, resolved, CSEncoderCodec.CSEncoderCodecRead(codec, mode));
    }
}

// The encoder window's video codec: the chosen encoder and rate, the notice each mode shows, the encoders a
// container offers, and whether an encoder is available.
public sealed class CSEncoderVideo
{
    private readonly LSTrialPort _csEncoderVideoPort;
    private readonly LSRepertoirePort _csEncoderVideoRepertoire;
    private readonly bool _csEncoderVideoSmart;
    private CSEncoderState _csEncoderVideoState;

    internal CSEncoderVideo(string encoder, string rate, LSTrialPort port, LSRepertoirePort repertoire, bool smart)
    {
        _csEncoderVideoPort = port;
        _csEncoderVideoRepertoire = repertoire;
        _csEncoderVideoSmart = smart;
        _csEncoderVideoState = CSEncoderState.CSEncoderStateRead(repertoire, encoder, rate);
    }

    // Raised once per kept change of encoder or rate, with the whole new choice.
    public event Action<CSEncoderState>? CSEncoderVideoChanged;

    public CSEncoderState CSEncoderVideoState => _csEncoderVideoState;

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
        CSEncoderState state = CSEncoderState.CSEncoderStateRead(_csEncoderVideoRepertoire, encoder, rate);
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
    public string[] CSEncoderVideoRead(string container, string keep)
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
            return items;
        }

        bool fits = repertoire.LSRepertoireEncodersRead()
                .Any(candidate => string.Equals(candidate.LRepertoireText, keep, StringComparison.Ordinal))
            && (!known || repertoire.LSRepertoireContainerCheck(keep, container));
        return fits ? [keep, .. items] : items;
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

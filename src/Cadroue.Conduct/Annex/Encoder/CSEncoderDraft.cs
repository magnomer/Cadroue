using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// One encoder window's draft: the video choice starts from it, and apply decides what the draft keeps before the
// port copies it back into the source preset.
public sealed class CSEncoderDraft
{
    private readonly LSEncoderPort _csEncoderDraftPort;

    internal CSEncoderDraft(LSEncoderPort draft, LSTrialPort trial, LSRepertoirePort repertoire, bool smart)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentNullException.ThrowIfNull(trial);
        ArgumentNullException.ThrowIfNull(repertoire);
        _csEncoderDraftPort = draft;
        (_, string encoder, string rate) = draft.LSEncoderDraftRead();
        CSEncoderDraftVideo = new CSEncoderVideo(encoder, rate, trial, repertoire, draft, smart);
        CSEncoderDraftTrial = new CSEncoderTrial(trial, repertoire, CSEncoderDraftVideo);
        CSEncoderDraftSize = new CSEncoderSize(draft.LSEncoderSizeRead());
        (string audioEncoder, string audioRate, _, _, _, _, _, _, _) = draft.LSEncoderAudioRead();
        CSEncoderDraftAudio = new CSEncoderAudio(audioEncoder, audioRate, trial, repertoire, draft);
        CSEncoderDraftSuffix = new CSEncoderSuffix(draft);
        CSEncoderDraftLocation = new CSEncoderLocation(draft);
        CSEncoderDraftOutput = new CSEncoderOutput(draft);
        draft.LSEncoderHelpAttach(CSEncoderDraftHandle);
    }

    public const string CSEncoderDraftSource = "Same as source";

    public event Action? CSEncoderDraftReady;

    public CSEncoderVideo CSEncoderDraftVideo { get; }

    public CSEncoderTrial CSEncoderDraftTrial { get; }

    public CSEncoderSize CSEncoderDraftSize { get; }

    public CSEncoderAudio CSEncoderDraftAudio { get; }

    public CSEncoderSuffix CSEncoderDraftSuffix { get; }

    public CSEncoderLocation CSEncoderDraftLocation { get; }

    public CSEncoderOutput CSEncoderDraftOutput { get; }

    // The draft keeps a display name, the video stream, the chosen video encoder and rate, the size its gate formats,
    // and the chosen audio encoder and rate.
    public void CSEncoderDraftApply()
    {
        string display = CSEncoderDraftNormalize(_csEncoderDraftPort.LSEncoderDraftRead().LSEncoderDisplay);
        CSEncoderState state = CSEncoderDraftVideo.CSEncoderVideoState;
        CSEncoderAudioState audio = CSEncoderDraftAudio.CSEncoderAudioState;
        _csEncoderDraftPort.LSEncoderDraftSave(
            display,
            "Include",
            state.CSEncoderStateEncoder,
            state.CSEncoderStateRate,
            CSEncoderDraftSize.CSEncoderSizeFormat(),
            audio.CSEncoderAudioEncoder,
            audio.CSEncoderAudioRate);
    }

    public void CSEncoderDraftSave() => _csEncoderDraftPort.LSEncoderSave();

    public (string, bool, string, string, string) CSEncoderDraftOrigin => _csEncoderDraftPort.LSEncoderVideoRead();

    public void CSEncoderDraftSet(
        string quality,
        string speed,
        bool? reactive,
        string fps,
        int layout,
        IReadOnlyDictionary<string, string> extras) =>
        _csEncoderDraftPort.LSEncoderVideoSave(
            CSEncoderDraftVideo.CSEncoderVideoMode,
            quality.Trim(),
            speed,
            reactive == true,
            fps,
            CSEncoderVideo.CSEncoderLayoutRead(layout),
            extras);

    // Closing the window cancels the trial still running.
    public void CSEncoderDraftClose()
    {
        _csEncoderDraftPort.LSEncoderHelpDetach(CSEncoderDraftHandle);
        CSEncoderDraftTrial.CSEncoderTrialCancel();
        CSEncoderDraftAudio.CSEncoderAudioCancel();
        _csEncoderDraftPort.LSEncoderClose();
    }

    private void CSEncoderDraftHandle(string encoder) => CSEncoderDraftReady?.Invoke();

    private static string CSEncoderDraftNormalize(string display) =>
        string.IsNullOrWhiteSpace(display) ? "{OriginalName}_export" : display.Trim();
}

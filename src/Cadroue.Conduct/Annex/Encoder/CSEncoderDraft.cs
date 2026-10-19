using System;
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
    }

    public CSEncoderVideo CSEncoderDraftVideo { get; }

    public CSEncoderTrial CSEncoderDraftTrial { get; }

    // The draft keeps a display name, the video stream, the chosen encoder and rate, and the size text. The size
    // text and audio's encoder and rate arrive as values until their own gates hold them.
    public void CSEncoderDraftApply(string size, string audioEncoder, string audioRate)
    {
        string display = CSEncoderDraftNormalize(_csEncoderDraftPort.LSEncoderDraftRead().LSEncoderDisplay);
        CSEncoderState state = CSEncoderDraftVideo.CSEncoderVideoState;
        _csEncoderDraftPort.LSEncoderDraftSave(
            display,
            "Include",
            state.CSEncoderStateEncoder,
            state.CSEncoderStateRate,
            size,
            audioEncoder,
            audioRate);
    }

    // Closing the window cancels the trial still running.
    public void CSEncoderDraftClose() => CSEncoderDraftTrial.CSEncoderTrialCancel();

    private static string CSEncoderDraftNormalize(string display) =>
        string.IsNullOrWhiteSpace(display) ? "{OriginalName}_export" : display.Trim();
}

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
        CSEncoderDraftSize = new CSEncoderSize(draft.LSEncoderSizeRead());
    }

    public const string CSEncoderDraftSource = "Same as source";

    public CSEncoderVideo CSEncoderDraftVideo { get; }

    public CSEncoderTrial CSEncoderDraftTrial { get; }

    public CSEncoderSize CSEncoderDraftSize { get; }

    // The draft keeps a display name, the video stream, the chosen encoder and rate, and the size its gate formats.
    // Audio's encoder and rate arrive as values until their own gate holds them.
    public void CSEncoderDraftApply(string audioEncoder, string audioRate)
    {
        string display = CSEncoderDraftNormalize(_csEncoderDraftPort.LSEncoderDraftRead().LSEncoderDisplay);
        CSEncoderState state = CSEncoderDraftVideo.CSEncoderVideoState;
        _csEncoderDraftPort.LSEncoderDraftSave(
            display,
            "Include",
            state.CSEncoderStateEncoder,
            state.CSEncoderStateRate,
            CSEncoderDraftSize.CSEncoderSizeFormat(),
            audioEncoder,
            audioRate);
    }

    // Closing the window cancels the trial still running.
    public void CSEncoderDraftClose() => CSEncoderDraftTrial.CSEncoderTrialCancel();

    private static string CSEncoderDraftNormalize(string display) =>
        string.IsNullOrWhiteSpace(display) ? "{OriginalName}_export" : display.Trim();
}

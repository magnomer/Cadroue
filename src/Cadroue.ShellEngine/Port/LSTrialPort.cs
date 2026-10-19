using Cadroue.Core;

namespace Cadroue.ShellEngine;

// The video encoder trial as the encoder window's gates reach it: one trial per encoder token, the available set
// kept for every later window, the set's verdict on one repertoire entry, and the verification trace.
public interface LSTrialPort
{
    // Runs one encoder token's trial; answers whether it passed and its message.
    Task<(bool LSTrialSuccess, string LSTrialMessage)> LSTrialRun(string lEncoder, CancellationToken lToken);

    // Keeps the tokens that passed as the available set; an empty set keeps the one held.
    void LSTrialApply(IEnumerable<string> lAvailable);

    // Whether the available set lets the entry through: always while no trial has kept a set.
    bool LSTrialCheck(LRepertoireEncoder lCandidate);

    Task<(bool LSTrialSuccess, string LSTrialMessage)> LSTrialAudioRun(string lName, CancellationToken lToken);

    void LSTrialAudioApply(IEnumerable<string> lAvailable);

    // Whether the audio available set holds the codec name; while no audio trial has kept a set, whether it is
    // installed.
    bool LSTrialAudioCheck(string lName);

    // Records the verification trace line with its detail.
    void LSTrialRecord(string lMessage, string lDetail);
}

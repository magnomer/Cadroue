using System;
using System.Collections.Generic;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

// The audio tab's sidecar saves: whether the inspector's persistent audio plan is saved, and with which skip switch.
// CEdit makes one over its own port.
public sealed class CEditAudio
{
    private readonly LEditPort _cEditAudioPort;

    internal CEditAudio(LEditPort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        _cEditAudioPort = port;
    }

    // The inspector's persistent audio plan is saved into each path's sidecar, merged with its saved plan, unless
    // saving is suspended or the user keeps no audio tool and no skip switch. The skip switch rides as kept and on.
    public void CEditAudioSave(CInspector inspector, bool suspended, IReadOnlyList<string> paths)
    {
        ArgumentNullException.ThrowIfNull(inspector);
        ArgumentNullException.ThrowIfNull(paths);
        bool kept = inspector.CInspectorVolume.CVolumePersistent
            || inspector.CInspectorLoudness.CLoudnessPersistent
            || inspector.CInspectorNoise.CNoisePersistent
            || inspector.CInspectorHighpass.CFilterPersistent
            || inspector.CInspectorLowpass.CFilterPersistent
            || inspector.CInspectorEqualizer.CEqualizerPersistent
            || inspector.CInspectorSkip.CSkipPersistent;
        if (suspended || !kept)
        {
            return;
        }

        _cEditAudioPort.LEditAudioSave(
            inspector.CInspectorAudio.CInspectorPersistentRead(),
            paths,
            inspector.CInspectorSkip.CSkipPersistent,
            inspector.CInspectorSkip.CSkipActive);
    }
}

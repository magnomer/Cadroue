namespace Cadroue.UIDeportment;

// Watches the persistent switch of every inspector tool and raises once when the set of kept tools changes.
public sealed class QInspectorPersistence
{
    private readonly QInspector qInspector;
    private int qInspectorPersistenceMask;

    public QInspectorPersistence(QInspector lOwner)
    {
        qInspector = lOwner;
        QInspectorPersistenceAttach();
        qInspectorPersistenceMask = QInspectorPersistenceResolve();
    }

    public event Action? QInspectorPersistenceChanged;

    private void QInspectorPersistenceAttach()
    {
        qInspector.QInspectorVideo.QInspectorSectionChanged += QInspectorPersistenceHandle;
        qInspector.QInspectorCrop.QInspectorCropbox.LCropboxStateChanged += QInspectorPersistenceHandle;
        qInspector.QInspectorAudio.QInspectorAudioChanged += QInspectorPersistenceHandle;
        qInspector.QInspectorSkip.QSkipChanged += QInspectorPersistenceHandle;
    }

    private void QInspectorPersistenceHandle()
    {
        int lMask = QInspectorPersistenceResolve();
        if (lMask == qInspectorPersistenceMask)
        {
            return;
        }

        qInspectorPersistenceMask = lMask;
        QInspectorPersistenceChanged?.Invoke();
    }

    private int QInspectorPersistenceResolve()
    {
        QInspectorVideo lVideo = qInspector.QInspectorVideo;
        QInspectorAudio lAudio = qInspector.QInspectorAudio;
        bool[] lFlags =
        {
            qInspector.QInspectorCrop.QInspectorPersistent,
            lVideo.QInspectorTone.CTonePersistents[0],
            lVideo.QInspectorTone.CTonePersistents[1],
            lVideo.QInspectorTone.CTonePersistents[2],
            lVideo.QInspectorGamma.CGammaPersistent,
            lVideo.QInspectorExposure.CExposurePersistent,
            lVideo.QInspectorCurve.QCurvePersistent,
            lVideo.QInspectorWhitebalance.CWhitebalancePersistent,
            lAudio.QInspectorVolume.CVolumePersistent,
            qInspector.QInspectorGate.CInspectorLoudness.CLoudnessPersistent,
            lAudio.QInspectorNoise.QNoisePersistent,
            qInspector.QInspectorGate.CInspectorHighpass.CFilterPersistent,
            qInspector.QInspectorGate.CInspectorLowpass.CFilterPersistent,
            lAudio.QInspectorEqualizer.QEqualizerPersistent,
            qInspector.QInspectorSkip.QSkipPersistent
        };
        return lFlags.Select((lFlag, lIndex) => lFlag ? 1 << lIndex : 0).Sum();
    }
}

using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// The clinic's wiring: hands every input to the clinic gate and shows the gate's wording keys through the catalog;
// the salvage block's wiring is QClinicSalvage. The members that only pass a gate answer or input on serve PClinic
// until its painting moves here.
public sealed class QClinic
{
    private readonly CClinic qClinicGate;

    public QClinic(CClinic lClinic)
    {
        ArgumentNullException.ThrowIfNull(lClinic);
        qClinicGate = lClinic;
        QClinicSalvage = new QClinicSalvage(lClinic);
    }

    public QClinicSalvage QClinicSalvage { get; }

    public event Action? QClinicChanged
    {
        add => qClinicGate.CClinicChanged += value;
        remove => qClinicGate.CClinicChanged -= value;
    }

    public event Action<bool>? QClinicMinimizeChanged
    {
        add => qClinicGate.CClinicMinimizeChanged += value;
        remove => qClinicGate.CClinicMinimizeChanged -= value;
    }

    public bool QClinicSalvageShown => qClinicGate.CClinicSalvageShown;

    public bool QClinicKnown => qClinicGate.CClinicKnown;

    public bool QClinicPersistentAllowed => qClinicGate.CClinicPersistentAllowed;

    public bool QClinicRepairChecked => qClinicGate.CClinicRepairChecked;

    public bool QClinicPersistentChecked => qClinicGate.CClinicPersistentChecked;

    public bool QClinicResultShown => qClinicGate.CClinicResultShown;

    public bool QClinicScanShown => qClinicGate.CClinicScanShown;

    public double QClinicProgressValue => qClinicGate.CClinicProgressValue;

    public bool QClinicMinimized => qClinicGate.CClinicMinimized;

    public string QClinicTitleRead() => QLocalizationCatalog.QLocalizationTextRead(qClinicGate.CClinicTitleKey);

    public string QClinicSimpleRead() => QClinicTextRead(qClinicGate.CClinicSimpleKey);

    public string QClinicTechnicalRead() => QClinicTextRead(qClinicGate.CClinicTechnicalKey);

    public string QClinicResultFormat() => qClinicGate.CClinicResultFormat(
    [
        QLocalizationCatalog.QLocalizationTextRead("Clinic.Result.Empty"),
        QLocalizationCatalog.QLocalizationTextRead("Clinic.Result.Scanning"),
        QLocalizationCatalog.QLocalizationTextRead("Clinic.Result.Clean"),
        QLocalizationCatalog.QLocalizationTextRead("Clinic.Result.Failed"),
        QLocalizationCatalog.QLocalizationTextRead("Clinic.Result.Defect"),
        QLocalizationCatalog.QLocalizationTextRead("Clinic.Result.Evidence"),
        QLocalizationCatalog.QLocalizationTextRead("Clinic.Result.Repair")
    ]);

    public void QClinicMinimizedSet(bool lMinimized) => qClinicGate.CClinicMinimizedSet(lMinimized);

    public void QClinicStepSet(string? lStepName) => qClinicGate.CClinicStepSet(lStepName);

    public void QClinicActiveSet(bool lActive) => qClinicGate.CClinicRepairSet(lActive);

    public void QClinicPersistentSet(bool lPersistent) => qClinicGate.CClinicPersistentSet(lPersistent);

    private static string QClinicTextRead(string? lKey) =>
        lKey is null ? string.Empty : QLocalizationCatalog.QLocalizationTextRead(lKey);
}

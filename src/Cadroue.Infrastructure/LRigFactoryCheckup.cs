using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LRigFactoryCheckup
{
    public static void LRigCheckupAttach(
        Func<
            string,
            IReadOnlyCollection<LFlawKind>,
            CancellationToken,
            IProgress<double>?,
            Task<IReadOnlyList<LDossier>>> lRigScanner)
    {
        LCheckup.LCheckupScannerSeam = lRigScanner;
    }

    public static LRigCheckup LRigCheckupCreate()
    {
        return new LRigCheckup(
            LCheckup.LCheckupCachedRead,
            LCheckup.LCheckupCachedSave);
    }
}

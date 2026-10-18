using System.IO;

using Cadroue.Conduct;
using Cadroue.UIDeportment.Capsule;

namespace Cadroue.UIDeportment;

// The placement store's reads and writes; the gate decides what is kept or imported and traces each failure.
public static class QPlacement
{
    private static readonly LCapsule qPlacementCapsule = new();

    private static readonly object qPlacementLock = new();

    public static Func<string> QPlacementRootSeam { get; set; } = static () => string.Empty;

    // Set once by the host before any read or save; interim until the subwindow drivers hold a placement.
    public static CPlacement QPlacementGateSeam { get; set; } = null!;

    public static bool QPlacementRead(
        string lPlacementKey,
        out double lLeft,
        out double lTop,
        out double lWidth,
        out double lHeight)
    {
        LPlacement? lPlacement = QPlacementFind(lPlacementKey);
        return QPlacementResolve(lPlacement, out lLeft, out lTop, out lWidth, out lHeight);
    }

    private static bool QPlacementResolve(
        LPlacement? lPlacement,
        out double lLeft,
        out double lTop,
        out double lWidth,
        out double lHeight)
    {
        lLeft = lPlacement?.LPlacementLeft ?? 0;
        lTop = lPlacement?.LPlacementTop ?? 0;
        lWidth = lPlacement?.LPlacementWidth ?? 0;
        lHeight = lPlacement?.LPlacementHeight ?? 0;
        return lPlacement is not null;
    }

    public static bool QPlacementSave(string lPlacementKey, double lLeft, double lTop, double lWidth, double lHeight) =>
        QPlacementGateSeam.CPlacementSave(lPlacementKey, lLeft, lTop, lWidth, lHeight, QPlacementCapsuleSave);

    public static void QPlacementImport(
        IReadOnlyList<(string QPlacementPrefix, string QPlacementKey)> lPlacementEntries) =>
        QPlacementGateSeam.CPlacementImport(lPlacementEntries, QPlacementKeyExist, QPlacementSave);

    // The store answers its own outcome; the gate traces it.
    private static (bool, string?) QPlacementCapsuleSave(
        string lPlacementKey, double lLeft, double lTop, double lWidth, double lHeight)
    {
        lock (qPlacementLock)
        {
            string lRoot = QPlacementRootSeam();
            return qPlacementCapsule.LCapsuleSave(
                lRoot, lPlacementKey, new LPlacement(lLeft, lTop, lWidth, lHeight));
        }
    }

    private static bool QPlacementKeyExist(string lPlacementKey) => QPlacementFind(lPlacementKey) is not null;

    private static LPlacement? QPlacementFind(string lPlacementKey)
    {
        lock (qPlacementLock)
        {
            string lRoot = QPlacementRootSeam();
            try
            {
                return qPlacementCapsule.LCapsuleFind(lRoot, lPlacementKey);
            }
            catch (InvalidDataException lUnreadable)
            {
                QPlacementGateSeam.CPlacementUnreadableRecord(lUnreadable);
                return null;
            }
        }
    }
}

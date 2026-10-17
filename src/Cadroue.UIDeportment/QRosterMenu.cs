using Cadroue.Conduct;
using Cadroue.Application;
using Cadroue.Media;

namespace Cadroue.UIDeportment;

public sealed record QRosterItem(
    string QRosterItemText,
    string QRosterItemIcon,
    bool QRosterItemEnabled,
    Action QRosterItemAction);

public sealed class QRosterMenu
{
    private static readonly IReadOnlyList<QRosterItem> QRosterMenuNone = [];

    private readonly CRoster qRosterGate;

    public QRosterMenu(CRoster lRoster)
    {
        qRosterGate = lRoster;
    }

    public IReadOnlyList<QRosterItem> QRosterMenuRead(Guid lId, QStrip? lStrip)
    {
        qRosterGate.CRosterMenuRead(lId);
        return QRosterItemsRead(
            qRosterGate.CRosterMenuRestartable,
            qRosterGate.CRosterMenuCancellable,
            qRosterGate.CRosterMenuRelay,
            qRosterGate.CRosterMenuCount,
            lStrip);
    }

    // Paints the gate's answer: one run item worded for its count, or the relay header over the strip's tabs.
    private IReadOnlyList<QRosterItem> QRosterItemsRead(
        bool lRestart, bool lCancel, bool lRelay, int lCount, QStrip? lStrip)
    {
        if (lRestart)
        {
            return [QRosterItemCreate(lCount, "Roster.Menu.Restart", "Roster.Menu.RestartMany")];
        }

        if (lCancel)
        {
            return [QRosterItemCreate(lCount, "Roster.Menu.Cancel", "Roster.Menu.CancelMany")];
        }

        if (!lRelay || lStrip is null)
        {
            return QRosterMenuNone;
        }

        QRosterItem[] lTargetItems = QRosterTabsRead(lStrip);
        if (lTargetItems.Length == 0)
        {
            return QRosterMenuNone;
        }

        return [QRosterHeaderCreate(lCount), .. lTargetItems];
    }

    private QRosterItem[] QRosterTabsRead(QStrip lStrip) =>
        lStrip.QStripTabs
            .Where(lStripTab => lStripTab.QStripTabDocket is not null)
            .Select(lStripTab => new QRosterItem(
                lStripTab.QStripTabTitle,
                lStripTab.QStripTabKey,
                true,
                () => _ = QRosterRelayRun(lStrip, lStripTab)))
            .ToArray();

    private static QRosterItem QRosterHeaderCreate(int lCount) => new(
        lCount > 1
            ? QLocalizationCatalog.QLocalizationFormat("Roster.Relay.Many", lCount)
            : QLocalizationCatalog.QLocalizationTextRead("Roster.Relay.One"),
        string.Empty,
        false,
        () => { });

    private QRosterItem QRosterItemCreate(int lCount, string lOneKey, string lManyKey) => new(
        lCount > 1
            ? QLocalizationCatalog.QLocalizationFormat(lManyKey, lCount)
            : QLocalizationCatalog.QLocalizationTextRead(lOneKey),
        string.Empty,
        true,
        qRosterGate.CRosterMenuRun);

    private async Task QRosterRelayRun(QStrip lStrip, QStripTab lTargetTab)
    {
        if (lTargetTab.QStripTabDocket is not { } lTargetOwner)
        {
            return;
        }

        lStrip.QStripSelect(lTargetTab);
        lTargetOwner.LDocketPathsAdd(
            (await LMedia.LMediaPathScan(qRosterGate.CRosterMenuPaths).ConfigureAwait(true)).LMediaScanPaths,
            LGate.LGateBatchCreate());
    }
}

using Cadroue.Application;
using Cadroue.Conduct;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

public sealed record QGroupFile(
    int QGroupFileGroup,
    string QGroupFileNumber,
    string QGroupFilePath,
    string QGroupFileName);

public sealed record QGroupCard(
    int QGroupCardIndex,
    string QGroupCardName,
    bool QGroupCardEditing,
    IReadOnlyList<QGroupFile> QGroupCardFiles);

public sealed record QGroupSide(string QGroupSideText, string QGroupSideTip, bool QGroupSideActive);

public sealed record QGroupSwitch(string QGroupToggleKey, QGroupSide QGroupToggleLeft, QGroupSide QGroupToggleRight);

public sealed class QGroupFace
{
    private const string QGroupModeKey = "Mode";
    private const string QGroupStrictKey = "Strict";
    private const string QGroupNameKey = "Name";

    private readonly QGroup qGroup;
    private readonly CUsher qGroupUsher;

    public QGroupFace(QGroup lOwner, CUsher lUsher)
    {
        qGroup = lOwner;
        qGroupUsher = lUsher;
    }

    public IReadOnlyList<QGroupCard> QGroupCardsRead() =>
        qGroup.QGroupRecords
            .Select((lRecord, lIndex) => new QGroupCard(
                lIndex,
                lRecord.QGroupRecordName,
                qGroup.QGroupEditingIndex == lIndex,
                lRecord.QGroupRecordPaths
                    .Select((lPath, lOrder) =>
                        new QGroupFile(lIndex, (lOrder + 1).ToString(), lPath, qGroupUsher.CUsherNameRead(lPath)))
                    .ToArray()))
            .ToArray();

    public QGroupSwitch QGroupModeRead() =>
        new(
            QGroupModeKey,
            QGroupSideCreate("Manual", !qGroup.QGroupSelection.LGroupAuto),
            QGroupSideCreate("Auto", qGroup.QGroupSelection.LGroupAuto));

    public IReadOnlyList<QGroupSwitch> QGroupSwitchesRead() =>
    [
        new(
            QGroupStrictKey,
            QGroupSideCreate("Strict", qGroup.QGroupSelection.LGroupStrict),
            QGroupSideCreate("Loose", !qGroup.QGroupSelection.LGroupStrict)),
        new(
            QGroupNameKey,
            QGroupSideCreate("First", qGroup.QGroupSelection.LGroupNameMode == LSeriesNameMode.LSeriesNameFirst),
            QGroupSideCreate("NumberRemove", qGroup.QGroupSelection.LGroupNameMode == LSeriesNameMode.LSeriesNameBase)),
    ];

    public void QGroupToggleRun(string lKey, bool lRight)
    {
        switch (lKey)
        {
            case QGroupModeKey:
                qGroup.QGroupSelection.LGroupAutoChange(lRight);
                break;
            case QGroupStrictKey:
                qGroup.QGroupSelection.LGroupStrictChange(!lRight);
                break;
            case QGroupNameKey:
                qGroup.QGroupSelection.LGroupModeChange(
                    lRight ? LSeriesNameMode.LSeriesNameBase : LSeriesNameMode.LSeriesNameFirst);
                break;
        }
    }

    private static QGroupSide QGroupSideCreate(string lKey, bool lActive) =>
        new(
            LLocalization.LLocalizationTextRead($"Group.{lKey}.Label"),
            LLocalization.LLocalizationTextRead($"Group.{lKey}.Tooltip"),
            lActive);
}

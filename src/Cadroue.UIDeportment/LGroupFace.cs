using Cadroue.Application;
using Cadroue.Core;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed record LGroupFile(
    int LGroupFileGroup,
    string LGroupFileNumber,
    string LGroupFilePath,
    string LGroupFileName);

public sealed record LGroupCard(
    int LGroupCardIndex,
    string LGroupCardName,
    bool LGroupCardEditing,
    IReadOnlyList<LGroupFile> LGroupCardFiles);

public sealed record LGroupSide(string LGroupSideText, string LGroupSideTip, bool LGroupSideActive);

public sealed record LGroupToggle(string LGroupToggleKey, LGroupSide LGroupToggleLeft, LGroupSide LGroupToggleRight);

public sealed class LGroupFace
{
    private const string LGroupModeKey = "Mode";
    private const string LGroupStrictKey = "Strict";
    private const string LGroupNameKey = "Name";

    private readonly LGroup lGroup;

    public LGroupFace(LGroup lOwner)
    {
        lGroup = lOwner;
    }

    public IReadOnlyList<LGroupCard> LGroupCardsRead() =>
        lGroup.LGroupRecords
            .Select((lRecord, lIndex) => new LGroupCard(
                lIndex,
                lRecord.LGroupRecordName,
                lGroup.LGroupEditingIndex == lIndex,
                lRecord.LGroupRecordPaths
                    .Select((lPath, lOrder) =>
                        new LGroupFile(lIndex, (lOrder + 1).ToString(), lPath, LUsher.LUsherNameRead(lPath)))
                    .ToArray()))
            .ToArray();

    public LGroupToggle LGroupModeRead() =>
        new(
            LGroupModeKey,
            LGroupSideCreate("Manual", !lGroup.LGroupSelection.LGroupAuto),
            LGroupSideCreate("Auto", lGroup.LGroupSelection.LGroupAuto));

    public IReadOnlyList<LGroupToggle> LGroupSwitchesRead() =>
    [
        new(
            LGroupStrictKey,
            LGroupSideCreate("Strict", lGroup.LGroupSelection.LGroupStrict),
            LGroupSideCreate("Loose", !lGroup.LGroupSelection.LGroupStrict)),
        new(
            LGroupNameKey,
            LGroupSideCreate("First", lGroup.LGroupSelection.LGroupNameMode == LSeriesNameMode.LSeriesNameFirst),
            LGroupSideCreate("NumberRemove", lGroup.LGroupSelection.LGroupNameMode == LSeriesNameMode.LSeriesNameBase)),
    ];

    public void LGroupToggleRun(string lKey, bool lRight)
    {
        switch (lKey)
        {
            case LGroupModeKey:
                lGroup.LGroupSelection.LGroupAutoChange(lRight);
                break;
            case LGroupStrictKey:
                lGroup.LGroupSelection.LGroupStrictChange(!lRight);
                break;
            case LGroupNameKey:
                lGroup.LGroupSelection.LGroupModeChange(
                    lRight ? LSeriesNameMode.LSeriesNameBase : LSeriesNameMode.LSeriesNameFirst);
                break;
        }
    }

    private static LGroupSide LGroupSideCreate(string lKey, bool lActive) =>
        new(
            LLocalization.LLocalizationTextRead($"Group.{lKey}.Label"),
            LLocalization.LLocalizationTextRead($"Group.{lKey}.Tooltip"),
            lActive);
}

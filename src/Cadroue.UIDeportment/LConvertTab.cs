using Cadroue.Application;
using Cadroue.Core;
using Cadroue.ShellEngine;

namespace Cadroue.UIDeportment;

public sealed class LConvertTab
{
    private readonly LPresetSelection lConvertPreset;
    private readonly LList lConvertList;
    private readonly LDocket lConvertDocket;

    public LConvertTab(LPresetSelection lPreset, LList lList, LDocket lDocket)
    {
        lConvertPreset = lPreset;
        lConvertList = lList;
        lConvertDocket = lDocket;
    }

    public event Action? LConvertPresetMissing;

    public void LConvertRun(LWorkPriority lPriority, Guid lRelayTarget, Guid lSourceTab)
    {
        if (!LConvertPresetCheck())
        {
            return;
        }

        _ = LMessenger.LMessengerConvertDescribe(
            lPriority,
            LConvertSelectedRead() is { } lSelected ? [LConvertSourceCreate(lSelected)] : [],
            lConvertPreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab);
    }

    public void LConvertAllRun(Guid lRelayTarget, Guid lSourceTab)
    {
        if (!LConvertPresetCheck())
        {
            return;
        }

        _ = LMessenger.LMessengerConvertDescribe(
            LWorkPriority.LWorkPriorityNormal,
            lConvertDocket.LDocketUnlockedRead().Select(LConvertSourceCreate).ToArray(),
            lConvertPreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab);
    }

    public void LConvertItemsRun(IReadOnlyList<string> lPaths, Guid lRelayTarget, Guid lSourceTab)
    {
        if (!LConvertPresetCheck())
        {
            return;
        }

        _ = LMessenger.LMessengerConvertDescribe(
            LWorkPriority.LWorkPriorityNormal,
            lConvertDocket.LDocketUnlockedRead()
                .Where(lItem => lPaths.Contains(lItem.LDocketEntryPath, StringComparer.OrdinalIgnoreCase))
                .Select(LConvertSourceCreate)
                .ToArray(),
            lConvertPreset.LPresetSelectionEncoding,
            lRelayTarget,
            lSourceTab);
    }

    private bool LConvertPresetCheck()
    {
        if (lConvertPreset.LPresetSelectionValid)
        {
            return true;
        }

        LConvertPresetMissing?.Invoke();
        return false;
    }

    private LDocketEntry? LConvertSelectedRead() =>
        lConvertList.LListPathCurrent is { } lPath
        && lConvertDocket.LDocketItemFind(lPath) is { LDocketEntryLocked: false } lItem
            ? lItem
            : null;

    private static LWorkSource LConvertSourceCreate(LDocketEntry lItem) =>
        new(lItem.LDocketEntryPath, lItem.LDocketEntryBatch);
}

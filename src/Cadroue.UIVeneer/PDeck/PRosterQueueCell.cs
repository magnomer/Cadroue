using Cadroue.Application;
﻿using Cadroue.Core;

namespace Cadroue.UIVeneer.PDeck;

public sealed partial class PRoster
{
    private string PRosterStepRead(LWorkItem pWorkItem) =>
        pRosterRowPlaces.TryGetValue(pWorkItem.LWorkId, out PRosterRowPlace? pPlace)
            ? pPlace.PRosterPlaceStep
            : pWorkItem.LWorkOutputName;

    private string PRosterPlaceFormat(LWorkItem pWorkItem) =>
        pRosterRowPlaces.TryGetValue(pWorkItem.LWorkId, out PRosterRowPlace? pPlace)
            ? PLineageRatioFormat(pWorkItem, pPlace.PRosterPlaceSubject, pPlace.PRosterOriginBytes)
            : PRosterRatioFormat(pWorkItem);

    private string PRosterOwnerFormat(LWorkItem pWorkItem)
    {
        if (pWorkItem.LWorkOwnerRunner == Guid.Empty)
        {
            return "-";
        }

        if (pRosterStation.LStationRunner.LRunnerOwnerCheck(pWorkItem))
        {
            return LLocalization.LLocalizationTextRead("Roster.Owner.ThisTab");
        }

        return pWorkItem.LWorkOwnerProcess == Environment.ProcessId
            ? LLocalization.LLocalizationTextRead("Roster.Owner.OtherTab")
            : LLocalization.LLocalizationTextRead("Roster.Owner.OtherWindow");
    }

    private static string PRosterProgressFormat(LWorkItem pWorkItem) =>
        pWorkItem.LWorkStateCurrent == LWorkState.LWorkStateRunning || pWorkItem.LWorkProgress > 0
            ? $"{pWorkItem.LWorkProgress:P0}"
            : "-";

    private static string PRosterPriorityFormat(LWorkPriority pPriority) =>
        pPriority == LWorkPriority.LWorkPriorityHigh
            ? LLocalization.LLocalizationTextRead("Roster.Priority.High")
            : LLocalization.LLocalizationTextRead("Roster.Priority.Normal");

    private static string PRosterSpanFormat(TimeSpan pSpan) => $"{pSpan:hh\\:mm\\:ss}";
}

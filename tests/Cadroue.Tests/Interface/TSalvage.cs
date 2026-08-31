using System.Collections.Generic;

using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.Tests;

internal static class TSalvage
{
    internal static IReadOnlyList<LSalvageOutput> TSalvagePlanRead(
        IReadOnlyList<LSalvageSpan> spans, LSalvageMode mode, string source, LEncoding output) =>
        LSalvage.LSalvagePlanCreate(spans, mode, source, output);

    internal static LWorkFix TSalvagePlanCreate(
        bool active,
        LSalvageMode mode,
        bool persistent,
        LSalvageBasis basis = LSalvageBasis.LSalvageBasisSource) =>
        new(Array.Empty<LWorkFixStep>())
        {
            LWorkFixSalvage = new LWorkFixSalvage(active, mode, basis, persistent)
        };

    internal static LWorkFix TSalvageDefaultCreate() => LWorkFix.LWorkFixCreate();

    internal static LWorkFix TSalvagePersistMatch(LWorkFix plan) =>
        LFix.LFixPersistentRead(
            LFix.LFixPersistentCreate(plan), plan.LWorkFixSalvage.LWorkSalvagePersistent);

    internal static LWorkFix TSalvagePersistResolve(LWorkFix plan) =>
        LFix.LFixPersistentResolve(plan);

    internal static LWorkFix TSalvagePlanResolve(LWorkFix saved, LWorkFix persistent) =>
        LFix.LFixPlanResolve(saved, persistent);
}

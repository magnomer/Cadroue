using System.Collections.Generic;

using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.Tests;

internal static class TSalvage
{
    internal static IReadOnlyList<LSalvageOutput> PlanOutputs(
        IReadOnlyList<LSalvageSpan> spans, LSalvageMode mode, string source, LEncoding output) =>
        LSalvage.LSalvagePlanCreate(spans, mode, source, output);

    internal static LWorkFix PlanCreate(
        bool active,
        LSalvageMode mode,
        bool persistent,
        LSalvageBasis basis = LSalvageBasis.LSalvageBasisSource) =>
        new(Array.Empty<LWorkFixStep>())
        {
            LWorkFixSalvage = new LWorkFixSalvage(active, mode, basis, persistent)
        };

    internal static LWorkFix DefaultCreate() => LWorkFix.LWorkFixCreate();

    internal static LWorkFix PersistentRoundTrip(LWorkFix plan) =>
        LFix.LFixPersistentRead(
            LFix.LFixPersistentCreate(plan), plan.LWorkFixSalvage.LWorkSalvagePersistent);

    internal static LWorkFix PersistentResolve(LWorkFix plan) =>
        LFix.LFixPersistentResolve(plan);

    internal static LWorkFix PlanResolve(LWorkFix saved, LWorkFix persistent) =>
        LFix.LFixPlanResolve(saved, persistent);
}

using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.Tests;

internal static class TSalvage
{
    internal static LWorkFix PlanCreate(bool active, LSalvageMode mode, bool persistent) =>
        new(Array.Empty<LWorkFixStep>())
        {
            LWorkFixSalvage = new LWorkFixSalvage(active, mode, persistent)
        };

    internal static LWorkFix DefaultCreate() => LWorkFix.LWorkFixCreate();

    internal static LWorkFix PersistentRoundTrip(LWorkFix plan) =>
        LFix.LFixPersistentRead(LFix.LFixPersistentCreate(plan));

    internal static LWorkFix PersistentResolve(LWorkFix plan) =>
        LFix.LFixPersistentResolve(plan);

    internal static LWorkFix PlanResolve(LWorkFix saved, LWorkFix persistent) =>
        LFix.LFixPlanResolve(saved, persistent);
}

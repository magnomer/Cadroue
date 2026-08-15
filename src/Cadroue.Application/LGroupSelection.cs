using Cadroue.Core;

namespace Cadroue.Application;

public sealed class LGroupSelection
{
    public LGroupSelection(
        bool lGroupAuto = false,
        bool lGroupStrict = true,
        LSeriesNameMode lGroupNameMode = LSeriesNameMode.LSeriesNameBase)
    {
        LGroupAuto = lGroupAuto;
        LGroupStrict = lGroupStrict;
        LGroupNameMode = lGroupNameMode;
    }

    public event Action? LGroupSelectionChange;

    public bool LGroupAuto { get; private set; }

    public bool LGroupStrict { get; private set; }

    public LSeriesNameMode LGroupNameMode { get; private set; }

    public void LGroupAutoChange(bool lGroupAuto) =>
        LGroupChange(lGroupAuto, LGroupStrict, LGroupNameMode);

    public void LGroupStrictChange(bool lGroupStrict) =>
        LGroupChange(LGroupAuto, lGroupStrict, LGroupNameMode);

    public void LGroupModeChange(LSeriesNameMode lGroupNameMode) =>
        LGroupChange(LGroupAuto, LGroupStrict, lGroupNameMode);

    public IReadOnlyList<LSeriesGroup> LGroupResolve(
        IReadOnlyList<string> lGroupPaths,
        bool? lGroupStrict = null) =>
        LSeries.LSeriesResolve(lGroupPaths, lGroupStrict ?? LGroupStrict, LGroupNameMode);

    private void LGroupChange(
        bool lGroupAuto,
        bool lGroupStrict,
        LSeriesNameMode lGroupNameMode)
    {
        if (LGroupAuto == lGroupAuto
            && LGroupStrict == lGroupStrict
            && LGroupNameMode == lGroupNameMode)
        {
            return;
        }

        LGroupAuto = lGroupAuto;
        LGroupStrict = lGroupStrict;
        LGroupNameMode = lGroupNameMode;
        LGroupSelectionChange?.Invoke();
    }
}

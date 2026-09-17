using Cadroue.Application;

namespace Cadroue.UIDeportment;

public sealed class LInspector
{
    private bool lInspectorMinimized;
    private string? lInspectorStep;
    private double lInspectorSourceWidth = 1920;
    private double lInspectorSourceHeight = 1080;
    private bool lInspectorSourcePresent;
    private bool lInspectorCropCapable = true;
    private bool lInspectorOrientationCapable = true;
    private bool lInspectorToolArmed;
    private bool lInspectorRestoring;
    private string? lInspectorOwnerPath;
    private string? lInspectorFailurePath;

    public event Action? LInspectorChange;

    public LCropboxEdgeLock LInspectorEdgeLock { get; } = new();

    public bool LInspectorMinimized => lInspectorMinimized;

    public string? LInspectorStep => lInspectorStep;

    public double LInspectorSourceWidth => lInspectorSourceWidth;

    public double LInspectorSourceHeight => lInspectorSourceHeight;

    public bool LInspectorSourcePresent => lInspectorSourcePresent;

    public bool LInspectorCropCapable => lInspectorCropCapable;

    public bool LInspectorOrientationCapable => lInspectorOrientationCapable;

    public bool LInspectorToolArmed => lInspectorToolArmed;

    public bool LInspectorRestoring => lInspectorRestoring;

    public string? LInspectorOwnerPath => lInspectorOwnerPath;

    public void LInspectorRestoreSet(bool lRestoring) => lInspectorRestoring = lRestoring;

    public void LInspectorMinimizedSet(bool lMinimized)
    {
        if (lInspectorMinimized == lMinimized)
        {
            return;
        }

        lInspectorMinimized = lMinimized;
        LInspectorChange?.Invoke();
    }

    public void LInspectorStepSet(string? lStep)
    {
        if (lInspectorStep == lStep)
        {
            return;
        }

        lInspectorStep = lStep;
        LInspectorChange?.Invoke();
    }

    public void LInspectorSourceSet(double lSourceWidth, double lSourceHeight)
    {
        bool lPresent = lSourceWidth > 0 && lSourceHeight > 0;
        double lWidth = lPresent ? lSourceWidth : 0;
        double lHeight = lPresent ? lSourceHeight : 0;
        if (lInspectorSourcePresent == lPresent
            && lInspectorSourceWidth == lWidth
            && lInspectorSourceHeight == lHeight)
        {
            return;
        }

        lInspectorSourcePresent = lPresent;
        lInspectorSourceWidth = lWidth;
        lInspectorSourceHeight = lHeight;
        LInspectorChange?.Invoke();
    }

    public void LInspectorCapableSet(bool lCropCapable, bool lOrientationCapable)
    {
        if (lInspectorCropCapable == lCropCapable && lInspectorOrientationCapable == lOrientationCapable)
        {
            return;
        }

        lInspectorCropCapable = lCropCapable;
        lInspectorOrientationCapable = lOrientationCapable;
        LInspectorChange?.Invoke();
    }

    public void LInspectorToolSet(bool lToolArmed)
    {
        if (lInspectorToolArmed == lToolArmed)
        {
            return;
        }

        lInspectorToolArmed = lToolArmed;
        LInspectorChange?.Invoke();
    }

    public bool LInspectorOwnerSet(string lOwnerPath)
    {
        bool lOwnerFirst = lInspectorOwnerPath is null;
        lInspectorOwnerPath = lOwnerPath;
        return lOwnerFirst;
    }

    public bool LInspectorFailureSet(string? lFailurePath)
    {
        if (lFailurePath is null)
        {
            lInspectorFailurePath = null;
            return false;
        }

        if (string.Equals(lInspectorFailurePath, lFailurePath, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        lInspectorFailurePath = lFailurePath;
        return true;
    }
}

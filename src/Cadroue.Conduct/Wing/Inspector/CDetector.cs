using System;
using System.Collections.Generic;
using System.Linq;
using Cadroue.Core;

namespace Cadroue.Conduct;

public sealed class CDetector
{
    private readonly Dictionary<LDetectorKind, LDetectorStep> _cDetectorSteps = new();
    private LDetectorStillMode _cDetectorMode = LDetectorStillMode.LDetectorStillDiscard;
    private LDetectorLuminanceMode _cDetectorSpeed = LDetectorLuminanceMode.LDetectorLuminanceNormal;
    private LDetectorMetricMode _cDetectorMetric = LDetectorMetricMode.LDetectorMetricLufs;

    private static readonly LDetectorStillMode[] CDetectorModes =
    {
        LDetectorStillMode.LDetectorStillDiscard, LDetectorStillMode.LDetectorStillTreat
    };

    private static readonly LDetectorLuminanceMode[] CDetectorSpeeds =
    {
        LDetectorLuminanceMode.LDetectorLuminanceFast,
        LDetectorLuminanceMode.LDetectorLuminanceNormal,
        LDetectorLuminanceMode.LDetectorLuminanceFull
    };

    private static readonly LDetectorMetricMode[] CDetectorMetrics =
    {
        LDetectorMetricMode.LDetectorMetricLufs, LDetectorMetricMode.LDetectorMetricRms
    };

    public CDetector()
    {
        CDetectorPreset = new CDetectorPreset(this);
        CDetectorStepPrepare();
        CDetectorPlans = _cDetectorSteps.Keys.Select(CDetectorPlan.CDetectorPlanCreate).ToList().AsReadOnly();
        CDetectorChangeAttach();
    }

    public event Action? CDetectorChanged;

    public IReadOnlyList<CDetectorPlan> CDetectorPlans { get; }

    public CDetectorPreset CDetectorPreset { get; }

    public int CDetectorModeIndex => Array.IndexOf(CDetectorModes, _cDetectorMode);

    public int CDetectorSpeedIndex => Array.IndexOf(CDetectorSpeeds, _cDetectorSpeed);

    public int CDetectorMetricIndex => Array.IndexOf(CDetectorMetrics, _cDetectorMetric);

    public LDetectorStillMode CDetectorMode => _cDetectorMode;

    public LDetectorLuminanceMode CDetectorSpeed => _cDetectorSpeed;

    public LDetectorMetricMode CDetectorMetric => _cDetectorMetric;

    private static IEnumerable<LDetectorKind> CDetectorKindsRead() =>
        LDetector.LDetectorKinds.Where(kind => kind != LDetectorKind.LDetectorKindBlank);

    private void CDetectorStepPrepare()
    {
        foreach (LDetectorKind kind in CDetectorKindsRead())
        {
            _cDetectorSteps[kind] = LDetector.LDetectorCreate(kind);
            CDetectorPreset.CDetectorKindAdd(kind);
        }
    }

    private void CDetectorChangeAttach() =>
        CDetectorPreset.CDetectorChanged += () => CDetectorChanged?.Invoke();

    public LDetectorStep CDetectorStepRead(LDetectorKind kind) =>
        _cDetectorSteps.TryGetValue(kind, out LDetectorStep step) ? step : LDetector.LDetectorCreate(kind);

    public bool CDetectorEnabledRead(LDetectorKind kind) => CDetectorStepRead(kind).LDetectorStepEnabled;

    public double CDetectorValueRead(LDetectorKind kind, int row)
    {
        LDetectorStep step = CDetectorStepRead(kind);
        return row switch
        {
            1 => step.LDetectorStepWindow,
            2 => step.LDetectorStepMinimum,
            _ => step.LDetectorStepThreshold
        };
    }

    public double CDetectorDefaultRead(LDetectorKind kind, int row) => row switch
    {
        1 => LDetector.LDetectorWindowRead(kind).LDetectorBoundDefault,
        2 => LDetector.LDetectorMinimumRead(kind).LDetectorBoundDefault,
        _ => LDetector.LDetectorThresholdRead(kind).LDetectorBoundDefault
    };

    public string CDetectorUnitRead(LDetectorKind kind, int row)
    {
        CDetectorPlan plan = CDetectorPlans.First(entry => entry.CDetectorPlanKind == kind);
        return row switch
        {
            0 when kind == LDetectorKind.LDetectorKindVolume =>
                _cDetectorMetric == LDetectorMetricMode.LDetectorMetricRms ? "dB" : "LU",
            0 => plan.CDetectorPlanUnit,
            1 when plan.CDetectorPlanWindow is not null => "s",
            2 => "s",
            _ => throw new InvalidOperationException("Sequence contains no matching element")
        };
    }

    public void CDetectorValueSet(LDetectorKind kind, int row, double value)
    {
        switch (row)
        {
            case 1:
                CDetectorWindowSet(kind, value);
                break;
            case 2:
                CDetectorMinimumSet(kind, value);
                break;
            default:
                CDetectorThresholdSet(kind, value);
                break;
        }
    }

    public void CDetectorModeSelect(int index)
    {
        if (CDetectorIndexCheck(index, CDetectorModes.Length))
        {
            CDetectorModeSet(CDetectorModes[index]);
        }
    }

    public void CDetectorSpeedSelect(int index)
    {
        if (CDetectorIndexCheck(index, CDetectorSpeeds.Length))
        {
            CDetectorSpeedSet(CDetectorSpeeds[index]);
        }
    }

    public void CDetectorMetricSelect(int index)
    {
        if (CDetectorIndexCheck(index, CDetectorMetrics.Length))
        {
            CDetectorMetricSet(CDetectorMetrics[index]);
        }
    }

    private static bool CDetectorIndexCheck(int index, int count) => index >= 0 && index < count;

    public void CDetectorStepSet(LDetectorStep step)
    {
        if (!_cDetectorSteps.ContainsKey(step.LDetectorStepKind))
        {
            return;
        }

        CDetectorStepApply(LDetector.LDetectorStepNormalize(step));
    }

    public void CDetectorEnabledSet(LDetectorKind kind, bool enabled) =>
        CDetectorStepApply(CDetectorStepRead(kind) with { LDetectorStepEnabled = enabled });

    public void CDetectorThresholdSet(LDetectorKind kind, double threshold) =>
        CDetectorStepApply(LDetector.LDetectorStepNormalize(CDetectorStepRead(kind) with
        {
            LDetectorStepThreshold = threshold
        }));

    public void CDetectorMinimumSet(LDetectorKind kind, double minimum) =>
        CDetectorStepApply(CDetectorStepRead(kind) with
        {
            LDetectorStepMinimum = LDetector.LDetectorMinimumClamp(kind, minimum)
        });

    public void CDetectorWindowSet(LDetectorKind kind, double window) =>
        CDetectorStepApply(CDetectorStepRead(kind) with
        {
            LDetectorStepWindow = LDetector.LDetectorWindowClamp(kind, window)
        });

    public void CDetectorModeSet(LDetectorStillMode mode)
    {
        if (_cDetectorMode == mode)
        {
            return;
        }

        _cDetectorMode = mode;
        CDetectorChanged?.Invoke();
    }

    public void CDetectorSpeedSet(LDetectorLuminanceMode speed)
    {
        if (_cDetectorSpeed == speed)
        {
            return;
        }

        _cDetectorSpeed = speed;
        CDetectorChanged?.Invoke();
    }

    public void CDetectorMetricSet(LDetectorMetricMode metric)
    {
        if (_cDetectorMetric == metric)
        {
            return;
        }

        string? match = CDetectorPreset.CDetectorMatchRead(LDetectorKind.LDetectorKindVolume);
        CDetectorMetricCommit(metric);
        if (CDetectorVolumeResolve(match, metric) is { } normal)
        {
            CDetectorStepCommit(normal);
        }

        CDetectorChanged?.Invoke();
    }

    private void CDetectorMetricCommit(LDetectorMetricMode metric) => _cDetectorMetric = metric;

    private LDetectorStep? CDetectorVolumeResolve(string? match, LDetectorMetricMode metric)
    {
        if (match is { } token && LDetectorCatalog.LDetectorPresetRead(token) is { } preset)
        {
            LDetectorStep step = CDetectorStepRead(LDetectorKind.LDetectorKindVolume);
            return LDetector.LDetectorStepNormalize(step with
            {
                LDetectorStepThreshold = LDetectorCatalog.LDetectorPresetResolve(preset, metric)
            });
        }

        return null;
    }

    internal void CDetectorStepCommit(LDetectorStep normal) => _cDetectorSteps[normal.LDetectorStepKind] = normal;

    private void CDetectorStepApply(LDetectorStep normal)
    {
        if (_cDetectorSteps[normal.LDetectorStepKind] == normal)
        {
            return;
        }

        CDetectorStepCommit(normal);
        CDetectorChanged?.Invoke();
    }
}

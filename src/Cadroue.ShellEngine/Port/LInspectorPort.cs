namespace Cadroue.ShellEngine;

public interface LInspectorPort
{
    (bool LInspectorActive, IReadOnlyList<double> LInspectorValues) LInspectorGammaRead();

    (bool LInspectorActive, double LInspectorValue) LInspectorExposureRead();

    (bool LInspectorActive, double LInspectorValue) LInspectorVolumeRead();
}

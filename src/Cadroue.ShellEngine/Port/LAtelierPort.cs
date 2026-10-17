namespace Cadroue.ShellEngine;

// Every port the atelier's gates reach, handed as one value: a new gate's port is one more property here.
public sealed record LAtelierPort
{
    public required LUsherPort LAtelierUsher { get; init; }

    public required LLineagePort LAtelierLineage { get; init; }

    public required LWorkspacePort LAtelierWorkspace { get; init; }

    public required LPlacementPort LAtelierPlacement { get; init; }

    public required LViewerPort LAtelierViewer { get; init; }

    public required LEditPort LAtelierEdit { get; init; }

    public required LRosterPort LAtelierRoster { get; init; }

    public required LFixPort LAtelierFix { get; init; }

    public required LConvertPort LAtelierConvert { get; init; }

    public required LTonePort LAtelierTone { get; init; }

    public required LEqualizerPort LAtelierEqualizer { get; init; }

    public required LClinicPort LAtelierClinic { get; init; }

    public required LRosterCardPort LAtelierRosterCard { get; init; }

    public required LMergePort LAtelierMerge { get; init; }

    public required LNoisePort LAtelierNoise { get; init; }

    public required LConsolePort LAtelierConsole { get; init; }

    public required LFlowPort LAtelierFlow { get; init; }

    public required LRosterDetailPort LAtelierRosterDetail { get; init; }

    public required LWhitebalancePort LAtelierWhitebalance { get; init; }
    public required LFilterPort LAtelierFilter { get; init; }

    public required LConsoleScenePort LAtelierScene { get; init; }

    // Each list's own port, built for it over the name reader its traces use.
    public required Func<Func<string, string>, LListPort> LAtelierList { get; init; }
}

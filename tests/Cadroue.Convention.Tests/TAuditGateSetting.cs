namespace Convention.Tests;

internal static class TAuditGateSetting
{
    public const int TAuditGeneration = 8;
    public const string TAuditVeneerRoot = "src/Cadroue.UIVeneer";
    public const string TAuditDeportmentRoot = "src/Cadroue.UIDeportment";

    public static readonly string[] TAuditGateInclude =
    [
        "*.cs",
    ];

    public static readonly string[] TAuditGateSegments =
    [
        ".git",
        ".vs",
        "artifacts",
        "bin",
        "node_modules",
        "obj",
        "packages",
        "publish",
    ];

    public static readonly string[] TAuditVeneerForbidden =
    [
        "using System.IO",
        "System.Diagnostics.Process",
        "Process.Start",
        "Task.Run",
    ];

    public static readonly string[] TAuditDeportmentForbidden =
    [
        "using System.Windows",
    ];

    public static readonly string[] TAuditVeneerKnown =
    [
        "App.xaml.cs",
        "PAsset/PIcon.cs",
        "PDeck/PConsoleScene.cs",
        "PDeck/PConsoleSceneFile.cs",
        "PDeck/PRosterFormat.cs",
        "PDeck/PRosterLineage.cs",
        "PDeck/PRosterMenu.cs",
        "PFlow/PFlowLosslesscut.cs",
        "PPanel/PGroupCard.cs",
        "PPanel/PGroupDrag.cs",
        "PPanel/PListRelay.cs",
        "PPanel/PListRow.cs",
        "PPanel/PSEncoderCodec.cs",
        "PPanel/PSEncoderName.cs",
        "PPanel/PSEncoderOutput.cs",
        "PPanel/PViewerLoad.cs",
        "PPanel/PViewerMpv.cs",
        "PPanel/PViewerNeutral.cs",
        "PSDiagnosis/PSDiagnosisContent.cs",
        "PSLoupe/PSLoupePlayback.cs",
    ];

    public static readonly string[] TAuditDeportmentKnown =
    [
    ];
}

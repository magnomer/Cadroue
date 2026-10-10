using System.Collections.Generic;

namespace Cadroue.Core;

public sealed class LSidecarCoreDocument
{
    public int LSidecarVersion { get; set; } = 2;
    public LSidecarSourceEntry LSidecarSource { get; set; } = new();
    public List<LSidecarSectionEntry> LSidecarSections { get; set; } = new();
    public LSidecarEditEntry? LSidecarEdit { get; set; }
    public LSidecarAudioEntry? LSidecarAudio { get; set; }
    public LSidecarSplitEntry? LSidecarSplit { get; set; }
    public LSidecarFixEntry? LSidecarFix { get; set; }
    public double LSidecarLoudness { get; set; }
}

public enum LSidecarReadKind
{
    LSidecarReadMissing,
    LSidecarReadUnreadable,
    LSidecarReadMalformed,
    LSidecarReadForeign,
    LSidecarReadMatched
}

public sealed record LSidecarCoreResult(
    LSidecarReadKind LSidecarCoreState,
    LSidecarCoreDocument? LSidecarCoreValue,
    string? LSidecarCoreJson);

public enum LSidecarSourceKind
{
    LSidecarSourceSibling,
    LSidecarSourceRelative,
    LSidecarSourceAbsolute,
    LSidecarSourceMissing
}

public sealed record LSidecarSourceResult(
    string LSidecarResultPath,
    LSidecarSourceKind LSidecarResultKind,
    bool LSidecarResultVerified,
    string LSidecarResultName);

public sealed class LSidecarCacheDocument
{
    public int LSidecarVersion { get; set; } = 2;
    public long LSidecarLength { get; set; }
    public string LSidecarPartialHash { get; set; } = string.Empty;
    public int LSidecarSpanGrid { get; set; }
    public int LSidecarKeyframeCount { get; set; }
    public long LSidecarKeyframeLast { get; set; }
    public List<int> LSidecarScannedSpans { get; set; } = new();
    public List<long> LSidecarKeyframeDeltas { get; set; } = new();
    public LSidecarWaveformEntry? LSidecarWaveform { get; set; }
    public LSidecarDiagnosisEntry? LSidecarDiagnosis { get; set; }
}

public sealed class LSidecarDiagnosisEntry
{
    public long LSidecarLength { get; set; }
    public string LSidecarPartialHash { get; set; } = string.Empty;
    public List<LSidecarDossier> LSidecarDossiers { get; set; } = new();

    public List<LFlawKind> LSidecarScannedKinds { get; set; } = new();
}

public sealed class LSidecarDossier
{
    public string LSidecarDefect { get; set; } = string.Empty;
    public double LSidecarConfidence { get; set; }
    public string LSidecarEvidenceMechanism { get; set; } = string.Empty;
    public string LSidecarEvidenceSource { get; set; } = string.Empty;
    public string LSidecarEvidenceCoverage { get; set; } = string.Empty;
    public string LSidecarScope { get; set; } = string.Empty;
    public string LSidecarRepair { get; set; } = string.Empty;
    public string LSidecarRepairCoverage { get; set; } = string.Empty;
    public LDossierPreservation LSidecarPreservation { get; set; }
    public string LSidecarEquivalence { get; set; } = string.Empty;
    public string LSidecarTiming { get; set; } = string.Empty;
    public string LSidecarLoss { get; set; } = string.Empty;
    public LDossierValidation LSidecarValidation { get; set; }
    public LDossierCategory LSidecarCategory { get; set; }
    public string LSidecarRepairArgument { get; set; } = string.Empty;
    public string LSidecarRepairInput { get; set; } = string.Empty;
    public LFlawKind LSidecarKind { get; set; }
}

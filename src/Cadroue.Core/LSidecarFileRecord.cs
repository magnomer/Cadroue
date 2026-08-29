using System.Collections.Generic;

namespace Cadroue.Core;

public sealed class LSidecarCoreRecord
{
    public int LSidecarVersion { get; set; } = 2;
    public LSidecarSourceRecord LSidecarSource { get; set; } = new();
    public List<LSidecarSectionRecord> LSidecarSections { get; set; } = new();
    public LSidecarEditRecord? LSidecarEdit { get; set; }
    public LSidecarAudioRecord? LSidecarAudio { get; set; }
    public LSidecarSplitRecord? LSidecarSplit { get; set; }
    public LSidecarFixRecord? LSidecarFix { get; set; }
    public double LSidecarLoudness { get; set; }
}

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

public sealed class LSidecarCacheRecord
{
    public int LSidecarVersion { get; set; } = 2;
    public long LSidecarLength { get; set; }
    public string LSidecarPartialHash { get; set; } = string.Empty;
    public int LSidecarSpanGrid { get; set; }
    public int LSidecarKeyframeCount { get; set; }
    public long LSidecarKeyframeLast { get; set; }
    public List<int> LSidecarScannedSpans { get; set; } = new();
    public List<long> LSidecarKeyframeDeltas { get; set; } = new();
    public LSidecarWaveformRecord? LSidecarWaveform { get; set; }
    public List<LSidecarDossier> LSidecarDiagnosis { get; set; } = new();
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

using Xunit;

namespace Convention.Tests;

public sealed class TAssayControl
{
    [Fact]
    public void AuditTruth_ControlRequest_ReportsPuppeteering()
    {
        TAssayControlCheck("""
            using Cadroue.Conduct.Assay;

            namespace Cadroue.UIDeportment.Assay;

            public sealed class PAssay
            {
                public void PAssaySend(CAssay gate)
                {
                    gate.CAssayApply();
                }

                public void PAssayApply(CAssay gate)
                {
                    new QAssayRelay().QAssayRelayRun(gate);
                }
            }

            public sealed class QAssayRelay
            {
                public void QAssayRelayRun(CAssay gate)
                {
                    gate.CAssayApply();
                }
            }
            """,
            [
                new TViolation(TAssayTruth.TAssayDriverPath, 7, "PAssay.PAssaySend", "Puppeteering",
                    "a control member requests logic"),
                new TViolation(TAssayTruth.TAssayDriverPath, 12, "PAssay.PAssayApply", "Puppeteering",
                    "a control member requests logic"),
            ]);
    }

    [Fact]
    public void AuditTruth_ControlRecord_AllowsRead()
    {
        TAssayControlCheck("""
            using Cadroue.Conduct.Assay;

            namespace Cadroue.UIDeportment.Assay;

            public sealed class PAssay
            {
                public string PAssayShow(CAssayNote note)
                {
                    return note.CAssayNoteText;
                }
            }
            """, []);
    }

    [Fact]
    public void AuditTruth_ControlSignal_AllowsRaise()
    {
        TAssayControlCheck("""
            using System;
            using Cadroue.Conduct.Assay;

            namespace Cadroue.UIDeportment.Assay;

            public sealed class PAssay
            {
                public event Action? PAssayPicked;

                public void PAssayPickRaise()
                {
                    PAssayPicked?.Invoke();
                }
            }

            public sealed class QAssay
            {
                public void QAssayAttach(PAssay control, CAssay gate)
                {
                    control.PAssayPicked += () => gate.CAssayApply();
                }
            }
            """, []);
    }

    private static void TAssayControlCheck(string driver, IReadOnlyList<TViolation> expected)
    {
        const string notePath = "src/Cadroue.Conduct/Assay/CAssayNote.cs";
        Dictionary<string, string> sources = new(StringComparer.Ordinal)
        {
            [TAssayTruth.TAssayDriverPath] = driver,
            [TAssayTruth.TAssayGatePath] = TAssayTruth.TAssayGateText,
            [notePath] = """
                namespace Cadroue.Conduct.Assay;

                public sealed record CAssayNote(string CAssayNoteText);
                """,
        };
        string driverPath = Path.GetFullPath(Path.Combine(TAuditBinder.TAuditRoot, TAssayTruth.TAssayDriverPath));
        List<TViolation> hits = TAuditBinder.TAuditAssayRun(sources, () => TAuditTruthWalker.TAuditRun([driverPath]))
            .Where(hit => hit.TViolationKind == "Puppeteering")
            .Select(hit => hit with { TViolationPath = TAuditBinder.TAuditRelativeRead(hit.TViolationPath) })
            .ToList();
        Assert.Equal(expected, hits);
    }
}

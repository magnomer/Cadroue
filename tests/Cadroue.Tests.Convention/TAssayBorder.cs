using Xunit;

namespace Convention.Tests;

public sealed class TAssayBorder
{
    private const string TAssayPortPath = "src/Cadroue.ShellEngine/Assay/LAssayPort.cs";

    private const string TAssayHolderPath = "src/Cadroue.Conduct/Assay/CAssayHolder.cs";

    private const string TAssayPortText = """
        using System;

        namespace Cadroue.ShellEngine.Assay;

        public interface LAssayPort
        {
            event Action LAssayChanged;
        }
        """;

    [Fact]
    public void AuditBorder_UnremovedHandler_ReportsLingering()
    {
        IReadOnlyList<TAuditHit> hits = TAssayBorderRun(TAssayHolderFormat("CAssayHandle", ""));

        TAuditHit[] expected =
        [
            new(TAssayHolderPath, 17, "Cadroue.Conduct", "Lingering", "Cadroue.ShellEngine",
                "CAssayHolder LAssayPort.LAssayChanged CAssayHandle"),
        ];

        Assert.Equal(expected, hits);
    }

    [Fact]
    public void AuditBorder_DetachedHandler_AllowsSubscription()
    {
        IReadOnlyList<TAuditHit> hits = TAssayBorderRun(
            TAssayHolderFormat("CAssayHandle", "_port.LAssayChanged -= CAssayHandle;"));

        Assert.Empty(hits);
    }

    [Fact]
    public void AuditBorder_OtherHandlerRemoval_ReportsLingering()
    {
        IReadOnlyList<TAuditHit> hits = TAssayBorderRun(
            TAssayHolderFormat("CAssayHandle", "_port.LAssayChanged -= CAssayOther;"));

        TAuditHit[] expected =
        [
            new(TAssayHolderPath, 17, "Cadroue.Conduct", "Lingering", "Cadroue.ShellEngine",
                "CAssayHolder LAssayPort.LAssayChanged CAssayHandle"),
        ];

        Assert.Equal(expected, hits);
    }

    [Fact]
    public void AuditBorder_LambdaHandler_ReportsLingering()
    {
        IReadOnlyList<TAuditHit> hits = TAssayBorderRun(
            TAssayHolderFormat("() => CAssayHandle()", "_port.LAssayChanged -= CAssayHandle;"));

        TAuditHit[] expected =
        [
            new(TAssayHolderPath, 17, "Cadroue.Conduct", "Lingering", "Cadroue.ShellEngine",
                "CAssayHolder LAssayPort.LAssayChanged (lambda)"),
        ];

        Assert.Equal(expected, hits);
    }

    [Fact]
    public void AuditBorder_ConductEvent_AllowsSubscription()
    {
        IReadOnlyList<TAuditHit> hits = TAssayBorderRun("""
            using System;

            namespace Cadroue.Conduct.Assay;

            public sealed class CAssaySource
            {
                public event Action? CAssayChanged;

                public void CAssayRaise()
                {
                    CAssayChanged?.Invoke();
                }
            }

            public sealed class CAssayHolder
            {
                public CAssayHolder(CAssaySource source)
                {
                    source.CAssayChanged += CAssayHandle;
                }

                private void CAssayHandle()
                {
                }
            }
            """);

        Assert.Empty(hits);
    }

    private static IReadOnlyList<TAuditHit> TAssayBorderRun(string holder)
    {
        Dictionary<string, string> sources = new(StringComparer.Ordinal)
        {
            [TAssayPortPath] = TAssayPortText,
            [TAssayHolderPath] = holder,
        };
        return TAuditBinder.TAuditAssayRun(sources, TAuditBorderLinger.TAuditLingerScan);
    }

    private static string TAssayHolderFormat(string handler, string detach)
    {
        return $$"""
            using System;
            using Cadroue.ShellEngine.Assay;

            namespace Cadroue.Conduct.Assay;

            public sealed class CAssayHolder
            {
                private readonly LAssayPort _port;

                public CAssayHolder(LAssayPort port)
                {
                    _port = port;
                }

                public void CAssayAttach()
                {
                    _port.LAssayChanged += {{handler}};
                }

                public void CAssayDetach()
                {
                    {{detach}}
                }

                private void CAssayHandle()
                {
                }

                private void CAssayOther()
                {
                }
            }
            """;
    }
}

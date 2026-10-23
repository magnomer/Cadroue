using Xunit;

namespace Convention.Tests;

public sealed class TAssayContract
{
    [Fact]
    public void AuditContract_ForwardInside_AllowsDangling()
    {
        TAssayContractCheck("""
            namespace Cadroue.UIDeportment.Assay;

            public sealed class QContract
            {
                public string QContractResolve(string id)
                {
                    return QContractPart(id);
                }

                public string QContractPart(string id)
                {
                    return id;
                }
            }
            """, "Dangling", null);
    }

    [Fact]
    public void AuditContract_ForwardOutside_ReportsDangling()
    {
        TAssayContractCheck("""
            namespace Cadroue.UIDeportment.Assay;

            public sealed class QContract
            {
                public string QContractResolve(string id)
                {
                    return QContractPart(id);
                }

                public string QContractPart(string id)
                {
                    return id;
                }
            }

            public sealed class QAssay
            {
                public string QAssayRun(QContract contract, string id)
                {
                    return contract.QContractResolve(id);
                }
            }
            """, "Dangling", new TViolation(
            TAssayTruth.TAssayDriverPath, 20, "id", "Dangling", "contract ID is not a constant"));
    }

    [Fact]
    public void AuditContract_NestedForward_ReportsDangling()
    {
        TAssayContractCheck("""
            namespace Cadroue.UIDeportment.Assay;

            public sealed class QContract
            {
                public string QContractPart(string id)
                {
                    return id;
                }

                public sealed class QContractNested
                {
                    public string QContractNestedRun(QContract contract, string id)
                    {
                        return contract.QContractPart(id);
                    }
                }
            }
            """, "Dangling", new TViolation(
            TAssayTruth.TAssayDriverPath, 14, "id", "Dangling", "contract ID is not a constant"));
    }

    [Fact]
    public void AuditContract_ConstantInside_AllowsDangling()
    {
        TAssayContractCheck("""
            namespace Cadroue.UIDeportment.Assay;

            public sealed class QContract
            {
                public string QContractResolve()
                {
                    return QContractPart("kind");
                }

                public string QContractPart(string id)
                {
                    return id;
                }
            }
            """, "Dangling", null);
    }

    [Fact]
    public void AuditContract_ConstantOutside_ReportsDangling()
    {
        TAssayContractCheck("""
            namespace Cadroue.UIDeportment.Assay;

            public sealed class QContract
            {
                public string QContractResolve()
                {
                    return QContractPart("kind");
                }

                public string QContractPart(string id)
                {
                    return id;
                }
            }

            public sealed class QAssay
            {
                public string QAssayRun(QContract contract)
                {
                    return contract.QContractPart("kind");
                }
            }
            """, "Dangling", new TViolation(
            TAssayTruth.TAssayDriverPath, 20, "kind", "Dangling",
            "contract ID has no element or resource in the surface"));
    }

    [Fact]
    public void AuditContract_UndeclaredId_ReportsDangling()
    {
        TAssayContractCheck("""
            namespace Cadroue.UIDeportment.Assay;

            public sealed class QContract
            {
                public string QContractResolve()
                {
                    return QContractPart("kind");
                }

                public string QContractPart(string id)
                {
                    return id;
                }
            }

            public sealed class QAssay
            {
                public string QAssayRun(QContract contract)
                {
                    return contract.QContractPart("kind");
                }
            }
            """, "Dangling", new TViolation(
            TAssayTruth.TAssayDriverPath, 20, "kind", "Dangling",
            "contract ID has no element or resource in the surface"), """
            <Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                <TextBlock x:Name="other" />
            </Grid>
            """);
    }

    [Fact]
    public void AuditContract_DeclaredId_AllowsDangling()
    {
        TAssayContractCheck("""
            namespace Cadroue.UIDeportment.Assay;

            public sealed class QContract
            {
                public string QContractResolve()
                {
                    return QContractPart("kind");
                }

                public string QContractPart(string id)
                {
                    return id;
                }
            }

            public sealed class QAssay
            {
                public string QAssayRun(QContract contract)
                {
                    return contract.QContractPart("kind");
                }
            }
            """, "Dangling", null, """
            <Grid xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
                  xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
                <TextBlock x:Name="kind" />
            </Grid>
            """);
    }

    [Fact]
    public void AuditContract_PackLine_ReportsHardwiring()
    {
        TAssayContractCheck("""
            namespace Cadroue.UIDeportment.Assay;

            public sealed class QAssay
            {
                public string QAssayRun()
                {
                    return "pack://application:,,,/QAssay.xaml";
                }
            }
            """, "Hardwiring", new TViolation(
            TAssayTruth.TAssayDriverPath, 7, "pack://", "Hardwiring", "driver line names the surface"));
    }

    [Fact]
    public void AuditContract_PlainLine_AllowsHardwiring()
    {
        TAssayContractCheck("""
            namespace Cadroue.UIDeportment.Assay;

            public sealed class QAssay
            {
                public string QAssayRun()
                {
                    return "application:,,,/QAssay.xaml";
                }
            }
            """, "Hardwiring", null);
    }

    [Fact]
    public void AuditContract_BuiltUri_ReportsHardwiring()
    {
        TAssayContractCheck("""
            using System;
            using System.Windows;

            namespace Cadroue.UIDeportment.Assay;

            public sealed class QAssay
            {
                public void QAssayRun(string name)
                {
                    Application.LoadComponent(this, new Uri(name, UriKind.Relative));
                }
            }
            """, "Hardwiring", new TViolation(
            TAssayTruth.TAssayDriverPath, 10, "new Uri(name, UriKind.Relative)", "Hardwiring",
            "driver loads the surface through a built URI"));
    }

    [Fact]
    public void AuditContract_LiteralUri_AllowsHardwiring()
    {
        TAssayContractCheck("""
            using System;
            using System.Windows;

            namespace Cadroue.UIDeportment.Assay;

            public sealed class QAssay
            {
                public void QAssayRun()
                {
                    Application.LoadComponent(this, new Uri("/QAssay.xaml", UriKind.Relative));
                }
            }
            """, "Hardwiring", null);
    }

    private static void TAssayContractCheck(string driver, string kind, TViolation? expected, string? markup = null)
    {
        List<TViolation> hits = TAssayContractRun(driver, markup).Where(hit => hit.TViolationKind == kind).ToList();
        List<TViolation> wanted = expected is null ? [] : [expected];
        Assert.Equal(wanted, hits);
    }

    private static IReadOnlyList<TViolation> TAssayContractRun(string driver, string? markup)
    {
        const string surface = "src/Cadroue.UIVeneer/Assay/PAssay.xaml";
        string path = Path.GetFullPath(Path.Combine(TAuditBinder.TAuditRoot, TAssayTruth.TAssayDriverPath));
        Dictionary<string, string> sources = new(StringComparer.Ordinal) { [TAssayTruth.TAssayDriverPath] = driver };
        List<string> markups = [];
        if (markup is not null)
        {
            sources[surface] = markup;
            markups.Add(Path.GetFullPath(Path.Combine(TAuditBinder.TAuditRoot, surface)));
        }

        return TAuditBinder.TAuditAssayRun(sources, () => TAuditContractWalker.TAuditRun([path], markups))
            .Select(hit => hit with { TViolationPath = TAuditBinder.TAuditRelativeRead(hit.TViolationPath) })
            .ToList();
    }
}

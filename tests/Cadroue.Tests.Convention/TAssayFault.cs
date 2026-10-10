using Xunit;

namespace Convention.Tests;

public sealed class TAssayFault
{
    internal const string TAssayFaultPath = "src/Cadroue.Application/Assay/LAssay.cs";

    [Fact]
    public void AuditFault_InterfaceRecord_ReportsSwallowing()
    {
        TAssayFaultCheck("""
                    catch (Exception exception)
                    {
                        _lAssayRecord.LAssayWrite(exception);
                    }
            """, new TViolation(
            TAssayFaultPath, 30, "LAssayRun", "Swallowing", "LAssay.LAssayRun catch (Exception) swallows the fault"));
    }

    [Fact]
    public void AuditFault_NullReturn_ReportsSwallowing()
    {
        TAssayFaultCheck("""
                    catch (InvalidOperationException)
                    {
                        return null;
                    }
            """, new TViolation(
            TAssayFaultPath,
            30,
            "LAssayRun",
            "Swallowing",
            "LAssay.LAssayRun catch (InvalidOperationException) swallows the fault"));
    }

    [Fact]
    public void AuditFault_Rethrow_AllowsCatch()
    {
        TAssayFaultCheck("""
                    catch (Exception exception)
                    {
                        _lAssayRecord.LAssayWrite(exception);
                        throw;
                    }
            """, null);
    }

    [Fact]
    public void AuditFault_OutParameter_AllowsCatch()
    {
        TAssayFaultCheck("""
                    catch (Exception exception)
                    {
                        fault = exception;
                    }
            """, null);
    }

    [Fact]
    public void AuditFault_EventInvoke_AllowsCatch()
    {
        TAssayFaultCheck("""
                    catch (Exception)
                    {
                        LAssayFaulted?.Invoke();
                    }
            """, null);
    }

    [Fact]
    public void AuditFault_Cancellation_AllowsCatch()
    {
        TAssayFaultCheck("""
                    catch (OperationCanceledException)
                    {
                    }
            """, null);
    }

    [Fact]
    public void AuditFault_FieldHelper_AllowsCatch()
    {
        TAssayFaultCheck("""
                    catch (Exception exception)
                    {
                        LAssayKeep(exception);
                    }
            """, null);
    }

    internal static void TAssayFaultCheck(string clause, TViolation? expected)
    {
        Dictionary<string, string> sources = new(StringComparer.Ordinal)
        {
            [TAssayFaultPath] = TAssayFaultFormat(clause),
        };
        IReadOnlyList<TViolation> hits = TAuditBinder.TAuditAssayRun(sources, TAuditFaultWalker.TAuditFaultScan);
        List<TViolation> wanted = expected is null ? [] : [expected];
        Assert.Equal(wanted, hits);
    }

    internal static string TAssayFaultFormat(string clause)
    {
        return $$"""
            using System;

            namespace Cadroue.Application.Assay;

            public interface LAssayRecord
            {
                void LAssayWrite(Exception exception);
            }

            public sealed class LAssay
            {
                private readonly LAssayRecord _lAssayRecord;

                private Exception? _lAssayFault;

                public LAssay(LAssayRecord record)
                {
                    _lAssayRecord = record;
                }

                public event Action? LAssayFaulted;

                public object? LAssayRun(out Exception? fault)
                {
                    fault = _lAssayFault;
                    try
                    {
                        return _lAssayRecord.ToString();
                    }
            {{clause}}

                    return null;
                }

                private void LAssayKeep(Exception exception)
                {
                    _lAssayFault = exception;
                }
            }
            """;
    }
}

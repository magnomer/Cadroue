using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace Convention.Tests;

public sealed class TAuditEncoding
{
    private const string TAuditEncodingAudit = "AUDITENCODING";

    private static readonly byte[] TAuditEncodingMark = [0xEF, 0xBB, 0xBF];

    private static readonly UTF8Encoding TAuditEncodingStrict = new(false, true);

    private static readonly Regex TAuditControlPattern =
        new(TAuditEncodingSetting.TAuditEncodingControl, RegexOptions.Compiled);

    private static readonly Regex TAuditMojibakePattern =
        new(TAuditEncodingSetting.TAuditEncodingMojibake, RegexOptions.Compiled);

    private static readonly Regex TAuditTabPattern = new("\t", RegexOptions.Compiled);

    [Fact]
    public void AuditEncoding_TrackedText_DecodesAsUtf8()
    {
        List<string> hits = TAuditEncodingScan(static (_, bytes) =>
        {
            try
            {
                TAuditEncodingStrict.GetString(bytes);
                return null;
            }
            catch (DecoderFallbackException exception)
            {
                return $"invalid byte sequence at offset {exception.Index}";
            }
        });

        Assert.True(hits.Count == 0, TAuditConvention.TAuditReportFormat(
            TAuditEncodingAudit,
            $"{hits.Count} tracked file(s) are not valid UTF-8:\n{string.Join('\n', hits)}"));
    }

    [Fact]
    public void AuditEncoding_TrackedText_HoldsNoMark()
    {
        List<string> hits = TAuditEncodingScan(static (_, bytes) =>
        {
            int offset = bytes.AsSpan().IndexOf(TAuditEncodingMark);
            return offset < 0 ? null : $"byte order mark at offset {offset}";
        });

        Assert.True(hits.Count == 0, TAuditConvention.TAuditReportFormat(
            TAuditEncodingAudit,
            $"{hits.Count} tracked file(s) carry a byte order mark:\n{string.Join('\n', hits)}"));
    }

    [Fact]
    public void AuditEncoding_TrackedText_BreaksLinesWithLf()
    {
        List<string> hits = TAuditEncodingScan(static (_, bytes) =>
        {
            int returns = bytes.AsSpan().Count((byte)'\r');
            return returns == 0 ? null : $"{returns} carriage return(s)";
        });

        Assert.True(hits.Count == 0, TAuditConvention.TAuditReportFormat(
            TAuditEncodingAudit,
            $"{hits.Count} tracked file(s) break lines with a carriage return:\n{string.Join('\n', hits)}"));
    }

    [Fact]
    public void AuditEncoding_TrackedText_EndsWithNewline()
    {
        List<string> hits = TAuditEncodingScan(static (_, bytes) =>
            bytes.Length == 0 || bytes[^1] == (byte)'\n' ? null : "no newline at end of file");

        Assert.True(hits.Count == 0, TAuditConvention.TAuditReportFormat(
            TAuditEncodingAudit,
            $"{hits.Count} tracked file(s) end without a newline:\n{string.Join('\n', hits)}"));
    }

    [Fact]
    public void AuditEncoding_TrackedText_HoldsNoControl()
    {
        List<string> hits = TAuditEncodingScan(static (_, bytes) =>
            TAuditLineFind(bytes, TAuditControlPattern, "control character"));

        Assert.True(hits.Count == 0, TAuditConvention.TAuditReportFormat(
            TAuditEncodingAudit,
            $"{hits.Count} tracked file(s) hold a raw control character:\n{string.Join('\n', hits)}"));
    }

    [Fact]
    public void AuditEncoding_TrackedText_HoldsNoMojibake()
    {
        List<string> hits = TAuditEncodingScan(static (_, bytes) =>
            TAuditLineFind(bytes, TAuditMojibakePattern, "double-encoded text"));

        Assert.True(hits.Count == 0, TAuditConvention.TAuditReportFormat(
            TAuditEncodingAudit,
            $"{hits.Count} tracked file(s) hold UTF-8 read back as a single-byte page:\n{string.Join('\n', hits)}"));
    }

    [Fact]
    public void AuditEncoding_Sources_HoldNoTab()
    {
        List<string> hits = TAuditEncodingScan(static (path, bytes) =>
            TAuditEncodingSetting.TAuditEncodingTabless.Any(suffix =>
                path.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
                ? TAuditLineFind(bytes, TAuditTabPattern, "tab")
                : null);

        Assert.True(hits.Count == 0, TAuditConvention.TAuditReportFormat(
            TAuditEncodingAudit,
            $"{hits.Count} source file(s) hold a tab:\n{string.Join('\n', hits)}"));
    }

    [Fact]
    public void AuditEncoding_Scripts_HoldOnlyAscii()
    {
        List<string> hits = TAuditEncodingScan(static (path, bytes) =>
        {
            if (!path.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            int offset = bytes.AsSpan().IndexOfAnyInRange((byte)0x80, (byte)0xFF);
            if (offset < 0)
            {
                return null;
            }

            int line = bytes.AsSpan(0, offset).Count((byte)'\n') + 1;
            return $"non-ASCII byte at line {line}";
        });

        Assert.True(hits.Count == 0, TAuditConvention.TAuditReportFormat(
            TAuditEncodingAudit,
            $"{hits.Count} script(s) hold a byte outside ASCII:\n{string.Join('\n', hits)}"));
    }

    private static string? TAuditLineFind(byte[] bytes, Regex pattern, string label)
    {
        string text = Encoding.UTF8.GetString(bytes);
        Match match = pattern.Match(text);
        if (!match.Success)
        {
            return null;
        }

        int line = text.AsSpan(0, match.Index).Count('\n') + 1;
        return $"{label} at line {line}";
    }

    private static List<string> TAuditEncodingScan(Func<string, byte[], string?> verdict)
    {
        string repoRoot = TAuditSource.TAuditRootRead();
        TAuditScope scope = new(
            [],
            TAuditEncodingSetting.TAuditEncodingInclude,
            TAuditNameSetting.TAuditExcludedSegments,
            [],
            [],
            TAuditEncodingSetting.TAuditEncodingSkip);
        IReadOnlyList<string> files = TAuditSource.TAuditFileRead(repoRoot, scope);
        Assert.True(files.Count > 0, TAuditConvention.TAuditReportFormat(
            TAuditEncodingAudit, "No tracked text file was enumerated; the audit would pass vacuously."));

        List<string> hits = [];
        foreach (string path in files)
        {
            string relative = Path.GetRelativePath(repoRoot, path).Replace('\\', '/');
            string? reason = verdict(relative, File.ReadAllBytes(path));
            if (reason is not null)
            {
                hits.Add($"  {relative}: {reason}");
            }
        }

        return hits;
    }
}

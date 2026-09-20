namespace Convention.Tests;

internal static class TAuditEncodingSetting
{
    public const int TAuditGeneration = 10;

    public static readonly string[] TAuditEncodingInclude =
    [
        "*.cs",
        "*.xaml",
        "*.md",
        "*.json",
        "*.csproj",
        "*.props",
        "*.sln",
        "*.slnx",
        "*.ps1",
    ];

    public static readonly string[] TAuditEncodingSkip =
    [
        "version.json",
    ];

    public static readonly string[] TAuditEncodingTabless =
    [
        ".cs",
        ".xaml",
    ];

    public const string TAuditEncodingControl = @"[\u0000-\u0008\u000B\u000C\u000E-\u001F\u007F]";

    private const string TAuditEncodingTrail =
        @"[\u00A0-\u00BF\u0152\u0153\u0160\u0161\u0178\u017D\u017E\u0192\u02C6\u02DC"
        + @"\u2013\u2014\u2018-\u201E\u2020-\u2022\u2026\u2030\u2039\u203A\u20AC\u2122]";

    public const string TAuditEncodingMojibake =
        @"\u00EF\u00BB\u00BF"
        + @"|(?:[\u00E0-\u00EF]" + TAuditEncodingTrail + "{2}){2}"
        + @"|(?:[\u00C2-\u00DF]" + TAuditEncodingTrail + "){2}";
}

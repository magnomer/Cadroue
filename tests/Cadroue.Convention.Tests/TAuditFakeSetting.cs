namespace Convention.Tests;

internal static class TAuditFakeSetting
{
    public const int TAuditGeneration = 11;
    public const bool TAuditFakeEnforced = true;
    public const string TAuditFakeReport = "temp/audit/Fake-{0}.md";

    public static readonly IReadOnlyDictionary<string, int> TAuditFakeCeiling = new Dictionary<string, int>
    {
        ["Orphan"] = 188,
        ["Tested"] = 123,
    };

    public static readonly string[] TAuditFakeInclude =
    [
        "tests/Cadroue.Tests/*.cs",
    ];

    public static readonly string[] TAuditMarkupInclude =
    [
        "src/*.xaml",
    ];

    public static readonly string[] TAuditFakeUsing =
    [
        "System",
        "System.Collections.Generic",
        "System.IO",
        "System.Linq",
        "System.Net.Http",
        "System.Threading",
        "System.Threading.Tasks",
    ];
}

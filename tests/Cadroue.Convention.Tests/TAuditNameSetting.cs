namespace Convention.Tests;

internal static class TAuditNameSetting
{
    public const int TAuditGeneration = 10;
    public const string TAuditProject = "Cadroue";
    public const string TAuditTestPrefix = "T";
    public const string TAuditComponentPattern = "[A-Z]+(?=[A-Z][a-z]|[0-9]|$)|[A-Z]?[a-z]+|[0-9]+";
    public const int TAuditComponentLimit = 3;
    public const int TAuditComponentReview = 3;
    public const string TAuditXamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";
    public const string TAuditCancelArgument = "IncludeCancelCommand";
    public const string TAuditAsyncSuffix = "Async";
    public const string TAuditCommandSuffix = "Command";
    public const string TAuditCancelSuffix = "CancelCommand";

    public static readonly string[] TAuditPrefixes =
    [
        "PS",
        "LS",
        "ps",
        "ls",
        "P",
        "L",
        "T",
        "p",
        "l",
        "t",
    ];

    public static readonly string[] TAuditSourceInclude =
    [
        "*.cs",
        "*.xaml",
    ];

    public static readonly string[] TAuditExcludedSegments =
    [
        ".git",
        "bin",
        "obj",
        "out",
        "publish",
        "snapshots",
        "TestResults",
    ];

    public static readonly string[] TAuditExcludedSuffixes =
    [
        ".md",
        ".g.cs",
        ".g.i.cs",
        ".AssemblyInfo.cs",
        ".GlobalUsings.g.cs",
        ".Designer.cs",
    ];

    public static readonly string[] TAuditExcludedPrefixes =
    [
        "TemporaryGeneratedFile_",
        "GeneratedInternalTypeHelper",
    ];

    public static readonly string[] TAuditMethodKinds =
    [
        "Method",
        "LocalFunction",
    ];

    public static readonly string[] TAuditDataKinds =
    [
        "Field",
        "Property",
        "EnumMember",
        "RecordProperty",
        "XamlName",
        "GeneratedCommand",
        "TupleElement",
        "TypeParameter",
        "AnonymousMember",
    ];

    public static readonly string[] TAuditTestAttributes =
    [
        "Fact",
        "Theory",
    ];

    public static readonly string[] TAuditGeneratedAttributes =
    [
        "GeneratedCode",
        "CompilerGenerated",
    ];

    public static readonly string[] TAuditExternalAttributes =
    [
        "DllImport",
        "LibraryImport",
    ];

    public static readonly string[] TAuditCommandAttributes =
    [
        "RelayCommand",
        "RelayCommandAttribute",
    ];

    public static readonly Dictionary<string, string[]> TAuditFrameworkContracts = new(StringComparer.Ordinal)
    {
        ["IAsyncDisposable"] = ["DisposeAsync"],
        ["IDisposable"] = ["Dispose"],
        ["IEquatable"] = ["Equals"],
        ["IMultiValueConverter"] = ["Convert", "ConvertBack"],
        ["INotifyPropertyChanged"] = ["PropertyChanged"],
        ["INotifyPropertyChanging"] = ["PropertyChanging"],
        ["IProgress"] = ["Report"],
        ["IValueConverter"] = ["Convert", "ConvertBack"],
    };
}

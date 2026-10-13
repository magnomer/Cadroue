namespace Convention.Tests;

internal static class TAuditNameSetting
{
    public const int TAuditGeneration = 25;
    public const string TAuditProject = "Cadroue";
    public const string TAuditTestPrefix = "T";
    public const string TAuditComponentPattern = "[A-Z]+(?=[A-Z][a-z]|[0-9]|$)|[A-Z]?[a-z]+|[0-9]+";
    public const int TAuditComponentLimit = 3;
    public const string TAuditXamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";
    public const string TAuditCancelArgument = "IncludeCancelCommand";
    public const string TAuditAsyncSuffix = "Async";
    public const string TAuditCommandSuffix = "Command";
    public const string TAuditCancelSuffix = "CancelCommand";

    public const int TAuditPrefixCeiling = 0;

    public static readonly string[] TAuditPrefixes =
    [
        "PS",
        "QS",
        "CS",
        "LS",
        "ps",
        "qs",
        "cs",
        "ls",
        "P",
        "Q",
        "C",
        "L",
        "T",
        "p",
        "q",
        "c",
        "l",
        "t",
    ];

    public static readonly Dictionary<string, string[]> TAuditPrefixTurfs = new(StringComparer.Ordinal)
    {
        ["src/Cadroue.Application"] = ["L", "LS"],
        ["src/Cadroue.Conduct"] = ["C", "CS"],
        ["src/Cadroue.Core"] = ["L", "LS"],
        ["src/Cadroue.Core.Windows"] = ["L", "LS"],
        ["src/Cadroue.Host"] = ["L", "LS"],
        ["src/Cadroue.Infrastructure"] = ["L", "LS"],
        ["src/Cadroue.ShellEngine"] = ["L", "LS"],
        ["src/Cadroue.UIDemeanor"] = ["Q", "QS"],
        ["src/Cadroue.UIDeportment"] = ["Q", "QS"],
        ["src/Cadroue.UITerminal"] = ["P", "PS"],
        ["src/Cadroue.UIVeneer"] = ["P", "PS"],
        ["tests/Cadroue.Tests.Interface"] = ["T"],
        ["tests/Cadroue.Tests.Engine"] = ["T"],
        ["tests/Cadroue.Tests.Conduct"] = ["T"],
        ["tests/Cadroue.Tests.Convention"] = ["T"],
        ["tests/Cadroue.Tests.Windows"] = ["T"],
    };

    public static readonly string[] TAuditTurfSealed =
    [
        "src/Cadroue.Conduct",
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

    public static readonly string[] TAuditSelfExcluded =
    [
        "TAuditComment.cs",
        "TAuditCommentSetting.cs",
        "TAuditConvention.cs",
        "TAuditLine.cs",
        "TAuditLineSetting.cs",
        "TAuditName.cs",
        "TAuditNameFilter.cs",
        "TAuditNameRegistry.cs",
        "TAuditNameSetting.cs",
        "TAuditNameWalker.cs",
        "TAuditRegistry.cs",
        "TAuditScope.cs",
        "TAuditSource.cs",
        "TAuditVerbRegistry.cs",
        "TSpecimen.cs",
        "TViolation.cs",
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
        "ClassDeclaration",
        "StructDeclaration",
        "InterfaceDeclaration",
        "RecordDeclaration",
        "RecordStructDeclaration",
        "EnumDeclaration",
        "Delegate",
        "Event",
        "EventField",
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

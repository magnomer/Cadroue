namespace Convention.Tests;

internal static class TAuditStrictSetting
{
    public const int TAuditGeneration = 21;
    public const bool TAuditStrictEnforced = true;
    public const string TAuditStrictReport = "temp/audit/Strict-{0}.md";
    public const string TAuditLedgerFile = "TAuditStrictLedger";

    public static readonly string[] TAuditReachInclude =
    [
        "src/Cadroue.UIVeneer/*.xaml",
    ];

    public static readonly string[] TAuditVeneerInclude =
    [
        "src/Cadroue.UIVeneer/*.cs",
        "src/Cadroue.UITerminal/*.cs",
    ];

    public static readonly string[] TAuditDeportmentInclude =
    [
        "src/Cadroue.UIDeportment/*.cs",
        "src/Cadroue.UIDemeanor/*.cs",
    ];

    public static readonly string[] TAuditHostInclude =
    [
        "src/Cadroue.Host/*.cs",
    ];

    public const string TAuditDeportmentNamespace = "Cadroue.UIDeportment";

    public const string TAuditVeneerNamespace = "Cadroue.UIVeneer";

    public const string TAuditContractType = "QContract";

    public static readonly string[] TAuditHardwiringMarkers =
    [
        "pack://",
        ";component/",
        "Cadroue.UIVeneer",
    ];

    public static readonly string[] TAuditMasqueradingTypes =
    [
        "System.Windows.Application",
        "System.Windows.Controls.ContentControl",
        "System.Windows.Controls.ItemsControl",
        "System.Windows.Controls.Page",
        "System.Windows.ResourceDictionary",
    ];

    public static readonly string[] TAuditContractIds =
    [
        "Key",
        "Name",
    ];

    public static readonly string[] TAuditQueryTypes =
    [
        "System.Linq.Enumerable",
        "System.Linq.ParallelEnumerable",
        "System.Linq.Queryable",
    ];

    public static readonly string[] TAuditCatalogPatterns =
    [
        @"^\s*using\s+System\.IO\s*;",
        @"^\s*using\s+System\.Text\.Json",
        @"^\s*using\s+System\.Text\.RegularExpressions",
        @"^\s*using\s+System\.Diagnostics\s*;",
        @"\bJsonSerializer\b",
        @"\bJsonDocument\b",
        @"\bRegex\b",
        @"\bProcess\.Start\b",
        @"\bProcessStartInfo\b",
        @"\bTask\.Run\b",
        @"\bFile\.\w+\(",
        @"\bDirectory\.\w+\(",
        @"\bPath\.\w+\(",
    ];

    public static readonly string[] TAuditCatalogExempt = [];

    public static readonly string[] TAuditDiskPatterns =
    [
        @"\bFile\.\w+\(",
        @"\bDirectory\.\w+\(",
        @"\bPath\.\w+\(",
        @"\b(FileInfo|DirectoryInfo|DriveInfo|FileStream|FileSystemWatcher|FileSystemInfo)\b",
        @"\bSystem\.IO\.(File|Directory|Path|Drive|FileSystem)",
    ];

    public static readonly string[] TAuditDiskExempt = [];

    public static readonly string[] TAuditOverreachingNamespaces =
    [
        "Cadroue.Core",
        "Cadroue.Application",
        "Cadroue.ShellEngine",
        "Cadroue.Conduct",
        "Cadroue.Infrastructure",
        "Cadroue.Core.Windows",
    ];

    public static readonly string[] TAuditBranchingElements =
    [
        "DataTrigger",
        "EventTrigger",
        "MultiDataTrigger",
        "MultiTrigger",
        "Trigger",
        "VisualState",
        "VisualStateGroup",
        "VisualStateManager.VisualStateGroups",
        "VisualTransition",
    ];

    public static readonly string[] TAuditBranchingSlots =
    [
        "CellTemplateSelector",
        "ContentStringFormat",
        "ContentTemplateSelector",
        "Converter",
        "FallbackValue",
        "HeaderStringFormat",
        "HeaderTemplateSelector",
        "ItemContainerStyleSelector",
        "ItemStringFormat",
        "ItemTemplateSelector",
        "StringFormat",
        "StyleSelector",
        "TargetNullValue",
        "TemplateSelector",
        "ValidatesOnDataErrors",
        "ValidatesOnExceptions",
        "ValidatesOnNotifyDataErrors",
        "ValidationRules",
    ];

    public static readonly string[] TAuditTetheringElements =
    [
        "Binding",
        "CommandBinding",
        "InputBinding",
        "KeyBinding",
        "MouseBinding",
        "MultiBinding",
        "PLocalizationBinder",
        "PriorityBinding",
        "RelativeSource",
    ];

    public static readonly string[] TAuditTetheringSlots =
    [
        "Command",
        "CommandParameter",
        "CommandTarget",
        "DisplayMemberPath",
        "RelativeSource",
        "SelectedValuePath",
    ];

    public static readonly string[] TAuditTetheringLiterals =
    [
        "Tag",
    ];

    public static readonly string[] TAuditTetheringExtensions =
    [
        "Binding",
        "MultiBinding",
        "RelativeSource",
        "TemplateBinding",
        "x:Static",
    ];

    public static readonly string[] TAuditTetheringTypes =
    [
        "System.Windows.Controls.DataTemplateSelector",
        "System.Windows.Controls.StyleSelector",
        "System.Windows.Data.IMultiValueConverter",
        "System.Windows.Data.IValueConverter",
    ];
}

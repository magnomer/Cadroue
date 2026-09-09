// Generated file. Do not edit by hand: every line is overwritten when it is rebuilt.
//
// AUDITNAMES GENERATION 7 - settings sidecar for Cadroue.
// This is the only file in the convention-test project that carries a project-specific
// value. Every other file is identical in every project at this generation.

namespace Convention.Tests;

internal static class TAuditSetting
{
    public const int TAuditGeneration = 7;
    public const string TAuditProject = "Cadroue";
    public const string TAuditTestPrefix = "T";
    public const string TAuditComponentPattern = "[A-Z]+(?=[A-Z][a-z]|[0-9]|$)|[A-Z]?[a-z]+|[0-9]+";
    public const int TAuditComponentLimit = 3;
    public const int TAuditComponentReview = 3;
    public const int TAuditLineLimit = 500;
    public const string TAuditXamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";
    public const string TAuditCommandCancelArgument = "IncludeCancelCommand";
    public const string TAuditCommandAsyncSuffix = "Async";
    public const string TAuditCommandSuffix = "Command";
    public const string TAuditCommandCancelSuffix = "CancelCommand";

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

    public static readonly string[] TAuditSelfExcluded =
    [
        "TAuditConvention.cs",
        "TAuditName.cs",
        "TAuditRegistry.cs",
        "TAuditSetting.cs",
        "TAuditSize.cs",
        "TAuditSource.cs",
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
        ["IMultiValueConverter"] = ["Convert", "ConvertBack"],
        ["INotifyPropertyChanged"] = ["PropertyChanged"],
        ["INotifyPropertyChanging"] = ["PropertyChanging"],
        ["IProgress"] = ["Report"],
        ["IValueConverter"] = ["Convert", "ConvertBack"],
    };

    // AUDIT:SIDECAR:BASES:START
    public static readonly string[] TAuditBases =
    [
        "About",
        "Action",
        "Alert",
        "Announcement",
        "Asset",
        "Audio",
        "Audit",
        "Autopsy",
        "Bastion",
        "Binding",
        "Blank",
        "Bridge",
        "Button",
        "Capability",
        "Card",
        "Cargo",
        "Cartographer",
        "Casement",
        "Checkbox",
        "Checkup",
        "Chord",
        "Chrome",
        "Circle",
        "Classifier",
        "Clinic",
        "Clip",
        "Codec",
        "Color",
        "Column",
        "Combo",
        "Compass",
        "Console",
        "Contour",
        "Convert",
        "Courier",
        "Crest",
        "Crop",
        "Cropbox",
        "Cursor",
        "Curve",
        "Custody",
        "Debug",
        "Decision",
        "Deck",
        "Depot",
        "Detector",
        "Diagnosis",
        "Dialog",
        "Divider",
        "Docket",
        "Dossier",
        "Drop",
        "Dropdown",
        "Dynamic",
        "Edit",
        "Employer",
        "Encode",
        "Encoder",
        "Encoding",
        "Entry",
        "Equalizer",
        "Export",
        "Exposure",
        "Fader",
        "Field",
        "Filter",
        "Fix",
        "Flaw",
        "Flow",
        "Flyleaf",
        "Footer",
        "Frame",
        "Funnel",
        "Gamma",
        "Gate",
        "General",
        "Ghost",
        "Grabber",
        "Grain",
        "Grip",
        "Group",
        "Header",
        "Headline",
        "Histogram",
        "History",
        "House",
        "Icon",
        "Info",
        "Inline",
        "Inspector",
        "Interaction",
        "Interface",
        "Inventory",
        "Job",
        "Keyframe",
        "Keymap",
        "Latch",
        "Ledger",
        "Leveling",
        "Librarian",
        "Line",
        "Lineage",
        "List",
        "Localization",
        "Location",
        "Log",
        "Logo",
        "Losslesscut",
        "Loudness",
        "Loupe",
        "Map",
        "Media",
        "Menu",
        "Merge",
        "Messenger",
        "Mode",
        "Monitor",
        "Mpv",
        "Name",
        "Nameplate",
        "Navigator",
        "Neutral",
        "Noise",
        "Notice",
        "Options",
        "Output",
        "Panel",
        "Passband",
        "Picker",
        "Piece",
        "Placement",
        "Plate",
        "Playback",
        "Player",
        "Preference",
        "Preset",
        "Preview",
        "Processing",
        "Program",
        "Radio",
        "Rail",
        "Rect",
        "Reel",
        "Relay",
        "Remedy",
        "Renderer",
        "Repertoire",
        "Resize",
        "Retention",
        "Roster",
        "Rotate",
        "Rule",
        "Runner",
        "Salvage",
        "Sash",
        "Scene",
        "Schedule",
        "Scout",
        "Scrollbar",
        "Seal",
        "Section",
        "Segment",
        "Sensor",
        "Sentinel",
        "Separator",
        "Series",
        "Sheet",
        "Shortcut",
        "Sidecar",
        "Signet",
        "Skip",
        "Slider",
        "Source",
        "Specimen",
        "Spectrum",
        "Split",
        "Spool",
        "Station",
        "Strip",
        "Subsidiary",
        "Subwindow",
        "Summary",
        "Surface",
        "Swatch",
        "Sweep",
        "System",
        "Tab",
        "Tabset",
        "Textbox",
        "Timecode",
        "Timeline",
        "Token",
        "Tone",
        "Tool",
        "Toolbar",
        "Trace",
        "Trial",
        "Vault",
        "Verdict",
        "Video",
        "Viewer",
        "Viewfinder",
        "Violation",
        "Volume",
        "Warning",
        "Waveform",
        "Whitebalance",
        "Window",
        "Work",
        "Worklist",
        "Workspace",
    ];
    // AUDIT:SIDECAR:BASES:END

    // AUDIT:SIDECAR:VERBS:START
    public static readonly string[] TAuditVerbs =
    [
        "Accept",
        "Add",
        "Adjust",
        "Append",
        "Apply",
        "Attach",
        "Build",
        "Cancel",
        "Change",
        "Check",
        "Claim",
        "Clamp",
        "Clear",
        "Clone",
        "Close",
        "Commit",
        "Compact",
        "Confirm",
        "Copy",
        "Create",
        "Defer",
        "Delete",
        "Describe",
        "Detach",
        "Dispatch",
        "Dispose",
        "Divide",
        "Draw",
        "Exist",
        "Find",
        "Format",
        "Handle",
        "Hide",
        "Hook",
        "Import",
        "Insert",
        "Interrupt",
        "Load",
        "Match",
        "Move",
        "Normalize",
        "Open",
        "Parse",
        "Pause",
        "Persist",
        "Place",
        "Play",
        "Prepare",
        "Propagate",
        "Publish",
        "Raise",
        "Read",
        "Rebuild",
        "Record",
        "Redo",
        "Release",
        "Remove",
        "Reset",
        "Resolve",
        "Restore",
        "Resume",
        "Run",
        "Save",
        "Scan",
        "Scroll",
        "Seek",
        "Select",
        "Send",
        "Set",
        "Shorten",
        "Show",
        "Sort",
        "Start",
        "Stop",
        "Suspend",
        "Sync",
        "Tick",
        "Toggle",
        "Undo",
        "Update",
        "Validate",
        "Zoom",
    ];
    // AUDIT:SIDECAR:VERBS:END

    // AUDIT:SIDECAR:EXEMPT:START
    public static readonly Dictionary<string, string[]> TAuditExempt = new(StringComparer.Ordinal)
    {
    };
    // AUDIT:SIDECAR:EXEMPT:END
}

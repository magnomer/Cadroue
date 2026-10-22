using System;
using Cadroue.ShellEngine;

namespace Cadroue.Conduct;

public sealed class CAtelier
{
    private readonly LWorkspacePort _cAtelierWorkspacePort;
    private readonly LViewerPort _cAtelierViewerPort;
    private readonly LRosterPort _cAtelierRosterPort;
    private readonly LRosterMenuPort _cAtelierMenuPort;
    private readonly LTonePort _cAtelierTonePort;
    private readonly LEqualizerPort _cAtelierEqualizerPort;
    private readonly LClinicPort _cAtelierClinicPort;
    private readonly LRosterCardPort _cAtelierCardPort;
    private readonly LNoisePort _cAtelierNoisePort;
    private readonly LConsolePort _cAtelierConsolePort;
    private readonly LFlowPort _cAtelierFlowPort;
    private readonly LMapPort _cAtelierMapPort;
    private readonly LRosterDetailPort _cAtelierDetailPort;
    private readonly LWhitebalancePort _cAtelierWhitebalancePort;
    private readonly LFilterPort _cAtelierFilterPort;
    private readonly LAudioPort _cAtelierAudioPort;
    private readonly LLoudnessPort _cAtelierLoudnessPort;
    private readonly LVideoPort _cAtelierVideoPort;
    private readonly LConsoleScenePort _cAtelierScenePort;
    private readonly Func<Func<string, string>, LListPort> _cAtelierListPort;
    private readonly LAboutPort _cAtelierAboutPort;

    internal CAtelier(LAtelierPort port)
    {
        ArgumentNullException.ThrowIfNull(port);
        ArgumentNullException.ThrowIfNull(port.LAtelierUsher);
        ArgumentNullException.ThrowIfNull(port.LAtelierLineage);
        ArgumentNullException.ThrowIfNull(port.LAtelierWorkspace);
        ArgumentNullException.ThrowIfNull(port.LAtelierPlacement);
        ArgumentNullException.ThrowIfNull(port.LAtelierViewer);
        ArgumentNullException.ThrowIfNull(port.LAtelierEdit);
        ArgumentNullException.ThrowIfNull(port.LAtelierRoster);
        ArgumentNullException.ThrowIfNull(port.LAtelierRosterMenu);
        ArgumentNullException.ThrowIfNull(port.LAtelierFix);
        ArgumentNullException.ThrowIfNull(port.LAtelierFixPlan);
        ArgumentNullException.ThrowIfNull(port.LAtelierConvert);
        ArgumentNullException.ThrowIfNull(port.LAtelierTone);
        ArgumentNullException.ThrowIfNull(port.LAtelierEqualizer);
        ArgumentNullException.ThrowIfNull(port.LAtelierClinic);
        ArgumentNullException.ThrowIfNull(port.LAtelierRosterCard);
        ArgumentNullException.ThrowIfNull(port.LAtelierMerge);
        ArgumentNullException.ThrowIfNull(port.LAtelierNoise);
        ArgumentNullException.ThrowIfNull(port.LAtelierConsole);
        ArgumentNullException.ThrowIfNull(port.LAtelierFlow);
        ArgumentNullException.ThrowIfNull(port.LAtelierMap);
        ArgumentNullException.ThrowIfNull(port.LAtelierRosterDetail);
        ArgumentNullException.ThrowIfNull(port.LAtelierWhitebalance);
        ArgumentNullException.ThrowIfNull(port.LAtelierFilter);
        ArgumentNullException.ThrowIfNull(port.LAtelierAudio);
        ArgumentNullException.ThrowIfNull(port.LAtelierLoudness);
        ArgumentNullException.ThrowIfNull(port.LAtelierVideo);
        ArgumentNullException.ThrowIfNull(port.LAtelierScene);
        ArgumentNullException.ThrowIfNull(port.LAtelierList);
        ArgumentNullException.ThrowIfNull(port.LAtelierRelay);
        ArgumentNullException.ThrowIfNull(port.LAtelierTrial);
        ArgumentNullException.ThrowIfNull(port.LAtelierRepertoire);
        ArgumentNullException.ThrowIfNull(port.LAtelierEncoder);
        ArgumentNullException.ThrowIfNull(port.LAtelierAbout);

        CAtelierUsher = new CUsher(port.LAtelierUsher);
        CAtelierLineage = new CLineage(port.LAtelierLineage);
        _cAtelierWorkspacePort = port.LAtelierWorkspace;
        _cAtelierViewerPort = port.LAtelierViewer;
        CAtelierPlacement = new CPlacement(port.LAtelierPlacement);
        CAtelierInfo = new CInfo();
        CAtelierEdit = new CEdit(port.LAtelierEdit);
        _cAtelierRosterPort = port.LAtelierRoster;
        _cAtelierMenuPort = port.LAtelierRosterMenu;
        CAtelierFix = new CFix(port.LAtelierFix, port.LAtelierFixPlan);
        CAtelierConvert = new CConvert(port.LAtelierConvert);
        _cAtelierTonePort = port.LAtelierTone;
        _cAtelierEqualizerPort = port.LAtelierEqualizer;
        _cAtelierClinicPort = port.LAtelierClinic;
        _cAtelierCardPort = port.LAtelierRosterCard;
        CAtelierMerge = new CMerge(port.LAtelierMerge);
        _cAtelierNoisePort = port.LAtelierNoise;
        _cAtelierConsolePort = port.LAtelierConsole;
        _cAtelierFlowPort = port.LAtelierFlow;
        _cAtelierMapPort = port.LAtelierMap;
        _cAtelierDetailPort = port.LAtelierRosterDetail;
        _cAtelierWhitebalancePort = port.LAtelierWhitebalance;
        _cAtelierFilterPort = port.LAtelierFilter;
        _cAtelierAudioPort = port.LAtelierAudio;
        _cAtelierLoudnessPort = port.LAtelierLoudness;
        _cAtelierVideoPort = port.LAtelierVideo;
        _cAtelierScenePort = port.LAtelierScene;
        _cAtelierListPort = port.LAtelierList;
        _cAtelierAboutPort = port.LAtelierAbout;
        CAtelierRelay = new CRelay(port.LAtelierRelay, CAtelierUsher);
        CAtelierEncoder = new CSEncoder(port.LAtelierTrial, port.LAtelierRepertoire, port.LAtelierEncoder);
    }

    public CUsher CAtelierUsher { get; }

    public CLineage CAtelierLineage { get; }

    public CPlacement CAtelierPlacement { get; }

    public CInfo CAtelierInfo { get; }

    public CEdit CAtelierEdit { get; }

    public CFix CAtelierFix { get; }

    public CConvert CAtelierConvert { get; }

    public CMerge CAtelierMerge { get; }

    // The one relay across every tab of the window, keyed by tab id.
    public CRelay CAtelierRelay { get; }

    // The encoder subwindow's shared gate: every encoder window opens its own gates through it.
    public CSEncoder CAtelierEncoder { get; }

    // A viewer's workspace with the viewer's own trace gate and the flow beside it: each holds state for one viewer.
    public CWorkspace CAtelierWorkspaceCreate() =>
        new(
            _cAtelierWorkspacePort,
            new CViewer(_cAtelierViewerPort),
            new CFlow(_cAtelierFlowPort, _cAtelierMapPort, CAtelierUsher));

    public CInspector CAtelierInspectorCreate() =>
        new(
            _cAtelierTonePort,
            _cAtelierEqualizerPort,
            _cAtelierClinicPort,
            _cAtelierNoisePort,
            _cAtelierWhitebalancePort,
            _cAtelierFilterPort,
            _cAtelierAudioPort,
            _cAtelierLoudnessPort,
            _cAtelierVideoPort);

    // A roster's preference gate; it builds the roster's own card, detail and menu gates.
    public CRoster CAtelierRosterCreate() =>
        new(_cAtelierRosterPort, _cAtelierCardPort, _cAtelierDetailPort, _cAtelierMenuPort);

    // A window's console: it holds the station it shows and the progress it glides to.
    public CConsole CAtelierConsoleCreate() => new(_cAtelierConsolePort, _cAtelierScenePort);

    // A tab's list over its own docket.
    public CList CAtelierListCreate() => new(_cAtelierListPort, CAtelierUsher);

    // An About window's gate: it holds the tap count of that window alone.
    public CSAbout CAtelierAboutCreate() => new(_cAtelierAboutPort, CAtelierUsher);
}

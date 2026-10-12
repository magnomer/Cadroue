# LHost.cs
Hash: `9bb76486c8216f78`

## `int code = 1;`

The file uses top-level statements, since a method named `Main` fails the name audit.
The exit code stays 1 unless the application runs, so a failed start exits with 1.

## `Thread thread = new(() =>`

WPF needs a single-threaded apartment, and top-level statements cannot carry `[STAThread]`.
So the whole run happens on one STA thread the entry point starts and joins.

## `PProgram application = new();`

The veneer's application, which the host only creates, wires and runs.
Creating it makes it `Application.Current`, and its constructor loads `App.xaml`.

## `QProgram program = new();`

The deportment's startup sequence, which owns every load, attach and start of the run.
The host hands it the seams that need the dispatcher, the resources or Flyleaf.

## `DispatcherTimer debounce = new() { Interval = TimeSpan.FromMilliseconds(700) };`

The preference save debounce, built on the UI thread so its tick runs there.
Each restart stops and starts it, and the tick commits the pending preference save.

## `Action<EngineConfig, bool> logApply = (config, verbose) =>`

Sets the Flyleaf log level and output from the verbose switch.
The engine start and the later verbose change share it.

## `application.DispatcherUnhandledException += (_, e) =>`

An unhandled UI exception is recorded to the trace and marked handled, so the window stays up.
The domain and unobserved task exceptions are recorded the same way.

## `LLocalization.LLocalizationTraceSeam = LTraceLog.LTraceErrorRecord;`

Application, Media and Core cannot name Infrastructure, so the host points their seams at its stores.
The seams cover trace, preference, sidecar librarian, section and preset storage.
The preset selection seams point at the preset owner, since the store holds no preset logic.
They are filled first, so the startup load already reaches the stores.
The classifier and preset trace targets take an optional detail, so a one-call lambda adapts them.

## `LTraceEngine.LTraceInfoSeam = LTraceLog.LTraceInfoRecord;`

The engine never names Infrastructure, so the host points each engine seam at its Infrastructure target.
The seams cover the trace, depot folders, filter inventory, owner liveness, lease period and Fix dossier cache.
Infrastructure cannot name the engine either, so the checkup's flaw scanner seam points at the engine scanner.
The engine never names Media either, so process, tool, probe, keyframe and lane seams point at Media.
Each employer seam builds one employer per run, so every spawn keeps its own family and level.
The suspend, resume and live seams point at the Media child table, which holds the queue children.
They are filled before the application runs, so no engine record made during the start is lost.
The warning target takes an optional detail, so a one-call lambda adapts it to the seam.

## `LRenderer.LRendererFolderSeam = () => LPreference.LPreferenceStateCurrent.LPreferenceFfmpegFolder;`

These Infrastructure owners reach their Application inputs only through seams, never by naming Application.
The seams cover renderer folder, engine and text, palette, shared worklist, batch time, retention rules and librarian load.
Each preference seam is a one-call lambda, so the current preference is read at every use.

## `application.Startup += (_, e) =>`

The start runs inside `Run`, so the dispatcher is live before any step queues onto it.
It passes the command-line arguments, which carry a relayed tab.

## `program.QProgramStart(`

The dispatch seam blocks on the UI thread and the defer seam queues onto it.
The debounce restart and both Flyleaf hooks are handed in, since only the host holds them.
The localization catalogs stay embedded in the veneer, so the resource seams read the veneer's assembly.
The placement keys of the encoder and options windows are handed over for the placement import.

## `EngineConfig config = new() { UIRefresh = false, UIRefreshInterval = 250 };`

The Flyleaf engine start, which `LRenderer` invokes through its Flyleaf seam.
The FFmpeg folder is resolved at that moment, so a folder chosen in the preferences applies.

## `PNameplate.PNameplateAttach();`

Installs the developer-mode name overlay before the first window exists.
The veneer grants the host its internals, so the host reaches this internal class.

## `application.MainWindow = new PWindow();`

The host creates and shows the main window, and the application closes with it.

## `application.Exit += (_, e) => program.QProgramClose(e.ApplicationExitCode);`

The close saves the preferences, stops the relay channel and persists the sidecars and the trace.
It runs on exit, before `Run` returns the exit code.

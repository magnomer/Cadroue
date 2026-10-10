# LHost.cs

## `int code = 1;`

The file uses top-level statements, since a method named `Main` fails the name audit.
The exit code stays 1 unless the application runs, so a failed start exits with 1.

## `Thread thread = new(() =>`

WPF needs a single-threaded apartment, and top-level statements cannot carry `[STAThread]`.
So the whole run happens on one STA thread the entry point starts and joins.

## `PProgram application = new();`

The veneer's application, which the host only creates, wires and runs.
Creating it makes it `Application.Current`, and its constructor loads `App.xaml`.

## `LProgram program = new();`

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

## `application.Startup += (_, e) =>`

The start runs inside `Run`, as it did in the veneer's former `OnStartup`.
It passes the command-line arguments, which carry a relayed tab.

## `program.LProgramStart(`

The dispatch seam blocks on the UI thread and the defer seam queues onto it.
The localization catalogs stay embedded in the veneer, so the resource seams read the veneer's assembly.
The placement keys of the encoder and options windows are handed over for the placement import.

## `() => { EngineConfig config = new() { UIRefresh = false, UIRefreshInterval = 250 };`

The Flyleaf engine start, which `LRenderer` calls with the resolved FFmpeg folder.

## `PNameplate.PNameplateAttach();`

Installs the developer-mode name overlay before the first window exists.
The veneer grants the host its internals, so the host reaches this internal class.

## `application.MainWindow = new PWindow();`

The host creates and shows the main window, and the application closes with it.

## `application.Exit += (_, e) => program.LProgramClose(e.ApplicationExitCode);`

The close saves the preferences, stops the relay channel and persists the sidecars and the trace.
It runs on exit, before `Run` returns the exit code.

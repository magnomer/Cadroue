# LRenderer.cs
Hash: `800e3a70f962a0dc`

## `public sealed record LRendererFacts(`

What a preview engine reports about its renderer and decoder at one moment.
The engine wrapper reads the values, and this owner alone turns them into trace text.

## `public static void LRendererFactsRecord(string lReason, LRendererFacts lFacts, double? lMilliseconds = null) =>`

Records the renderer facts under the reason the caller names, with no config text.

## `public static void LRendererSetupRecord(`

Records the facts of a freshly built player under the reason `Player created`.
The config line is built here from the raw values the wrapper read.
The milliseconds are the creation time the wrapper's GUI clock measured.

## `private static void LRendererFactsRecord(`

Writes nothing unless the UI trace is on, so building the text costs nothing otherwise.
The verdict and facts go out as one UI line.
The config text is appended only when the caller hands one in.

## `public static Func<string> LRendererFolderSeam { get; set; } = () => string.Empty;`

The FFmpeg folder chosen in the options, read through a seam the host binds to the preference.
Unattached, it is empty, so the settings file and the search decide.

## `public static Func<string> LRendererEngineSeam { get; set; } = () => LRendererFlyleafToken;`

The preview engine token chosen in the options, bound by the host to the preference.
Unattached, it names Flyleaf, as an empty preference would.

## `public static Func<string, string> LRendererTextSeam { get; set; } = lRendererKey => lRendererKey;`

Localizes the renderer's error text without this ring reading the catalog.
Unattached, it returns the key itself.

## `public static string LRendererFolderCurrent =>`

The FFmpeg folder chosen in the options wins over the one in the renderer settings file.

## `public static void LRendererProcessorRecord(string lProcessorWas, string lReason)`

Records why the video processor was forced to Flyleaf.
Nothing is written when Flyleaf was already in use, since nothing changed.

## `public static string LRendererFfmpegResolve(string lRendererDefaultPath)`

Picks the first valid FFmpeg folder: the options, then the settings file, then a search.
The caller's default path is the last resort when none is found.

## `public static Action? LRendererFlyleafSeam { get; set; }`

Starts the Flyleaf engine, which only the host can name.
Unattached, the engine start skips Flyleaf and still resolves the preview engine.

## `public static Action<Action>? LRendererDispatchSeam { get; set; }`

Carries the engine update after a background probe onto the UI thread.
Unattached, the update runs on the probe's own thread, which suits a run with no UI.

## `public static Action<bool>? LRendererVerboseSeam { get; set; }`

Applies the verbose switch to the Flyleaf log.
Unattached, a verbose change leaves the Flyleaf log as it is.

## `public static string LRendererLogResolve(bool lRendererVerbose) =>`

An empty path keeps Flyleaf from writing a log file while verbose is off.
Each process writes its own file, so two launches never share one log.

## `public static void LRendererEngineStart()`

Starts the Flyleaf engine and routes the verbose switch to its log.
A startup failure is recorded and the program keeps going on the preferred engine.
An mpv library never probed is checked in the background.

## `public static void LRendererEngineSet(LPreviewEngine lRendererEngine)`

Falls back to Flyleaf while mpv is not available.
The change notice also fires when mpv availability changes, so pickers update their choices.

## `public static Task<LMpvProbe> LRendererEngineCheck()`

Runs the mpv viability probe once at a time, and a second caller shares the running probe.
A crashing probe counts as unusable.
The result is saved, then the engine is resolved again on the UI thread through the dispatch seam.

## `public static async Task<bool> LRendererMpvCheck()`

Probes mpv only when no stored result exists, so a known answer returns at once.

# Cadroue.slnx

Lists the twelve source projects that `dotnet build Cadroue.slnx` compiles.
This file is the single authoritative statement of the target structure.
It states the target, and the source tree may lag it.
The audits and convention tests measure the lag as falling ceilings.

## Layers

- Veneer → Deportment → Conduct → ShellEngine → Application → Core.
- Deportment → Deportment.Capsule.
- UITerminal → Demeanor → Conduct.
- Each layer names only the layer directly below it.
- A driver also names its own Capsule, and no other layer names it.
- Data travels freely below Conduct and never crosses the cut above it.
- Host is the only exception.

## Two media, one shape

- Each medium has a surface that draws and a driver that drives it.
- The GUI surface is the Veneer, and its driver is Deportment.
- The CUI surface is UITerminal, and its driver is Demeanor.
- Both drivers stand on the same Conduct.
- A rule stated for one surface holds for the other surface.
- A rule stated for one driver holds for the other driver.

| Role | GUI | CUI | Prefix |
|---|---|---|---|
| Surface | `Cadroue.UIVeneer` | `Cadroue.UITerminal` | `P` |
| Driver | `Cadroue.UIDeportment` | `Cadroue.UIDemeanor` | `Q` |
| Shared | `Cadroue.Conduct` | `Cadroue.Conduct` | `C` |

## Data and the cut

- A hard cut runs between the UI and Conduct.
- Above the cut stand Veneer with Deportment, and UITerminal with Demeanor.
- Below the cut stand Conduct and every layer under it.
- Below the cut, data travels freely, so a value made in Core may pass up to Conduct.
- Above the cut, data travels only within its own medium.
- Nothing crosses the cut but .NET values and Conduct's own types.
- A gate's signature names only .NET types and Conduct types.
- A driver sends a gate only .NET values or Conduct types.
- Conduct's types stop at the driver, which hands its surface shapes of its own.
- A layer never names a project beyond its neighbour: no `using`, no type name, no call, no `ProjectReference`.
- A UI layer naming a type from below the cut, other than its neighbour's, is a violation.
- Deportment or Demeanor naming a Core record is a violation, however plain the record.
- A XAML binding that reads a member of a deeper type crosses the cut as surely as C# does.

## `<Project Path="src/Cadroue.UIVeneer/Cadroue.UIVeneer.csproj" />`

- Veneer is what the user sees: pure XAML, themes and assets.
- A visual that C# builds, draws, generates or selects is logic, and it lives in Deportment.
- Veneer XAML carries no hook: no trigger, binding, converter, event attribute, command or `x:Static`.
- Deportment sets every property, subscribes every event and switches every visual state.
- A code-behind is a shell: an `x:Class` constructor that only calls `InitializeComponent()`.
- An `x:Class` naming a Veneer type is not a hook.
- A Veneer member only calls a function.
  No exception.
- The Veneer defines no method, handler, helper, property or field.
- No `if`, no loop, no operator, no pattern, no local, no computed argument.
  Veneer has no reason to hold logic.
- Veneer names only Deportment.

## `<Project Path="src/Cadroue.UITerminal/Cadroue.UITerminal.csproj" />`

- UITerminal is what the console user sees: the drawn text, frames and fields.
- UITerminal offers two modes: an interactive CUI and a command-line CUI.
- In the interactive CUI the user navigates by key presses.
- The interactive CUI supports every key feature the Veneer offers.
- Only GUI-only work such as drag, clipboard and printing is left out.
- In the command-line CUI, UITerminal relays the command to Demeanor and prints the result.
- In both modes UITerminal only presents what Demeanor hands it and relays input to Demeanor.
- UITerminal never decides what is drawn.
- A UITerminal member only calls a function.
  No exception.
- No `if`, no loop, no operator, no pattern, no computation.
  UITerminal has no reason to hold logic.
- UITerminal names only Demeanor.

## `<Project Path="src/Cadroue.UIDeportment/Cadroue.UIDeportment.csproj" />`

- Deportment is the only project that manipulates the Veneer.
- Deportment is the master of the Veneer and owns the contract.
- Deportment holds no scaffold and pulls every part by contract ID.
- Deportment is not the Veneer.
  It is not what is shown to the user.
- Deportment uses WPF freely.
  Nothing forbids it.
- Deportment holds only what a GUI needs: visibility, focus, dispatcher, dialogs, drag, clipboard, printing.
- Deportment also holds every visual written by logic: custom controls, converters, item models, commands, localization.
- Deportment names Conduct and its own Capsule, and nothing else.

## `<Project Path="src/Cadroue.UIDeportment.Capsule/Cadroue.UIDeportment.Capsule.csproj" />`

- Capsule is Deportment's own storage.
- It holds the GUI-only state that must outlive a run: window geometry, subwindow placement, column widths and flow height.
- It reads and writes that state on disk under the workspace root.
- It holds no rule about what the state means.
  Deportment clamps, propagates and decides.
- It names no Cadroue project.
  It uses .NET alone.
- Only Deportment names it.
  Conduct and every layer below never see GUI-only state.
- Its types take `L`, since it handles no Veneer part and is no driver.
- Demeanor may have its own Capsule by the same rules, and none exists yet.

## `<Project Path="src/Cadroue.UIDemeanor/Cadroue.UIDemeanor.csproj" />`

- Demeanor is the only project that manipulates UITerminal.
- Demeanor is Deportment's mirror for the CUI.
- Demeanor decides what UITerminal draws, in both modes.
- Demeanor is not UITerminal.
  It is not what is shown to the user.
- Demeanor uses the console freely.
  Nothing forbids it.
- Demeanor holds only what a CUI needs: cursor, key reading, redraw, prompts, screen size.
- Demeanor names Conduct and its own Capsule, and nothing else.

## Driver

- A driver does three things: gather input in its medium, call a gate, show the verdict.
- A driver controls its surface completely: what is drawn, focus, navigation and redraw.
- A driver manipulates its surface but never the data.
- Conduct holds every behaviour of the application, down to the smallest action.
- Deportment manipulates the Veneer, and nothing else.
- A driver never defines what the application does.
  It only defines how its medium shows it and how its medium hears the user.
- Every line of a driver answers one question.
  Would the other driver need this same answer?
- If yes, it is behaviour, and it moves into Conduct or deeper.
- If no, it only concerns pixels, controls, focus or the console, and it stays in the driver.
- Behaviour includes creating, changing, removing, ordering or linking anything.
- It includes validation, trimming, empty and duplicate checks, limits, clamps, defaults and fallbacks.
- It includes choosing a localization key, parsing text, and deciding what happens next.
- It includes dirty, undo, redo and held state, mapping engine records, and the order of engine calls.
- A driver never builds, checks or decides what a gate owns.
- Both drivers call the same gate for the same user action.
- Creating a new entry is one Conduct gate.
  Deportment and Demeanor both call it.
- A behaviour found in one driver and absent from the other is either medium work or a leak.

## Contract

- Deportment is the master, and the Veneer is its subordinate.
- Deportment owns the contract: the IDs of the roles it drives.
- The Veneer supplies an element or a resource for every ID in the contract.
- An element carries its ID as `x:Name`, and a resource carries it as `x:Key`.
- The Veneer never announces, maps or injects a role.
- Anything the Veneer asks of Deportment carries no role.
- Deportment pulls each part by its ID.
- Deportment never names the Veneer: no pack URI, no `Cadroue.UIVeneer`.
- The Veneer may offer a referent list, and Deportment never needs it.
- The Veneer holds the scaffold: application, windows, pages, controls and resource dictionaries.
- Each Veneer page has an `x:Class` shell whose constructor only calls `InitializeComponent()`.
- Deportment holds no scaffold.
- Deportment may draw.
  A drawn leaf is a `Q` element, and the Veneer places it.

## `<Project Path="src/Cadroue.Conduct/Cadroue.Conduct.csproj" />`

- Conduct is the abstraction that serves both Deportment and Demeanor.
  The layers above the cut must keep working when every layer below Conduct is swapped and Conduct is not.
- A driver relies only on what Conduct promises, never on how the engine below behaves.
  A driver that trusts an engine habit Conduct does not guarantee breaks under the swap.
- Conduct holds every behaviour behind the screen that does not depend on the medium.
- Conduct uses no WPF and no console.
- Conduct asks the user through ports that each driver implements.
- Conduct is the only path from a driver to the engine.
- Conduct names only ShellEngine.

## Gate

- Conduct is organized by gates.
- A gate is one way into the inner code: one user action with one function and one signature.
- Every user action, down to one field edit, is a gate.
- A gate is named as a user action: create an entry, save an entry, delete, search, record, navigate.
- A function that cannot be named as a user action is not a gate.
  It is a helper inside one.
- Panel state, shown and enabled flags, drafts, undo and redo live inside the gate that owns them.
- State that no gate owns is a smell.
- A gate adds the medium-free rules around the engine call: confirm, dirty check, navigation after the action.
- A gate that adds nothing is still the only door.
  A driver never skips it to reach ShellEngine.
- A gate that must ask the user asks through a port.
  Each driver implements the port in its medium.

## `<Project Path="src/Cadroue.ShellEngine/Cadroue.ShellEngine.csproj" />`

- ShellEngine is the engine that connects application workflows to Conduct.
- ShellEngine names only Application.
- Infrastructure adapters reach it from outside, wired by Host.

## `<Project Path="src/Cadroue.Application/Cadroue.Application.csproj" />`

- Application holds the workflows over the Core model.
- Application names only Core.

## `<Project Path="src/Cadroue.Core/Cadroue.Core.csproj" />`

- Core holds the domain model and the contracts every outer layer implements or calls.
- Core names no project.

## `<Project Path="src/Cadroue.Infrastructure/Cadroue.Infrastructure.csproj" />`

- Infrastructure implements Core's contracts: database, files, network sources and external programs.
- Infrastructure names only Core.
- No layer names Infrastructure except Host, which wires it into the engine.

## `<Project Path="src/Cadroue.Core.Windows/Cadroue.Core.Windows.csproj" />`

- Core.Windows is Core's Windows twin.
- It follows the twin rules below.

## `<Project Path="src/Cadroue.Host/Cadroue.Host.csproj" />`

- Host is the only project that names every project except a driver's Capsule.
- Host builds the engine and the Conduct root and hands them to a driver.
- Host holds no behaviour.
- Each medium's entry point adds only what depends on its medium.

## Platform

- Platform is a second axis that crosses every layer.
- A layer splits into a portable half and a Windows twin.
- The portable half targets `net10.0` only and holds no Windows code.
- The Windows twin targets a `net10.0-windows` framework and holds only that layer's Windows adaptation.
- Conduct and Deportment already follow this pattern for the GUI.
- Demeanor and UITerminal are portable.
  The console needs no Windows twin.
- Below the UI, a layer gets a Windows twin only when it has a real Windows need.
- No twin is created empty.

## Twin

- A twin names only its portable half.
- The portable half never names its twin.
- The portable half declares a port for each platform need, and its twin implements the port.
- Callers reach Windows code only through the portable half's ports.
- A twin holds no domain logic.
  It maps a port call to a Windows API and back.
- A twin never becomes a shortcut to another layer.
- Host is the only project that names a twin and wires it into its layer.
- Another platform adds another column of twins, not a change to the portable halves.
- Twin naming is not settled.

## Current twins

| Portable | Windows twin |
|---|---|
| Core | Core.Windows: the Windows halves of `Custody`, `Usher`, `Relay`, `Mpv` and `Runner` |
| Conduct | Deportment, Veneer |
| Application | none |
| ShellEngine | none |
| Infrastructure | none |

- `Custody` maps the job object and child priority.
- `Usher` maps the Explorer reveal.
- `Relay` maps the target window lookup.
- `Mpv` maps the window embed.
- `Runner` maps process suspend and resume.
- Each reaches Core.Windows through a port its portable owner declares.
- `Cadroue.Media` dissolves into Infrastructure.
  It holds no Windows code, and ffmpeg and ffprobe are external programs behind Core's contracts.

## Enforcement

- A portable project targets exactly `net10.0`.
- A portable project never references a `-windows` project.
- CA1416 is an error in every portable source project.
- A Windows API in a portable source project therefore fails the build.
- The surface rules of the UI audit cover UITerminal as they cover the Veneer.
- The driver rules of the UI audit cover Demeanor as they cover Deportment.
- Both drivers name only the public Conduct types on their offer list.
- The structure audit counts every type a UI layer names from below the cut, data included.
- The Border family's Leaking kind reads each type on Conduct's offer list.
- It counts every public signature there that names a type below Conduct.

## Where logic goes

- Logic leaves the surface and goes wherever appropriate.
- If the other medium would need the same answer, the logic goes to a Conduct gate.
- If the answer depends on WPF, the logic goes to Deportment.
- If the answer depends on the console, the logic goes to Demeanor.
- If the answer is GUI-only state that must persist, Deportment decides it and Capsule stores it.
- Engine work goes to ShellEngine, Application or Core.
- First pick the layer that owns the concern.
  Then pick its portable half or its twin.
- Platform never decides the layer.

## Earlier misreadings

- Deportment was treated as a WPF-free helper that the Veneer consults.
  Wrong.
  Deportment drives the Veneer.
- The Veneer was treated as the actor that asks the engine, holds answers and branches on them.
  Wrong.
  The Veneer only calls.
- The Veneer was treated as free to build, draw, place and hold gesture transients.
  Wrong.
  Each of those is logic, and it lives in Deportment.
- The UI audit allowed some Veneer branches as view state.
  Wrong.
  Any branch in the Veneer is a violation, and so is any engine reach.
- Deportment was treated as the home of every decision behind the screen.
  Wrong.
  Medium-free decisions belong to Conduct, so that the CUI shares them.
- Deportment was treated as free to name Core, Application, ShellEngine, Infrastructure and Media.
  Wrong.
  Deportment names only Conduct and its Capsule.
- Deportment types were treated as logic types under `L`.
  Wrong.
  A driver's types take `Q`.
- Veneer was treated as free to name Core, Application and ShellEngine.
  Wrong.
  The Veneer names only Deportment.
- The Veneer was treated as the place that builds the application.
  Wrong.
  Host builds it, so that no UI names what lies below Conduct.
- Windows adaptation was treated as one project for every layer.
  Wrong.
  That project would mix every layer's role, so each layer gets its own twin.
- `Cadroue.Media` was treated as a separate media layer.
  Wrong.
  It was Infrastructure.
- Windows code was treated as free to sit in a portable project.
  Wrong.
  It sits in a twin behind a port.
- UITerminal was treated as one layer that both draws and drives.
  Wrong.
  It only draws, and Demeanor drives it, as Deportment drives the Veneer.
- The interactive UITerminal was treated as deciding what to draw.
  Wrong.
  Demeanor decides, and UITerminal only presents.
- Demeanor was treated as Deportment's twin.
  Wrong.
  It is Deportment's mirror, and twin names only the platform axis.
- Conduct was treated as a pile of panel state.
  Wrong.
  It is organized by gates, and state lives inside the gate that owns it.
- Data was treated as free to travel from Core up to the Veneer.
  Wrong.
  It travels freely below Conduct and never crosses the cut.
- Controls, converters and triggers were treated as Veneer structure.
  Wrong.
  What logic writes or manipulates is logic, and it lives in Deportment.
- The Veneer was treated as holding no C#.
  Wrong.
  Each Veneer page keeps an `x:Class` shell that only calls `InitializeComponent()`.
- Deportment was treated as loading the Veneer by pack URI.
  Wrong.
  The Veneer holds the scaffold, and Deportment pulls each part by contract ID.
- GUI-only state was treated as Conduct's because a medium-free part sat beside it.
  Wrong.
  Each piece is split by concern first, and GUI-only state never goes below Deportment.

## Test projects

The projects under `tests` are left out on purpose.
`scripts/Test.ps1` finds, builds and runs them from the tests folder instead.
`Cadroue.Tests.Interface` holds the relays, fakes and workspace, and references Core, Infrastructure, ShellEngine and Conduct.
`Cadroue.Tests.Engine` tests Core, Application, Infrastructure and ShellEngine, and references only `Cadroue.Tests.Interface`.
`Cadroue.Tests.Conduct` tests the Conduct gates, and references only `Cadroue.Tests.Interface`.
`Cadroue.Tests.Windows` tests Capsule and Deportment, and references `Cadroue.Tests.Interface` and Deportment.
`Cadroue.Tests.Convention` audits the source tree as text, and references no project.

## Performance project

`performance/Cadroue.Performance` is left out on purpose too.
It holds the drills, fixed workloads that no script runs at present.
It references portable engine projects only, never a UI project.

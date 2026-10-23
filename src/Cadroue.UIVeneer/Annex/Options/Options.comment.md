# Options

This folder defines the passive Options window and its General, Playback, Timeline and Work presentation surfaces.
The controls expose named composition points without choosing behaviour.
No markup in this folder binds a model or declares an event handler.
The System presentation surfaces remain in `System`.

## PSOptions.xaml

The window starts at 900 by 660 and refuses to shrink below 780 by 520.
Its bottom dock reserves three named apply, confirm and cancel buttons.
The tab deck owns five scrollable pages in General, System, Playback, Timeline and Work order.
Vertical scrolling remains available on every page while horizontal scrolling is disabled.

The window resources instantiate `PSFlyleaf`, `PSMpv` and `PSSpectrum` under matching keys.
The markup does not assign any of those resource instances to a content host.

## PSOptions.cs

Construction loads the XAML, then calls `PSSubwindow.PSSubwindowApply(this)`.
The class contains no close, save or command policy.

## PSGeneral.xaml

The General surface is a passive stack of startup, recent-work, safety, vacancy, orientation and language controls.
`PSGeneralStartup` and `PSGeneralSelection` are separate named controls placed beside each other.
The remaining named controls expose the recent, safety and vacancy checks, the orientation host and the language combo box.

## PSGeneral.xaml.cs

The constructor only calls `InitializeComponent`.

## PSOptionsPlayback.xaml

The Playback page reserves `PSPlaybackSelector` for the engine choice and separate `PSPlaybackPrimary` and `PSPlaybackAlternate` content hosts.
`PSPlaybackNotice` is a sibling of those hosts.
The remaining named parts expose autoplay, volume scope, mouse-wheel behaviour and paused dragging.
The loudness slider accepts 0 through 100 and has a separate readout.

## PSOptionsPlayback.cs

The constructor only calls `InitializeComponent`.

## PSOptionsTimeline.xaml

The Timeline page exposes hosts for strip arrangement and palette editing.
Its spacing slider accepts 1 through 50, and its delay slider accepts 0 through 5000.
The readouts and notice blocks have no text in the markup.
Named checks expose overlap and waveform choices.

## PSOptionsTimeline.cs

The constructor only calls `InitializeComponent`.

## PSOptionsWork.xaml

The Work page exposes paused and retry checks and a named retry-limit row.
Its limit slider accepts 0 through 10 and has a separate readout.
The retry-limit row has its own `PSWorkLimit` name, but the markup declares no state trigger or handler for it.
Three combo boxes under `PSWorkLevels` reserve independent encoding, keyframing and sampling priority choices.

## PSOptionsWork.cs

The constructor only calls `InitializeComponent`.
No priority, retry or failure policy is implemented in this folder.

## Stamp

Commit: `9a34a722aa84`
- `PSGeneral.xaml` `4fd3cac6f15aaa5c`
- `PSGeneral.xaml.cs` `2b32f815d462d331`
- `PSOptions.cs` `0159598490656f46`
- `PSOptions.xaml` `420d9b9aa759d3ed`
- `PSOptionsPlayback.cs` `8cba15cb45ca0db2`
- `PSOptionsPlayback.xaml` `3e6733e947f9e755`
- `PSOptionsTimeline.cs` `b369ccd97b75fd6d`
- `PSOptionsTimeline.xaml` `415941bea0684690`
- `PSOptionsWork.cs` `76f178ae14ecc783`
- `PSOptionsWork.xaml` `65a75aa9b8263b31`

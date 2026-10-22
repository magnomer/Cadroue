# Options

The Options window is passive controls.
Behaviour lives in Conduct policy and Deportment drivers.
This folder builds the window and the General, Playback, Timeline and Work surfaces.
The System page and its surfaces live in `System`.
The window reads the controls into the draft and tears down on close.

## PSOptions.cs

The window creates every page control before it builds any page, so apply can read a page never shown.
Each sheet carries its own Apply, OK and Cancel footer.
Cancel only closes, so the draft is dropped.
Apply copies the controls into the draft, saves through the driver, then runs the caller's callback.
The callback runs even when the save failed, so the program reflects the choice.
After the callback the System page refreshes its workspace state.
A failed save is reported and skips the restart notice.
Close detaches every driver and saves the window placement, and the draft releases the mpv subscription.
Test `Close_DetachesMpvProjection` reads the source text, since a window cannot be shown in a test.
It pins the mpv subscribe, the unsubscribe and the close call, not the other detaches.

## PSOptionsDraft.cs

The draft owns the Playback, Timeline and Work controls and writes them into the draft preference.
It also writes the System page's record mode, cleanup choice, workspace folder and renderer path.
It subscribes to mpv availability and unsubscribes at close, so the mpv engine option follows availability.
It starts an availability check when built.
The draft asks the workspace driver to commit, and that driver restores the box and warns.

## PSGeneral.xaml

The surface is markup with a code file that only builds it.
The General driver fills every text and handler.

## PSOptionsPlayback.cs

The engine choice shares a plate with the Flyleaf and mpv install surfaces.
The plate ends with the mpv edit notice.
The Playback sheet hosts those two surfaces, though their files sit in `System`.

## PSOptionsTimeline.cs

The page hosts the palette editor surface, and the palette dialog driver fills it.

## PSOptionsWork.cs

The retry limit row is enabled only while retry is checked.
A priority combo writes its level at once, so apply does not read it back.

## PSOptionsField.cs

The stored slider value is clamped into range, so a stale preference cannot break the page.
The readout shows whole numbers only.

## Stamp

Commit: `b84e5458de50`
- `PSGeneral.xaml` `4fd3cac6f15aaa5c`
- `PSGeneral.xaml.cs` `2b32f815d462d331`
- `PSOptions.cs` `a9f5185d9590b6ab`
- `PSOptionsDraft.cs` `323e0f24c243b7fd`
- `PSOptionsField.cs` `2e5e467eaef11200`
- `PSOptionsPlayback.cs` `8f951610efe45269`
- `PSOptionsTimeline.cs` `9fb8bb4b343a1fc5`
- `PSOptionsWork.cs` `32016d5d0abad9e5`

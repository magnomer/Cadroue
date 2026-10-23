# System

This folder defines passive System, workspace, renderer, Flyleaf and mpv presentation surfaces.
Named parts expose the surface structure without attaching behaviour.
The markup fixes composition, ranges and presentation resources but owns no persistence, installation, cleanup or window policy.

## PSOptionsSystem.xaml

The System page composes `PSRenderer` and `PSOptionsSystemWorkspace` before its history and ledger sections.
History exposes a finished-work host, a purge check and a retention row.
The retention slider accepts 1 through 365 beside a readout and caution block.
Ledger exposes a location host, explanatory text blocks and an erase button.

## PSOptionsSystem.cs

The internal page constructor only calls `InitializeComponent`.

## PSOptionsSystemWorkspace.xaml

The workspace surface exposes the folder box, browse and open buttons, default-location text, current size, cleaner button and maintenance explanation.
Its `PSSystemDone` resource is a styled 190 by 32 button rather than an element in the visual tree.

The markup resolves `PTextbox` and `PButtonWhite` through dynamic resources, allowing the active presentation resources to supply those styles.

## PSOptionsSystemWorkspace.cs

The internal constructor only calls `InitializeComponent`.

## PSRenderer.xaml

The renderer surface exposes a path box, finder, opener, examiner and status text in one presentation block.
The three buttons are separate named controls and declare no content, command or event handler.

## PSRenderer.xaml.cs

The constructor only calls `InitializeComponent`.

## PSFlyleaf.xaml

The Flyleaf surface provides installer and opener buttons beside a progress indicator.
Progress accepts 0 through 1 and starts collapsed.
Its local template names `PART_Track` and `PART_Indicator`, clips the rounded track and keeps the indicator left-aligned.

## PSFlyleaf.xaml.cs

The constructor only calls `InitializeComponent`.

## PSMpv.xaml

The mpv surface mirrors the Flyleaf installer, opener and collapsed 0 through 1 progress presentation.
It retains the same named template parts and rounded clipped indicator.

## PSMpv.xaml.cs

The constructor only calls `InitializeComponent`.
Availability checks, installation and opening behaviour are not implemented by this surface.

## Stamp

Commit: `9a34a722aa84`
- `PSFlyleaf.xaml` `a97303de0c58b55b`
- `PSFlyleaf.xaml.cs` `c2057ba204d5c650`
- `PSMpv.xaml` `9b902508047e8b7b`
- `PSMpv.xaml.cs` `dbc40429a4661ac1`
- `PSOptionsSystem.cs` `6a5ffe931133fa7b`
- `PSOptionsSystem.xaml` `eb0246e781c202c3`
- `PSOptionsSystemWorkspace.cs` `c48daa1b5a236bf4`
- `PSOptionsSystemWorkspace.xaml` `fcc3de6d5c49ac41`
- `PSRenderer.xaml` `3cb3068d3dc543ff`
- `PSRenderer.xaml.cs` `f6aea06e456235e1`

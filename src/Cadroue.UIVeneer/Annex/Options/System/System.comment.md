# System

## PSOptionsSystem.cs

This file only composes the System page.
It attaches the workspace and renderer surfaces to their drivers and owns the cleanup controls.
After apply it refreshes the workspace state and the record notice.
Clearing records refreshes only the workspace size.
That split follows the driver, and the tests that pin it are named in the Deportment sidecar.

## PSOptionsSystemSidecar.cs

The record mode writes the draft at once, so the driver can pick the notice key from it.
The driver supplies the notice key and folder and this page formats them.
Clearing records reports a missing folder instead of asking.
After a clear it refreshes only the workspace size.

## PSOptionsSystemWorkspace.xaml

The surface has named controls and a code file that only builds it.
The Done button is a resource, so the System page can place it on the work plate.
The workspace driver finds each control by name.
Tests load the compiled assembly by reflection from its Debug build path, because the surface class is internal.
The workspace tests therefore need a built Debug Veneer assembly.

## PSFlyleaf.xaml, PSMpv.xaml and PSRenderer.xaml

Each surface is markup with a code file that only builds it.
Its driver fills every text and handler.
The Playback sheet hosts the Flyleaf and mpv surfaces, and the System page hosts the renderer surface.

## Stamp

Commit: `b84e5458de50`
- `PSFlyleaf.xaml` `a97303de0c58b55b`
- `PSFlyleaf.xaml.cs` `c2057ba204d5c650`
- `PSMpv.xaml` `9b902508047e8b7b`
- `PSMpv.xaml.cs` `dbc40429a4661ac1`
- `PSOptionsSystem.cs` `5e1ac4ec1534564f`
- `PSOptionsSystemSidecar.cs` `b26aadbf0a8b4140`
- `PSOptionsSystemWorkspace.cs` `586da2e2a115ac06`
- `PSOptionsSystemWorkspace.xaml` `fcc3de6d5c49ac41`
- `PSRenderer.xaml` `3cb3068d3dc543ff`
- `PSRenderer.xaml.cs` `f6aea06e456235e1`

# Casement

These retained helpers build and style reusable WPF controls in code.
They own presentation construction and local interaction state rather than serving as passive markup shells.

## PSGrabber.cs

`PSGrabberPairBuild` binds the shared attach and detach callback pair to one window-specific grabber instance.

Release or lost capture ends an active drag.
Detachment removes handlers and clears the forced cursor.
Final bounds flow through `QSashWindow`, which owns applying a complete bounds value to a WPF window.

## PSField.cs

`PSField` centralizes the font, control and chip dimensions shared by code-built fields.
It also owns their line, text, muted, accent and inactive brushes.
Each brush is frozen immediately after creation, so consumers receive an immutable presentation resource.
`PSFieldLabelBuild` creates the standard vertically centered muted label while leaving layout and content ownership to its caller.

## PSMode.cs

`PSModeBuild` creates a segmented token selector from localized choices and returns an enable delegate for later availability changes.
The requested token is retained only when it occurs in the choices.
Otherwise the first choice becomes selected, or the empty string when there are no choices.
The host stores the selected token in its tag, and `PSModeTextRead` returns that token.

Each segment renders `LLocalizationChoice.ToString()` and records its token separately.
A click on the already selected segment does nothing.
A different enabled segment updates the token and styling before invoking the supplied change callback.
Disabled segments reject hit testing, use the arrow cursor and render at 0.45 opacity.

Disabling the selected token moves selection to the first enabled segment when one exists.
If every segment is disabled, the previous token remains selected.
Changing availability restyles the selector but does not invoke the change callback.

## PSSheet.cs

`PSSheetControlBuild` creates a white borderless tab control, adds supplied sheets in order, then applies the shared template and item style.
`PSSheetApply` also attaches selection tracking and computes separator state immediately.
A separator appears only after a non-final tab that is neither selected nor immediately before the selected tab.

Sheet headers optionally place a 17 by 17 icon before the title with a 10-unit gap.
`PSSheetScrollBuild` permits vertical scrolling and disables horizontal scrolling.

The tab template reserves the Casement band, leading column, button strip and content overlap dimensions.
The selected-content presenter uses the required `PART_SelectedContentHost` name.
The item style gives each tab a 42-unit height, caller-supplied minimum width and selected, hover and separator states.
All numeric values substituted into runtime XAML use invariant culture so parsing does not depend on the current locale.

## Stamp

Commit: `9a34a722aa84`
- `PSAlert.cs` `821e8205617f0012`
- `PSAnnouncement.cs` `b8f18e01d768478f`
- `PSCasement.cs` `c169a7bfba97083d`
- `PSCasementButton.cs` `8714e8499091fd06`
- `PSCasementDwm.cs` `6c2d094fdcbff662`
- `PSCasementGlyph.cs` `146109da294ef511`
- `PSCasementWindow.cs` `55f135961dd7c175`
- `PSDecision.cs` `72ef025cfb99925a`
- `PSDialog.cs` `f6c5cf4203e7b392`
- `PSDialogLocation.cs` `75adb00ad976a5e9`
- `PSDialogMessage.cs` `f3fc8cadac583dd7`
- `PSDialogTheme.cs` `5768ce3f953fbc09`
- `PSField.cs` `07c96ac9728a2384`
- `PSFooter.cs` `2d80d0bfe96f96d3`
- `PSGrabber.cs` `89cfb40d25184d07`
- `PSGrabberPlacement.cs` `dc63521c59bb2b5b`
- `PSHeadline.cs` `14bb04ee29afa3bf`
- `PSInline.cs` `679995f0c91afed5`
- `PSMode.cs` `53721c3d3e086055`
- `PSPlate.cs` `9653ba1892e6674b`
- `PSSheet.cs` `6846c339a2dfc7ef`
- `PSSubwindow.cs` `27c1bc117afe4de0`
- `PSWarning.cs` `61d3343a050718ee`

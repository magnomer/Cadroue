# TViewerSource.cs
Hash: `e86fbeee8dcc1962`

## `public sealed class TViewerSource`

Checks how the viewer's source deportment opens a path or a drop without any control.
Each test builds a viewer with commands enabled and reads its source sibling.
The Logging collection keeps it from running beside tests that share the trace.

## `public void SourceOpen_CommandInactive_Refuses()`

An open is a command.
While commands are off the viewer records no intent and treats the path as not loaded.

## `public void SourceOpen_PlainPath_StartsLoad_AndReportsMissingFile()`

A plain path starts a load even when the file is absent.
The load reports its failure through the media notice and leaves no intent behind.

## `public void SourceOpen_SidecarUnresolved_AsksWarning_AndRefuses()`

A sidecar whose media cannot be found raises a warning question.
Whatever the answer, nothing opens.

## `public void SourceOpen_SidecarMismatch_DecisionAccepted_OpensResolvedPath()`

A sidecar that resolves to a sibling media file raises a decision question.
Accepting it opens the resolved media path and not the sidecar.

## `public void PathHandle_SameSource_DoesNotReopen_OtherSourceDoes()`

A path notice from the list must not reload what is already loaded or pending.
The load serial shows whether a reload happened.

## `public void DropResolve_ListPresent_AcceptsFoldersAndMedia()`

With a list attached a drop is judged for the list.
Folders are accepted and a missing file or a missing payload is not.
Resolving never opens anything in the viewer.

## `public void DropHandle_ListPresent_HandsPathsToList()`

With a list attached the dropped paths go to the list instead of the viewer.

## `public void DropResolve_ViewerOnly_NeedsExistingFile_AndAudioAllowance()`

Without a list only an existing file is accepted.
An audio file is accepted only after the viewer allows audio-only media.
The answer follows the file on disk, so a deleted file is refused.

## `public void DropHandle_ViewerOnly_OpensFirstExistingFile()`

Without a list a drop opens the first existing file in the viewer.

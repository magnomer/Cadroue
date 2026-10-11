# TSourceFilter.cs
Hash: `eacea6f5751f0fd8`

## `public sealed class TSourceFilter`

Checks the source panel deportment decisions without any control.
Most tests build a source over a real viewer.

## `public void Filter_ListsVideoPatterns_AudioOnlyWhenAllowed()`

The open dialog filter widens when the viewer allows audio-only files.

## `public void Placeholder_ShownForBlankText()`

Empty or whitespace text asks for the placeholder.

## `public void MediaChange_PublishesThePathToShow()`

A media notice from the viewer sets the shown path and raises it once.

## `public void Open_RefusesAudioWhenNotAllowed_KeyRunNeedsReturnAndAnExistingFile()`

An audio file is refused with one announcement when audio-only is not allowed.
Only the Return key commits, and only for a file that exists.
A cancelled dialog opens nothing and announces nothing.

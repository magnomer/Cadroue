# QWorkspaceFacts.cs
Hash: `4d5b26e2f3c0e7b8`

## `public sealed class QWorkspaceFacts`

Answers the fixed facts a tab key implies, so the veneer picks the right layout.
The key never changes after construction, so every fact is a plain read.

## `public bool QWorkspaceSourcePresent`

False for Merge and Worklist, which have no single source to open.

## `public bool QWorkspaceAudioOnly`

True only for the Audio tab, which accepts audio files without video.

## `public bool QWorkspaceSectionVisible`

True only for the Split tab, the one tab that edits sections.

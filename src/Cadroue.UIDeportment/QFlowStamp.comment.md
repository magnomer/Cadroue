# QFlowStamp.cs
Hash: `00628b6ac69f31a9`

## `public sealed class QFlowStamp`

Keeps the last keys that let keyframe scans and LosslessCut checks skip repeat work.
Each set reports whether the key changed, so the caller runs only on a new key.

## `public bool LFlowStampSet(string lKeyframeStamp)`

Compares keyframe stamps exactly, because a stamp is a machine key.

## `public bool LFlowLosslesscutSet(string lLosslesscutPath)`

Compares paths ignoring case, because Windows paths are case-insensitive.

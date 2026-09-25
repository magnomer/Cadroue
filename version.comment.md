# version.json

The single source of the version, read by `Directory.Build.props` at compile time.
Only `scripts/version.ps1` changes it.

## `current-version`

The version the working tree builds as, stamped into every assembly.

## `stable-version`

The last version released as stable.

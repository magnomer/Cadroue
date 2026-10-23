# PSDebug.cs

## `internal sealed class PSDebug : Window`

The catalog keeps one Debug window current, activates it on repeated requests and releases it when closed.

## `private void PSDebugBuild(QLogoMenu qLogoMenuOwner, QLogoAnswer qLogoAnswerOwner)`

About, Options and Shortcuts receive the Debug window as owner through the shared `QLogoAnswer` routes.
Log deliberately receives the captured application `PWindow` instead.
Other modal demonstrations use the Debug window, while Monitor uses its application-window owner.

## `private void PSDebugEncoderShow()`

Encoder requires a selected application tab and does nothing when none is selected.

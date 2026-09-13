# LMessenger.cs

## `private static void LMessengerDefer(Action lMessengerAction)`

Route a schedule-mutating action onto the post thread the worklist writes on.
Fall back to inline when no post owner is wired.

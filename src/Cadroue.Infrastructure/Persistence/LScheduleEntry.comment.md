# LScheduleEntry.cs

## `internal static LWorkState LScheduleStateRead(LDepotFolder lDepotFolder, LWorkState lScheduleFileState) =>`

The folder a record sits in decides its state over the state stored in its file.
A done or failed record keeps a finer terminal state stored in its file.
A scheduled record still stamped running reads as pending, since no runner holds it.

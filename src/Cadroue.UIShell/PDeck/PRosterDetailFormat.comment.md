# PRosterDetailFormat.cs

## `internal static long? PRosterSourceRead(LWorkItem pWorkItem)`

Source size is read only from the record measured while the job ran.
A merge item stores one byte total per input.
Nothing is measured from disk here, so a deleted source still shows its recorded size.

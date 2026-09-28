# LFixTab.cs

## `public static readonly IReadOnlyList<LFixRow> LFixKinds`

Presentation order is by real-world defect frequency (most common first).
So the defect a user most likely faces is nearest the top.
It deliberately differs from the actual repair order, which `LRemedy` fixes by safety and dependency.
Lossless carriage repairs come first, lossy decode-reencode last.
List position never decides repair semantics.

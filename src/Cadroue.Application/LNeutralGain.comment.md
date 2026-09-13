# LNeutralGain.cs

## `private static LNeutralSample LNeutralSampleCreate(`

Resolve the correction sample for one gray triple.
It may come from a sampled region, a colour-wheel pick, or a whole-frame analysis.
Work on the gamma-encoded channel values, the domain FFmpeg's colorchannelmixer multiplies in.
So a gain means the same thing where it is derived and where it is applied.
Two targets drive the diagonal gain of target / channel: Grey — target = the sample's own Rec.709 luma.
Every channel is driven to that single luma.
A truly neutral sample yields gains of 1, the corrected channels come out equal, and brightness is preserved.
Strict: assumes the pick is genuinely neutral grey.
White — target = the sample's brightest linear channel.
Only the deficient channels are lifted to that max (gains >= 1), and nothing is pushed down.
Lenient: the pick need only sit on the black-to-white axis, its own channel ratio naming the cast.
Brightness rises slightly.
Clamping each gain to 0..2 is the only cap on amplification.
A near-black channel would otherwise demand an unbounded multiplier and blow out highlights.
The clamp bounds that to a controlled 2x while leaving ordinary casts untouched.

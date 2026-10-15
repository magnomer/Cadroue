namespace Cadroue.UIDeportment;

[Flags]
internal enum QLookCue
{
    QLookCueBase = 0,

    QLookCueHover = 1 << 0,

    QLookCueFocus = 1 << 1,

    QLookCueChecked = 1 << 2,

    QLookCueMixed = 1 << 3,

    QLookCueDisabled = 1 << 4,

    QLookCueDrag = 1 << 5,
}

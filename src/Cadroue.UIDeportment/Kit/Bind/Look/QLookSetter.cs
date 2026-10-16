using System.Windows;

namespace Cadroue.UIDeportment;

internal sealed record QLookSetter(
    string QLookSetterStyle,
    QLookCue QLookSetterCue,
    string QLookSetterPart,
    DependencyProperty QLookSetterProperty,
    string QLookSetterKey);

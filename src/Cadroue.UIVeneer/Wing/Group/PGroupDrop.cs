using System.Windows;

namespace Cadroue.UIVeneer;

internal static class PGroupDrop
{
    internal const string PGroupMoveKind = "CadroueGroupMove";

    private static readonly IReadOnlyDictionary<bool, DragDropEffects> pGroupEffects =
        new Dictionary<bool, DragDropEffects>
        {
            [true] = DragDropEffects.Move,
            [false] = DragDropEffects.Copy,
        };

    internal static void PGroupOverHandle(object pSender, DragEventArgs pEvent)
    {
        pEvent.Effects = pGroupEffects[pEvent.Data.GetDataPresent(PGroupMoveKind)];
        pEvent.Handled = true;
    }

    internal sealed record PGroupMovePayload(int PGroupMoveIndex, string PGroupMovePath);
}

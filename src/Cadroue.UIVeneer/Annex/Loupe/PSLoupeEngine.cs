using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FlyleafLib;
using FlyleafLib.Controls.WPF;
using Cadroue.UIDeportment;

namespace Cadroue.UIVeneer;

internal sealed class PSLoupeEngine
{
    private static readonly IReadOnlyDictionary<bool, Action<PSLoupeEngine>> psLoupeEngineCreates =
        new Dictionary<bool, Action<PSLoupeEngine>>
        {
            [true] = psLoupeEngine => psLoupeEngine.PSLoupeMpvCreate(),
            [false] = psLoupeEngine => psLoupeEngine.PSLoupeFlyleafCreate(),
        };

    private readonly QSLoupe lsLoupe;
    private readonly FlyleafHost psLoupeFlyleafHost;
    private readonly PViewerMpvHost psLoupeMpvHost;

    internal PSLoupeEngine(QSLoupe lLoupe)
    {
        lsLoupe = lLoupe;
        psLoupeFlyleafHost = PSLoupeFlyleafBuild();
        psLoupeMpvHost = PSLoupeViewerBuild();
    }

    internal UIElement PSLoupeMediaBuild()
    {
        var pMediaHost = new Grid { Background = Brushes.Black, ClipToBounds = true };
        pMediaHost.Children.Add(psLoupeFlyleafHost);
        pMediaHost.Children.Add(psLoupeMpvHost);
        return pMediaHost;
    }

    internal void PSLoupeEngineCreate()
    {
        PSLoupeHostApply();
        psLoupeEngineCreates[lsLoupe.QSLoupeMpvActive](this);
    }

    internal void PSLoupeFlyleafDispose()
    {
        psLoupeFlyleafHost.Player = null;
        ((IDisposable)psLoupeFlyleafHost).Dispose();
    }

    internal void PSLoupeMpvDispose() => psLoupeMpvHost.Dispose();

    private static FlyleafHost PSLoupeFlyleafBuild() => new()
    {
        VideoBackground = Brushes.Black,
        ToggleFullScreenOnDoubleClick = AvailableWindows.None,
        AttachedDragMove = AttachedDragMoveOptions.None,
        Visibility = Visibility.Collapsed
    };

    private static PViewerMpvHost PSLoupeViewerBuild() => new() { Visibility = Visibility.Collapsed };

    private void PSLoupeHostApply()
    {
        psLoupeMpvHost.Visibility = PLook.PLookVisible[lsLoupe.QSLoupeMpvActive];
        psLoupeFlyleafHost.Visibility = PLook.PLookVisible[lsLoupe.QSLoupeFlyleafActive];
    }

    private void PSLoupeFlyleafCreate()
    {
        var pConfig = new Config();
        pConfig.Player.KeyBindings.Keys.Clear();
        lsLoupe.QPlayer.QPlayerEngineSet(
            new PPlayerFlyleaf(psLoupeFlyleafHost, lsLoupe.QPlayer, pConfig).PPlayerSeamRead());
    }

    private void PSLoupeMpvCreate() =>
        lsLoupe.QPlayer.QPlayerMpvCreate(psLoupeMpvHost.PViewerHandleRead());
}

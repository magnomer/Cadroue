using System.Windows;
using Cadroue.Core;

namespace Cadroue.UIDeportment;

// The deck's builders by tab key: each tab kind registers how its surface is built, and a tab's surface is built by
// the builder of its key.
public sealed class QDeckCatalog
{
    public delegate FrameworkElement QDeckBuilder(QWorkspace lWorkspace, LSceneTabEntry? lPreferenceTabLayout);

    private readonly Dictionary<string, QDeckBuilder> qDeckBuilders = [];

    public void QDeckBuilderAdd(string lKey, QDeckBuilder lBuilder) =>
        qDeckBuilders.Add(lKey, lBuilder);

    public FrameworkElement QDeckSurfaceBuild(QStripTab lStripTab, LSceneTabEntry? lPreferenceTabLayout)
    {
        FrameworkElement lSurface =
            qDeckBuilders[lStripTab.QStripTabKey](lStripTab.QStripTabWorkspace, lPreferenceTabLayout);
        QLook.QLookStyleAttach(lSurface);
        return lSurface;
    }
}

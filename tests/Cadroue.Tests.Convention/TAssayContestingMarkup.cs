using Xunit;

namespace Convention.Tests;

public sealed class TAssayContestingMarkup
{
    private const string TAssayMarkupPath = "src/Cadroue.UIVeneer/obj/Assay/PAssayView.g.cs";

    private const string TAssayMarkupDriver = """
        using Cadroue.Conduct.Assay;

        namespace Cadroue.UIDeportment.Assay;

        public sealed class QAssay
        {
            private int _count;

            public void QAssayRun(CAssay gate, PAssayView view)
            {
                view.PAssayViewCount = gate.CAssayRead();
                _count = gate.CAssayRead();
                _count = view.PAssayViewCount;
            }
        }

        public partial class PAssayView : System.Windows.Controls.UserControl
        {
            public int PAssayViewCount { get; set; }
        }
        """;

    private const string TAssayMarkupText = """
        namespace Cadroue.UIDeportment.Assay
        {
            public partial class PAssayView :
                System.Windows.Controls.UserControl, System.Windows.Markup.IComponentConnector
            {
                private bool _contentLoaded;

                public void InitializeComponent()
                {
                    if (_contentLoaded)
                    {
                        return;
                    }

                    _contentLoaded = true;
                    System.Uri resourceLocater =
                        new System.Uri("/Cadroue.UIVeneer;component/assay/passayview.xaml", System.UriKind.Relative);
                    System.Windows.Application.LoadComponent(this, resourceLocater);
                }

                void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target)
                {
                    this._contentLoaded = true;
                }
            }
        }
        """;

    [Fact]
    public void AuditTruth_MarkupProperty_ReportsContesting()
    {
        Assert.Equal(
            [
                new TViolation(
                    TAssayTruth.TAssayDriverPath, 13, "QAssay._count", "Contesting",
                    "written by the engine at line 12 and by the shell here")
            ],
            TAssayMarkupRun(true));
    }

    [Fact]
    public void AuditTruth_CodeProperty_AllowsEngineCopy()
    {
        Assert.Empty(TAssayMarkupRun(false));
    }

    private static List<TViolation> TAssayMarkupRun(bool markup)
    {
        Dictionary<string, string> sources = new(StringComparer.Ordinal)
        {
            [TAssayTruth.TAssayDriverPath] = TAssayMarkupDriver,
            [TAssayTruth.TAssayGatePath] = TAssayTruth.TAssayGateText,
        };
        if (markup)
        {
            sources[TAssayMarkupPath] = TAssayMarkupText;
        }

        string driverPath = Path.GetFullPath(Path.Combine(TAuditBinder.TAuditRoot, TAssayTruth.TAssayDriverPath));
        return TAuditBinder.TAuditAssayRun(sources, () => TAuditTruthWalker.TAuditRun([driverPath]))
            .Where(hit => hit.TViolationKind == "Contesting")
            .Select(hit => hit with { TViolationPath = TAuditBinder.TAuditRelativeRead(hit.TViolationPath) })
            .ToList();
    }
}

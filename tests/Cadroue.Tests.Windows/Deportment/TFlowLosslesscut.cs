using Cadroue.Core;
using Cadroue.UIDeportment;
using Cadroue.Tests.Interface;

using Xunit;

namespace Cadroue.Tests.Windows;

public sealed class TFlowLosslesscut
{
    private const string TFlowProjectText =
        "{\"version\":1,\"mediaFileName\":\"clip.mp4\","
        + "\"cutSegments\":[{\"start\":1,\"end\":5},{\"start\":10,\"end\":20}]}";

    private static QFlow TFlowBuild(string sourcePath)
    {
        QFlow flow = TInterface.TFlowCreate();
        TInterface.TFlowCommandSet(flow, true);
        TInterface.TFlowSectionSet(flow, true);
        TInterface.TFlowSourceSet(flow, TInterfaceViewer.TViewerInfoCreate(
            TimeSpan.FromSeconds(60), 640, 360), sourcePath);
        return flow;
    }

    private static List<QFlowLosslesscutKind> TFlowPromptsAttach(QFlow flow, Func<QFlowLosslesscutKind, int> answer)
    {
        List<QFlowLosslesscutKind> kinds = [];
        TInterface.TFlowLosslesscutAttach(flow, (prompt, choose) =>
        {
            kinds.Add(prompt.QFlowPromptKind);
            choose(answer(prompt.QFlowPromptKind));
        });
        return kinds;
    }

    [Fact]
    public void Run_ReplacesTheSections_WhenPrimaryIsChosen()
    {
        using var fixture = new TLosslesscut();
        string source = fixture.TSourceCreate("clip.mp4");
        string project = fixture.TLosslesscutAdjacentCreate("clip.llc", TFlowProjectText);
        QFlow flow = TFlowBuild(source);
        TInterface.TFlowSectionAdd(flow);
        List<QFlowLosslesscutKind> kinds = TFlowPromptsAttach(flow, _ => 0);

        TInterface.TFlowLosslesscutRun(flow, project);

        Assert.Equal([QFlowLosslesscutKind.QFlowKindChoice], kinds);
        IReadOnlyList<LPiece> sections = TInterface.TFlowSectionsRead(flow);
        Assert.Equal(2, sections.Count);
        Assert.Equal(TimeSpan.FromSeconds(1), sections[0].LPieceOrigin);
        Assert.Equal(TimeSpan.FromSeconds(20), sections[1].LPieceEnd);
    }

    [Fact]
    public void Run_AppendsOrCancels_ByTheChoice()
    {
        using var fixture = new TLosslesscut();
        string source = fixture.TSourceCreate("clip.mp4");
        string project = fixture.TLosslesscutAdjacentCreate("clip.llc", TFlowProjectText);
        QFlow flow = TFlowBuild(source);
        TInterface.TFlowCursorSet(flow, TimeSpan.FromSeconds(30));
        TInterface.TFlowSectionAdd(flow);

        int answer = -1;
        TFlowPromptsAttach(flow, _ => answer);
        TInterface.TFlowLosslesscutRun(flow, project);
        Assert.Single(TInterface.TFlowSectionsRead(flow));

        answer = 1;
        TInterface.TFlowLosslesscutRun(flow, project);
        Assert.Equal(3, TInterface.TFlowSectionsRead(flow).Count);
    }

    [Fact]
    public void Run_AsksOnMediaMismatch_AndWarnsOnUnreadable()
    {
        using var fixture = new TLosslesscut();
        string source = fixture.TSourceCreate("other.mp4");
        string project = fixture.TLosslesscutAdjacentCreate("other.llc", TFlowProjectText);
        string broken = fixture.TLosslesscutAdjacentCreate("broken.llc", "not json");
        QFlow flow = TFlowBuild(source);
        List<QFlowLosslesscutKind> kinds = TFlowPromptsAttach(
            flow, kind => kind == QFlowLosslesscutKind.QFlowKindDecision ? -1 : 0);

        TInterface.TFlowLosslesscutRun(flow, project);
        Assert.Equal([QFlowLosslesscutKind.QFlowKindDecision], kinds);
        Assert.Empty(TInterface.TFlowSectionsRead(flow));

        kinds.Clear();
        TInterface.TFlowLosslesscutRun(flow, broken);
        Assert.Equal([QFlowLosslesscutKind.QFlowKindWarning], kinds);
    }

    [Fact]
    public void Run_NoticesWhenNoMediaIsLoaded()
    {
        QFlow flow = TInterface.TFlowCreate();
        List<QFlowLosslesscutKind> kinds = TFlowPromptsAttach(flow, _ => 0);
        TInterface.TFlowLosslesscutRun(flow, "C:\\nowhere.llc");
        Assert.Equal([QFlowLosslesscutKind.QFlowKindNotice], kinds);
    }

    [Fact]
    public void Find_OffersEachSibling_AndImportsTheAcceptedOne()
    {
        using var fixture = new TLosslesscut();
        string source = fixture.TSourceCreate("clip.mp4");
        fixture.TLosslesscutAdjacentCreate("clip.llc", TFlowProjectText);
        fixture.TLosslesscutAdjacentCreate("clip-2.llc", TFlowProjectText);
        QFlow flow = TFlowBuild(source);
        int confirms = 0;
        List<QFlowLosslesscutKind> kinds = TFlowPromptsAttach(flow, kind =>
            kind == QFlowLosslesscutKind.QFlowKindDecision ? (++confirms == 2 ? 0 : -1) : 0);

        TInterface.TFlowLosslesscutFind(flow);

        Assert.Equal(
            [
                QFlowLosslesscutKind.QFlowKindDecision,
                QFlowLosslesscutKind.QFlowKindDecision,
                QFlowLosslesscutKind.QFlowKindChoice
            ],
            kinds);
        Assert.Equal(2, TInterface.TFlowSectionsRead(flow).Count);
    }

    [Fact]
    public void Find_DoesNothingWhileNotEditable()
    {
        using var fixture = new TLosslesscut();
        string source = fixture.TSourceCreate("clip.mp4");
        fixture.TLosslesscutAdjacentCreate("clip.llc", TFlowProjectText);
        QFlow flow = TFlowBuild(source);
        TInterface.TFlowEditSet(flow, false);
        List<QFlowLosslesscutKind> kinds = TFlowPromptsAttach(flow, _ => 0);
        TInterface.TFlowLosslesscutFind(flow);
        Assert.Empty(kinds);
    }
}

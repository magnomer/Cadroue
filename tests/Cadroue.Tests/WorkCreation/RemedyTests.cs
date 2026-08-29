using Cadroue.Application;
using Cadroue.Core;

using Xunit;

namespace Cadroue.Tests;

public sealed class RemedyTests
{
    [Fact]
    public void NoDefect_ProducesCopyOnlyPlan()
    {
        LRemedyPlan plan = TInterface.RemedyPlanCreate(Array.Empty<LDossier>());

        Assert.Equal(LRemedyOutcome.LRemedyOutcomeClean, plan.LRemedyOutcome);
        Assert.Empty(plan.LRemedyActions);
    }

    [Fact]
    public void CleanDiagnoses_AreExcludedAsCopyOnly()
    {
        LRemedyPlan plan = TInterface.RemedyPlanCreate(new[]
        {
            TInterface.DossierDefectCreate(string.Empty, LDossierCategory.LDossierCategoryContainer)
        });

        Assert.Equal(LRemedyOutcome.LRemedyOutcomeClean, plan.LRemedyOutcome);
        Assert.Empty(plan.LRemedyActions);
    }

    [Fact]
    public void TwoDefects_ComposeInPrecedenceOrder_IndependentOfInputOrder()
    {
        LDossier reencode = TInterface.DossierDefectCreate("bitstream", LDossierCategory.LDossierCategoryReencode);
        LDossier container = TInterface.DossierDefectCreate("moov", LDossierCategory.LDossierCategoryContainer);

        LRemedyPlan forward = TInterface.RemedyPlanCreate(new[] { container, reencode });
        LRemedyPlan reversed = TInterface.RemedyPlanCreate(new[] { reencode, container });

        foreach (LRemedyPlan plan in new[] { forward, reversed })
        {
            Assert.Equal(LRemedyOutcome.LRemedyOutcomeCompose, plan.LRemedyOutcome);
            Assert.Collection(
                plan.LRemedyActions,
                first => Assert.Equal(LDossierCategory.LDossierCategoryContainer, first.LRemedyCategory),
                second => Assert.Equal(LDossierCategory.LDossierCategoryReencode, second.LRemedyCategory));
        }
    }

    [Fact]
    public void SecondaryComposes_AfterTimeline_BeforeReencode()
    {
        LDossier reencode = TInterface.DossierDefectCreate("bitstream", LDossierCategory.LDossierCategoryReencode);
        LDossier secondary = TInterface.DossierDefectCreate("subtitle", LDossierCategory.LDossierCategorySecondary);
        LDossier timeline = TInterface.DossierDefectCreate("timing", LDossierCategory.LDossierCategoryTimeline);

        LRemedyPlan plan = TInterface.RemedyPlanCreate(new[] { reencode, secondary, timeline });

        Assert.Collection(
            plan.LRemedyActions,
            first => Assert.Equal(LDossierCategory.LDossierCategoryTimeline, first.LRemedyCategory),
            second => Assert.Equal(LDossierCategory.LDossierCategorySecondary, second.LRemedyCategory),
            third => Assert.Equal(LDossierCategory.LDossierCategoryReencode, third.LRemedyCategory));
    }

    [Fact]
    public void SameCategory_OrdersByPreservationThenDefect()
    {
        LDossier lossy = TInterface.DossierDefectCreate(
            "b", LDossierCategory.LDossierCategoryPacket, LDossierPreservation.LDossierPreservationLossy);
        LDossier lossless = TInterface.DossierDefectCreate(
            "a", LDossierCategory.LDossierCategoryPacket, LDossierPreservation.LDossierPreservationExact);

        LRemedyPlan plan = TInterface.RemedyPlanCreate(new[] { lossy, lossless });

        Assert.Collection(
            plan.LRemedyActions,
            first => Assert.Equal("a", first.LRemedyDossier.LDossierDefect),
            second => Assert.Equal("b", second.LRemedyDossier.LDossierDefect));
    }
}

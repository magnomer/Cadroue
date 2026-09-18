using Cadroue.Application;
using Cadroue.Core;
using Cadroue.UIDeportment;

using Xunit;

namespace Cadroue.Tests;

public sealed class TClinicResult
{
    private const string TClinicResultPath = @"C:\Media\Clip.mp4";
    private const string TClinicResultRecased = @"c:\media\clip.MP4";

    [Fact]
    public void ResultAndProgress_ReadBack_UnderRecasedPath()
    {
        LClinic clinic = TInterface.TClinicCreate();
        int changes = 0;
        TInterface.TClinicChangeAttach(clinic, () => changes++);
        TInterface.TClinicSourceSet(clinic, TClinicResultPath);
        TInterface.TClinicStepSet(clinic, "Container");
        changes = 0;

        TInterface.TClinicResultSet(clinic, TClinicResultRecased, LFlawKind.LFlawKindContainer, LCheckupOutcome.LCheckupOutcomeScanning);
        TInterface.TClinicProgressSet(clinic, TClinicResultRecased, 0.5);

        Assert.Equal(LCheckupOutcome.LCheckupOutcomeScanning, TInterface.TClinicOutcomeRead(clinic));
        Assert.Equal(0.5, TInterface.TClinicProgressRead(clinic));
        Assert.Equal(2, changes);
    }

    [Fact]
    public void ResultsRemove_DropsResultAndProgress_NotifiesShownSource()
    {
        LClinic clinic = TInterface.TClinicCreate();
        TInterface.TClinicSourceSet(clinic, TClinicResultPath);
        TInterface.TClinicStepSet(clinic, "Container");
        TInterface.TClinicResultSet(clinic, TClinicResultPath, LFlawKind.LFlawKindContainer, LCheckupOutcome.LCheckupOutcomeScanning);
        TInterface.TClinicProgressSet(clinic, TClinicResultPath, 0.5);
        int changes = 0;
        TInterface.TClinicChangeAttach(clinic, () => changes++);

        TInterface.TClinicResultsRemove(clinic, TClinicResultRecased);

        Assert.Equal(LCheckupOutcome.LCheckupOutcomeUntested, TInterface.TClinicOutcomeRead(clinic));
        Assert.Equal(0, TInterface.TClinicProgressRead(clinic));
        Assert.Equal(1, changes);

        TInterface.TClinicResultsRemove(clinic, TClinicResultPath);

        Assert.Equal(1, changes);
    }

    [Fact]
    public void ResultsRemove_OtherPath_KeepsShownSourceQuiet()
    {
        LClinic clinic = TInterface.TClinicCreate();
        TInterface.TClinicSourceSet(clinic, TClinicResultPath);
        TInterface.TClinicStepSet(clinic, "Container");
        TInterface.TClinicResultSet(clinic, TClinicResultPath, LFlawKind.LFlawKindContainer, LCheckupOutcome.LCheckupOutcomeClean);
        int changes = 0;
        TInterface.TClinicChangeAttach(clinic, () => changes++);

        TInterface.TClinicResultsRemove(clinic, @"C:\Media\Other.mp4");

        Assert.Equal(LCheckupOutcome.LCheckupOutcomeClean, TInterface.TClinicOutcomeRead(clinic));
        Assert.Equal(0, changes);
    }
}

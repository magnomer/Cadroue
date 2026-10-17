using Cadroue.Application;
using Cadroue.Core;

namespace Cadroue.Infrastructure;

public static class LCheckupCache
{
    public static IReadOnlyList<LDossier>? LCheckupCachedRead(string lCheckupPath)
    {
        IReadOnlyList<LSidecarDossier>? lCheckupStored = LLibrarian.LLibrarianDiagnosisLoad(lCheckupPath);
        if (lCheckupStored is null)
        {
            return null;
        }

        var lCheckupDossiers = new List<LDossier>(lCheckupStored.Count);
        foreach (LSidecarDossier lCheckupDto in lCheckupStored)
        {
            lCheckupDossiers.Add(LCheckupDossier.LCheckupDossierResolve(lCheckupDto));
        }

        return lCheckupDossiers;
    }

    public static async Task LCheckupCachedSave(string lCheckupPath, IReadOnlyList<LDossier> lCheckupDossiers)
    {
        TimeSpan lCheckupDuration = await LLibrarian.LLibrarianDurationResolve(lCheckupPath).ConfigureAwait(false);
        LKeyframeSourceIdentity lCheckupIdentity;
        try
        {
            lCheckupIdentity = LKeyframeSourceIdentity.LKeyframeIdentityCreate(lCheckupPath, lCheckupDuration);
        }
        catch (Exception lCheckupException) when (
            lCheckupException is ArgumentException
                or FileNotFoundException
                or IOException
                or UnauthorizedAccessException)
        {
            return;
        }

        var lCheckupDtos = new List<LSidecarDossier>(lCheckupDossiers.Count);
        foreach (LDossier lCheckupDossier in lCheckupDossiers)
        {
            lCheckupDtos.Add(LCheckupDossier.LCheckupDossierResolve(lCheckupDossier));
        }

        LLibrarian.LLibrarianDiagnosisSave(lCheckupPath, lCheckupIdentity, lCheckupDtos);
    }
}

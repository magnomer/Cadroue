using Cadroue.Application;

namespace Cadroue.UIVeneer;

internal static class PProgramResource
{
    internal static string[] PProgramNamesRead() => typeof(PProgramResource).Assembly.GetManifestResourceNames();

    internal static string? PProgramResourceRead(string pName) =>
        LLocalizationResource.LLocalizationStreamRead(
            typeof(PProgramResource).Assembly.GetManifestResourceStream(pName));
}

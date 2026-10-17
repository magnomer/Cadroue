namespace Cadroue.Infrastructure;

public static class LRendererError
{
    public static Func<string, string> LRendererTextSeam { get; set; } = lRendererKey => lRendererKey;

    public static string LRendererErrorResolve(string? lError) =>
        lError ?? LRendererTextSeam("Viewer.Error.FlyleafOpen");
}

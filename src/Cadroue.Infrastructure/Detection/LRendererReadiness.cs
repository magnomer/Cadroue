namespace Cadroue.Infrastructure;

public sealed record LRendererReadiness(
    bool LRendererReadinessProgram,
    bool LRendererReadinessLibrary)
{
    public static LRendererReadiness LRendererReadinessRead(string path) =>
        LRendererReadinessRead(
            path,
            LRendererLibrary.LRendererProgramExist,
            LRendererLibrary.LRendererFolderValidate);

    public static LRendererReadiness LRendererReadinessRead(
        string path,
        Func<string, bool> program,
        Func<string, bool> library)
    {
        bool programReady = program(path);
        bool libraryReady = library(path);
        return new LRendererReadiness(programReady, libraryReady);
    }
}

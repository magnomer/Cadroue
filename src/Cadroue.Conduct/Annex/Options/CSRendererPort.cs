namespace Cadroue.Conduct;

public sealed record CSRendererFact(
    bool CSRendererFactProgram,
    bool CSRendererFactLibrary);

public interface CSRendererPort
{
    CSRendererFact CSRendererPortRead(string path);
}

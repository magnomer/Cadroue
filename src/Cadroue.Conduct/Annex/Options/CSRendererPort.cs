namespace Cadroue.Conduct;

public interface CSRendererPort
{
    bool CSRendererProgramCheck(string path);

    bool CSRendererLibraryCheck(string path);
}

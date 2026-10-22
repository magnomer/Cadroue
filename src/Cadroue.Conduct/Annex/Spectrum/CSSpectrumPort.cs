namespace Cadroue.Conduct;

public interface CSSpectrumPort
{
    CSSpectrumImportResult CSSpectrumPortImport(
        string sourcePath, out string targetPath, out string? loadedName);
    CSSpectrumSnapshot CSSpectrumPortRead();
    bool CSSpectrumPortRemove(string name, bool native, string? path);
    bool CSSpectrumPortSave(string targetPath, string name);
}

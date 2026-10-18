using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

public sealed class QLogoMenu
{
    private readonly CUsher qLogoMenuUsher;

    public QLogoMenu(CUsher qUsher)
    {
        qLogoMenuUsher = qUsher;
    }

    public QLog QLogoLogCreate() => new(qLogoMenuUsher);

    public QSAbout QLogoAboutCreate() => new(qLogoMenuUsher);

    public QSOptions QLogoOptionsCreate() => new(qLogoMenuUsher);

    public QSKeymap QLogoKeymapCreate() => new();

    public QSVerdict QLogoVerdictCreate() => new("Debug verdict", new[]
    {
        new QSVerdictRow("Video", "debug-pass", true, "Simulated successful verification."),
        new QSVerdictRow("Audio", "debug-fail", false, "Simulated verification detail.")
    });

    public QSMonitor QLogoMonitorCreate() => new();
}

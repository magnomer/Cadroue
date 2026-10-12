using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

public sealed class QLogoMenu
{
    private readonly CUsher qLogoMenuUsher;

    public QLogoMenu(CUsher qUsher)
    {
        qLogoMenuUsher = qUsher;
    }

    public LLog QLogoLogCreate() => new(qLogoMenuUsher);

    public LSAbout QLogoAboutCreate() => new(qLogoMenuUsher);

    public LSOptions QLogoOptionsCreate() => new(qLogoMenuUsher);

    public LSKeymap QLogoKeymapCreate() => new();

    public LSVerdict QLogoVerdictCreate() => new("Debug verdict", new[]
    {
        new LSVerdictRow("Video", "debug-pass", true, "Simulated successful verification."),
        new LSVerdictRow("Audio", "debug-fail", false, "Simulated verification detail.")
    });

    public LSMonitor QLogoMonitorCreate() => new();
}

using System.Windows;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

public sealed class QLogoMenu
{
    private readonly CUsher qLogoMenuUsher;
    private readonly Func<CSAbout> qLogoMenuAbout;

    public QLogoMenu(CUsher qUsher, Func<CSAbout> qAbout)
    {
        qLogoMenuUsher = qUsher;
        qLogoMenuAbout = qAbout;
    }

    public QLog QLogoLogCreate() => new(qLogoMenuUsher);

    public QSAbout QLogoAboutCreate(
        Func<Window> surface,
        Func<Window, string?, FrameworkElement, UIElement> shell,
        Func<Window, (Action qsAboutGrabberAttachment, Action qsAboutGrabberDetachment)> grabber,
        Action<Window?, string, string> warning,
        Action<Window, string> restore,
        Action<Window, string> save) =>
        new(qLogoMenuAbout(), surface, shell, grabber, warning, restore, save);

    public QSOptions QLogoOptionsCreate() => new(qLogoMenuUsher);

    public QSKeymap QLogoKeymapCreate() => new();

    public QSVerdict QLogoVerdictCreate() => new("Debug verdict", new[]
    {
        new QSVerdictRow("Video", "debug-pass", true, "Simulated successful verification."),
        new QSVerdictRow("Audio", "debug-fail", false, "Simulated verification detail.")
    });

    public QSMonitor QLogoMonitorCreate() => new();
}

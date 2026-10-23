using System.Windows;

namespace Cadroue.UIDeportment;

public sealed class QLogoAnswer
{
    public QLogoAnswer(
        Action<Window> about,
        Action<Window, Action?> options,
        Action<Window, Action?> shortcut,
        Action<Window> log,
        Action<Window, QLogoAnswer> debug)
    {
        QLogoAnswerAbout = about;
        QLogoAnswerOptions = options;
        QLogoAnswerShortcut = shortcut;
        QLogoAnswerLog = log;
        QLogoAnswerDebug = debug;
    }

    public Action<Window> QLogoAnswerAbout { get; }

    public Action<Window, Action?> QLogoAnswerOptions { get; }

    public Action<Window, Action?> QLogoAnswerShortcut { get; }

    public Action<Window> QLogoAnswerLog { get; }

    public Action<Window, QLogoAnswer> QLogoAnswerDebug { get; }
}

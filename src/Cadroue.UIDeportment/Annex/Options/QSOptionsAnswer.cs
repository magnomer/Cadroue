using System.Windows;
using System.Windows.Controls;

namespace Cadroue.UIDeportment;

public sealed class QSOptionsAnswer
{
    private readonly Delegate[] qsOptionsAnswer;

    public QSOptionsAnswer(
        Func<Window> surface,
        Func<Window, double, UIElement, UIElement> shell,
        Action<TabControl, double> look,
        Func<string, string?, UIElement> header,
        Func<Window, (Action, Action)> grabber,
        (Action<Window, string>, Action<Window, string>) placement,
        (Action<Window, string, string>, Action<Window, string, string>) notice,
        Func<Window, string, string, string, bool> confirmation,
        Action<Window, Func<QSDiagnosis>> diagnosis,
        QSOptionsMode mode,
        Func<Border, string> choice)
    {
        qsOptionsAnswer =
        [
            surface, shell, look, header, grabber, placement.Item1, placement.Item2, notice.Item1, notice.Item2,
            confirmation, diagnosis, mode, choice
        ];
    }

    public Window QSOptionsSurfaceBuild() => ((Func<Window>)qsOptionsAnswer[0])();

    public UIElement QSOptionsShellBuild(Window window, double strip, UIElement body) =>
        ((Func<Window, double, UIElement, UIElement>)qsOptionsAnswer[1])(window, strip, body);

    public void QSOptionsDeckApply(TabControl deck, double tab) =>
        ((Action<TabControl, double>)qsOptionsAnswer[2])(deck, tab);

    public UIElement QSOptionsHeaderBuild(string text, string? icon) =>
        ((Func<string, string?, UIElement>)qsOptionsAnswer[3])(text, icon);

    public (Action, Action) QSOptionsGrabberBuild(Window window) =>
        ((Func<Window, (Action, Action)>)qsOptionsAnswer[4])(window);

    public void QSOptionsPlacementRestore(Window window, string key) =>
        ((Action<Window, string>)qsOptionsAnswer[5])(window, key);

    public void QSOptionsPlacementSave(Window window, string key) =>
        ((Action<Window, string>)qsOptionsAnswer[6])(window, key);

    public void QSOptionsWarningShow(Window owner, string title, string message) =>
        ((Action<Window, string, string>)qsOptionsAnswer[7])(owner, title, message);

    public void QSOptionsAnnouncementShow(Window owner, string title, string message) =>
        ((Action<Window, string, string>)qsOptionsAnswer[8])(owner, title, message);

    public bool QSOptionsConfirmationShow(Window owner, string title, string question, string acceptance) =>
        ((Func<Window, string, string, string, bool>)qsOptionsAnswer[9])(owner, title, question, acceptance);

    public void QSOptionsDiagnosisShow(Window owner, Func<QSDiagnosis> diagnosis) =>
        ((Action<Window, Func<QSDiagnosis>>)qsOptionsAnswer[10])(owner, diagnosis);

    public QSOptionsMode QSOptionsModeRead() => (QSOptionsMode)qsOptionsAnswer[11];

    public string QSOptionsChoiceRead(Border mode) => ((Func<Border, string>)qsOptionsAnswer[12])(mode);
}

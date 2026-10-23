using System.Windows.Threading;
using Cadroue.Application;
using Cadroue.Infrastructure;

namespace Cadroue.UIDeportment;

public sealed class QFixFace
{
    private readonly QFixTab qFixFaceOwner;
    private Action qFixCheckupSubscription = static () => { };
    private Action? qFixMissingNotice;

    public QFixFace(QFixTab lOwner) => qFixFaceOwner = lOwner;

    public void QFixFaceAttach(LCheckup lCheckup, Dispatcher lDispatcher, Action lMissingNotice)
    {
        QFixFaceDetach();
        Action<Action> lLifetime = static lAction => lAction();
        Func<Action<Action>> lLifetimeRead = () => lLifetime;
        qFixMissingNotice = lMissingNotice;
        Action<LCheckupResult> lCheckupDefer = lResult =>
            QFixCheckupDefer(lDispatcher, lLifetimeRead, lResult);
        Action<string, double> lProgressDefer = (lPath, lProgress) =>
            QFixProgressDefer(lDispatcher, lLifetimeRead, lPath, lProgress);
        lCheckup.LCheckupReady += lCheckupDefer;
        lCheckup.LCheckupProgress += lProgressDefer;
        qFixCheckupSubscription = () =>
        {
            lLifetime = static _ => { };
            lCheckup.LCheckupReady -= lCheckupDefer;
            lCheckup.LCheckupProgress -= lProgressDefer;
        };
        qFixFaceOwner.QFixPresetMissing += lMissingNotice;
    }

    public void QFixFaceDetach()
    {
        qFixCheckupSubscription();
        if (qFixMissingNotice is { } lMissingNotice)
        {
            qFixFaceOwner.QFixPresetMissing -= lMissingNotice;
        }

        qFixMissingNotice = null;
    }

    private void QFixCheckupDefer(
        Dispatcher lDispatcher, Func<Action<Action>> lLifetimeRead, LCheckupResult lResult) =>
        lDispatcher.BeginInvoke(() =>
            lLifetimeRead()(() => qFixFaceOwner.QFixCheckupHandle(lResult)));

    private void QFixProgressDefer(
        Dispatcher lDispatcher,
        Func<Action<Action>> lLifetimeRead,
        string lPath,
        double lProgress) =>
        lDispatcher.BeginInvoke(() =>
            lLifetimeRead()(() => qFixFaceOwner.QFixProgressHandle(lPath, lProgress)));
}

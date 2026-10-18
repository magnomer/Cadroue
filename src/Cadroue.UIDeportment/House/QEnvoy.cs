using Cadroue.Application;
using Cadroue.Conduct;

namespace Cadroue.UIDeportment;

// The window's one ask port. The window hands its dialogs in; until then every ask answers false
// and every show is dropped, as the static notices do while no window listens.
public sealed class QEnvoy : CEnvoy
{
    private Action<QAsk, Action<bool>> qEnvoyAskSeam = static (_, lAnswer) => lAnswer(false);
    private Action<string, string> qEnvoyWarningSeam = static (_, _) => { };
    private Action<string, string> qEnvoyFailureSeam = static (_, _) => { };

    public void QEnvoyAttach(
        Action<QAsk, Action<bool>> lAsk,
        Action<string, string> lWarning,
        Action<string, string> lFailure)
    {
        ArgumentNullException.ThrowIfNull(lAsk);
        ArgumentNullException.ThrowIfNull(lWarning);
        ArgumentNullException.ThrowIfNull(lFailure);

        qEnvoyAskSeam = lAsk;
        qEnvoyWarningSeam = lWarning;
        qEnvoyFailureSeam = lFailure;
    }

    internal static bool QEnvoyDestructiveCheck() =>
        LPreference.LPreferenceStateCurrent.LPreferenceConfirmDestructive;

    public bool CEnvoyConfirm(string questionKey, string actionKey, string titleKey) =>
        QEnvoyConfirmShow(
            QEnvoyAskCreate(QLocalizationCatalog.QLocalizationTextRead(questionKey), actionKey, titleKey));

    public bool CEnvoyConfirm(string questionKey, string actionKey, string titleKey, object? argument) =>
        QEnvoyConfirmShow(
            QEnvoyAskCreate(QLocalizationCatalog.QLocalizationFormat(questionKey, argument), actionKey, titleKey));

    public bool CEnvoyConfirm(
        string questionKey, string subjectKey, object? subject, string actionKey, string titleKey, string dismissKey) =>
        QEnvoyConfirmShow(
            QEnvoyAskCreate(
                QLocalizationCatalog.QLocalizationFormat(
                    questionKey, QLocalizationCatalog.QLocalizationFormat(subjectKey, subject)),
                actionKey,
                titleKey) with
            {
                QAskDismiss = QLocalizationCatalog.QLocalizationTextRead(dismissKey)
            });

    public bool CEnvoyDestructiveConfirm(string questionKey, string actionKey, string titleKey) =>
        !QEnvoyDestructiveCheck() || CEnvoyConfirm(questionKey, actionKey, titleKey);

    public bool CEnvoyDestructiveConfirm(string questionKey, string actionKey, string titleKey, object? argument) =>
        !QEnvoyDestructiveCheck() || CEnvoyConfirm(questionKey, actionKey, titleKey, argument);

    public void CEnvoyWarningShow(string titleKey, string messageKey) =>
        qEnvoyWarningSeam(
            QLocalizationCatalog.QLocalizationTextRead(titleKey),
            QLocalizationCatalog.QLocalizationTextRead(messageKey));

    public void CEnvoyFailureShow(string titleKey, string messageKey) =>
        qEnvoyFailureSeam(
            QLocalizationCatalog.QLocalizationTextRead(titleKey),
            QLocalizationCatalog.QLocalizationTextRead(messageKey));

    private static QAsk QEnvoyAskCreate(string lQuestion, string lActionKey, string lTitleKey) =>
        new(lQuestion, QLocalizationCatalog.QLocalizationTextRead(lActionKey))
        {
            QAskTitle = QLocalizationCatalog.QLocalizationTextRead(lTitleKey)
        };

    private bool QEnvoyConfirmShow(QAsk lAsk)
    {
        bool lConfirmed = false;
        qEnvoyAskSeam(lAsk, lAnswer => lConfirmed = lAnswer);
        return lConfirmed;
    }
}

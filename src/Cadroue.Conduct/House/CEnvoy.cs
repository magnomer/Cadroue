namespace Cadroue.Conduct;

// The ask port: a gate receives it as a method argument and asks only through it, inside the gate.
// Every text arrives as a localization key; the window side localizes and shows.
public interface CEnvoy
{
    // Asked whatever the destructive-confirm preference says; false while no window answers.
    bool CEnvoyConfirm(string questionKey, string actionKey, string titleKey);

    bool CEnvoyConfirm(string questionKey, string actionKey, string titleKey, object? argument);

    // The question formats one subject, itself formatted from subjectKey; dismissKey names the refusal.
    bool CEnvoyConfirm(
        string questionKey, string subjectKey, object? subject, string actionKey, string titleKey, string dismissKey);

    // Asked only while the user keeps destructive confirmation on; answered true without asking otherwise.
    bool CEnvoyDestructiveConfirm(string questionKey, string actionKey, string titleKey);

    bool CEnvoyDestructiveConfirm(string questionKey, string actionKey, string titleKey, object? argument);

    void CEnvoyWarningShow(string titleKey, string messageKey);

    void CEnvoyFailureShow(string titleKey, string messageKey);
}

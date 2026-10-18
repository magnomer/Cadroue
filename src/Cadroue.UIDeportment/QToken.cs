
namespace Cadroue.UIDeportment;

// A token dropped on the template box goes to the box's owner as an insert.
public sealed class QToken
{
    public event Action<string>? QTokenInserted;

    public bool QTokenDropHandle(string? lToken)
    {
        if (lToken is null)
        {
            return false;
        }

        QTokenInserted?.Invoke(lToken);
        return true;
    }
}

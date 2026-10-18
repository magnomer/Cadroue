using System.Collections.Generic;

namespace Cadroue.Conduct;

// One text the window words: a wording key and its arguments, each a number, a ready string or another text.
// Without arguments (null) the key is read as plain text; with them it is formatted.
public sealed record CConsoleText(string CConsoleTextKey, IReadOnlyList<object>? CConsoleTextArguments)
{
    // A key given no arguments is a plain text.
    internal static CConsoleText CConsoleTextCreate(string key, params object[] arguments) =>
        new(key, arguments.Length == 0 ? null : arguments);
}

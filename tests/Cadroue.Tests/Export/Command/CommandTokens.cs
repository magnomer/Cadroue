namespace Cadroue.Tests;

internal static class CommandTokens
{
    internal static IReadOnlyList<string> Read(string command)
    {
        var tokens = new List<string>();
        var token = new System.Text.StringBuilder();
        bool quoted = false;

        foreach (char character in command)
        {
            if (character == '"')
            {
                quoted = !quoted;
                continue;
            }

            if (char.IsWhiteSpace(character) && !quoted)
            {
                if (token.Length > 0)
                {
                    tokens.Add(token.ToString());
                    token.Clear();
                }

                continue;
            }

            token.Append(character);
        }

        if (token.Length > 0)
        {
            tokens.Add(token.ToString());
        }

        return tokens;
    }

    internal static string ValueAfter(IReadOnlyList<string> tokens, string option)
    {
        int index = tokens.IndexOf(option);
        return index >= 0 && index + 1 < tokens.Count ? tokens[index + 1] : string.Empty;
    }

    internal static int Count(IReadOnlyList<string> tokens, string value) =>
        tokens.Count(token => string.Equals(token, value, StringComparison.Ordinal));

    private static int IndexOf(this IReadOnlyList<string> tokens, string value)
    {
        for (int index = 0; index < tokens.Count; index++)
        {
            if (string.Equals(tokens[index], value, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return -1;
    }
}

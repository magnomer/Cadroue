namespace Cadroue.Tests;

internal static class TEncodeToken
{
    internal static IReadOnlyList<string> TEncodeTokenRead(string command)
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

    internal static string TEncodeOptionRead(IReadOnlyList<string> tokens, string option)
    {
        int index = tokens.TEncodeIndexFind(option);
        return index >= 0 && index + 1 < tokens.Count ? tokens[index + 1] : string.Empty;
    }

    internal static int TEncodeCountRead(IReadOnlyList<string> tokens, string value) =>
        tokens.Count(token => string.Equals(token, value, StringComparison.Ordinal));

    private static int TEncodeIndexFind(this IReadOnlyList<string> tokens, string value)
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

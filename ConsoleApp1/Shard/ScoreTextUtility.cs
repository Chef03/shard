using System;
using System.Text;

namespace Shard;

internal static class ScoreTextUtility
{
    public static string NormalizeRequiredValue(string value, string paramName)
    {
        var cleaned = CleanSingleLineValue(value);
        if (cleaned.Length == 0)
        {
            throw new ArgumentException("Value cannot be empty.", paramName);
        }

        return cleaned;
    }
    

    public static string CleanSingleLineValue(string value)
    {
        if (value == null)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        var lastWasWhitespace = false;

        foreach (var character in value.Trim())
        {
            if (char.IsControl(character))
            {
                if (!lastWasWhitespace)
                {
                    builder.Append(' ');
                    lastWasWhitespace = true;
                }

                continue;
            }

            if (char.IsWhiteSpace(character))
            {
                if (!lastWasWhitespace)
                {
                    builder.Append(' ');
                    lastWasWhitespace = true;
                }

                continue;
            }

            builder.Append(character);
            lastWasWhitespace = false;
        }

        return builder.ToString().Trim();
    }
}

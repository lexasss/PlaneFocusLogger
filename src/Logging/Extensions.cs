using System;

namespace EMirrorsScores.Logging;

internal static class String
{
    /// <summary>
    /// Replaces characters forbidden in a path with a valid character
    /// </summary>
    /// <param name="s"></param>
    /// <param name="replacement"></param>
    /// <returns></returns>
    public static string ToPath(this string s, string replacement = "-")
    {
        var invalidChars = System.IO.Path.GetInvalidFileNameChars();
        string[] temp = s.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(replacement, temp);
    }
}

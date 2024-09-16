using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Jellyfin.Plugin.AutoOrganiser.Core;

/// <summary>
/// Provides extension methods for various classes.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Split and trim arguments given in a comma-delimited string.
    /// </summary>
    /// <param name="arg">The string representation of the argument list.</param>
    /// <returns>The separate arguments.</returns>
    public static IEnumerable<string> SplitArguments(this string arg) => arg
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
        .Select(x => x.TrimStart('.').ToLowerInvariant());

    /// <summary>
    /// Converts the given string to lower sentence case e.g. 'ThisIsASentence' -> 'this is a sentence'.
    /// </summary>
    /// <param name="value">The string to convert.</param>
    /// <returns>The formatted string.</returns>
    public static string ToLowerSentenceCase(this string value) =>
        string.Concat(value.Select(UpperCharToSpacedLower)).ToLowerInvariant();

    private static string UpperCharToSpacedLower(char c, int index) => index > 0 && char
        .IsUpper(c) ? " " + c.ToString().ToLowerInvariant() : c.ToString();

    /// <summary>
    /// Select many operation for enumerated async tasks.
    /// </summary>
    /// <param name="enumeration">The enumeration to operation on.</param>
    /// <param name="func">The function to run.</param>
    /// <typeparam name="TIn">The input task to await.</typeparam>
    /// <typeparam name="TOut">The output of the task.</typeparam>
    /// <returns>A single task that returns all the results as an enumerable.</returns>
    public static async Task<IEnumerable<TOut>> SelectManyAsync<TIn, TOut>(
        this IEnumerable<TIn> enumeration, Func<TIn, Task<IEnumerable<TOut>>> func)
    {
        return (await Task.WhenAll(enumeration.Select(func)).ConfigureAwait(false)).SelectMany(s => s);
    }
}
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.AutoOrganiser.Core.Formatters;

/// <summary>
/// Handles file name formatting for an item of <typeparamref name="T"/> type.
/// </summary>
/// <typeparam name="T">The <see cref="BaseItem"/> type that this formatter can process.</typeparam>
public interface IFormatter<in T>
    where T : BaseItem
{
    /// <summary>
    /// Formats a string for the given item based on its metadata.
    /// </summary>
    /// <param name="item">The item to format.</param>
    /// <returns>The formatted string for the item.</returns>
    public string Format(T item);
}
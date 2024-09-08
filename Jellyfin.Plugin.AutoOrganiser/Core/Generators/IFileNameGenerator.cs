using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.AutoOrganiser.Core.Generators;

/// <summary>
/// Handles generation of a file name for an item of <typeparamref name="T"/> type.
/// </summary>
/// <typeparam name="T">The <see cref="BaseItem"/> type that this generator can process.</typeparam>
public interface IFileNameGenerator<in T>
    where T : BaseItem
{
    /// <summary>
    /// Generates a file name for a given item based on its metadata.
    /// </summary>
    /// <param name="item">The item to generate a file name for.</param>
    /// <returns>The file name for the item.</returns>
    public string GetFileName(T item);
}
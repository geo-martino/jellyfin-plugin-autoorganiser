using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.AutoOrganiser.Core.Generators;

/// <summary>
/// Handles generation of a file path for an item of <typeparamref name="TItem"/> type.
/// </summary>
/// <typeparam name="TItem">The <see cref="BaseItem"/> type that this generator can process.</typeparam>
/// <typeparam name="TNameGenerator">Type of the <see cref="IFileNameGenerator{TItem}"/> interface capable of processing <typeparamref name="TItem"/>.</typeparam>
public interface IFilePathGenerator<in TItem, in TNameGenerator>
    where TItem : BaseItem
    where TNameGenerator : IFileNameGenerator<TItem>
{
    /// <summary>
    /// Generates a file path for a given item based on its metadata.
    /// </summary>
    /// <param name="item">The item to generate a path for.</param>
    /// <param name="nameGenerator">The generator to use to generate an item's file name.</param>
    /// <returns>The file path for the series.</returns>
    public string GeneratePath(TItem item, TNameGenerator nameGenerator);
}
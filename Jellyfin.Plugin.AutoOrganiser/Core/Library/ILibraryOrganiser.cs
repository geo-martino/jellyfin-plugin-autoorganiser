using System.Threading;
using Jellyfin.Plugin.AutoOrganiser.Core.Generators;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.AutoOrganiser.Core.Library;

/// <summary>
/// Handles organising items of <typeparamref name="TItem"/> type within a given library.
/// </summary>
/// <typeparam name="TItem">The <see cref="BaseItem"/> type that this generator can process.</typeparam>
/// <typeparam name="TNameGenerator">Type of the <see cref="IFileNameGenerator{TItem}"/> interface capable of processing <typeparamref name="TItem"/>.</typeparam>
/// <typeparam name="TPathGenerator">Type of the <see cref="IFilePathGenerator{TItem, TNameGenerator}"/> interface capable of processing <typeparamref name="TItem"/>.</typeparam>
public interface ILibraryOrganiser<TItem, in TNameGenerator, in TPathGenerator>
    where TItem : BaseItem
    where TNameGenerator : IFileNameGenerator<TItem>
    where TPathGenerator : IFilePathGenerator<TItem, TNameGenerator>
{
    /// <summary>
    /// Organises all items in the current library by moving the files to new paths based on the given generators.
    /// </summary>
    /// <param name="nameGenerator">Instance of the <see cref="IFileNameGenerator{TItem}"/> interface.</param>
    /// <param name="pathGenerator">Instance of the <see cref="IFilePathGenerator{TItem, TNameGenerator}"/> interface.</param>
    /// <param name="dryRun">Whether to execute as a dry run, which does not modify any files.</param>
    /// <param name="progressHandler">Instance of the <see cref="ProgressHandler"/>.</param>
    /// <param name="cancellationToken">Instance of the <see cref="CancellationToken"/>.</param>
    public void Organise(
        TNameGenerator nameGenerator,
        TPathGenerator pathGenerator,
        bool dryRun,
        ProgressHandler progressHandler,
        CancellationToken cancellationToken);
}
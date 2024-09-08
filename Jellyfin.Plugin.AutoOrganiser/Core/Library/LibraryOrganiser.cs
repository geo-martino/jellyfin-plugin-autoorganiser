using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AutoOrganiser.Core.Generators;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AutoOrganiser.Core.Library;

/// <inheritdoc />
public abstract class LibraryOrganiser<TItem, TNameGenerator, TPathGenerator> : ILibraryOrganiser<TItem, TNameGenerator, TPathGenerator>
    where TItem : BaseItem
    where TNameGenerator : IFileNameGenerator<TItem>
    where TPathGenerator : IFilePathGenerator<TItem, TNameGenerator>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LibraryOrganiser{TItem, TNameGenerator, TPathGenerator}"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{T}"/> interface.</param>
    protected LibraryOrganiser(
        ILibraryManager libraryManager,
        ILogger<ILibraryOrganiser<TItem, TNameGenerator, TPathGenerator>> logger)
    {
        LibraryManager = libraryManager;
        Logger = logger;
    }

    /// <summary>
    /// Gets the library manager.
    /// </summary>
    protected ILibraryManager LibraryManager { get; }

    /// <summary>
    /// Gets the logger.
    /// </summary>
    protected ILogger<ILibraryOrganiser<TItem, TNameGenerator, TPathGenerator>> Logger { get; }

    /// <inheritdoc />
    public abstract void Organise(
        TNameGenerator nameGenerator,
        TPathGenerator pathGenerator,
        bool dryRun,
        ProgressHandler progressHandler,
        CancellationToken cancellationToken);

    /// <summary>
    /// Move the item to a new path on the system and update the item's path reference in Jellyfin.
    /// </summary>
    /// <param name="item">The item to move.</param>
    /// <param name="newPath">The path to move the item to.</param>
    /// <param name="dryRun">Whether to execute as a dry run, which does not modify any files.</param>
    /// <param name="cancellationToken">Instance of the <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="Task"/> to be executed.</returns>
    protected Task? MoveItem(
        BaseItem item,
        string newPath,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        if (item.Path == newPath)
        {
            return null;
        }

        var logPrefix = dryRun ? "DRY RUN | Moving" : "Moving";
        var itemKind = item.GetBaseItemKind().ToString().ToLowerInvariant();
        Logger.LogInformation("{Prefix:l} {Kind:l}: {Old} -> {New}", logPrefix, itemKind, item.Path, newPath);

        if (dryRun)
        {
            return Task.CompletedTask;
        }

        var dirPath = Path.GetDirectoryName(newPath);
        if (dirPath is not null)
        {
            Directory.CreateDirectory(dirPath);
        }

        Directory.Move(item.Path, newPath);

        item.Path = newPath;
        return item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken);
    }
}
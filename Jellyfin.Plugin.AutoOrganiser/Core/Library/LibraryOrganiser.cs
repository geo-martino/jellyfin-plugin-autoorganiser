using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.AutoOrganiser.Core.Formatters;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AutoOrganiser.Core.Library;

/// <summary>
/// Handles organising items within a given library for a specific item type and related types.
/// </summary>
/// <typeparam name="TItem">The <see cref="BaseItem"/> type that this organiser can process.</typeparam>
/// <typeparam name="TFileHandler">Type of the <see cref="FileHandler{TItem,FilePathFormatter}"/> interface capable of processing <typeparamref name="TItem"/>.</typeparam>
/// <typeparam name="TPathFormatter">Type of the <see cref="FilePathFormatter{TItem}"/> interface capable of processing <typeparamref name="TItem"/>.</typeparam>
public abstract class LibraryOrganiser<TItem, TFileHandler, TPathFormatter>
    where TItem : BaseItem
    where TFileHandler : FileHandler<TItem, TPathFormatter>
    where TPathFormatter : FilePathFormatter<TItem>
{
    private readonly IDirectoryService _directoryService;
    private readonly IServerConfigurationManager _serverConfig;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibraryOrganiser{TItem,TFileHandler,TPathFormatter}"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="directoryService">Instance of the <see cref="IDirectoryService"/>.</param>
    /// <param name="serverConfig">Instance of the <see cref="IServerConfigurationManager"/>.</param>
    /// <param name="fileHandler">Instance of the <see cref="FileHandler{TItem,TPathFormatter}"/>.</param>
    /// <param name="dryRun">Whether to execute as a dry run, which does not modify any items.</param>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    protected LibraryOrganiser(
        ILibraryManager libraryManager,
        IDirectoryService directoryService,
        IServerConfigurationManager serverConfig,
        TFileHandler fileHandler,
        bool dryRun,
        ILogger<LibraryOrganiser<TItem, TFileHandler, TPathFormatter>> logger)
    {
        LibraryManager = libraryManager;
        _directoryService = directoryService;
        _serverConfig = serverConfig;
        FileHandler = fileHandler;

        DryRun = dryRun;
        Logger = logger;
    }

    /// <summary>
    /// Gets the library manager.
    /// </summary>
    protected ILibraryManager LibraryManager { get; }

    /// <summary>
    /// Gets the instance of the <see cref="FileHandler{TItem,TPathFormatter}"/>.
    /// </summary>
    protected TFileHandler FileHandler { get; }

    /// <summary>
    /// Gets a value indicating whether to execute as a dry run.
    /// </summary>
    protected bool DryRun { get; }

    /// <summary>
    /// Gets the logger.
    /// </summary>
    protected ILogger<LibraryOrganiser<TItem, TFileHandler, TPathFormatter>> Logger { get; }

    private string TempMetadataDir => Path.Combine(_serverConfig.ApplicationPaths.TempDirectory, "metadata", "library");

    /// <summary>
    /// Organises all items in the current library by moving the files to new paths based on the given formatters.
    /// </summary>
    /// <param name="progressHandler">Instance of the <see cref="ProgressHandler"/>.</param>
    /// <param name="cancellationToken">Instance of the <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public abstract Task Organise(ProgressHandler progressHandler, CancellationToken cancellationToken);

    /// <summary>
    /// Organise the extras for a given item.
    /// </summary>
    /// <param name="item">The item for which to organise extras.</param>
    /// <param name="parentName">The parent name. Used for logging only.</param>
    /// <param name="cancellationToken">Instance of the <see cref="CancellationToken"/>.</param>
    /// <returns>The number of extras moved.</returns>
    protected int OrganiseExtras(BaseItem item, string parentName, CancellationToken cancellationToken) =>
        FileHandler.MoveExtras(
            item.GetExtras().ToArray(),
            item is Folder ? item.Path : Path.GetDirectoryName(item.Path)!,
            parentName,
            item.GetBaseItemKind(),
            cancellationToken);

    /// <summary>
    /// Logs the results of moving items to new locations.
    /// </summary>
    /// <param name="items">The items which have been moved.</param>
    protected void LogResults(IReadOnlyCollection<BaseItem> items)
    {
        if (items.Count.Equals(0))
        {
            Logger.LogInformation("No items updated.");
            return;
        }

        Logger.LogInformation("{Prefix:l}Moved {N} items", FileHandler.GetLogPrefix(), items.Count);
    }

    /// <summary>
    /// Refresh the top parent folders (i.e. top libraries) of the given items after moving the files.
    /// </summary>
    /// <param name="items">The items that were moved.</param>
    /// <param name="progress">Instance of the <see cref="IProgress{T}"/>.</param>
    /// <param name="cancellationToken">Instance of the <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    protected async Task RefreshLibraries(
        IEnumerable<BaseItem> items, IProgress<double> progress, CancellationToken cancellationToken)
    {
        var refreshOptions = new MetadataRefreshOptions(_directoryService)
        {
            MetadataRefreshMode = MetadataRefreshMode.Default,
            ReplaceAllImages = false,
            // The following args are temporarily set until a better fix
            // for the dropping metadata problem can be implemented.
            // Replace the enabled args with the commented args when implemented.
            RemoveOldMetadata = true,
            ImageRefreshMode = MetadataRefreshMode.Default,
            EnableRemoteContentProbe = true,
            // RemoveOldMetadata = false,
            // ImageRefreshMode = MetadataRefreshMode.None,
            // EnableRemoteContentProbe = false,
            ForceSave = false,
        };
        var folders = items.Select(item => item.GetTopParent()).OfType<Folder>().ToHashSet().ToArray();

        Logger.LogInformation("Refreshing {N} libraries", folders.Length);

        var tasks = folders.Select(parent => parent.ValidateChildren(
            progress, refreshOptions, true, false, cancellationToken));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    /// <summary>
    /// After moving a file to a new path and refreshing the library,
    /// replace the metadata of the newly created item with the original item's metadata.
    /// </summary>
    /// <param name="items">The original items containing the metadata.</param>
    /// <param name="cancellationToken">Instance of the <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    protected async Task ReplaceMetadata(IEnumerable<TItem> items, CancellationToken cancellationToken)
    {
        var tasks = items.Select(item => ReplaceMetadata(item, cancellationToken)).ToArray();
        Logger.LogInformation("{Prefix:l}Reapplying metadata for {N} items", FileHandler.GetLogPrefix(), tasks.Length);
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private async Task ReplaceMetadata(TItem item, CancellationToken cancellationToken)
    {
        var newIdNullable = LibraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [item.GetBaseItemKind()],
            Path = item.Path
        }).FirstOrDefault(i => i.Id != item.Id)?.Id;

        if (newIdNullable == null)
        {
            return;
        }

        var newId = (Guid)newIdNullable;

        Logger.LogInformation(
            "{Prefix:l}Updating ID for {Name}: {OldId} -> {NewId}",
            FileHandler.GetLogPrefix(),
            item.Name,
            item.Id,
            newId);

        var oldId = item.Id;
        var tempMetadataDir = FormatTempMetadataPath(item);
        var oldStem = FormatMetadataStemPath(oldId);
        var newStem = FormatMetadataStemPath(newId);

        if (!DryRun)
        {
            LibraryManager.DeleteItem(item, new DeleteOptions { DeleteFileLocation = false });
        }

        item.Id = newId;
        foreach (var image in item.ImageInfos)
        {
            image.Path = image.Path.Replace(oldStem, newStem, StringComparison.Ordinal);
        }

        if (DryRun || Directory.Exists(tempMetadataDir))
        {
            var newMetadataDir = item.GetInternalMetadataPath();

            if (!DryRun && Directory.Exists(newMetadataDir))
            {
                Directory.Delete(newMetadataDir, true);
            }

            FileHandler.MoveDirectoryRecursively(tempMetadataDir, newMetadataDir, "metadata");
            if (!DryRun)
            {
                await item.UpdateToRepositoryAsync(ItemUpdateType.ImageUpdate, cancellationToken).ConfigureAwait(false);
            }
        }

        if (!DryRun)
        {
            await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataImport, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Copies the metadata folder for the given item to a temporary location.
    /// </summary>
    /// <param name="item">The item to move metadata for.</param>
    /// <typeparam name="T">The <see cref="BaseItem"/> type to be processed.</typeparam>
    /// <returns>The given item.</returns>
    protected T CopyMetadataToTempDir<T>(T item)
        where T : BaseItem
    {
        var metadataDir = item.GetInternalMetadataPath();
        var tempDir = FormatTempMetadataPath(item);

        if (!DryRun && Directory.Exists(metadataDir))
        {
            FileHandler.CopyFilesRecursively(metadataDir, tempDir, "metadata");
        }

        return item;
    }

    /// <summary>
    /// Deletes the temporary metadata directory recursively.
    /// </summary>
    protected void ClearTempMetadataDir()
    {
        if (Directory.Exists(TempMetadataDir))
        {
            Directory.Delete(TempMetadataDir, true);
        }
    }

    /// <summary>
    /// Format a temporary path for storing the metadata of the given item.
    /// </summary>
    /// <param name="item">The item to generate a path for.</param>
    /// <returns>The generated path.</returns>
    private string FormatTempMetadataPath(BaseItem item) => Path
        .Combine(TempMetadataDir, FormatMetadataStemPath(item.Id));

    private string FormatMetadataStemPath(Guid id) => Path
        .Combine(id.ToString()[..2], id.ToString("N", CultureInfo.InvariantCulture));

    /// <summary>
    /// Attempt to match the given items to a set of given folders by matching on the
    /// names of the parent folders to the folder names in the item.
    /// Removes the item from the given list of items when a match is found.
    /// </summary>
    /// <param name="items">The items to match.</param>
    /// <param name="parents">The parents to match on.</param>
    /// <param name="link">
    /// When true, link the item to parent. When false, add the item as a child of the parent.
    /// </param>
    /// <param name="cancellationToken">Instance of the <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    protected async Task MatchItemsToParentFolders(
        ICollection<TItem> items, IEnumerable<Folder> parents, bool link, CancellationToken cancellationToken)
    {
        foreach (var parent in parents)
        {
            var moviesAdded = items.Where(item => MatchItemToParentFolder(item, parent, link)).ToArray();
            if (moviesAdded.Length == 0)
            {
                continue;
            }

            await parent.UpdateToRepositoryAsync(ItemUpdateType.None, cancellationToken).ConfigureAwait(false);
            foreach (var movie in moviesAdded)
            {
                items.Remove(movie);
            }
        }
    }

    private bool MatchItemToParentFolder(TItem item, Folder parent, bool link)
    {
        var topParent = FindParentFolder(item)?.GetTopParent()?.Path;
        if (topParent == null)
        {
            return false;
        }

        var folderNameBase = Path.GetRelativePath(topParent, item.Path).Split(Path.DirectorySeparatorChar).First();
        var folderName = item.GetParents()
            .Select(i => Path.GetRelativePath(topParent, i.Path))
            .Concat([folderNameBase])
            .FirstOrDefault(folderName => folderName.Equals(parent.Name, StringComparison.OrdinalIgnoreCase));
        if (folderName == null)
        {
            return false;
        }

        if (link)
        {
            LinkItemToParentFolder(item, parent);
        }
        else
        {
            AddItemToParentFolder(item, parent);
        }

        return true;
    }

    /// <summary>
    /// Add the given item to its relevant Folder in the database.
    /// Items in sub-folders sometimes get abandoned when updating paths metadata.
    /// This means they have no parents and therefore cannot be processed on subsequent runs.
    /// This method fixes that.
    /// </summary>
    /// <param name="item">The item to process.</param>
    /// <param name="parent">
    /// The parent to link the item to. If not given, attempts to find a Folder with
    /// a matching path one of the item's parents.
    /// </param>
    protected void AddItemToParentFolder(BaseItem item, Folder? parent = null)
    {
        parent ??= FindParentFolder(item);
        if (parent is null || parent.Children.Select(child => child.Path).Contains(item.Path))
        {
            return;
        }

        Logger.LogInformation(
            "Adding {ItemKind:l} {ItemName} to {ParentKind:l} {ParentName}",
            item.GetBaseItemKind().ToString().ToLowerSentenceCase(),
            item.Name,
            parent.GetBaseItemKind().ToString().ToLowerSentenceCase(),
            parent.Name);

        parent.AddChild(item);
    }

    /// <summary>
    /// Links the given item to its relevant Folder in the database.
    /// </summary>
    /// <param name="item">The item to process.</param>
    /// <param name="parent">
    /// The parent to link the item to. If not given, attempts to find a Folder with
    /// a matching path one of the item's parents.
    /// </param>
    // ReSharper disable once MemberCanBePrivate.Global
    protected void LinkItemToParentFolder(BaseItem item, Folder? parent = null)
    {
        parent ??= FindParentFolder(item);
        if (parent is null || parent.LinkedChildren.Select(child => child.Path).Contains(item.Path))
        {
            return;
        }

        Logger.LogInformation(
            "Linking {ItemKind:l} {ItemName} to {ParentKind:l} {ParentName}",
            item.GetBaseItemKind().ToString().ToLowerSentenceCase(),
            item.Name,
            parent.GetBaseItemKind().ToString().ToLowerSentenceCase(),
            parent.Name);

        var linkedChild = LinkedChild.Create(item);
        parent.LinkedChildren = parent.LinkedChildren.AsEnumerable()
            .Where(child => File.Exists(child.Path))
            .Append(linkedChild).ToArray();
    }

    private Folder? FindParentFolder(BaseItem item) => LibraryManager
        .GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Folder],
            Path = Path.GetDirectoryName(item.Path)
        }).OfType<Folder>().FirstOrDefault();
}
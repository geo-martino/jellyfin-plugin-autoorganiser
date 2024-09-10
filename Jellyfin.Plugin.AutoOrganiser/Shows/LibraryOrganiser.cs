using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.AutoOrganiser.Core;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AutoOrganiser.Shows;

/// <summary>
/// Handles organising items within a given library.
/// </summary>
public class LibraryOrganiser
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LibraryOrganiser"/> class.
    /// </summary>
    /// <param name="itemHandler">Instance of the <see cref="ItemHandler"/>.</param>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    public LibraryOrganiser(
        ItemHandler itemHandler,
        ILibraryManager libraryManager,
        ILogger<LibraryOrganiser> logger)
    {
        ItemHandler = itemHandler;

        LibraryManager = libraryManager;
        Logger = logger;
    }

    /// <summary>
    /// Gets the instance of the <see cref="ItemHandler"/>.
    /// </summary>
    private ItemHandler ItemHandler { get; }

    /// <summary>
    /// Gets the library manager.
    /// </summary>
    private ILibraryManager LibraryManager { get; }

    /// <summary>
    /// Gets the logger.
    /// </summary>
    private ILogger<LibraryOrganiser> Logger { get; }

    private IEnumerable<Series> GetShowsFromLibrary() => LibraryManager
        .GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Series],
            IsVirtualItem = false,
            OrderBy = new List<(ItemSortBy, SortOrder)>
            {
                new(ItemSortBy.SortName, SortOrder.Ascending)
            },
            Recursive = true
        }).OfType<Series>().Where(series => Directory.Exists(series.Path));

    /// <summary>
    /// Organises all items in the current library by moving the files to new paths based on the given formatters.
    /// </summary>
    /// <param name="progressHandler">Instance of the <see cref="ProgressHandler"/>.</param>
    /// <param name="cancellationToken">Instance of the <see cref="CancellationToken"/>.</param>
    public void Organise(ProgressHandler progressHandler, CancellationToken cancellationToken)
    {
        var shows = GetShowsFromLibrary().ToArray();
        Logger.LogInformation("Found {N} shows to organise", shows.Length);

        progressHandler.SetProgressToInitial();
        var tasks = shows
            .Select((series, idx) => progressHandler.Progress(idx, shows.Length, series))
            .SelectMany(series => OrganiseFolder(series, cancellationToken))
            .Where(task => task is not null)
            .OfType<Task>()
            .ToArray();

        progressHandler.SetProgressToFinal();
        ItemHandler.RunUpdateMetadataTasks(tasks);
    }

    private IEnumerable<Task?> OrganiseFolder(Folder folder, CancellationToken cancellationToken)
    {
        var tasks = OrganiseChildren(folder, cancellationToken);

        foreach (var task in tasks)
        {
            yield return task;
        }

        var newPath = folder switch
        {
            Series series => ItemHandler.PathFormatter.Format(series),
            Season season => ItemHandler.PathFormatter.Format(season),
            _ => null
        };

        if (newPath != null && folder.Path != newPath)
        {
            yield return ItemHandler.UpdatePathMetadata(folder, newPath, cancellationToken);
        }
    }

    private IEnumerable<Task?> OrganiseChildren(Folder folder, CancellationToken cancellationToken) => folder switch
    {
        Series series => OrganiseChildSeasons(series, cancellationToken)
            .Concat(OrganiseChildEpisodes(series, cancellationToken)),
        Season season => OrganiseChildEpisodes(season, cancellationToken)
            .Concat(OrganiseExtras(season, cancellationToken)),
        _ => throw new ArgumentOutOfRangeException(nameof(folder), folder, "Unrecognized show folder type")
    };

    private IEnumerable<Task?> OrganiseChildSeasons(Folder folder, CancellationToken cancellationToken) => folder
        .GetRecursiveChildren(item => item.GetBaseItemKind() == BaseItemKind.Season)
        .OfType<Season>()
        .Where(season => Directory.Exists(season.Path))
        .SelectMany(season => OrganiseFolder(season, cancellationToken));

    private IEnumerable<Task?> OrganiseChildEpisodes(Folder folder, CancellationToken cancellationToken) => folder
        .GetRecursiveChildren(item => item.GetBaseItemKind() == BaseItemKind.Episode)
        .OfType<Episode>()
        .Where(episode => File.Exists(episode.Path))
        .Select(episode => OrganiseEpisode(episode, cancellationToken));

    private Task? OrganiseEpisode(Episode episode, CancellationToken cancellationToken)
    {
        if (episode.Series is null)
        {
            Logger.LogWarning(
                "Cannot process episode: it has not been assigned to a series | {Episode} ",
                episode.Path);
            return null;
        }

        var newEpisodePath = ItemHandler.PathFormatter.Format(episode);
        return ItemHandler.MoveItem(episode, newEpisodePath, cancellationToken);
    }

    private IEnumerable<Task?> OrganiseExtras(Season season, CancellationToken cancellationToken) => ItemHandler
        .MoveExtras(season.GetExtras().ToArray(), cancellationToken, season);
}
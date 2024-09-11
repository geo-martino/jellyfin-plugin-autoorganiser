using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.AutoOrganiser.Core;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AutoOrganiser.Movies;

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

    private IEnumerable<Movie> GetMoviesFromLibrary(IEnumerable<Guid>? excludeItemIds = null) => LibraryManager
        .GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Movie],
            IsVirtualItem = false,
            OrderBy = new List<(ItemSortBy, SortOrder)>
            {
                new(ItemSortBy.SortName, SortOrder.Ascending)
            },
            Recursive = true,
            ExcludeItemIds = excludeItemIds?.ToArray() ?? []
        }).OfType<Movie>().Where(movie => File.Exists(movie.Path));

    private IEnumerable<BoxSet> GetBoxSetsFromLibrary() => LibraryManager
        .GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.BoxSet],
            OrderBy = new List<(ItemSortBy, SortOrder)>
            {
                new(ItemSortBy.SortName, SortOrder.Ascending)
            },
            Recursive = true
        }).OfType<BoxSet>();

    /// <summary>
    /// Organises all items in the current library by moving the files to new paths based on the given formatters.
    /// </summary>
    /// <param name="progressHandler">Instance of the <see cref="ProgressHandler"/>.</param>
    /// <param name="cancellationToken">Instance of the <see cref="CancellationToken"/>.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public Task Organise(
        ProgressHandler progressHandler,
        CancellationToken cancellationToken)
    {
        // Get combined list of box sets and movies
        // Filter out movies in box sets from all available movies
        var boxSets = GetBoxSetsFromLibrary().ToArray();
        var boxSetMovieIds = boxSets
            .SelectMany(boxSet => boxSet.GetRecursiveChildren().OfType<Movie>())
            .Select(item => item.Id);
        var items = GetMoviesFromLibrary(boxSetMovieIds)
            .OfType<BaseItem>().Concat(boxSets).ToArray();

        Logger.LogInformation("Found {N} movies/box sets to organise", items.Length);
        progressHandler.SetProgressToInitial();

        var tasks = items
            .Select((task, idx) => progressHandler.Progress(idx, items.Length, task))
            .SelectMany(movie => OrganiseItem(movie, cancellationToken));

        return ItemHandler.RunTasks(tasks);
    }

    private IEnumerable<Task<bool>> OrganiseItem(BaseItem item, CancellationToken cancellationToken) => item switch
    {
        BoxSet boxSet =>
            ItemHandler.PathFormatter
                .GetPathsFromBoxSet(boxSet)
                .Select(pair => ItemHandler.MoveItem(pair.Item1, pair.Item2, cancellationToken)),
        Movie movie =>
            Enumerable.Empty<Task<bool>>()
                .Concat([ItemHandler.MoveItem(movie, ItemHandler.Format(movie), cancellationToken)])
                .Concat(OrganiseExtras(movie, cancellationToken)),
        _ => []
    };

    private IEnumerable<Task<bool>> OrganiseExtras(Movie movie, CancellationToken cancellationToken) => ItemHandler
        .MoveExtras(movie.GetExtras().ToArray(), cancellationToken);
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.AutoOrganiser.Core;
using Jellyfin.Plugin.AutoOrganiser.Core.Library;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AutoOrganiser.Movies;

/// <inheritdoc />
public class LibraryOrganiser : LibraryOrganiser<Movie, FileNameGenerator, FilePathGenerator>
{
    /// <inheritdoc />
    public LibraryOrganiser(
        ILibraryManager libraryManager,
        ILogger<ILibraryOrganiser<Movie, FileNameGenerator, FilePathGenerator>> logger) : base(libraryManager, logger)
    {
    }

    private IEnumerable<Movie> GetMoviesFromLibrary(IEnumerable<Guid>? excludeItemIds = null)
    {
        var movies = LibraryManager.GetItemList(new InternalItemsQuery
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

        return movies;
    }

    private IEnumerable<BoxSet> GetBoxSetsFromLibrary()
    {
        var movies = LibraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.BoxSet],
            OrderBy = new List<(ItemSortBy, SortOrder)>
            {
                new(ItemSortBy.SortName, SortOrder.Ascending)
            },
            Recursive = true
        }).OfType<BoxSet>();

        return movies;
    }

    /// <inheritdoc />
    public override async void Organise(
        FileNameGenerator nameGenerator,
        FilePathGenerator pathGenerator,
        bool dryRun,
        ProgressHandler progressHandler,
        CancellationToken cancellationToken)
    {
        // Get combined list of box sets and movies
        // Filter out movies in box sets from all available movies
        var boxSets = GetBoxSetsFromLibrary().ToList();
        var boxSetMovieIds = boxSets
            .SelectMany(boxSet => boxSet
                .GetRecursiveChildren()
                .Where(item => item.GetBaseItemKind() == BaseItemKind.Movie)
                .Select(item => item.Id));
        var items = GetMoviesFromLibrary(boxSetMovieIds)
            .OfType<BaseItem>().Concat(boxSets).ToList();

        Logger.LogInformation("Found {N} movies/box sets to organise", items.Count);
        progressHandler.SetProgressToInitial();

        var tasks = items
            .Select((task, idx) => progressHandler.Progress(idx, items.Count, task))
            .SelectMany(movie => OrganiseItem(movie, nameGenerator, pathGenerator, dryRun, cancellationToken))
            .Where(task => task is not null)
            .OfType<Task>()
            .ToList();

        progressHandler.SetProgressToFinal();

        var logPrefix = dryRun ? "DRY RUN | Updating" : "Updating";
        Logger.LogInformation("{Prefix:l} metadata on {N} moved items", logPrefix, tasks.Count);
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private IEnumerable<Task?> OrganiseItem(
        BaseItem item,
        FileNameGenerator nameGenerator,
        FilePathGenerator pathGenerator,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        if (item is BoxSet boxSet)
        {
            return pathGenerator.GetPathsFromBoxSet(boxSet, nameGenerator)
                .Select(pair => MoveItem(pair.Item1, pair.Item2, dryRun, cancellationToken));
        }

        if (item is Movie movie)
        {
            var newPath = pathGenerator.GeneratePath(movie, nameGenerator);
            return [MoveItem(movie, newPath, dryRun, cancellationToken)];
        }

        return [];
    }
}
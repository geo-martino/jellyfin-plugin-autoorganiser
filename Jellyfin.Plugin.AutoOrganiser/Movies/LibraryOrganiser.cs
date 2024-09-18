using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.AutoOrganiser.Core;
using Jellyfin.Plugin.AutoOrganiser.Core.Library;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AutoOrganiser.Movies;

/// <inheritdoc />
public class LibraryOrganiser : LibraryOrganiser<Movie, FileHandler, FilePathFormatter>
{
    /// <inheritdoc />
    public LibraryOrganiser(
        ILibraryManager libraryManager,
        IDirectoryService directoryService,
        IServerConfigurationManager serverConfig,
        FileHandler fileHandler,
        bool dryRun,
        ILogger<LibraryOrganiser<Movie, FileHandler, FilePathFormatter>> logger)
        : base(libraryManager, directoryService, serverConfig, fileHandler, dryRun, logger)
    {
    }

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

    /// <inheritdoc />
    public override async Task Organise(ProgressHandler progressHandler, CancellationToken cancellationToken)
    {
        var boxSets = GetBoxSetsFromLibrary().ToArray();
        var boxSetMovieIds = boxSets
            .SelectMany(boxSet => boxSet.GetRecursiveChildren().OfType<Movie>())
            .Select(item => item.Id);
        var movies = GetMoviesFromLibrary(boxSetMovieIds).ToList();

        await MatchItemsToParentFolders(movies, boxSets, true, cancellationToken).ConfigureAwait(false);
        var items = movies.OfType<BaseItem>().Concat(boxSets).ToArray();

        var boxSetMovieCount = boxSets.Sum(set => set.GetRecursiveChildren().OfType<Movie>().Count());
        Logger.LogInformation(
            "Organising {BoxSets} box sets containing {BoxSetMovies} movies and {Movies} movies not in box sets",
            boxSets.Length,
            boxSetMovieCount,
            movies.Count);

        progressHandler.SetProgressToInitial();
        var updatedItems = items
            .Select((task, idx) => progressHandler.Report(idx, items.Length, task))
            .SelectMany(item => OrganiseItem(item, cancellationToken))
            .OfType<Movie>()
            .ToList();

        LogResults(updatedItems);
        progressHandler.SetProgressToFinal();

        await RefreshLibraries(items, progressHandler.Progress, cancellationToken).ConfigureAwait(false);
        // await ReplaceMetadata(updatedItems, cancellationToken).ConfigureAwait(false);
        ClearTempMetadataDir();

        if (DryRun)
        {
            return;
        }

        // await MatchItemsToParentFolders(updatedItems, boxSets, true, cancellationToken).ConfigureAwait(false);
        // foreach (var movie in updatedItems)
        // {
        //     AddItemToParentFolder(movie);
        // }
        //
        // await RefreshLibraries(items, progressHandler.Progress, cancellationToken).ConfigureAwait(false);
    }

    private IEnumerable<Movie?> OrganiseItem(BaseItem item, CancellationToken cancellationToken) => item switch
    {
        BoxSet boxSet => OrganiseBoxSet(boxSet, cancellationToken),
        Movie movie => [OrganiseMovie(movie, FileHandler.Format(movie), null, cancellationToken) ? movie : null],
        _ => []
    };

    private IEnumerable<Movie?> OrganiseBoxSet(
        BoxSet boxSet, CancellationToken cancellationToken) => boxSet
        .GetRecursiveChildren()
        .OfType<Movie>().Where(i => i.GetTopParent() is not null)
        .Select(movie => OrganiseMovie(movie, FileHandler.Format(movie, boxSet), boxSet, cancellationToken) ? movie : null);

    private bool OrganiseMovie(
        Movie movie, string newPath, Folder? parent, CancellationToken cancellationToken) =>
        MoveMovie(movie, newPath, parent, cancellationToken) ||
        OrganiseExtras(movie, FormatParentName(movie, parent), cancellationToken) > 0;

    private string FormatParentName(Movie movie, Folder? parent) =>
        parent == null ? movie.Name : $"{parent.Name}: {movie.Name}";

    private bool MoveMovie(Movie movie, string newPath, Folder? parent, CancellationToken cancellationToken)
    {
        var moved = FileHandler.MoveItem(movie, newPath, cancellationToken);
        if (!moved)
        {
            return false;
        }

        if (DryRun)
        {
            return moved;
        }

        // CopyMetadataToTempDir(movie);
        // AddItemToParentFolder(movie, parent);
        return moved;
    }
}
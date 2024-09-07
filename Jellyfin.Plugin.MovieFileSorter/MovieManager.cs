using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MovieFileSorter;

/// <summary>
/// Manages movie items within the library.
/// </summary>
public class MovieManager
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<MovieManager> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MovieManager"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    public MovieManager(
        ILibraryManager libraryManager,
        ILogger<MovieManager> logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
    }

    private IEnumerable<Movie> GetMoviesFromLibrary()
    {
        var movies = _libraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Movie],
            IsVirtualItem = false,
            OrderBy = new List<(ItemSortBy, SortOrder)>
            {
                new(ItemSortBy.SortName, SortOrder.Ascending)
            },
            Recursive = true
        }).OfType<Movie>().Where(m => File.Exists(m.Path));

        return movies;
    }

    private IEnumerable<BoxSet> GetBoxSetsFromLibrary()
    {
        var movies = _libraryManager.GetItemList(new InternalItemsQuery
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

    /// <summary>
    /// Organises all movies in the library by moving the files to new paths based on the given generator.
    /// </summary>
    /// <param name="filePathGenerator">Instance of the <see cref="MovieFilePathGenerator"/> interface.</param>
    /// <param name="fileNameGenerator">Instance of the <see cref="MovieFileNameGenerator"/> interface.</param>
    /// <param name="cancellationToken">Instance of the <see cref="CancellationToken"/> interface.</param>
    public async void OrganiseMovies(
        MovieFilePathGenerator filePathGenerator,
        MovieFileNameGenerator fileNameGenerator,
        CancellationToken cancellationToken)
    {
        var boxSets = GetBoxSetsFromLibrary().ToList();
        var moviePathsMap = GetMoviesFromLibrary()
            .ToDictionary(
                movie => movie,
                movie => filePathGenerator.GeneratePath(movie, boxSets, fileNameGenerator));

        var tasks = new List<Task>();
        foreach (var pair in moviePathsMap)
        {
            if (pair.Key.Path == pair.Value)
            {
                continue;
            }

            _logger.LogInformation("Moving '{Old}' to '{New}'", pair.Key.Path, pair.Value);
            var dirPath = Path.GetDirectoryName(pair.Value);
            Directory.CreateDirectory(dirPath!);
            File.Move(pair.Key.Path, pair.Value);

            pair.Key.Path = pair.Value;
            tasks.Add(pair.Key.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken));
        }

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MovieFileSorter;

/// <summary>
/// Manages movie items within the library.
/// </summary>
public class MovieLibraryOrganiser
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<MovieLibraryOrganiser> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="MovieLibraryOrganiser"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    public MovieLibraryOrganiser(
        ILibraryManager libraryManager,
        ILogger<MovieLibraryOrganiser> logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
    }

    private int InitialProgress => 5;

    private int FinalProgress => 95;

    private T Progress<T>(int index, int total, IProgress<double> progress, T obj)
    {
        var percentageModifier = FinalProgress - InitialProgress;
        var progressPercentage = (index / (double)total) * percentageModifier;
        progress.Report(InitialProgress + progressPercentage);

        return obj;
    }

    /// <summary>
    /// Removes empty directories recursively for all folders of every 'Shows' library.
    /// </summary>
    /// <param name="ignoreExtensions">Ignore the following extensions when checking files in a directory.</param>
    public void CleanLibrary(IReadOnlyCollection<string> ignoreExtensions)
    {
        _logger.LogInformation(
            "Cleaning library of empty folders, ignoring extensions: {0}", string.Join(", ", ignoreExtensions));

        var directories = _libraryManager.GetVirtualFolders()
            .Where(virtualFolder => virtualFolder.CollectionType == CollectionTypeOptions.movies)
            .SelectMany(directory => directory.Locations.SelectMany(Directory.GetDirectories));

        foreach (var directory in directories)
        {
            RemoveEmptyDirectories(directory, ignoreExtensions);
        }
    }

    private void RemoveEmptyDirectories(string directory, IReadOnlyCollection<string> ignoreExtensions)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        foreach (var dir in Directory.GetDirectories(directory))
        {
            RemoveEmptyDirectories(dir, ignoreExtensions);
        }

        var files = GetFilesInDirectory(directory, ignoreExtensions).ToList();
        if (files.Count == 0 && Directory.GetDirectories(directory).Length == 0)
        {
            _logger.LogInformation("Deleting directory {Dir}", directory);
            foreach (var file in Directory.GetFiles(directory))
            {
                File.Delete(file);
            }

            Directory.Delete(directory, false);
        }
    }

    private IEnumerable<string> GetFilesInDirectory(string directory, IReadOnlyCollection<string> ignoreExtensions)
    {
        var files = Directory.GetFiles(directory)
            .Where(file => !ignoreExtensions.Contains(Path.GetExtension(file).TrimStart('.').ToLowerInvariant()));

        return files;
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
        }).OfType<Movie>().Where(movie => File.Exists(movie.Path));

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

    private Task? OrganiseMovie(
        Movie movie,
        IReadOnlyCollection<BoxSet> boxSets,
        MovieFilePathGenerator filePathGenerator,
        MovieFileNameGenerator fileNameGenerator,
        CancellationToken cancellationToken)
    {
        var newPath = filePathGenerator.GeneratePath(movie, boxSets, fileNameGenerator);
        if (movie.Path == newPath)
        {
            return null;
        }

        _logger.LogInformation("Moving movie: '{Old}' -> '{New}'", movie.Path, newPath);

        var dirPath = Path.GetDirectoryName(newPath);
        Directory.CreateDirectory(dirPath!);
        File.Move(movie.Path, newPath);

        movie.Path = newPath;
        return movie.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken);
    }

    /// <summary>
    /// Organises all movies in the library by moving the files to new paths based on the given generator.
    /// </summary>
    /// <param name="filePathGenerator">Instance of the <see cref="MovieFilePathGenerator"/> interface.</param>
    /// <param name="fileNameGenerator">Instance of the <see cref="MovieFileNameGenerator"/> interface.</param>
    /// <param name="progress">Instance of the IProgress interface.</param>
    /// <param name="cancellationToken">Instance of the <see cref="CancellationToken"/> interface.</param>
    public async void OrganiseMovies(
        MovieFilePathGenerator filePathGenerator,
        MovieFileNameGenerator fileNameGenerator,
        IProgress<double> progress,
        CancellationToken cancellationToken)
    {
        var movies = GetMoviesFromLibrary().ToList();
        _logger.LogInformation("Found {N} movies to sort", movies.Count);
        progress.Report(InitialProgress);

        var boxSets = GetBoxSetsFromLibrary().ToList();
        var tasks = movies
            .Select(movie => OrganiseMovie(movie, boxSets, filePathGenerator, fileNameGenerator, cancellationToken))
            .Select((task, idx) => Progress(idx, movies.Count, progress, task))
            .Where(task => task is not null)
            .OfType<Task>()
            .ToList();

        _logger.LogInformation("Updating metadata on {N} moved files", tasks.Count);
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}
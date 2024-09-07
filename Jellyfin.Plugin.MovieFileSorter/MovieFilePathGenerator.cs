using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities.Movies;

namespace Jellyfin.Plugin.MovieFileSorter;

/// <summary>
/// Generates a path from the given configuration for a movie based on its metadata.
/// </summary>
public class MovieFilePathGenerator
{
    private readonly bool _forceSubFolder;

    /// <summary>
    /// Initializes a new instance of the <see cref="MovieFilePathGenerator"/> class.
    /// </summary>
    /// <param name="forceSubFolder">Instance of the <see cref="bool"/> interface.</param>
    public MovieFilePathGenerator(bool forceSubFolder)
    {
        _forceSubFolder = forceSubFolder;
    }

    /// <summary>
    /// Generates a file path for a given movie based on its metadata.
    /// </summary>
    /// <param name="movie">Instance of the <see cref="Movie"/> interface.</param>
    /// <param name="boxSets">All available box sets in the library.</param>
    /// <param name="fileNameGenerator">The generator to use to generate the movie's file name.</param>
    /// <returns>The file path for the movie.</returns>
    public string GeneratePath(Movie movie, IReadOnlyCollection<BoxSet> boxSets, MovieFileNameGenerator fileNameGenerator)
    {
        var path = movie.GetTopParent().GetTopParent().Path;
        path = AppendBoxSetName(movie, path, fileNameGenerator, boxSets);
        path = AppendSubFolder(movie, path, fileNameGenerator);

        var fileName = fileNameGenerator.GetFileName(movie);
        return Path.Combine(path, fileName);
    }

    private string AppendBoxSetName(
        Movie movie, string path, MovieFileNameGenerator fileNameGenerator, IReadOnlyCollection<BoxSet> boxSets)
    {
        var boxSet = boxSets
            .FirstOrDefault(bs => bs
                .GetRecursiveChildren()
                .Where(i => i.GetBaseItemKind() == BaseItemKind.Movie)
                .OfType<Movie>()
                .FirstOrDefault(m => m.Id.Equals(movie.Id)) is not null);

        if (boxSet is not null)
        {
            var boxSetName = fileNameGenerator.SanitiseValue(boxSet.Name);
            var boxSetYear = boxSet.PremiereDate?.Year;
            if (boxSetYear is not null)
            {
                boxSetName += $" ({boxSetYear})";
            }

            path = Path.Combine(path, boxSetName);
        }
        else if (movie.CollectionName is not null && !movie.DisplayParent.Equals(movie.GetTopParent()))
        {
            var collectionName = fileNameGenerator.SanitiseValue(movie.DisplayParent.Name);
            path = Path.Combine(path, collectionName);
        }

        return path;
    }

    private string AppendSubFolder(Movie movie, string path, MovieFileNameGenerator fileNameGenerator)
    {
        if (_forceSubFolder || movie.ExtraIds.Length > 0)
        {
            var folderName = fileNameGenerator.SanitiseValue(movie.Name);

            var year = movie.PremiereDate?.Year;
            if (year is not null)
            {
                folderName += $" ({year})";
            }

            path = Path.Combine(path, folderName);
        }

        return path;
    }
}
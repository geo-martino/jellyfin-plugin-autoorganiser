using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.AutoOrganiser.Core.Generators;
using MediaBrowser.Controller.Entities.Movies;

namespace Jellyfin.Plugin.AutoOrganiser.Movies;

/// <inheritdoc />
public class FilePathGenerator : IFilePathGenerator<Movie, FileNameGenerator>
{
    private readonly bool _forceSubFolder;

    /// <summary>
    /// Initializes a new instance of the <see cref="FilePathGenerator"/> class.
    /// </summary>
    /// <param name="forceSubFolder">Whether to always place the movie within a subfolder even when it has no extra files.</param>
    public FilePathGenerator(bool forceSubFolder)
    {
        _forceSubFolder = forceSubFolder;
    }

    /// <inheritdoc />
    public string GeneratePath(Movie item, FileNameGenerator nameGenerator)
    {
        var parentPath = item.GetTopParent().Path;
        return Path.Combine(parentPath, GetBasePath(item, nameGenerator));
    }

    private string GetBasePath(Movie item, FileNameGenerator nameGenerator)
    {
        var parentPath = string.Empty;
        parentPath = AppendSubFolder(item, parentPath, nameGenerator);

        var fileName = nameGenerator.GetFileName(item);
        return Path.Combine(parentPath, fileName);
    }

    /// <summary>
    /// Generates movie file paths from movies within a box set.
    /// </summary>
    /// <param name="boxSet">The box set to generate paths from.</param>
    /// <param name="nameGenerator">The generator to use to generate a movie's file name.</param>
    /// <returns>The new paths for the items in the box set.</returns>
    public IEnumerable<Tuple<Movie, string>> GetPathsFromBoxSet(BoxSet boxSet, FileNameGenerator nameGenerator)
    {
        var boxSetName = nameGenerator.SanitiseValue(boxSet.Name);
        boxSetName = nameGenerator.AppendYear(boxSet, boxSetName);

        var paths = boxSet.GetRecursiveChildren()
            .Where(i => i.GetBaseItemKind() == BaseItemKind.Movie)
            .OfType<Movie>()
            .Select(movie => new Tuple<Movie, string>(
                movie, Path.Combine(movie.GetTopParent().Path, boxSetName, GetBasePath(movie, nameGenerator))));

        return paths;
    }

    private string AppendSubFolder(Movie movie, string path, FileNameGenerator nameGenerator)
    {
        if (_forceSubFolder || movie.ExtraIds.Length > 0)
        {
            var folderName = nameGenerator.SanitiseValue(movie.Name);

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
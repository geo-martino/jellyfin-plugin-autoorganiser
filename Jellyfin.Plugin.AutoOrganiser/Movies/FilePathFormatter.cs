using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.AutoOrganiser.Core.Formatters;
using MediaBrowser.Controller.Entities.Movies;

namespace Jellyfin.Plugin.AutoOrganiser.Movies;

/// <inheritdoc />
public class FilePathFormatter : FilePathFormatter<Movie, BoxSet>
{
    private readonly bool _forceSubFolder;

    /// <inheritdoc cref="FilePathFormatter{Movie,BoxSet}(LabelFormatter)" />
    /// <param name="labelFormatter">The object which handles label formatting for the item.</param>
    /// <param name="forceSubFolder">Whether to always place the movie within a subfolder even when it has no extra files.</param>
    public FilePathFormatter(LabelFormatter labelFormatter, bool forceSubFolder) : base(labelFormatter)
    {
        _forceSubFolder = forceSubFolder;
    }

    /// <inheritdoc />
    public override string Format(Movie item) => Path.Combine(item.GetTopParent().Path, GetStemPath(item));

    /// <inheritdoc />
    public override string Format(BoxSet item)
    {
        var parentPath = item.GetTopParent().Path;
        var boxSetName = SanitiseValue(item.Name);

        return Path.Combine(parentPath, boxSetName);
    }

    private string GetStemPath(Movie item)
    {
        var parentPath = string.Empty;
        parentPath = AppendSubFolder(item, parentPath);

        var fileName = SanitiseValue(item.Name);
        fileName = AppendYear(item, fileName);
        fileName = LabelFormatter.AppendLabel(item, fileName);
        fileName = AppendExtension(item, fileName);

        return Path.Combine(parentPath, fileName);
    }

    /// <summary>
    /// Generates movie file paths from movies within a box set.
    /// </summary>
    /// <param name="boxSet">The box set to generate paths from.</param>
    /// <returns>The new paths for the items in the box set.</returns>
    public IEnumerable<Tuple<Movie, string>> GetPathsFromBoxSet(BoxSet boxSet)
    {
        var boxSetName = new DirectoryInfo(Format(boxSet)).Name!;

        var paths = boxSet.Children
            .OfType<Movie>().Where(i => i.GetTopParent() is not null)
            .Select(movie => new Tuple<Movie, string>(
                movie, Path.Combine(movie.GetTopParent().Path, boxSetName, GetStemPath(movie))));

        return paths;
    }

    private string AppendSubFolder(Movie movie, string path)
    {
        if (_forceSubFolder || movie.ExtraIds.Length > 0)
        {
            var folderName = SanitiseValue(movie.Name);

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
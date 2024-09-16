using System.IO;
using Jellyfin.Plugin.AutoOrganiser.Core.Formatters;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;

namespace Jellyfin.Plugin.AutoOrganiser.Movies;

/// <inheritdoc />
public class FilePathFormatter : FilePathFormatter<Movie>
{
    private readonly bool _forceSubFolder;

    /// <inheritdoc cref="FilePathFormatter{Movie}(LabelFormatter)" />
    /// <param name="labelFormatter">The object which handles label formatting for the item.</param>
    /// <param name="forceSubFolder">Whether to always place the movie within a subfolder even when it has no extra files.</param>
    public FilePathFormatter(LabelFormatter labelFormatter, bool forceSubFolder) : base(labelFormatter)
    {
        _forceSubFolder = forceSubFolder;
    }

    /// <inheritdoc />
    public override string Format(Movie item) => Path.Combine(item.GetTopParent().Path, GetStemPath(item));

    /// <inheritdoc />
    public override string Format(Folder folder)
    {
        var parentPath = folder.GetTopParent().Path;
        var boxSetName = SanitiseValue(folder.Name);

        return Path.Combine(parentPath, boxSetName);
    }

    /// <summary>
    /// Formats a file path for the given item based on its metadata and the metadata of its parent box set.
    /// </summary>
    /// <param name="item">The item to format a file path for.</param>
    /// <param name="boxSet">The box set within which the item can be found.</param>
    /// <returns>The file path.</returns>
    public string Format(Movie item, BoxSet boxSet)
    {
        var boxSetName = new DirectoryInfo(Format(boxSet)).Name;
        return Path.Combine(item.GetTopParent().Path, boxSetName, GetStemPath(item));
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

    private string AppendSubFolder(Movie movie, string path)
    {
        if (!_forceSubFolder && movie.ExtraIds.Length == 0)
        {
            return path;
        }

        var folderName = SanitiseValue(movie.Name);
        folderName = AppendYear(movie, folderName);

        return Path.Combine(path, folderName);
    }
}
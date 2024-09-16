using System;
using Jellyfin.Plugin.AutoOrganiser.Core.Library;
using MediaBrowser.Controller.Entities.Movies;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AutoOrganiser.Movies;

/// <inheritdoc />
public class FileHandler : FileHandler<Movie, FilePathFormatter>
{
    /// <inheritdoc />
    public FileHandler(
        FilePathFormatter pathFormatter,
        bool dryRun,
        bool overwrite,
        ILogger<FileHandler<Movie, FilePathFormatter>> logger)
        : base(pathFormatter, dryRun, overwrite, logger)
    {
    }

    /// <inheritdoc cref="FilePathFormatter.Format(Movie, BoxSet)"/>
    public string Format(Movie item, BoxSet boxSet)
    {
        if (!IsFormatPossible(item))
        {
            return item.Path;
        }

        try
        {
            return PathFormatter.Format(item, boxSet);
        }
        catch (Exception e)
        {
            Logger.LogCritical(
                e,
                "Could not format a new path for movie within box set: \"{BoxSet:l}: {Name:l}\" - {Path}",
                boxSet.Name,
                item.Name,
                item.Path);
            throw;
        }
    }
}
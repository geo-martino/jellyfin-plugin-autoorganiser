using System;
using Jellyfin.Plugin.AutoOrganiser.Core.Library;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AutoOrganiser.Shows;

/// <inheritdoc />
public class FileHandler : FileHandler<Episode, FilePathFormatter>
{
    /// <inheritdoc />
    public FileHandler(
        FilePathFormatter pathFormatter,
        bool dryRun,
        bool overwrite,
        ILogger<FileHandler<Episode, FilePathFormatter>> logger)
        : base(pathFormatter, dryRun, overwrite, logger)
    {
    }

    /// <inheritdoc cref="FileHandler{TItem,TPathFormatter}.Format(Folder)"/>
    public string Format(Series item)
    {
        if (!IsFormatPossible(item))
        {
            return item.Path;
        }

        try
        {
            return PathFormatter.Format(item);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Could not format a new path for folder: {Name} - {Path}", item.Name, item.Path);
            throw;
        }
    }
}
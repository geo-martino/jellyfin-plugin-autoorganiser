using System;
using Jellyfin.Plugin.AutoOrganiser.Core.Library;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AutoOrganiser.Shows;

/// <inheritdoc />
public class ItemHandler : ItemHandler<Episode, Season, FilePathFormatter>
{
    /// <inheritdoc />
    public ItemHandler(
        FilePathFormatter pathFormatter,
        bool dryRun,
        bool overwrite,
        ILogger<ItemHandler<Episode, Season, FilePathFormatter>> logger)
        : base(pathFormatter, dryRun, overwrite, logger)
    {
    }

    /// <inheritdoc cref="ItemHandler{TItem,TFolder,TPathFormatter}.Format(TFolder)"/>
    public string Format(Series item)
    {
        try
        {
            return PathFormatter.Format(item);
        }
        catch (Exception)
        {
            Logger.LogCritical("Count not format a new path for folder: {Name} - {Path}", item.Name, item.Path);
            throw;
        }
    }
}
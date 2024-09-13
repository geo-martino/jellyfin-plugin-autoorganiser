using System;
using Jellyfin.Plugin.AutoOrganiser.Core.Library;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AutoOrganiser.Shows;

/// <inheritdoc />
public class ItemHandler : ItemHandler<Episode, FilePathFormatter>
{
    /// <inheritdoc />
    public ItemHandler(
        FilePathFormatter pathFormatter,
        bool dryRun,
        bool overwrite,
        ILogger<ItemHandler<Episode, FilePathFormatter>> logger)
        : base(pathFormatter, dryRun, overwrite, logger)
    {
    }

    /// <inheritdoc cref="ItemHandler{TItem,TPathFormatter}.Format(Folder)"/>
    public string Format(Series item)
    {
        try
        {
            return PathFormatter.Format(item);
        }
        catch (Exception e)
        {
            Logger.LogCritical(e, "Count not format a new path for folder: {Name} - {Path}", item.Name, item.Path);
            throw;
        }
    }
}
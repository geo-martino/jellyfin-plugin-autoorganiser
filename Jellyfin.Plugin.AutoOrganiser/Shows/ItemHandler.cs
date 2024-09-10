using Jellyfin.Plugin.AutoOrganiser.Core.Library;
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
}
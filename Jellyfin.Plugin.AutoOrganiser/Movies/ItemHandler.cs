using Jellyfin.Plugin.AutoOrganiser.Core.Library;
using MediaBrowser.Controller.Entities.Movies;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AutoOrganiser.Movies;

/// <inheritdoc />
public class ItemHandler : ItemHandler<Movie, BoxSet, FilePathFormatter>
{
    /// <inheritdoc />
    public ItemHandler(
        FilePathFormatter pathFormatter,
        bool dryRun,
        bool overwrite,
        ILogger<ItemHandler<Movie, BoxSet, FilePathFormatter>> logger)
        : base(pathFormatter, dryRun, overwrite, logger)
    {
    }
}
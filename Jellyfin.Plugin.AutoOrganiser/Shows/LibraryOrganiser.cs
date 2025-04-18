using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.AutoOrganiser.Core;
using Jellyfin.Plugin.AutoOrganiser.Core.Library;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AutoOrganiser.Shows;

/// <inheritdoc />
public class LibraryOrganiser : LibraryOrganiser<Episode, FileHandler, FilePathFormatter>
{
    /// <inheritdoc />
    public LibraryOrganiser(
        ILibraryManager libraryManager,
        IDirectoryService directoryService,
        IServerConfigurationManager serverConfig,
        FileHandler fileHandler,
        bool dryRun,
        ILogger<LibraryOrganiser<Episode, FileHandler, FilePathFormatter>> logger)
        : base(libraryManager, directoryService, serverConfig, fileHandler, dryRun, logger)
    {
    }

    private IEnumerable<Series> GetShowsFromLibrary() => LibraryManager
        .GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Series],
            IsVirtualItem = false,
            OrderBy = new List<(ItemSortBy, SortOrder)>
            {
                new(ItemSortBy.SortName, SortOrder.Ascending)
            },
            Recursive = true
        }).OfType<Series>().Where(series => Directory.Exists(series.Path));

    /// <inheritdoc />
    public override async Task Organise(ProgressHandler progressHandler, CancellationToken cancellationToken)
    {
        var shows = GetShowsFromLibrary().ToArray();

        var seasonCount = shows.Sum(show => show.GetRecursiveChildren().OfType<Season>().Count());
        var episodeCount = shows.Sum(show => show.GetRecursiveChildren().OfType<Episode>().Count());
        Logger.LogInformation(
            "Organising {Shows} shows containing {Seasons} total seasons and {Episodes} total episodes",
            shows.Length,
            seasonCount,
            episodeCount);

        progressHandler.SetProgressToInitial();
        var updatedResults = await shows
            .Select((series, idx) => progressHandler.Report(idx, shows.Length, series))
            .SelectManyAsync(series => OrganiseFolder(series, cancellationToken))
            .ConfigureAwait(false);
        var updatedItems = updatedResults.OfType<Episode>().ToArray();

        LogResults(updatedItems);
        progressHandler.SetProgressToFinal();

        await LibraryManager.ValidateTopLibraryFolders(cancellationToken).ConfigureAwait(false);
        // await RefreshLibraries(updatedItems, progressHandler.Progress, cancellationToken).ConfigureAwait(false);
        // await ReplaceMetadata(updatedItems, cancellationToken).ConfigureAwait(false);
        ClearTempMetadataDir();
    }

    private async Task<IEnumerable<Episode?>> OrganiseFolder(Folder folder, CancellationToken cancellationToken)
    {
        var results = OrganiseChildren(folder, cancellationToken);

        folder.Path = folder switch
        {
            Series series => FileHandler.Format(series),
            Season season => FileHandler.Format(season),
            _ => folder.Path
        };

        return await results.ConfigureAwait(false);
    }

    private async Task<IEnumerable<Episode?>> OrganiseChildren(Folder folder, CancellationToken cancellationToken) => folder switch
    {
        Series series => (
                await OrganiseChildSeasons(series, cancellationToken).ConfigureAwait(false)
                ).Concat(OrganiseChildEpisodes(series, cancellationToken)),
        Season season => OrganiseChildEpisodes(season, cancellationToken),
        _ => throw new ArgumentOutOfRangeException(nameof(folder), folder, "Unrecognized show folder type")
    };

    private async Task<IEnumerable<Episode?>> OrganiseChildSeasons(Folder folder, CancellationToken cancellationToken) =>
        await folder.Children
            .OfType<Season>().Where(season => Directory.Exists(season.Path))
            .SelectManyAsync(season => OrganiseFolder(season, cancellationToken))
            .ConfigureAwait(false);

    private IEnumerable<Episode?> OrganiseChildEpisodes(Folder folder, CancellationToken cancellationToken)
    {
        var episodes = folder.Children
            .OfType<Episode>().Where(item => File.Exists(item.Path))
            .Select(episode => OrganiseEpisode(episode, cancellationToken) ? episode : null);

        var parentDirectory = FileHandler.Format(folder);
        var parentName = folder switch
        {
            Series series => series.Name,
            Season season => $"{season.Series.Name}: {season.Name}",
            _ => string.Empty
        };

        _ = FileHandler.MoveExtras(
            folder.GetExtras().ToArray(),
            parentDirectory,
            parentName,
            folder.GetBaseItemKind(),
            cancellationToken) > 0;

        return episodes;
    }

    private bool OrganiseEpisode(Episode episode, CancellationToken cancellationToken)
    {
        if (episode.Series is null)
        {
            Logger.LogWarning(
                "Cannot process episode: it has not been assigned to a series | {Episode} ",
                episode.Path);
            return false;
        }

        var newEpisodePath = FileHandler.Format(episode);
        return FileHandler.MoveItem(episode, newEpisodePath, cancellationToken);
    }
}
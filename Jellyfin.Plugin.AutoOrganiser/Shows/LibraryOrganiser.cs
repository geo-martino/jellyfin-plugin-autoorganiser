using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.AutoOrganiser.Core;
using Jellyfin.Plugin.AutoOrganiser.Core.Library;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AutoOrganiser.Shows;

/// <inheritdoc />
public class LibraryOrganiser : LibraryOrganiser<Episode, FileNameGenerator, FilePathGenerator>
{
    /// <inheritdoc />
    public LibraryOrganiser(
        ILibraryManager libraryManager,
        ILogger<ILibraryOrganiser<Episode, FileNameGenerator, FilePathGenerator>> logger) : base(libraryManager, logger)
    {
    }

    private IEnumerable<Series> GetShowsFromLibrary()
    {
        var shows = LibraryManager.GetItemList(new InternalItemsQuery
        {
            IncludeItemTypes = [BaseItemKind.Series],
            IsVirtualItem = false,
            OrderBy = new List<(ItemSortBy, SortOrder)>
            {
                new(ItemSortBy.SortName, SortOrder.Ascending)
            },
            Recursive = true
        }).OfType<Series>().Where(series => Directory.Exists(series.Path));

        return shows;
    }

    /// <inheritdoc />
    public override async void Organise(
        FileNameGenerator nameGenerator,
        FilePathGenerator pathGenerator,
        bool dryRun,
        ProgressHandler progressHandler,
        CancellationToken cancellationToken)
    {
        var shows = GetShowsFromLibrary().ToList();
        Logger.LogInformation("Found {N} shows to organise", shows.Count);

        progressHandler.SetProgressToInitial();
        var tasks = shows
            .Select((series, idx) => progressHandler.Progress(idx, shows.Count, series))
            .SelectMany(series => OrganiseShow(series, pathGenerator, nameGenerator, dryRun, cancellationToken))
            .Where(task => task is not null)
            .OfType<Task>()
            .ToList();
        progressHandler.SetProgressToFinal();

        var logPrefix = dryRun ? "DRY RUN | Updating" : "Updating";
        Logger.LogInformation("{Prefix:l} metadata on {N} moved items", logPrefix, tasks.Count);
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    private IEnumerable<Task?> OrganiseShow(
        Series series,
        FilePathGenerator pathGenerator,
        FileNameGenerator nameGenerator,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        var newPath = pathGenerator.GeneratePath(series, nameGenerator);
        yield return MoveItem(series, newPath, dryRun, cancellationToken);

        var tasks = series.GetRecursiveChildren()
            .Where(item => item.GetBaseItemKind() == BaseItemKind.Season)
            .OfType<Season>()
            .Where(season => Directory.Exists(season.Path))
            .SelectMany(season => OrganiseSeason(
                    season, pathGenerator, nameGenerator, dryRun, cancellationToken));

        foreach (var task in tasks)
        {
            yield return task;
        }
    }

    private IEnumerable<Task?> OrganiseSeason(
        Season season,
        FilePathGenerator pathGenerator,
        FileNameGenerator nameGenerator,
        bool dryRun,
        CancellationToken cancellationToken)
    {
        var newPath = pathGenerator.GeneratePath(season, nameGenerator);
        yield return MoveItem(season, newPath, dryRun, cancellationToken);

        var tasks = season
            .GetRecursiveChildren()
            .Where(item => item.GetBaseItemKind() == BaseItemKind.Episode)
            .OfType<Episode>()
            .Where(episode => File.Exists(episode.Path))
            .Select(episode =>
            {
                var newEpisodePath = pathGenerator.GeneratePath(episode, nameGenerator);
                return MoveItem(episode, newEpisodePath, dryRun, cancellationToken);
            });

        foreach (var task in tasks)
        {
            yield return task;
        }
    }
}
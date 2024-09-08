using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.AutoOrganiser.Core.Generators;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;

namespace Jellyfin.Plugin.AutoOrganiser.Shows;

/// <inheritdoc />
public class FileNameGenerator : FileNameGenerator<Episode>
{
    private readonly bool _addEpisodeName;

    /// <inheritdoc cref="FileNameGenerator{T}(LabelGenerator)" />
    /// <param name="labelGenerator">The object which handles label generation for the item.</param>
    /// <param name="addEpisodeName">Whether to add the episode name to the file name.</param>
    public FileNameGenerator(LabelGenerator labelGenerator, bool addEpisodeName) : base(labelGenerator)
    {
        _addEpisodeName = addEpisodeName;
    }

    /// <inheritdoc />
    public override string GetFileName(Episode item)
    {
        var fileName = SanitiseValue(item.Series.Name);
        fileName = AppendIdentifier(item, fileName);
        fileName = AppendEpisodeName(item, fileName);
        fileName = LabelGenerator.AppendLabel(item, fileName);

        return $"{fileName}{Path.GetExtension(item.Path).ToLowerInvariant()}";
    }

    private string AppendIdentifier(Episode episode, string fileName)
    {
        var seasonIndex = GetSeasonIndex(episode.Season);
        var episodeIndex = GetEpisodeIndex(episode);

        return $"{fileName} S{seasonIndex}E{episodeIndex}";
    }

    /// <summary>
    /// Returns a padded season index number.
    /// </summary>
    /// <param name="season">The season for which to get an index number.</param>
    /// <returns>The padded season index number.</returns>
    public string GetSeasonIndex(Season season)
    {
        var seriesSeasons = season.Series
            .GetRecursiveChildren()
            .Where(item => item.GetBaseItemKind() == BaseItemKind.Season);
        var seasonIndex = PadIndexNumber(season.IndexNumber, seriesSeasons);

        return seasonIndex;
    }

    /// <summary>
    /// Returns a padded episode index number.
    /// </summary>
    /// <param name="episode">The episode for which to get an index number.</param>
    /// <returns>The padded episode index number.</returns>
    // ReSharper disable once MemberCanBePrivate.Global
    public string GetEpisodeIndex(Episode episode)
    {
        var seasonEpisodes = episode.Season
            .GetRecursiveChildren()
            .Where(item => item.GetBaseItemKind() == BaseItemKind.Episode);
        var episodeIndex = PadIndexNumber(episode.IndexNumber, seasonEpisodes);

        return episodeIndex;
    }

    private string PadIndexNumber(int? index, IEnumerable<BaseItem> items)
    {
        var padMin = 2;
        var indexNumber = index ?? 0;

        var pad = items
            .Max(item => item.IndexNumber ?? indexNumber)
            .ToString(CultureInfo.InvariantCulture).Length;
        var indexPadded = indexNumber
            .ToString(CultureInfo.InvariantCulture)
            .PadLeft(Math.Max(pad, padMin), '0');

        return indexPadded;
    }

    private string AppendEpisodeName(BaseItem item, string fileName)
    {
        if (_addEpisodeName)
        {
            fileName += $" - {item.Name}";
        }

        return fileName;
    }
}
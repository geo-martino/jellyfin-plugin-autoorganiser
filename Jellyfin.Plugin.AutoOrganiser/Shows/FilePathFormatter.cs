using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.AutoOrganiser.Core.Formatters;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;

namespace Jellyfin.Plugin.AutoOrganiser.Shows;

/// <inheritdoc />
public class FilePathFormatter : FilePathFormatter<Episode, Season>
{
    private readonly bool _addEpisodeName;

    /// <inheritdoc cref="FilePathFormatter{Episode,Season}(LabelFormatter)" />
    /// <param name="addEpisodeName">Whether to add the episode name to the file name.</param>
#pragma warning disable CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
    public FilePathFormatter(LabelFormatter labelFormatter, bool addEpisodeName) : base(labelFormatter)
#pragma warning restore CS1573 // Parameter has no matching param tag in the XML comment (but other parameters do)
    {
        _addEpisodeName = addEpisodeName;
    }

    /// <inheritdoc />
    public override string Format(Episode item)
    {
        var parentPath = item.Season is not null ? Format(item.Season)
            : FormatSeasonPath(item.Series, GetSeasonIndex(item));

        var fileName = SanitiseValue(item.Series.Name);
        fileName = AppendIdentifier(item, fileName);
        fileName = AppendEpisodeName(item, fileName);
        fileName = LabelFormatter.AppendLabel(item, fileName);
        fileName = AppendExtension(item, fileName);

        return Path.Combine(parentPath, fileName);
    }

    /// <inheritdoc />
    public override string Format(Season item) => FormatSeasonPath(item.Series, GetSeasonIndex(item));

    /// <inheritdoc cref="Format(Season)"/>
    public string Format(Series item)
    {
        var parentPath = item.GetTopParent().Path;
        var seriesName = SanitiseValue(item.Name);

        var year = item.PremiereDate?.Year;
        if (year is not null)
        {
            seriesName += $" ({year})";
        }

        return Path.Combine(parentPath, seriesName);
    }

    private string FormatSeasonPath(Series series, string seasonIndex) => Path
        .Combine(Format(series), $"Season {seasonIndex}");

    private string AppendIdentifier(Episode episode, string fileName) => string
        .Format(
            CultureInfo.InvariantCulture,
            "{0} S{1}E{2}",
            fileName,
            episode.Season is not null ? GetSeasonIndex(episode.Season) : GetSeasonIndex(episode),
            GetEpisodeIndex(episode));

    private string GetSeasonIndex(Season season)
    {
        var seriesSeasons = season.Series
            .GetRecursiveChildren()
            .Where(item => item.GetBaseItemKind() == BaseItemKind.Season);

        return PadIndexNumber(season.IndexNumber, seriesSeasons);
    }

    private string GetSeasonIndex(Episode episode)
    {
        var seriesSeasons = episode.Series
            .GetRecursiveChildren()
            .Where(item => item.GetBaseItemKind() == BaseItemKind.Season);

        return PadIndexNumber(episode.ParentIndexNumber, seriesSeasons);
    }

    private string GetEpisodeIndex(Episode episode)
    {
        var episodes = episode.Season
            .GetRecursiveChildren()
            .Where(item => item.GetBaseItemKind() == BaseItemKind.Episode).ToArray();

        if (episodes.Length == 0)
        {
            episodes = episode.Series
                .GetRecursiveChildren()
                .Where(item => item.GetBaseItemKind() == BaseItemKind.Episode).ToArray();
        }

        var episodeIndex = PadIndexNumber(episode.IndexNumber, episodes);

        return episodeIndex;
    }

    private string PadIndexNumber(int? index, IEnumerable<BaseItem> items)
    {
        var padMin = 2;
        var indexNumber = index ?? 0;

        var pad = items.DefaultIfEmpty()
            .Max(item => item?.IndexNumber ?? indexNumber)
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
            fileName += $" - {SanitiseValue(item.Name)}";
        }

        return fileName;
    }
}

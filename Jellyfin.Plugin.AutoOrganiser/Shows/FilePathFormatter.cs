using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Jellyfin.Plugin.AutoOrganiser.Core.Formatters;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;

namespace Jellyfin.Plugin.AutoOrganiser.Shows;

/// <inheritdoc />
public class FilePathFormatter : FilePathFormatter<Episode>
{
    private readonly bool _addEpisodeName;

    /// <inheritdoc cref="FilePathFormatter{Episode}(LabelFormatter)" />
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
    public override string Format(Folder folder) => folder switch
    {
        Series series => Format(series),
        Season season => Format(season),
        _ => throw new ArgumentOutOfRangeException(nameof(folder), folder, "Unrecognized show folder type")
    };

    /// <inheritdoc cref="Format(Folder)"/>
    // ReSharper disable once MemberCanBePrivate.Global
    public string Format(Season item) => FormatSeasonPath(item.Series, GetSeasonIndex(item));

    /// <inheritdoc cref="Format(Folder)"/>
    public string Format(Series item)
    {
        var parentPath = item.GetTopParent().Path;
        var seriesName = SanitiseValue(item.Name);
        seriesName = AppendYear(item, seriesName);

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

    private string GetSeasonIndex(Season season) =>
        PadIndexNumber(season.IndexNumber, season.Series.Children.OfType<Season>());

    private string GetSeasonIndex(Episode episode) =>
        PadIndexNumber(episode.ParentIndexNumber, episode.Series.Children.OfType<Season>());

    private string GetEpisodeIndex(Episode episode) => episode.Season switch
    {
        null => PadIndexNumber(episode.IndexNumber),
        _ => PadIndexNumber(episode.IndexNumber, episode.Season.Children.OfType<Episode>())
    };

    private string PadIndexNumber(int? index, IEnumerable<BaseItem>? items = null)
    {
        var padMin = 2;
        var indexNumber = index ?? 0;

        var pad = (items ?? []).DefaultIfEmpty()
            .Max(item => item?.IndexNumber ?? indexNumber)
            .ToString(CultureInfo.InvariantCulture).Length;
        var indexPadded = indexNumber
            .ToString(CultureInfo.InvariantCulture)
            .PadLeft(Math.Max(pad, padMin), '0');

        return indexPadded;
    }

    private string AppendEpisodeName(BaseItem item, string fileName) =>
        !_addEpisodeName ? fileName : $"{fileName} - {SanitiseValue(item.Name)}";
}

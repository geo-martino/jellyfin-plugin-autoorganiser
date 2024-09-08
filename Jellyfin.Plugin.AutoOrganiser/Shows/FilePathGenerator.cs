using System.IO;
using Jellyfin.Plugin.AutoOrganiser.Core.Generators;
using MediaBrowser.Controller.Entities.TV;

namespace Jellyfin.Plugin.AutoOrganiser.Shows;

/// <summary>
/// Generates a path from the given configuration for a show and its child files based on their metadata.
/// </summary>
public class FilePathGenerator : IFilePathGenerator<Episode, FileNameGenerator>
{
    /// <summary>
    /// Generates a file path for a given series based on its metadata.
    /// </summary>
    /// <param name="item">The series item to generate a path for.</param>
    /// <param name="nameGenerator">The generator to use to generate an episode's file name.</param>
    /// <returns>The file path for the series.</returns>
    public string GeneratePath(Series item, FileNameGenerator nameGenerator)
    {
        var parentPath = item.GetTopParent().Path;
        var seriesName = nameGenerator.SanitiseValue(item.Name);

        var year = item.PremiereDate?.Year;
        if (year is not null)
        {
            seriesName += $" ({year})";
        }

        return Path.Combine(parentPath, seriesName);
    }

    /// <summary>
    /// Generates a file path for a given season based on its metadata.
    /// </summary>
    /// <param name="item">The season item to generate a path for.</param>
    /// <param name="nameGenerator">The generator to use to generate an episode's file name.</param>
    /// <returns>The file path for the season.</returns>
    public string GeneratePath(Season item, FileNameGenerator nameGenerator)
    {
        var parentPath = GeneratePath(item.Series, nameGenerator);
        var seasonIndex = nameGenerator.GetSeasonIndex(item);

        return Path.Combine(parentPath, $"Season {seasonIndex}");
    }

    /// <inheritdoc />
    public string GeneratePath(Episode item, FileNameGenerator nameGenerator)
    {
        var parentPath = GeneratePath(item.Season, nameGenerator);
        var fileName = nameGenerator.GetFileName(item);

        return Path.Combine(parentPath, fileName);
    }
}

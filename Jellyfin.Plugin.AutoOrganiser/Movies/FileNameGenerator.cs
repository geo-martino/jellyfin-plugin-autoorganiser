using System.IO;
using Jellyfin.Plugin.AutoOrganiser.Core.Generators;
using MediaBrowser.Controller.Entities.Movies;

namespace Jellyfin.Plugin.AutoOrganiser.Movies;

/// <inheritdoc />
public class FileNameGenerator : FileNameGenerator<Movie>
{
    /// <inheritdoc />
    public FileNameGenerator(LabelGenerator labelGenerator) : base(labelGenerator)
    {
    }

    /// <inheritdoc />
    public override string GetFileName(Movie item)
    {
        var fileName = SanitiseValue(item.Name);
        fileName = AppendYear(item, fileName);
        fileName = LabelGenerator.AppendLabel(item, fileName);

        return $"{fileName}{Path.GetExtension(item.Path).ToLowerInvariant()}";
    }
}
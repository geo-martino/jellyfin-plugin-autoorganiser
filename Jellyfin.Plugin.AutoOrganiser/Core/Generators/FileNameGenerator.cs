using System.Linq;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.AutoOrganiser.Core.Generators;

/// <inheritdoc />
public abstract class FileNameGenerator<T> : IFileNameGenerator<T>
    where T : BaseItem
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileNameGenerator{T}"/> class.
    /// </summary>
    /// <param name="labelGenerator">The object which handles label generation for the item.</param>
    protected FileNameGenerator(LabelGenerator labelGenerator)
    {
        LabelGenerator = labelGenerator;
    }

    /// <summary>
    /// Gets the object which handles label generation for the item.
    /// </summary>
    protected LabelGenerator LabelGenerator { get; }

    /// <inheritdoc />
    public abstract string GetFileName(T item);

    /// <summary>
    /// Sanitises a file/directory name ensuring it does not contain any invalid characters.
    /// </summary>
    /// <param name="value">The value to be sanitised.</param>
    /// <returns>The sanitised value.</returns>
    public string SanitiseValue(string value)
    {
        var invalidChars = "\\/:*?\"<>|".ToArray();
        return string.Join("_", value.Split(invalidChars));
    }

    /// <summary>
    /// Adds the year as a suffix to the given file name for a given item.
    /// </summary>
    /// <param name="item">The item to generate a label for.</param>
    /// <param name="fileName">The file name to enrich.</param>
    /// <returns>The enriched file name.</returns>
    public string AppendYear(BaseItem item, string fileName)
    {
        var year = item.PremiereDate?.Year;
        if (year is not null)
        {
            fileName += $" ({year})";
        }

        return fileName;
    }
}
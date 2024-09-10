using System.IO;
using System.Linq;
using MediaBrowser.Controller.Entities;

namespace Jellyfin.Plugin.AutoOrganiser.Core.Formatters;

/// <summary>
/// Handles fil path formatting for an item.
/// </summary>
/// <typeparam name="TItem">The <see cref="BaseItem"/> type that this formatter can process.</typeparam>
/// <typeparam name="TFolder">The <see cref="Folder"/> type that can contain many <typeparamref name="TItem"/> types.</typeparam>
public abstract class FilePathFormatter<TItem, TFolder> : IFormatter<TItem>
    where TItem : BaseItem
    where TFolder : Folder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FilePathFormatter{TItem,TFolder}"/> class.
    /// </summary>
    /// <param name="labelFormatter">The object which handles label formatting for the item.</param>
    protected FilePathFormatter(LabelFormatter labelFormatter)
    {
        LabelFormatter = labelFormatter;
    }

    /// <summary>
    /// Gets the object which handles label formatting for the item.
    /// </summary>
    protected LabelFormatter LabelFormatter { get; }

    /// <summary>
    /// Formats a file path for the given item based on its metadata.
    /// </summary>
    /// <param name="item">The item to format a file path for.</param>
    /// <returns>The file path.</returns>
    public abstract string Format(TItem item);

    /// <summary>
    /// Formats a file path for the given folder based on its metadata.
    /// </summary>
    /// <param name="item">The folder to format a file path for.</param>
    /// <returns>The file path.</returns>
    public abstract string Format(TFolder item);

    /// <summary>
    /// Sanitises a file/directory name ensuring it does not contain any invalid characters.
    /// </summary>
    /// <param name="value">The value to be sanitised.</param>
    /// <returns>The sanitised value.</returns>
    internal string SanitiseValue(string value) => string.Join("_", value.Split("\\/:*?\"<>|".ToArray()));

    /// <summary>
    /// Adds the year as a suffix to the given file name for a given item.
    /// </summary>
    /// <param name="item">The item extract the year from.</param>
    /// <param name="fileName">The file name to enrich.</param>
    /// <returns>The enriched file name.</returns>
    protected string AppendYear(BaseItem item, string fileName)
    {
        var year = item.PremiereDate?.Year;
        if (year is not null)
        {
            fileName += $" ({year})";
        }

        return fileName;
    }

    /// <summary>
    /// Adds back the original extension to the given file name from a given item.
    /// </summary>
    /// <param name="item">The item extract the original file extension from.</param>
    /// <param name="fileName">The file name to enrich.</param>
    /// <returns>The enriched file name.</returns>
    protected internal string AppendExtension(BaseItem item, string fileName) => fileName + Path
        .GetExtension(item.Path).ToLowerInvariant();
}
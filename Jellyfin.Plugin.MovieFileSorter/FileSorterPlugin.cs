using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.Plugin.MovieFileSorter.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.MovieFileSorter;

/// <summary>
/// The main plugin.
/// </summary>
public class FileSorterPlugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FileSorterPlugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    public FileSorterPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static FileSorterPlugin? Instance { get; private set; }

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("5D5FAFD8-886C-442C-ADE9-7B5E39F908BD");

    /// <inheritdoc />
    public override string Name => "Movie File Sorter";

    /// <inheritdoc />
    public override string Description => "Renames and sorts movie files.";

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return new[]
        {
            new PluginPageInfo
            {
                Name = this.Name,
                EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.configPage.html", GetType().Namespace)
            }
        };
    }
}

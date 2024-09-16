using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.Plugin.AutoOrganiser.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.AutoOrganiser;

/// <summary>
/// The main plugin.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class AutoOrganiserPlugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AutoOrganiserPlugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    public AutoOrganiserPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
    }

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static AutoOrganiserPlugin? Instance { get; private set; }

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("5D5FAFD8-886C-442C-ADE9-7B5E39F908BD");

    /// <inheritdoc />
    public override string Name => "Auto-Organiser";

    /// <inheritdoc />
    public override string Description => "Automatically organise and rename files.";

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return
        [
            new PluginPageInfo
            {
                Name = this.Name,
                EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.configPage.html", GetType().Namespace)
            }
        ];
    }
}

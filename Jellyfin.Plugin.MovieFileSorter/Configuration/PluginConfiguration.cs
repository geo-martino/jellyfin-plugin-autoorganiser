using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.MovieFileSorter.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
    /// </summary>
    public PluginConfiguration()
    {
        // set default options here
        LabelResolution = false;
        LabelCodec = false;
        LabelBitDepth = false;
        LabelDynamicRange = false;
        ForceSubFolder = false;
    }

    /// <summary>
    /// Gets or sets a value indicating whether to add the video resolution on the file name's label.
    /// </summary>
    public bool LabelResolution { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to add the video codec on the file name's label.
    /// </summary>
    public bool LabelCodec { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to add the video bit depth on the file name's label.
    /// </summary>
    public bool LabelBitDepth { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to add the video range on the file name's label.
    /// </summary>
    public bool LabelDynamicRange { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to always put movie files in sub-folder.
    /// </summary>
    public bool ForceSubFolder { get; set; }
}

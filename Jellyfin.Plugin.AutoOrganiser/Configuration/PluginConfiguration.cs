using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.AutoOrganiser.Configuration;

/// <summary>
/// Plugin configuration.
/// </summary>
// ReSharper disable once ClassNeverInstantiated.Global
public class PluginConfiguration : BasePluginConfiguration
{
    /// <summary>
    /// Gets or sets a value indicating whether to execute as a dry run.
    /// </summary>
    public bool DryRun { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to overwrite any files that exist at the new path when moving files/directories.
    /// </summary>
    public bool Overwrite { get; set; } = false;

    /// <summary>
    /// Gets or sets the extensions to ignore when removing empty directories in a library.
    /// </summary>
    public string CleanIgnoreExtensions { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether to add the video resolution on the file name's label.
    /// </summary>
    public bool LabelResolution { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to add the video codec on the file name's label.
    /// </summary>
    public bool LabelCodec { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to add the video bit depth on the file name's label.
    /// </summary>
    public bool LabelBitDepth { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to add the video range on the file name's label.
    /// </summary>
    public bool LabelDynamicRange { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to always put movie files in sub-folder.
    /// </summary>
    public bool ForceSubFolder { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether to add the episode name to the file name.
    /// </summary>
    public bool EpisodeName { get; set; } = true;
}

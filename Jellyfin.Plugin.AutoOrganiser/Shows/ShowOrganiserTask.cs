using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AutoOrganiser.Core;
using Jellyfin.Plugin.AutoOrganiser.Core.Formatters;
using Jellyfin.Plugin.AutoOrganiser.Core.Library;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AutoOrganiser.Shows;

/// <inheritdoc />
public class ShowOrganiserTask : AutoOrganiserTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly IDirectoryService _directoryService;
    private readonly IServerConfigurationManager _serverConfig;

    private readonly ILogger<LibraryOrganiser> _loggerOrganiser;
    private readonly ILogger<FileHandler> _loggerFileHandler;
    private readonly ILogger<LibraryCleaner> _loggerCleaner;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShowOrganiserTask"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="directoryService">Instance of the <see cref="IDirectoryService"/> interface.</param>
    /// <param name="serverConfig">Instance of the <see cref="IServerConfigurationManager"/>.</param>
    /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
    public ShowOrganiserTask(
        ILibraryManager libraryManager,
        IDirectoryService directoryService,
        IServerConfigurationManager serverConfig,
        ILoggerFactory loggerFactory)
    {
        _libraryManager = libraryManager;
        _directoryService = directoryService;
        _serverConfig = serverConfig;

        _loggerOrganiser = loggerFactory.CreateLogger<LibraryOrganiser>();
        _loggerFileHandler = loggerFactory.CreateLogger<FileHandler>();
        _loggerCleaner = loggerFactory.CreateLogger<LibraryCleaner>();
    }

    /// <inheritdoc />
    public override string Name => "Organise show files";

    /// <inheritdoc />
    public override string Key => "OrganiseShowFiles";

    /// <inheritdoc />
    public override string Description => "Organises and renames show files according to recommended structure.";

    /// <inheritdoc />
    public override async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        progress.Report(0);
        ArgumentNullException.ThrowIfNull(AutoOrganiserPlugin.Instance?.Configuration);

        var dryRun = AutoOrganiserPlugin.Instance.Configuration.DryRun;
        var overwrite = AutoOrganiserPlugin.Instance.Configuration.Overwrite;
        var cleanIgnoreExtensions = AutoOrganiserPlugin.Instance.Configuration.CleanIgnoreExtensions
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.TrimStart('.').ToLowerInvariant())
            .ToArray();

        var addLabelResolution = AutoOrganiserPlugin.Instance.Configuration.LabelResolution;
        var addLabelCodec = AutoOrganiserPlugin.Instance.Configuration.LabelCodec;
        var addLabelBitDepth = AutoOrganiserPlugin.Instance.Configuration.LabelBitDepth;
        var addLabelDynamicRange = AutoOrganiserPlugin.Instance.Configuration.LabelDynamicRange;

        var addEpisodeName = AutoOrganiserPlugin.Instance.Configuration.EpisodeName;

        var labelFormatter = new LabelFormatter(
            addLabelResolution, addLabelCodec, addLabelBitDepth, addLabelDynamicRange);
        var pathFormatter = new FilePathFormatter(labelFormatter, addEpisodeName);
        var fileHandler = new FileHandler(pathFormatter, dryRun, overwrite, _loggerFileHandler);
        var progressHandler = new ProgressHandler(progress, 5, 95);

        var libraryOrganiser = new LibraryOrganiser(
            _libraryManager, _directoryService, _serverConfig, fileHandler, dryRun, _loggerOrganiser);

        await libraryOrganiser.Organise(progressHandler, cancellationToken).ConfigureAwait(false);
        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        progressHandler.SetProgressToFinal();

        var libraryCleaner = new LibraryCleaner(_libraryManager, _loggerCleaner);
        libraryCleaner.CleanLibrary(CollectionTypeOptions.tvshows, cleanIgnoreExtensions, dryRun);

        progress.Report(100);
    }
}
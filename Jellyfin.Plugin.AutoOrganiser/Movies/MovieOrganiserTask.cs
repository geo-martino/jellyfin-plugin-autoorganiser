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

namespace Jellyfin.Plugin.AutoOrganiser.Movies;

/// <inheritdoc />
public class MovieOrganiserTask : AutoOrganiserTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly IDirectoryService _directoryService;
    private readonly IServerConfigurationManager _serverConfig;

    private readonly ILogger<LibraryOrganiser> _loggerOrganiser;
    private readonly ILogger<FileHandler> _loggerFileHandler;
    private readonly ILogger<LibraryCleaner> _loggerCleaner;

    /// <summary>
    /// Initializes a new instance of the <see cref="MovieOrganiserTask"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="directoryService">Instance of the <see cref="IDirectoryService"/> interface.</param>
    /// <param name="serverConfig">Instance of the <see cref="IServerConfigurationManager"/>.</param>
    /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
    public MovieOrganiserTask(
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
    public override string Name => "Organise movie files";

    /// <inheritdoc />
    public override string Key => "OrganiseMovieFiles";

    /// <inheritdoc />
    public override string Description => "Organises and renames movie files according to recommended structure.";

    /// <inheritdoc />
    public override async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(AutoOrganiserPlugin.Instance?.Configuration);
        progress.Report(0);

        var dryRun = AutoOrganiserPlugin.Instance.Configuration.DryRun;
        var overwrite = AutoOrganiserPlugin.Instance.Configuration.Overwrite;
        var cleanIgnoreExtensions = AutoOrganiserPlugin.Instance.Configuration.CleanIgnoreExtensions
            .SplitArguments().ToArray();

        var addLabelResolution = AutoOrganiserPlugin.Instance.Configuration.LabelResolution;
        var addLabelCodec = AutoOrganiserPlugin.Instance.Configuration.LabelCodec;
        var addLabelBitDepth = AutoOrganiserPlugin.Instance.Configuration.LabelBitDepth;
        var addLabelDynamicRange = AutoOrganiserPlugin.Instance.Configuration.LabelDynamicRange;

        var forceSubFolder = AutoOrganiserPlugin.Instance.Configuration.ForceSubFolder;

        var labelFormatter = new LabelFormatter(
            addLabelResolution, addLabelCodec, addLabelBitDepth, addLabelDynamicRange);
        var pathFormatter = new FilePathFormatter(labelFormatter, forceSubFolder);
        var fileHandler = new FileHandler(pathFormatter, dryRun, overwrite, _loggerFileHandler);
        var progressHandler = new ProgressHandler(progress, 5, 95);

        var libraryOrganiser = new LibraryOrganiser(
            _libraryManager, _directoryService, _serverConfig, fileHandler, dryRun, _loggerOrganiser);

        await libraryOrganiser.Organise(progressHandler, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();

        var libraryCleaner = new LibraryCleaner(_libraryManager, _loggerCleaner);
        libraryCleaner.CleanLibrary(CollectionTypeOptions.movies, cleanIgnoreExtensions, dryRun);

        progress.Report(100);
    }
}

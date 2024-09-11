using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AutoOrganiser.Core;
using Jellyfin.Plugin.AutoOrganiser.Core.Formatters;
using Jellyfin.Plugin.AutoOrganiser.Core.Library;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AutoOrganiser.Movies;

/// <inheritdoc />
public class MovieOrganiserTask : AutoOrganiserTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<LibraryOrganiser> _loggerOrganiser;
    private readonly ILogger<ItemHandler> _loggerItemHandler;
    private readonly ILogger<LibraryCleaner> _loggerCleaner;

    /// <summary>
    /// Initializes a new instance of the <see cref="MovieOrganiserTask"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="loggerOrganiser">Instance of the <see cref="ILogger{ShowOrganiserTask}"/> interface.</param>
    /// <param name="loggerItemHandler">Instance of the <see cref="ItemHandler"/>.</param>
    /// <param name="loggerCleaner">Instance of the <see cref="ILogger{LibraryCleaner}"/> interface.</param>
    public MovieOrganiserTask(
        ILibraryManager libraryManager,
        ILogger<LibraryOrganiser> loggerOrganiser,
        ILogger<ItemHandler> loggerItemHandler,
        ILogger<LibraryCleaner> loggerCleaner)
    {
        _libraryManager = libraryManager;
        _loggerOrganiser = loggerOrganiser;
        _loggerItemHandler = loggerItemHandler;
        _loggerCleaner = loggerCleaner;
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
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.TrimStart('.').ToLowerInvariant())
            .ToArray();

        var addLabelResolution = AutoOrganiserPlugin.Instance.Configuration.LabelResolution;
        var addLabelCodec = AutoOrganiserPlugin.Instance.Configuration.LabelCodec;
        var addLabelBitDepth = AutoOrganiserPlugin.Instance.Configuration.LabelBitDepth;
        var addLabelDynamicRange = AutoOrganiserPlugin.Instance.Configuration.LabelDynamicRange;

        var forceSubFolder = AutoOrganiserPlugin.Instance.Configuration.ForceSubFolder;

        var labelGenerator = new LabelFormatter(
            addLabelResolution, addLabelCodec, addLabelBitDepth, addLabelDynamicRange);
        var pathGenerator = new FilePathFormatter(labelGenerator, forceSubFolder);
        var itemHandler = new ItemHandler(pathGenerator, dryRun, overwrite, _loggerItemHandler);
        var progressHandler = new ProgressHandler(progress, 5, 95);

        var libraryOrganiser = new LibraryOrganiser(itemHandler, _libraryManager, _loggerOrganiser);
        var libraryCleaner = new LibraryCleaner(_libraryManager, _loggerCleaner);

        await libraryOrganiser.Organise(progressHandler, cancellationToken).ConfigureAwait(false);
        libraryCleaner.CleanLibrary(CollectionTypeOptions.movies, cleanIgnoreExtensions, dryRun);

        progress.Report(100);
    }
}
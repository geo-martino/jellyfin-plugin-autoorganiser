using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AutoOrganiser.Core;
using Jellyfin.Plugin.AutoOrganiser.Core.Generators;
using Jellyfin.Plugin.AutoOrganiser.Core.Library;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AutoOrganiser.Movies;

/// <inheritdoc />
public class MovieOrganiserTask : AutoOrganiserTask
{
    private readonly LibraryOrganiser _libraryOrganiser;
    private readonly LibraryCleaner _libraryCleaner;

    /// <summary>
    /// Initializes a new instance of the <see cref="MovieOrganiserTask"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="loggerOrganiser">Instance of the <see cref="ILogger{LibraryOrganiser}"/> interface.</param>
    /// <param name="loggerCleaner">Instance of the <see cref="ILogger{LibraryCleaner}"/> interface.</param>
    public MovieOrganiserTask(
        ILibraryManager libraryManager,
        ILogger<LibraryOrganiser> loggerOrganiser,
        ILogger<LibraryCleaner> loggerCleaner)
    {
        _libraryOrganiser = new LibraryOrganiser(libraryManager, loggerOrganiser);
        _libraryCleaner = new LibraryCleaner(libraryManager, loggerCleaner);
    }

    /// <inheritdoc />
    public override string Name => "Organise movie files";

    /// <inheritdoc />
    public override string Key => "OrganiseMovieFiles";

    /// <inheritdoc />
    public override string Description => "Organises and renames movie files according to recommended structure.";

    /// <inheritdoc />
    public override Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(AutoOrganiserPlugin.Instance?.Configuration);
        progress.Report(0);

        var dryRun = AutoOrganiserPlugin.Instance.Configuration.DryRun;
        var cleanIgnoreExtensions = AutoOrganiserPlugin.Instance.Configuration.CleanIgnoreExtensions
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.TrimStart('.').ToLowerInvariant())
            .ToList();

        var addLabelResolution = AutoOrganiserPlugin.Instance.Configuration.LabelResolution;
        var addLabelCodec = AutoOrganiserPlugin.Instance.Configuration.LabelCodec;
        var addLabelBitDepth = AutoOrganiserPlugin.Instance.Configuration.LabelBitDepth;
        var addLabelDynamicRange = AutoOrganiserPlugin.Instance.Configuration.LabelDynamicRange;

        var forceSubFolder = AutoOrganiserPlugin.Instance.Configuration.ForceSubFolder;

        var progressHandler = new ProgressHandler(progress, 5, 95);
        var labelGenerator = new LabelGenerator(
            addLabelResolution, addLabelCodec, addLabelBitDepth, addLabelDynamicRange);
        var nameGenerator = new FileNameGenerator(labelGenerator);
        var pathGenerator = new FilePathGenerator(forceSubFolder);

        _libraryOrganiser.Organise(nameGenerator, pathGenerator, dryRun, progressHandler, cancellationToken);
        _libraryCleaner.CleanLibrary(CollectionTypeOptions.movies, cleanIgnoreExtensions, dryRun);

        progress.Report(100);
        return Task.CompletedTask;
    }
}
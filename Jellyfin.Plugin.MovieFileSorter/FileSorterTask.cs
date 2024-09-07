using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.MovieFileSorter;

/// <summary>
/// File sorter task.
/// </summary>
public class FileSorterTask : IScheduledTask, IConfigurableScheduledTask
{
    private readonly MovieLibraryOrganiser _movieLibraryOrganiser;
    private readonly ILogger<FileSorterTask> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSorterTask"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="movieLogger">Instance of the <see cref="ILogger"/> interface of type <see cref="MovieLibraryOrganiser"/>.</param>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface of type <see cref="FileSorterTask"/>.</param>
    public FileSorterTask(
        ILibraryManager libraryManager,
        ILogger<MovieLibraryOrganiser> movieLogger,
        ILogger<FileSorterTask> logger)
    {
        _movieLibraryOrganiser = new MovieLibraryOrganiser(libraryManager, movieLogger);
        _logger = logger;
    }

    /// <inheritdoc />
    public bool IsHidden => false;

    /// <inheritdoc />
    public bool IsEnabled => true;

    /// <inheritdoc />
    public bool IsLogged => true;

    /// <inheritdoc />
    public string Name => "Sort movie files";

    /// <inheritdoc />
    public string Key => "SortMovieFiles";

    /// <inheritdoc />
    public string Description => "Renames and sorts movie files according to recommended structure.";

    /// <inheritdoc />
    public string Category => "File Sorter";

    /// <inheritdoc />
    public Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(FileSorterPlugin.Instance?.Configuration);

        var cleanIgnoreExtensions = FileSorterPlugin.Instance.Configuration.CleanIgnoreExtensions
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.TrimStart('.').ToLowerInvariant())
            .ToList();

        var forceSubFolder = FileSorterPlugin.Instance.Configuration.ForceSubFolder;

        var addLabelResolution = FileSorterPlugin.Instance.Configuration.LabelResolution;
        var addLabelCodec = FileSorterPlugin.Instance.Configuration.LabelCodec;
        var addLabelBitDepth = FileSorterPlugin.Instance.Configuration.LabelBitDepth;
        var addLabelDynamicRange = FileSorterPlugin.Instance.Configuration.LabelDynamicRange;

        var filePathGenerator = new MovieFilePathGenerator(forceSubFolder);
        var fileNameGenerator = new MovieFileNameGenerator(
            addLabelResolution, addLabelCodec, addLabelBitDepth, addLabelDynamicRange);

        _movieLibraryOrganiser.OrganiseMovies(filePathGenerator, fileNameGenerator, progress, cancellationToken);
        _movieLibraryOrganiser.CleanLibrary(cleanIgnoreExtensions);

        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
    {
        return Enumerable.Empty<TaskTriggerInfo>();
    }
}
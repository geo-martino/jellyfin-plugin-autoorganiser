using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AutoOrganiser.Core;
using Jellyfin.Plugin.AutoOrganiser.Core.Library;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AutoOrganiser.Shows;

/// <inheritdoc />
public class ShowCleanerTask : AutoOrganiserTask
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<LibraryCleaner> _loggerCleaner;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShowCleanerTask"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="loggerFactory">Instance of the <see cref="ILoggerFactory"/> interface.</param>
    public ShowCleanerTask(ILibraryManager libraryManager, ILoggerFactory loggerFactory)
    {
        _libraryManager = libraryManager;
        _loggerCleaner = loggerFactory.CreateLogger<LibraryCleaner>();
    }

    /// <inheritdoc />
    public override string Category => "Maintenance";

    /// <inheritdoc />
    public override string Name => "Clean Show Library Directories";

    /// <inheritdoc />
    public override string Key => "CleanShowLibraryDirectories";

    /// <inheritdoc />
    public override string Description =>
        "Cleans up show library directories by deleting unwanted files and empty directories.";

    /// <inheritdoc />
    public override Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(AutoOrganiserPlugin.Instance?.Configuration);
        progress.Report(0);

        var dryRun = AutoOrganiserPlugin.Instance.Configuration.DryRun;
        var cleanIgnoreExtensions = AutoOrganiserPlugin.Instance.Configuration.CleanIgnoreExtensions
            .SplitArguments().ToArray();

        var libraryCleaner = new LibraryCleaner(_libraryManager, _loggerCleaner);
        libraryCleaner.CleanLibrary(CollectionTypeOptions.tvshows, cleanIgnoreExtensions, dryRun);

        progress.Report(100);
        return Task.CompletedTask;
    }
}

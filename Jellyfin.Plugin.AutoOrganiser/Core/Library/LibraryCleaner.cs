using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AutoOrganiser.Core.Library;

/// <summary>
/// Cleans the library structure by removing unwanted files and/or directories.
/// </summary>
public class LibraryCleaner
{
    private readonly ILibraryManager _libraryManager;
    private readonly ILogger<LibraryCleaner> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="LibraryCleaner"/> class.
    /// </summary>
    /// <param name="libraryManager">Instance of the <see cref="ILibraryManager"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="ILogger{LibraryCleaner}"/> interface.</param>
    public LibraryCleaner(
        ILibraryManager libraryManager,
        ILogger<LibraryCleaner> logger)
    {
        _libraryManager = libraryManager;
        _logger = logger;
    }

    /// <summary>
    /// Removes empty directories recursively for all folders of every 'Shows' library.
    /// </summary>
    /// <param name="kind">The kind of library to clean.</param>
    /// <param name="ignoreExtensions">Ignore the following extensions when checking files in a directory.</param>
    /// <param name="dryRun">Whether to execute as a dry run, which does not modify any files.</param>
    public void CleanLibrary(CollectionTypeOptions kind, IReadOnlyCollection<string> ignoreExtensions, bool dryRun)
    {
        _logger.LogInformation(
            "Cleaning library of empty folders. Case-insensitively ignoring extensions: {0:l}",
            string.Join(", ", ignoreExtensions));

        var parentDirectories = _libraryManager.GetVirtualFolders()
            .Where(virtualFolder => virtualFolder.CollectionType == kind)
            .SelectMany(virtualFolder => virtualFolder.Locations);

        foreach (var parentDirectory in parentDirectories)
        {
            _logger.LogInformation("Cleaning folder: {0}", parentDirectory);

            foreach (var directory in Directory.EnumerateDirectories(parentDirectory))
            {
                RemoveEmptyDirectories(directory, ignoreExtensions, dryRun);
            }
        }
    }

    private void RemoveEmptyDirectories(string directory, IReadOnlyCollection<string> ignoreExtensions, bool dryRun)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        foreach (var dir in Directory.EnumerateDirectories(directory))
        {
            RemoveEmptyDirectories(dir, ignoreExtensions, dryRun);
        }

        var files = GetFilesInDirectory(directory, ignoreExtensions).ToArray();
        if (files.Length > 0 || Directory.GetDirectories(directory).Length > 0)
        {
            return;
        }

        var logPrefix = dryRun ? "DRY RUN | " : string.Empty;

        foreach (var file in Directory.EnumerateFiles(directory))
        {
            _logger.LogInformation("{Prefix:l}Deleting file {Dir}", logPrefix, file);
            if (!dryRun)
            {
                File.Delete(file);
            }
        }

        _logger.LogInformation("{Prefix:l}Deleting directory {Dir}", logPrefix, directory);
        if (!dryRun)
        {
            Directory.Delete(directory, false);
        }
    }

    private IEnumerable<string> GetFilesInDirectory(
        string directory, IReadOnlyCollection<string> ignoreExtensions) => Directory
        .EnumerateFiles(directory)
        .Where(file => !ignoreExtensions.Contains(Path.GetExtension(file).TrimStart('.').ToLowerInvariant()));
}
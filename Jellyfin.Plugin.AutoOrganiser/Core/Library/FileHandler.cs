using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.AutoOrganiser.Core.Formatters;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AutoOrganiser.Core.Library;

/// <summary>
/// Handles movement of files on the system drive from a given library.
/// </summary>
/// <typeparam name="TItem">The <see cref="BaseItem"/> type that this formatter can process.</typeparam>
/// <typeparam name="TPathFormatter">Type of the <see cref="FilePathFormatter{TItem}"/> interface capable of processing <typeparamref name="TItem"/>.</typeparam>
public class FileHandler<TItem, TPathFormatter>
    where TItem : BaseItem
    where TPathFormatter : FilePathFormatter<TItem>
{
    /// <summary>
    /// The file name to give to extras of theme type.
    /// </summary>
    private const string ThemeExtrasFileName = "theme";

    /// <summary>
    /// The folder name to give to extras of unknown type.
    /// </summary>
    private const string UnknownExtrasFolderName = "extras";

    /// <summary>
    /// Initializes a new instance of the <see cref="FileHandler{TItem,TPathFormatter}"/> class.
    /// </summary>
    /// <param name="pathFormatter">Instance of the <see cref="FilePathFormatter{TItem}"/>.</param>
    /// <param name="dryRun">Whether to execute as a dry run, which does not modify any files.</param>
    /// <param name="overwrite">Whether to Overwrite any files that exist at the new path.</param>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    protected FileHandler(
        TPathFormatter pathFormatter,
        bool dryRun,
        bool overwrite,
        ILogger<FileHandler<TItem, TPathFormatter>> logger)
    {
        PathFormatter = pathFormatter;

        DryRun = dryRun;
        Overwrite = overwrite;

        Logger = logger;
    }

    /// <summary>
    /// Gets the instance of the <see cref="FilePathFormatter{TItem}"/> interface.
    /// </summary>
    protected TPathFormatter PathFormatter { get; }

    /// <summary>
    /// Gets a value indicating whether to execute as a dry run.
    /// </summary>
    private bool DryRun { get; }

    /// <summary>
    /// Gets a value indicating whether to Overwrite any files that exist at the new path when moving files/directories.
    /// </summary>
    private bool Overwrite { get; }

    /// <summary>
    /// Gets the logger.
    /// </summary>
    protected ILogger<FileHandler<TItem, TPathFormatter>> Logger { get; }

    /// <summary>
    /// Gets the log prefix to use if executing as a dry run.
    /// </summary>
    /// <returns>The log prefix.</returns>
    protected internal string GetLogPrefix() => DryRun ? "DRY RUN | " : string.Empty;

    /// <inheritdoc cref="FilePathFormatter{TItem}.Format(Folder)"/>
    public string Format(Folder item)
    {
        if (!IsFormatPossible(item))
        {
            return item.Path;
        }

        try
        {
            return PathFormatter.Format(item);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Could not format a new path for folder: {Name} - {Path}", item.Name, item.Path);
            throw;
        }
    }

    /// <inheritdoc cref="FilePathFormatter{TItem}.Format(TItem)"/>
    public string Format(TItem item)
    {
        if (!IsFormatPossible(item))
        {
            return item.Path;
        }

        try
        {
            return PathFormatter.Format(item);
        }
        catch (Exception e)
        {
            Logger.LogError(e, "Could not format a new path for item: {Name} - {Path}", item.Name, item.Path);
            throw;
        }
    }

    /// <summary>
    /// Validates that a new path can be generated for the given item. Logs any potential issues.
    /// </summary>
    /// <param name="item">The item to format a file path for.</param>
    /// <returns>Whether a new path can be formatted for the item.</returns>
    protected bool IsFormatPossible(BaseItem item)
    {
        if (item.GetTopParent() is not null)
        {
            return true;
        }

        Logger.LogWarning(
            "Could not format a new path for folder as it does not have a top parent: {Name} - {Path}",
            item.Name,
            item.Path);
        return false;
    }

    private void LogMove(string oldPath, string newPath, string itemKind, bool overwrite) => Logger.LogInformation(
        "{Prefix:l}{Operation:l} {Kind:l}:\n\t>> {Old}\n\tto {New}",
        GetLogPrefix(),
        overwrite ? "Overwriting" : "Moving",
        itemKind,
        oldPath,
        newPath);

    /// <summary>
    /// Move the item to a new path on the file system and set the new path to the item.
    /// If dry run is enabled, ignores checking for the existence of the source item
    /// and just simply logs the proposed move.
    /// </summary>
    /// <param name="item">The item to move.</param>
    /// <param name="newPath">The path to move the item to.</param>
    /// <param name="cancellationToken">Instance of the <see cref="CancellationToken"/>.</param>
    /// <returns>Bool indicating whether the item was moved.</returns>
    public bool MoveItem(
        BaseItem item, string newPath, CancellationToken cancellationToken) =>
        MoveItem(item, newPath, item.GetBaseItemKind().ToString().ToLowerInvariant(), cancellationToken);

    private bool MoveItem<T>(
        T item, string newPath, string itemKind, CancellationToken cancellationToken)
        where T : BaseItem
    {
        cancellationToken.ThrowIfCancellationRequested();

        var moved = item.Path switch
        {
            _ when Directory.Exists(item.Path) => MoveDirectoryRecursively(item.Path, newPath, itemKind),
            _ when File.Exists(item.Path) => MoveFile(item.Path, newPath, itemKind),
            _ => false
        };

        if (moved)
        {
            item.Path = newPath;
        }

        return moved;
    }

    /// <summary>
    /// Moves the directory from the source directory to the target directory recursively.
    /// If dry run is enabled, ignores checking for the existence of the source directory
    /// and just simply logs the proposed move.
    /// </summary>
    /// <param name="sourceDir">The directory to move.</param>
    /// <param name="targetDir">The directory to move the source directory to.</param>
    /// <param name="kind">The type of the directory being moved.</param>
    /// <returns>True if the directory was moved, false if not.</returns>
    public bool MoveDirectoryRecursively(string sourceDir, string targetDir, string kind)
    {
        if (sourceDir.Equals(targetDir, StringComparison.Ordinal))
        {
            return false;
        }

        if (!DryRun && !Directory.Exists(sourceDir))
        {
            Logger.LogWarning("Cannot move directory. Source directory does not exist at path: {Path}", sourceDir);
            return false;
        }

        if (!Directory.Exists(targetDir))
        {
            return MoveDirectory(sourceDir, targetDir, $"{kind} folder");
        }

        var moved = false;
        foreach (var oldDirSub in Directory.EnumerateDirectories(sourceDir))
        {
            var newDirSub = Path.Combine(targetDir, new DirectoryInfo(oldDirSub).Name);
            moved |= MoveDirectoryRecursively(oldDirSub, newDirSub, kind);
        }

        foreach (var oldFile in Directory.EnumerateFiles(sourceDir))
        {
            var newFile = Path.Combine(targetDir, Path.GetFileName(oldFile));
            moved |= MoveFile(oldFile, newFile, $"{kind} file");
        }

        return moved;
    }

    /// <summary>
    /// Moves the directory from the source directory to the target directory.
    /// If dry run is enabled, ignores checking for the existence of the source directory
    /// and just simply logs the proposed move.
    /// </summary>
    /// <param name="sourceDir">The directory to move.</param>
    /// <param name="targetDir">The directory to move the source directory to.</param>
    /// <param name="kind">The type of the directory being moved.</param>
    /// <returns>True if the directory was moved, false if not.</returns>
    // ReSharper disable once MemberCanBePrivate.Global
    public bool MoveDirectory(string sourceDir, string targetDir, string kind)
    {
        if (sourceDir.Equals(targetDir, StringComparison.Ordinal))
        {
            return false;
        }

        if (!DryRun && !Directory.Exists(sourceDir))
        {
            Logger.LogWarning("Cannot move directory. Source directory does not exist at path: {Path}", sourceDir);
            return false;
        }

        if (File.Exists(targetDir))
        {
            Logger.LogWarning("Cannot move directory. File exists at path: {Path}", targetDir);
            return false;
        }

        if (!Overwrite && Directory.Exists(targetDir))
        {
            Logger.LogWarning("Cannot move directory. Directory already exists at path: {Path}", targetDir);
            return false;
        }

        LogMove(sourceDir, targetDir, kind, false);
        if (DryRun)
        {
            return true;
        }

        CreateParentDirectory(targetDir);
        try
        {
            Directory.Move(sourceDir, targetDir);
            return true;
        }
        catch (UnauthorizedAccessException e)
        {
            Logger.LogError(e, "Insufficient permissions to write to {Path}", targetDir);
        }

        return false;
    }

    /// <summary>
    /// Moves the file from the source path to the target path.
    /// If dry run is enabled, ignores checking for the existence of the source file
    /// and just simply logs the proposed move.
    /// </summary>
    /// <param name="sourcePath">The file to move.</param>
    /// <param name="targetPath">The path to move the source file to.</param>
    /// <param name="kind">The type of the file being moved.</param>
    /// <returns>True if the path was moved, false if not.</returns>
    // ReSharper disable once MemberCanBePrivate.Global
    public bool MoveFile(string sourcePath, string targetPath, string kind)
    {
        if (sourcePath.Equals(targetPath, StringComparison.Ordinal))
        {
            return false;
        }

        if (!DryRun && !File.Exists(sourcePath))
        {
            Logger.LogWarning("Cannot move directory. Source directory does not exist at path: {Path}", sourcePath);
            return false;
        }

        if (Directory.Exists(targetPath))
        {
            Logger.LogWarning("Cannot move file. Directory exists at path: {Path}", targetPath);
            return false;
        }

        if (File.Exists(targetPath) && Overwrite)
        {
            LogMove(sourcePath, targetPath, kind, true);
            if (!DryRun)
            {
                try
                {
                    File.Delete(targetPath);
                }
                catch (UnauthorizedAccessException e)
                {
                    Logger.LogError(e, "Insufficient permissions to overwrite {Path}", targetPath);
                    return false;
                }
            }
        }
        else if (!File.Exists(targetPath))
        {
            LogMove(sourcePath, targetPath, kind, false);
        }

        if (File.Exists(targetPath))
        {
            if (!DryRun)
            {
                Logger.LogWarning("Cannot move file. File already exists at path: {Path}", targetPath);
            }

            return false;
        }

        if (DryRun)
        {
            return true;
        }

        CreateParentDirectory(targetPath);
        try
        {
            File.Move(sourcePath, targetPath);
        }
        catch (UnauthorizedAccessException e)
        {
            Logger.LogError(e, "Insufficient permissions to move {OldPath} -> {NewPath}", sourcePath, targetPath);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Copies the directory from the source directory to the target directory recursively.
    /// If dry run is enabled, ignores checking for the existence of the source directory
    /// and just simply logs the proposed move.
    /// </summary>
    /// <param name="sourceDir">The directory to copy.</param>
    /// <param name="targetDir">The directory to copy the source directory to.</param>
    /// <param name="kind">The type of the directory being copied.</param>
    public void CopyFilesRecursively(string sourceDir, string targetDir, string kind)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(dirPath.Replace(sourceDir, targetDir, StringComparison.Ordinal));
        }

        foreach (var newPath in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
        {
            File.Copy(
                newPath,
                newPath.Replace(sourceDir, targetDir, StringComparison.Ordinal),
                true);
        }
    }

    /// <summary>
    /// Move the extras to a new path on the system and update each extra's path reference in Jellyfin.
    /// If dry run is enabled, ignores checking for the existence of the source extras
    /// and just simply logs the proposed moves.
    /// </summary>
    /// <param name="extras">The extras to move.</param>
    /// <param name="parentDirectory">The parent directory within which to place the extras.</param>
    /// <param name="parentName">The parent name. Used for logging only.</param>
    /// <param name="parentKind">The parent kind. Used for logging only.</param>
    /// <param name="cancellationToken">Instance of the <see cref="CancellationToken"/>.</param>
    /// <returns>The number of extras moved.</returns>
    public int MoveExtras(
        IReadOnlyCollection<BaseItem> extras,
        string parentDirectory,
        string parentName,
        BaseItemKind parentKind,
        CancellationToken cancellationToken) => extras
            .Where(extra => File.Exists(extra.Path))
            .Select(extra => extra.ExtraType)
            .OfType<ExtraType>()
            .ToHashSet()
            .Sum(extraType =>
                MoveExtrasByType(extras, extraType, parentDirectory, parentName, parentKind, cancellationToken));

    private int MoveExtrasByType(
        IReadOnlyCollection<BaseItem> extras,
        ExtraType extraType,
        string parentDirectory,
        string parentName,
        BaseItemKind parentKind,
        CancellationToken cancellationToken)
    {
        var extrasFiltered = extras
            .Where(extra => File.Exists(extra.Path) && extra.ExtraType == extraType)
            .ToArray();

        var uniqueNames = extrasFiltered.Select(extra => extra.Name).ToHashSet();
        var useFileName = uniqueNames.Count < extrasFiltered.Length;
        if (useFileName)
        {
            Logger.LogWarning(
                "Extras names are not unique for extra type {ExtraType}. " +
                "Using original file names instead for extras of {Item}",
                extraType.ToString(),
                parentName);
        }

        return extrasFiltered.Sum(extra =>
        {
            var isUnique = extrasFiltered.Count(e => e.ExtraType == extra.ExtraType) > 1;
            return MoveExtra(extra, parentDirectory, parentKind, isUnique, useFileName, cancellationToken) ? 1 : 0;
        });
    }

    private bool MoveExtra(
        BaseItem extra,
        string parentDirectory,
        BaseItemKind parentKind,
        bool isUnique,
        bool useFileName,
        CancellationToken cancellationToken)
    {
        var extraKind = extra.ExtraType.ToString()!.ToLowerSentenceCase().TrimEnd('s') + 's';
        var folderName = extra.ExtraType switch
        {
            ExtraType.ThemeSong => null,
            ExtraType.Trailer or ExtraType.Sample when isUnique => null,
            ExtraType.Unknown or ExtraType.ThemeVideo => UnknownExtrasFolderName,
            _ => extraKind
        };
        if (folderName != null)
        {
            parentDirectory = Path.Combine(parentDirectory, folderName);
        }

        var fileName = extra.ExtraType switch
        {
            ExtraType.ThemeSong => ThemeExtrasFileName,
            ExtraType.Trailer or ExtraType.Sample when isUnique => extra.ExtraType.ToString()!.ToLowerInvariant(),
            _ when useFileName => PathFormatter.SanitiseValue(extra.FileNameWithoutExtension),
            _ => PathFormatter.SanitiseValue(extra.Name)
        };
        fileName = PathFormatter.AppendExtension(extra, fileName);

        var newPath = Path.Combine(parentDirectory, fileName);
        var itemKind = $"{parentKind.ToString().ToLowerInvariant()} {extraKind}";
        return MoveItem(extra, newPath, itemKind, cancellationToken);
    }

    /// <summary>
    /// Creates the parent directory of the given path.
    /// </summary>
    /// <param name="path">The path to create the parent directory for.</param>
    // ReSharper disable once MemberCanBePrivate.Global
    public void CreateParentDirectory(string path)
    {
        var dirPath = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dirPath) && !DryRun)
        {
            Directory.CreateDirectory(dirPath);
        }
    }
}
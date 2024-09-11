using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.AutoOrganiser.Core.Formatters;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.AutoOrganiser.Core.Library;

/// <summary>
/// Handles movement of files on the system drive from a given library.
/// </summary>
/// <typeparam name="TItem">The <see cref="BaseItem"/> type that this formatter can process.</typeparam>
/// <typeparam name="TFolder">The <see cref="Folder"/> type that can contain many <typeparamref name="TItem"/> types.</typeparam>
/// <typeparam name="TPathFormatter">Type of the <see cref="FilePathFormatter{TItem,TFolder}"/> interface capable of processing <typeparamref name="TItem"/>.</typeparam>
public class ItemHandler<TItem, TFolder, TPathFormatter>
    where TItem : BaseItem
    where TFolder : Folder
    where TPathFormatter : FilePathFormatter<TItem, TFolder>
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
    /// Initializes a new instance of the <see cref="ItemHandler{TItem,TFolder,TPathFormatter}"/> class.
    /// </summary>
    /// <param name="pathFormatter">Instance of the <see cref="FilePathFormatter{TItem,TFolder}"/> interface.</param>
    /// <param name="dryRun">Whether to execute as a dry run, which does not modify any files.</param>
    /// <param name="overwrite">Whether to Overwrite any files that exist at the new path.</param>
    /// <param name="logger">Instance of the <see cref="ILogger"/> interface.</param>
    protected ItemHandler(
        TPathFormatter pathFormatter,
        bool dryRun,
        bool overwrite,
        ILogger<ItemHandler<TItem, TFolder, TPathFormatter>> logger)
    {
        PathFormatter = pathFormatter;

        DryRun = dryRun;
        Overwrite = overwrite;

        Logger = logger;
    }

    /// <summary>
    /// Gets the instance of the <see cref="FilePathFormatter{TItem,TFolder}"/> interface.
    /// </summary>
    public TPathFormatter PathFormatter { get; }

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
    private ILogger<ItemHandler<TItem, TFolder, TPathFormatter>> Logger { get; }

    private void LogMove(string oldPath, string newPath, string itemKind, bool overwrite)
    {
        var operation = overwrite ? "Overwriting" : "Moving";
        var logPrefix = DryRun ? $"DRY RUN | {operation}" : operation;
        Logger.LogInformation("{Prefix:l} {Kind:l}:\n\t>> {Old}\n\t<< {New}", logPrefix, itemKind, oldPath, newPath);
    }

    /// <summary>
    /// Move the item to a new path on the system and update the item's path reference in Jellyfin.
    /// </summary>
    /// <param name="item">The item to move.</param>
    /// <param name="newPath">The path to move the item to.</param>
    /// <param name="cancellationToken">Instance of the <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="Task"/> to be executed to update the path metadata.</returns>
    public Task? MoveItem(BaseItem item, string newPath, CancellationToken cancellationToken)
    {
        var itemKind = item.GetBaseItemKind().ToString().ToLowerInvariant();
        var moved = MoveItem(item.Path, newPath, itemKind);
        return moved ? UpdatePathMetadata(item, newPath, cancellationToken) : null;
    }

    private bool MoveItem(string oldPath, string newPath, string itemKind)
    {
        var moved = false;

        if (Directory.Exists(oldPath))
        {
            moved = MoveDirectoryRecursively(oldPath, newPath, itemKind);
        }
        else if (File.Exists(oldPath))
        {
            moved = MoveFile(oldPath, newPath, itemKind);
        }

        return moved;
    }

    private bool MoveDirectoryRecursively(string oldDir, string newDir, string itemKind)
    {
        if (!Directory.Exists(newDir))
        {
            return MoveDirectory(oldDir, newDir, $"{itemKind} folder");
        }

        var moved = false;
        foreach (var oldDirSub in Directory.EnumerateDirectories(oldDir))
        {
            var newDirSub = Path.Combine(newDir, new DirectoryInfo(oldDirSub).Name);
            moved |= MoveDirectoryRecursively(oldDirSub, newDirSub, itemKind);
        }

        foreach (var oldFile in Directory.EnumerateFiles(oldDir))
        {
            var newFile = Path.Combine(newDir, Path.GetFileName(oldFile));
            moved |= MoveFile(oldFile, newFile, $"{itemKind} file");
        }

        return moved;
    }

    private bool MoveDirectory(string oldDir, string newDir, string itemKind)
    {
        if (oldDir == newDir)
        {
            return false;
        }

        if (File.Exists(newDir))
        {
            Logger.LogWarning("Cannot not move directory. File exists at path: {Path}", newDir);
        }

        if (!Overwrite && (Directory.Exists(newDir) || File.Exists(newDir)))
        {
            Logger.LogWarning("Cannot not move directory. Directory already exists at path: {Path}", newDir);
            return false;
        }

        LogMove(oldDir, newDir, itemKind, false);
        if (DryRun)
        {
            return true;
        }

        CreateParentDirectory(newDir);
        try
        {
            Directory.Move(oldDir, newDir);
            return true;
        }
        catch (UnauthorizedAccessException)
        {
            Logger.LogError("Insufficient permissions to write to {Path}", newDir);
        }

        return false;
    }

    private bool MoveFile(string oldPath, string newPath, string itemKind)
    {
        if (oldPath == newPath)
        {
            return false;
        }

        if (Directory.Exists(newPath))
        {
            Logger.LogWarning("Cannot not move file. Directory exists at path: {Path}", newPath);
            return false;
        }

        if (File.Exists(newPath) && Overwrite)
        {
            LogMove(oldPath, newPath, itemKind, true);
            if (!DryRun)
            {
                try
                {
                    File.Delete(newPath);
                }
                catch (UnauthorizedAccessException)
                {
                    Logger.LogError("Insufficient permissions to Overwrite {Path}", newPath);
                    return false;
                }
            }
        }
        else if (!File.Exists(newPath))
        {
            LogMove(oldPath, newPath, itemKind, false);
        }

        if (!File.Exists(newPath))
        {
            if (DryRun)
            {
                return true;
            }

            CreateParentDirectory(newPath);
            try
            {
                File.Move(oldPath, newPath);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                Logger.LogError("Insufficient permissions to move {OldPath} -> {NewPath}", oldPath, newPath);
            }
        }
        else if (!DryRun)
        {
            Logger.LogWarning("Cannot not move file. File already exists at path: {Path}", newPath);
        }

        return false;
    }

    /// <summary>
    /// Move the extras to a new path on the system and update each extra's path reference in Jellyfin.
    /// </summary>
    /// <param name="extras">The extras to move.</param>
    /// <param name="cancellationToken">Instance of the <see cref="CancellationToken"/>.</param>
    /// <param name="parent">The parent item to place the extras into.</param>
    /// <returns>The <see cref="Task"/>s to be executed to update the path metadata.</returns>
    public IEnumerable<Task?> MoveExtras(
        IReadOnlyCollection<BaseItem?> extras,
        CancellationToken cancellationToken,
        TFolder? parent = null) => extras
            .Where(extra => extra is not null && File.Exists(extra.Path))
            .OfType<BaseItem>()
            .Select(extra =>
            {
                var unique = extras.Count(e => e is not null && e.ExtraType == extra.ExtraType) > 1;
                return MoveExtra(extra, unique, cancellationToken, parent);
            });

    private Task? MoveExtra(BaseItem extra, bool unique, CancellationToken cancellationToken, TFolder? parent = null)
    {
        string? parentPath = null;
        if (parent != null)
        {
            parentPath = PathFormatter.Format(parent);
        }
        else if (extra is TItem item)
        {
            parentPath = Path.GetDirectoryName(PathFormatter.Format(item));
        }

        if (parentPath == null)
        {
            return null;
        }

        var itemKind = string.Concat(extra.ExtraType.ToString()!.Select(CharToSnakeCase))
            .ToLowerInvariant().TrimEnd('s') + 's';
        var folderName = extra.ExtraType switch
        {
            ExtraType.ThemeSong => null,
            ExtraType.Trailer or ExtraType.Sample when unique => null,
            ExtraType.Unknown or ExtraType.ThemeVideo => UnknownExtrasFolderName,
            _ => itemKind
        };
        if (folderName != null)
        {
            parentPath = Path.Combine(parentPath, folderName);
        }

        var fileName = extra.ExtraType switch
        {
            ExtraType.ThemeSong => ThemeExtrasFileName,
            ExtraType.Trailer or ExtraType.Sample when unique => extra.ExtraType.ToString()!.ToLowerInvariant(),
            _ => PathFormatter.SanitiseValue(extra.Name)
        };
        fileName = PathFormatter.AppendExtension(extra, fileName);

        var newPath = Path.Combine(parentPath, fileName);
        var moved = MoveItem(extra.Path, newPath, itemKind);
        return moved ? UpdatePathMetadata(extra, newPath, cancellationToken) : null;
    }

    private void CreateParentDirectory(string path)
    {
        var dirPath = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dirPath) && !DryRun)
        {
            Directory.CreateDirectory(dirPath);
        }
    }

    private string CharToSnakeCase(char c, int index) => index > 0 && char
        .IsUpper(c) ? "_" + c.ToString().ToLowerInvariant() : c.ToString();

    /// <summary>
    /// Update the given item's path reference in Jellyfin.
    /// </summary>
    /// <param name="item">The item to move.</param>
    /// <param name="newPath">The path to move the item to.</param>
    /// <param name="cancellationToken">Instance of the <see cref="CancellationToken"/>.</param>
    /// <returns>The <see cref="Task"/> to be executed to update the path metadata.</returns>
    public Task? UpdatePathMetadata(
        BaseItem item,
        string newPath,
        CancellationToken cancellationToken)
    {
        if (DryRun)
        {
            return Task.CompletedTask;
        }

        item.Path = newPath;
        return item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken);
    }

    /// <summary>
    /// Log and run the given 'update metadata' tasks.
    /// </summary>
    /// <param name="tasks">The tasks to run.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task RunUpdateMetadataTasks(Task[] tasks)
    {
        if (tasks is { Length: 0 })
        {
            Logger.LogInformation("No items updated.");
            return;
        }

        var logPrefix = DryRun ? "DRY RUN | " : string.Empty;
        Logger.LogInformation("{Prefix:l}Updating metadata on {N} moved items", logPrefix, tasks.Length);
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}
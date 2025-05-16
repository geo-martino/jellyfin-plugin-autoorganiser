using System;

namespace Jellyfin.Plugin.AutoOrganiser.Core;

/// <summary>
/// Handles organising of files in a given library.
/// </summary>
public class ProgressHandler
{
    private readonly IProgress<double> _progress;
    private readonly double _initial;
    private readonly double _final;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProgressHandler"/> class.
    /// </summary>
    /// <param name="progress">Instance of the <see cref="IProgress{T}"/> interface.</param>
    /// <param name="initial">The initial progress to display.</param>
    /// <param name="final">The final progress to display.</param>
    protected internal ProgressHandler(
        IProgress<double> progress,
        double initial = 0.0,
        double final = 100.0)
    {
        _progress = progress;
        _initial = initial;
        _final = final;
    }

    /// <summary>
    /// Updates the progress bar.
    /// </summary>
    /// <param name="index">The index of the current item.</param>
    /// <param name="total">The total number of items.</param>
    public void Progress(int index, int total)
    {
        var percentageModifier = _final - _initial;
        var progressPercentage = (index / (double)total) * percentageModifier;
        _progress.Report(_initial + progressPercentage);
    }

    /// <summary>
    /// Sets the progress to the initial value.
    /// </summary>
    public void SetProgressToInitial() => _progress.Report(_initial);

    /// <summary>
    /// Sets the progress to the initial value.
    /// </summary>
    public void SetProgressToFinal() => _progress.Report(_final);
}
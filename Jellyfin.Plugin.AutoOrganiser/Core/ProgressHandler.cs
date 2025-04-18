using System;

namespace Jellyfin.Plugin.AutoOrganiser.Core;

/// <summary>
/// Handles organising of files in a given library.
/// </summary>
public class ProgressHandler
{
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
        Progress = progress;
        _initial = initial;
        _final = final;
    }

    /// <summary>
    /// Gets the stored instance of the <see cref="IProgress{T}"/> interface.
    /// </summary>
    private IProgress<double> Progress { get; }

    /// <summary>
    /// Updates the progress bar.
    /// </summary>
    /// <param name="index">The index of the current item.</param>
    /// <param name="total">The total number of items.</param>
    /// <param name="obj">Object to return.</param>
    /// <typeparam name="TO">The object given to be returned. This may be of any type.</typeparam>
    /// <returns>The given `obj`.</returns>
    public TO Report<TO>(int index, int total, TO obj)
    {
        var percentageModifier = _final - _initial;
        var progressPercentage = index / (double)total * percentageModifier;
        Progress.Report(_initial + progressPercentage);

        return obj;
    }

    /// <summary>
    /// Sets the progress to the initial value.
    /// </summary>
    public void SetProgressToInitial() => Progress.Report(_initial);

    /// <summary>
    /// Sets the progress to the initial value.
    /// </summary>
    public void SetProgressToFinal() => Progress.Report(_final);
}
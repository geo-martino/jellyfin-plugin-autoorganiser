using System.Collections.Generic;
using System.IO;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.MovieFileSorter;

/// <summary>
/// Generates a file name from the given configuration for a movie based on its metadata.
/// </summary>
public class MovieFileNameGenerator
{
    private readonly bool _addLabelResolution;
    private readonly bool _addLabelCodec;
    private readonly bool _addLabelBitDepth;
    private readonly bool _addLabelDynamicRange;

    private readonly Dictionary<int, int> _widthHeightMap4X3;
    private readonly Dictionary<int, int> _widthHeightMap16X9;

    /// <summary>
    /// Initializes a new instance of the <see cref="MovieFileNameGenerator"/> class.
    /// </summary>
    /// <param name="addLabelResolution">Whether to the video resolution as part of the file name's label.</param>
    /// <param name="addLabelCodec">Whether to the video codec as part of the file name's label.</param>
    /// <param name="addLabelBitDepth">Whether to the video bit depth as part of the file name's label.</param>
    /// <param name="addLabelDynamicRange">Whether to the video dynamic range as part of the file name's label.</param>
    public MovieFileNameGenerator(
        bool addLabelResolution,
        bool addLabelCodec,
        bool addLabelBitDepth,
        bool addLabelDynamicRange)
    {
        _addLabelResolution = addLabelResolution;
        _addLabelCodec = addLabelCodec;
        _addLabelBitDepth = addLabelBitDepth;
        _addLabelDynamicRange = addLabelDynamicRange;

        _widthHeightMap4X3 = new Dictionary<int, int>
        {
            { 320, 240 },
            { 640, 480 },
            { 720, 576 },
            { 800, 600 },
            { 960, 720 },
            { 1024, 768 },
            { 1152, 864 },
            { 1280, 960 },
            { 1400, 1050 },
            { 1456, 1080 },
            { 1660, 1200 },
            { 1856, 1392 },
            { 1920, 1440 },
            { 2048, 1536 },
            { 2560, 1920 },
            { 2880, 2160 },
            { 3072, 2304 },
            { 3200, 2400 },
            { 3840, 2880 },
            { 4096, 3072 },
            { 5120, 3840 },
            { 6144, 4608 },
            { 6400, 4800 },
            { 7680, 5760 },
            { 8192, 6144 },
        };

        _widthHeightMap16X9 = new Dictionary<int, int>
        {
            { 640, 360 },
            { 854, 480 },
            { 896, 504 },
            { 960, 540 },
            { 1024, 576 },
            { 1280, 720 },
            { 1366, 768 },
            { 1600, 900 },
            { 1920, 1080 },
            { 2048, 1152 },
            { 2560, 1440 },
            { 3200, 1800 },
            { 3840, 2160 },
            { 5120, 2880 },
            { 7680, 4320 },
            { 15360, 8640 },
            { 30720, 17280 },
            { 61440, 34560 },
            { 122880, 69120 },
        };
    }

    /// <summary>
    /// Generates a file name for a given movie based on its metadata.
    /// </summary>
    /// <param name="movie">Instance of the <see cref="Movie"/> interface.</param>
    /// <returns>The file name for the movie.</returns>
    public string GetFileName(Movie movie)
    {
        var fileName = SanitiseValue(movie.Name);
        fileName = AppendYear(movie, fileName);
        fileName = AppendLabel(movie, fileName);

        return $"{fileName}{Path.GetExtension(movie.Path)}";
    }

    /// <summary>
    /// Sanitises a file/directory name ensuring it does not contain any invalid characters.
    /// </summary>
    /// <param name="value">The value to be sanitised.</param>
    /// <returns>The sanitised value.</returns>
    public string SanitiseValue(string value)
    {
        return string.Join("_", value.Split(Path.GetInvalidFileNameChars()));
    }

    private string AppendYear(Movie movie, string fileName)
    {
        var year = movie.PremiereDate?.Year;
        if (year is not null)
        {
            fileName += $" ({year})";
        }

        return fileName;
    }

    private string AppendLabel(Movie movie, string fileName)
    {
        var label = GetLabel(movie);
        if (label is not null)
        {
            fileName += $" - [{label}]";
        }

        return fileName;
    }

    private string? GetLabel(Movie movie)
    {
        var labelParts = new List<string>();
        if (_addLabelResolution)
        {
            labelParts.Add(GetResolution(movie));
        }

        if (_addLabelCodec)
        {
            labelParts.Add(GetCodec(movie));
        }

        if (_addLabelBitDepth)
        {
            labelParts.Add(GetBitDepth(movie));
        }

        if (_addLabelDynamicRange)
        {
            labelParts.Add(GetDynamicRange(movie));
        }

        return labelParts.Count > 0 ? string.Join(' ', labelParts) : null;
    }

    private static MediaStream GetVideoStream(Movie movie)
    {
        return movie.GetMediaStreams()
            .Find(s => s.Type == MediaStreamType.Video)!;
    }

    private string GetResolution(Movie movie)
    {
        var isSquare = movie.AspectRatio == "4:3" || (double)movie.Width / movie.Height < 1.35;
        var widthHeightMap = isSquare ? _widthHeightMap4X3 : _widthHeightMap16X9;

        var stream = GetVideoStream(movie);
        var height = CalculateHeight(movie.Width, movie.Height, widthHeightMap);
        var type = stream.IsInterlaced ? "i" : "p";

        return $"{height}{type}";
    }

    private static int CalculateHeight(int width, int height, Dictionary<int, int> widthHeightMap)
    {
        foreach (var widthHeightPair in widthHeightMap)
        {
            if (width <= widthHeightPair.Key)
            {
                height = widthHeightPair.Value;
                break;
            }
        }

        return height;
    }

    private static string GetCodec(Movie movie)
    {
        return GetVideoStream(movie).Codec.ToUpperInvariant();
    }

    private static string GetBitDepth(Movie movie)
    {
        return $"{GetVideoStream(movie).BitDepth}bit";
    }

    private static string GetDynamicRange(Movie movie)
    {
        return GetVideoStream(movie).VideoRange.ToString();
    }
}
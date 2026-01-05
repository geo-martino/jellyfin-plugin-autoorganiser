using System.Linq;
using J2N.Collections.Generic;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Entities;

namespace Jellyfin.Plugin.AutoOrganiser.Core.Formatters;

/// <summary>
/// Handles label formatting for use in a file name for an item.
/// </summary>
public class LabelFormatter : IFormatter<Video>
{
    private readonly bool _addLabelResolution;
    private readonly bool _addLabelCodec;
    private readonly bool _addLabelBitDepth;
    private readonly bool _addLabelDynamicRange;

    /// <summary>
    /// Initializes a new instance of the <see cref="LabelFormatter"/> class.
    /// </summary>
    /// <param name="addLabelResolution">Whether to add the video resolution as part of the file name's label.</param>
    /// <param name="addLabelCodec">Whether to add the video codec as part of the file name's label.</param>
    /// <param name="addLabelBitDepth">Whether to add the video bit depth as part of the file name's label.</param>
    /// <param name="addLabelDynamicRange">Whether to add the video dynamic range as part of the file name's label.</param>
    public LabelFormatter(
        bool addLabelResolution,
        bool addLabelCodec,
        bool addLabelBitDepth,
        bool addLabelDynamicRange)
    {
        _addLabelResolution = addLabelResolution;
        _addLabelCodec = addLabelCodec;
        _addLabelBitDepth = addLabelBitDepth;
        _addLabelDynamicRange = addLabelDynamicRange;

        WidthHeightMap4X3 = new Dictionary<int, int>
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

        WidthHeightMap16X9 = new Dictionary<int, int>
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

    private Dictionary<int, int> WidthHeightMap4X3 { get; }

    private Dictionary<int, int> WidthHeightMap16X9 { get; }

    /// <summary>
    /// Format a label for a given item.
    /// </summary>
    /// <param name="item">The item to format a label for.</param>
    /// <returns>The formatted label.</returns>
    public string Format(Video item)
    {
        var labelParts = new List<string>();
        if (_addLabelResolution)
        {
            labelParts.Add(GetResolution(item));
        }

        if (_addLabelCodec)
        {
            var codec = GetCodec(item);
            if (codec is not null)
            {
                labelParts.Add(codec);
            }
        }

        if (_addLabelBitDepth)
        {
            var bitDepth = GetBitDepth(item);
            if (bitDepth is not null)
            {
                labelParts.Add(bitDepth);
            }
        }

        if (_addLabelDynamicRange)
        {
            var dynamicRange = GetDynamicRange(item);
            if (dynamicRange is not null)
            {
                labelParts.Add(dynamicRange);
            }
        }

        return string.Join(' ', labelParts);
    }

    /// <summary>
    /// Adds a label as a suffix to the given file name for a given item.
    /// </summary>
    /// <param name="item">The item to format a label for.</param>
    /// <param name="fileName">The file name to enrich.</param>
    /// <param name="addDash">Separate the label from the fileName with a dash.</param>
    /// <returns>The enriched file name.</returns>
    public string AppendLabel(Video item, string fileName, bool addDash = true)
    {
        var label = Format(item);
        if (label.Length == 0)
        {
            return fileName;
        }

        if (addDash)
        {
            fileName = $"{fileName.TrimEnd(' ', '-')} -";
        }

        return $"{fileName.TrimEnd()} [{label}]";
    }

    private static MediaStream? GetVideoStream(BaseItem item) => item
        .GetMediaStreams().First(stream => stream.Type == MediaStreamType.Video);

    private string GetResolution(BaseItem item)
    {
        var stream = GetVideoStream(item);

        var isSquare = stream?.AspectRatio == "4:3" || (double)item.Width / item.Height < 1.35;
        var widthHeightMap = isSquare ? WidthHeightMap4X3 : WidthHeightMap16X9;

        var height = CalculateHeight(item.Width, item.Height, widthHeightMap);
        var type = stream?.IsInterlaced ?? false ? "i" : "p";

        return $"{height}{type}";
    }

    private static int CalculateHeight(int width, int height, Dictionary<int, int> widthHeightMap)
    {
        foreach (var widthHeightPair in widthHeightMap)
        {
            if (height == widthHeightPair.Value)
            {
                break;
            }

            if (width > widthHeightPair.Key)
            {
                continue;
            }

            height = widthHeightPair.Value;
            break;
        }

        return height;
    }

    private static string? GetCodec(BaseItem item) => GetVideoStream(item)?.Codec.ToUpperInvariant();

    private static string? GetBitDepth(BaseItem item)
    {
        var stream = GetVideoStream(item);
        return stream is not null ? $"{stream.BitDepth}bit" : null;
    }

    private static string? GetDynamicRange(BaseItem item) => GetVideoStream(item)?.VideoRange.ToString();
}
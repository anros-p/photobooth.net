using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using SkiaSharp;

namespace Photobooth.Imaging.Composition;

/// <summary>
/// Applies layout and overlays to each frame of a GIF/Boomerang capture sequence
/// and re-encodes the result as an animated GIF.
/// </summary>
public sealed class GifCompositor
{
    private readonly ImageCompositor _stillCompositor = new();

    /// <summary>
    /// For each frame path in <paramref name="frameCapturePaths"/>, composites the layout
    /// and overlays onto it, then encodes all composited frames as an animated GIF.
    /// </summary>
    /// <param name="frameCapturePaths">Ordered list of per-frame still capture paths.</param>
    /// <param name="request">Composition settings (background, slots, overlays, quality).</param>
    /// <param name="frameDelayMs">GIF frame delay in milliseconds.</param>
    /// <param name="outputPath">Destination .gif file path.</param>
    public async Task<CompositionResult> ComposeAsync(
        IReadOnlyList<string> frameCapturePaths,
        CompositionRequest request,
        int frameDelayMs,
        string outputPath,
        CancellationToken ct = default)
    {
        if (frameCapturePaths.Count == 0)
            throw new ArgumentException("At least one frame is required.", nameof(frameCapturePaths));

        var tempDir = Path.Combine(Path.GetDirectoryName(outputPath)!, "_gif_frames_tmp");
        Directory.CreateDirectory(tempDir);

        try
        {
            // Composite each frame as a still JPEG into the temp directory
            var composedPaths = new List<string>(frameCapturePaths.Count);
            for (var i = 0; i < frameCapturePaths.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var frameRequest = request with
                {
                    CaptureFilePaths = [frameCapturePaths[i]]
                };

                var framePath = Path.Combine(tempDir, $"frame_{i:D4}.jpg");
                _stillCompositor.Compose(frameRequest, framePath);
                composedPaths.Add(framePath);
            }

            // Encode composited frames as animated GIF
            await EncodeGifAsync(composedPaths, frameDelayMs, outputPath, ct).ConfigureAwait(false);

            // Read dimensions from the first composited frame
            using var firstBitmap = SKBitmap.Decode(composedPaths[0]);
            return new CompositionResult
            {
                OutputPath = outputPath,
                WidthPixels = firstBitmap?.Width ?? 0,
                HeightPixels = firstBitmap?.Height ?? 0
            };
        }
        finally
        {
            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, recursive: true);
        }
    }

    private static async Task EncodeGifAsync(
        List<string> framePaths, int frameDelayMs, string outputPath, CancellationToken ct)
    {
        var frameDelayCentiseconds = Math.Max(2, frameDelayMs / 10);
        var loadedFrames = new List<Image<Rgba32>>(framePaths.Count);

        try
        {
            foreach (var path in framePaths)
            {
                ct.ThrowIfCancellationRequested();
                loadedFrames.Add(await Image.LoadAsync<Rgba32>(path, ct).ConfigureAwait(false));
            }

            var gif = loadedFrames[0];
            gif.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay = frameDelayCentiseconds;

            for (var i = 1; i < loadedFrames.Count; i++)
            {
                var frame = loadedFrames[i].Frames.RootFrame;
                frame.Metadata.GetGifMetadata().FrameDelay = frameDelayCentiseconds;
                gif.Frames.AddFrame(frame);
            }

            gif.Metadata.GetGifMetadata().RepeatCount = 0;

            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
            await gif.SaveAsGifAsync(outputPath, ct).ConfigureAwait(false);
        }
        finally
        {
            for (var i = 1; i < loadedFrames.Count; i++)
                loadedFrames[i].Dispose();
        }
    }
}

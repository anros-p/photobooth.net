using Photobooth.Drivers.Models;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace Photobooth.Drivers.Camera.Gif;

/// <summary>
/// Drives a burst of captures from an <see cref="ICamera"/> and encodes
/// the frames into an animated GIF (forward) or Boomerang (forward + reverse) file.
/// </summary>
public sealed class GifCaptureSession
{
    private readonly ICamera _camera;
    private readonly int _frameCount;
    private readonly int _frameDelayMs;
    private readonly CaptureMode _mode;

    /// <param name="camera">Connected camera to capture from.</param>
    /// <param name="frameCount">Number of still frames to capture.</param>
    /// <param name="frameDelayMs">Delay between frames in milliseconds (controls GIF speed).</param>
    /// <param name="mode">GIF (forward loop) or Boomerang (ping-pong loop).</param>
    public GifCaptureSession(ICamera camera, int frameCount, int frameDelayMs, CaptureMode mode)
    {
        if (mode is not (CaptureMode.Gif or CaptureMode.Boomerang))
            throw new ArgumentException("Mode must be Gif or Boomerang.", nameof(mode));

        _camera = camera;
        _frameCount = frameCount;
        _frameDelayMs = frameDelayMs;
        _mode = mode;
    }

    /// <summary>
    /// Captures <see cref="_frameCount"/> frames and encodes them as an animated GIF
    /// saved to <paramref name="outputPath"/>.
    /// </summary>
    /// <param name="captureDirectory">Temporary directory for individual frame JPEGs.</param>
    /// <param name="outputPath">Destination path for the output .gif file.</param>
    public async Task<CapturedImage> CaptureAsync(
        string captureDirectory, string outputPath, CancellationToken ct = default)
    {
        Directory.CreateDirectory(captureDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

        // 1. Capture individual frames
        var framePaths = new List<string>(_frameCount);
        for (var i = 0; i < _frameCount; i++)
        {
            ct.ThrowIfCancellationRequested();
            var framePath = Path.Combine(captureDirectory, $"frame_{i:D3}.jpg");
            await _camera.CaptureAsync(framePath, ct).ConfigureAwait(false);
            framePaths.Add(framePath);

            if (i < _frameCount - 1)
                await Task.Delay(_frameDelayMs, ct).ConfigureAwait(false);
        }

        // 2. Assemble GIF
        await EncodeGifAsync(framePaths, outputPath, ct).ConfigureAwait(false);

        return new CapturedImage
        {
            FilePath = outputPath,
            CapturedAt = DateTimeOffset.UtcNow
        };
    }

    private async Task EncodeGifAsync(List<string> framePaths, string outputPath, CancellationToken ct)
    {
        // Build the frame sequence (add reversed frames for boomerang, excluding endpoints)
        var sequence = new List<string>(framePaths);
        if (_mode == CaptureMode.Boomerang && framePaths.Count > 2)
        {
            for (var i = framePaths.Count - 2; i >= 1; i--)
                sequence.Add(framePaths[i]);
        }

        // centiseconds per frame (GIF delay unit)
        var frameDelayCentiseconds = Math.Max(2, _frameDelayMs / 10);

        // Load all frames, then assemble into the gif
        var loadedFrames = new List<Image<Rgba32>>(sequence.Count);
        try
        {
            foreach (var framePath in sequence)
            {
                ct.ThrowIfCancellationRequested();
                loadedFrames.Add(await Image.LoadAsync<Rgba32>(framePath, ct).ConfigureAwait(false));
            }

            // Use the first frame as the base image
            var gif = loadedFrames[0];
            gif.Frames.RootFrame.Metadata.GetGifMetadata().FrameDelay = frameDelayCentiseconds;

            for (var i = 1; i < loadedFrames.Count; i++)
            {
                var frame = loadedFrames[i].Frames.RootFrame;
                frame.Metadata.GetGifMetadata().FrameDelay = frameDelayCentiseconds;
                gif.Frames.AddFrame(frame);
            }

            var gifMetadata = gif.Metadata.GetGifMetadata();
            gifMetadata.RepeatCount = 0; // loop forever

            await gif.SaveAsGifAsync(outputPath, ct).ConfigureAwait(false);
        }
        finally
        {
            // Skip index 0 — it's the gif itself and will be disposed separately
            for (var i = 1; i < loadedFrames.Count; i++)
                loadedFrames[i].Dispose();
        }

    }
}

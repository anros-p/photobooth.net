using SkiaSharp;

namespace Photobooth.Imaging.Composition;

/// <summary>
/// Composites a set of captured photos onto a background canvas
/// according to a <see cref="CompositionRequest"/>, then writes the result to disk.
///
/// Pipeline:
///   1. Draw background image (or solid black if none configured)
///   2. For each photo slot: load capture, apply rotation, draw at slot bounds
///   3. For each overlay: load PNG, apply transform, draw on top
///   4. Encode and save as JPEG
/// </summary>
public sealed class ImageCompositor
{
    /// <summary>
    /// Composes the request and saves the result to <paramref name="outputPath"/>.
    /// </summary>
    public CompositionResult Compose(CompositionRequest request, string outputPath)
    {
        var layout = request.Layout;
        var width = (int)layout.CanvasWidth;
        var height = (int)layout.CanvasHeight;

        if (width <= 0 || height <= 0)
            throw new ArgumentException("Layout canvas dimensions must be positive.", nameof(request));

        if (request.CaptureFilePaths.Count != layout.PhotoSlots.Count)
            throw new ArgumentException(
                $"Expected {layout.PhotoSlots.Count} capture(s) but got {request.CaptureFilePaths.Count}.",
                nameof(request));

        using var surface = SKSurface.Create(new SKImageInfo(width, height, SKColorType.Rgba8888));
        var canvas = surface.Canvas;
        canvas.Clear(SKColors.Black);

        // 1. Background
        DrawBackground(canvas, layout.BackgroundImagePath, width, height);

        // 2. Photo slots
        for (var i = 0; i < layout.PhotoSlots.Count; i++)
            DrawPhotoSlot(canvas, layout.PhotoSlots[i], request.CaptureFilePaths[i]);

        // 3. Overlays
        foreach (var overlay in request.Overlays)
            DrawOverlay(canvas, overlay);

        // 4. Encode
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        using var image = surface.Snapshot();
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, request.OutputQuality);
        using var stream = File.OpenWrite(outputPath);
        data.SaveTo(stream);

        return new CompositionResult
        {
            OutputPath = outputPath,
            WidthPixels = width,
            HeightPixels = height
        };
    }

    private static readonly SKSamplingOptions HighQualitySampling =
        new(SKFilterMode.Linear, SKMipmapMode.Linear);

    private static void DrawBackground(SKCanvas canvas, string backgroundPath, int canvasWidth, int canvasHeight)
    {
        if (string.IsNullOrEmpty(backgroundPath) || !File.Exists(backgroundPath))
            return;

        using var image = LoadImage(backgroundPath);
        if (image is null) return;

        var destRect = new SKRect(0, 0, canvasWidth, canvasHeight);
        canvas.DrawImage(image, destRect, HighQualitySampling);
    }

    private static void DrawPhotoSlot(SKCanvas canvas, Drivers.Models.PhotoSlot slot, string capturePath)
    {
        if (!File.Exists(capturePath)) return;

        using var image = LoadImage(capturePath);
        if (image is null) return;

        canvas.Save();

        var cx = (float)(slot.X + slot.Width / 2);
        var cy = (float)(slot.Y + slot.Height / 2);
        canvas.RotateDegrees((float)slot.Rotation, cx, cy);

        var destRect = new SKRect(
            (float)slot.X,
            (float)slot.Y,
            (float)(slot.X + slot.Width),
            (float)(slot.Y + slot.Height));

        canvas.DrawImage(image, destRect, HighQualitySampling);

        canvas.Restore();
    }

    private static void DrawOverlay(SKCanvas canvas, OverlayPlacement overlay)
    {
        if (!File.Exists(overlay.AssetPath)) return;

        using var image = LoadImage(overlay.AssetPath);
        if (image is null) return;

        canvas.Save();

        var cx = (float)(overlay.X + overlay.Width / 2);
        var cy = (float)(overlay.Y + overlay.Height / 2);
        canvas.RotateDegrees((float)overlay.Rotation, cx, cy);

        var destRect = new SKRect(
            (float)overlay.X,
            (float)overlay.Y,
            (float)(overlay.X + overlay.Width),
            (float)(overlay.Y + overlay.Height));

        canvas.DrawImage(image, destRect, HighQualitySampling);

        canvas.Restore();
    }

    private static SKImage? LoadImage(string path)
    {
        try
        {
            return SKImage.FromEncodedData(path);
        }
        catch
        {
            return null;
        }
    }
}

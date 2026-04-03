using Photobooth.Drivers.Models;
using Photobooth.Imaging.Composition;
using SkiaSharp;

namespace Photobooth.Tests.Imaging;

/// <summary>
/// Tests for ImageCompositor. Uses SkiaSharp to generate synthetic JPEG inputs
/// so tests are fully self-contained — no asset files required.
/// </summary>
public sealed class ImageCompositorTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly ImageCompositor _compositor = new();

    public ImageCompositorTests() => Directory.CreateDirectory(_dir);

    // ── Helpers ──────────────────────────────────────────────────────────

    private string MakeSolidJpeg(string name, int w, int h, SKColor color)
    {
        var path = Path.Combine(_dir, name);
        using var bmp = new SKBitmap(w, h);
        bmp.Erase(color);
        using var img = SKImage.FromBitmap(bmp);
        using var data = img.Encode(SKEncodedImageFormat.Jpeg, 95);
        File.WriteAllBytes(path, data.ToArray());
        return path;
    }

    private string MakeSolidPng(string name, int w, int h, SKColor color)
    {
        var path = Path.Combine(_dir, name);
        using var bmp = new SKBitmap(w, h);
        bmp.Erase(color);
        using var img = SKImage.FromBitmap(bmp);
        using var data = img.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(path, data.ToArray());
        return path;
    }

    private static SKColor SamplePixel(string jpegPath, int x, int y)
    {
        using var bmp = SKBitmap.Decode(jpegPath);
        return bmp.GetPixel(x, y);
    }

    private static LayoutTemplate OneSlotLayout(
        string backgroundPath, int cw, int ch,
        double slotX, double slotY, double slotW, double slotH, double rotation = 0)
        => new()
        {
            Name = "Test",
            BackgroundImagePath = backgroundPath,
            CanvasWidth = cw,
            CanvasHeight = ch,
            PhotoSlots =
            [
                new PhotoSlot { X = slotX, Y = slotY, Width = slotW, Height = slotH, Rotation = rotation }
            ]
        };

    // ── Tests ─────────────────────────────────────────────────────────────

    [Fact]
    public void Compose_ProducesOutputFile()
    {
        var capture = MakeSolidJpeg("cap.jpg", 800, 600, SKColors.Red);
        var bg = MakeSolidJpeg("bg.jpg", 1200, 900, SKColors.Blue);
        var layout = OneSlotLayout(bg, 1200, 900, 0, 0, 1200, 900);
        var output = Path.Combine(_dir, "out.jpg");

        var result = _compositor.Compose(new CompositionRequest
        {
            Layout = layout,
            CaptureFilePaths = [capture]
        }, output);

        Assert.True(File.Exists(output));
        Assert.Equal(output, result.OutputPath);
        Assert.Equal(1200, result.WidthPixels);
        Assert.Equal(900, result.HeightPixels);
    }

    [Fact]
    public void Compose_BackgroundCoversCanvas()
    {
        // Blue background, red capture placed in only the bottom half
        var capture = MakeSolidJpeg("cap.jpg", 400, 200, SKColors.Red);
        var bg = MakeSolidJpeg("bg.jpg", 400, 400, SKColors.Blue);
        var layout = OneSlotLayout(bg, 400, 400, 0, 200, 400, 200); // slot in bottom half
        var output = Path.Combine(_dir, "out.jpg");

        _compositor.Compose(new CompositionRequest
        {
            Layout = layout,
            CaptureFilePaths = [capture]
        }, output);

        // Top-left pixel should be close to blue (background)
        var topLeft = SamplePixel(output, 2, 2);
        Assert.True(topLeft.Blue > 100, $"Expected blue background at top-left, got {topLeft}");
    }

    [Fact]
    public void Compose_PhotoSlotPositionedCorrectly()
    {
        // Green background; red capture fills right half only
        var capture = MakeSolidJpeg("cap.jpg", 200, 400, SKColors.Red);
        var bg = MakeSolidJpeg("bg.jpg", 400, 400, SKColors.Green);
        var layout = OneSlotLayout(bg, 400, 400, 200, 0, 200, 400); // slot = right half
        var output = Path.Combine(_dir, "out.jpg");

        _compositor.Compose(new CompositionRequest
        {
            Layout = layout,
            CaptureFilePaths = [capture]
        }, output);

        // Left side → green; right side → red
        var leftPixel = SamplePixel(output, 10, 200);
        var rightPixel = SamplePixel(output, 390, 200);

        Assert.True(leftPixel.Green > 100, $"Expected green on left, got {leftPixel}");
        Assert.True(rightPixel.Red > 100, $"Expected red on right, got {rightPixel}");
    }

    [Fact]
    public void Compose_MultipleSlots()
    {
        var captureRed = MakeSolidJpeg("red.jpg", 200, 400, SKColors.Red);
        var captureBlue = MakeSolidJpeg("blue.jpg", 200, 400, SKColors.Blue);
        var bg = MakeSolidJpeg("bg.jpg", 400, 400, SKColors.White);
        var layout = new LayoutTemplate
        {
            Name = "Two Slot",
            BackgroundImagePath = bg,
            CanvasWidth = 400,
            CanvasHeight = 400,
            PhotoSlots =
            [
                new PhotoSlot { X = 0, Y = 0, Width = 200, Height = 400 },
                new PhotoSlot { X = 200, Y = 0, Width = 200, Height = 400 }
            ]
        };
        var output = Path.Combine(_dir, "out.jpg");

        _compositor.Compose(new CompositionRequest
        {
            Layout = layout,
            CaptureFilePaths = [captureRed, captureBlue]
        }, output);

        var leftPixel = SamplePixel(output, 10, 200);
        var rightPixel = SamplePixel(output, 390, 200);

        Assert.True(leftPixel.Red > 100, $"Expected red on left, got {leftPixel}");
        Assert.True(rightPixel.Blue > 100, $"Expected blue on right, got {rightPixel}");
    }

    [Fact]
    public void Compose_OverlayDrawnOnTop()
    {
        // Red background + capture; white overlay covering the full canvas
        var capture = MakeSolidJpeg("cap.jpg", 400, 400, SKColors.Red);
        var bg = MakeSolidJpeg("bg.jpg", 400, 400, SKColors.Red);
        var overlayPath = MakeSolidPng("overlay.png", 400, 400, SKColors.White);
        var layout = OneSlotLayout(bg, 400, 400, 0, 0, 400, 400);
        var output = Path.Combine(_dir, "out.jpg");

        _compositor.Compose(new CompositionRequest
        {
            Layout = layout,
            CaptureFilePaths = [capture],
            Overlays =
            [
                new OverlayPlacement
                {
                    AssetPath = overlayPath,
                    X = 0, Y = 0, Width = 400, Height = 400
                }
            ]
        }, output);

        // The white overlay should dominate the centre pixel
        var centre = SamplePixel(output, 200, 200);
        Assert.True(centre.Red > 200 && centre.Green > 200 && centre.Blue > 200,
            $"Expected white overlay at centre, got {centre}");
    }

    [Fact]
    public void Compose_ThrowsWhenCaptureCountMismatch()
    {
        var bg = MakeSolidJpeg("bg.jpg", 400, 400, SKColors.Black);
        var layout = OneSlotLayout(bg, 400, 400, 0, 0, 200, 200);
        var output = Path.Combine(_dir, "out.jpg");

        Assert.Throws<ArgumentException>(() =>
            _compositor.Compose(new CompositionRequest
            {
                Layout = layout,
                CaptureFilePaths = [] // zero captures for one-slot layout
            }, output));
    }

    [Fact]
    public void Compose_MissingBackgroundDoesNotThrow()
    {
        var capture = MakeSolidJpeg("cap.jpg", 400, 400, SKColors.Red);
        var layout = OneSlotLayout("nonexistent_bg.jpg", 400, 400, 0, 0, 400, 400);
        var output = Path.Combine(_dir, "out.jpg");

        // Should succeed — falls back to black background
        var result = _compositor.Compose(new CompositionRequest
        {
            Layout = layout,
            CaptureFilePaths = [capture]
        }, output);

        Assert.True(File.Exists(result.OutputPath));
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }
}

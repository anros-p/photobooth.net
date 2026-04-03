using Photobooth.Drivers.Models;
using Photobooth.Imaging.Composition;
using SkiaSharp;

namespace Photobooth.Tests.Imaging;

public sealed class GifCompositorTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly GifCompositor _compositor = new();

    public GifCompositorTests() => Directory.CreateDirectory(_dir);

    private string MakeSolidJpeg(string name, SKColor color)
    {
        var path = Path.Combine(_dir, name);
        using var bmp = new SKBitmap(200, 200);
        bmp.Erase(color);
        using var img = SKImage.FromBitmap(bmp);
        File.WriteAllBytes(path, img.Encode(SKEncodedImageFormat.Jpeg, 90).ToArray());
        return path;
    }

    private static LayoutTemplate SingleSlotLayout(int w, int h) => new()
    {
        Name = "Test",
        CanvasWidth = w,
        CanvasHeight = h,
        PhotoSlots = [new PhotoSlot { X = 0, Y = 0, Width = w, Height = h }]
    };

    [Fact]
    public async Task ComposeAsync_ProducesGifFile()
    {
        var frames = new[]
        {
            MakeSolidJpeg("f0.jpg", SKColors.Red),
            MakeSolidJpeg("f1.jpg", SKColors.Green),
            MakeSolidJpeg("f2.jpg", SKColors.Blue)
        };

        var request = new CompositionRequest
        {
            Layout = SingleSlotLayout(200, 200),
            CaptureFilePaths = ["placeholder"] // overridden per frame in GifCompositor
        };

        var output = Path.Combine(_dir, "result.gif");
        var result = await _compositor.ComposeAsync(frames, request, 100, output);

        Assert.True(File.Exists(output));
        Assert.Equal(output, result.OutputPath);

        var header = System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(output)[..6]);
        Assert.StartsWith("GIF", header);
    }

    [Fact]
    public async Task ComposeAsync_OutputHasCorrectDimensions()
    {
        var frames = new[]
        {
            MakeSolidJpeg("f0.jpg", SKColors.Red),
            MakeSolidJpeg("f1.jpg", SKColors.Blue)
        };

        var request = new CompositionRequest
        {
            Layout = SingleSlotLayout(200, 200),
            CaptureFilePaths = ["placeholder"]
        };

        var output = Path.Combine(_dir, "sized.gif");
        var result = await _compositor.ComposeAsync(frames, request, 100, output);

        Assert.Equal(200, result.WidthPixels);
        Assert.Equal(200, result.HeightPixels);
    }

    [Fact]
    public async Task ComposeAsync_ThrowsOnEmptyFrameList()
    {
        var request = new CompositionRequest
        {
            Layout = SingleSlotLayout(200, 200),
            CaptureFilePaths = ["placeholder"]
        };

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _compositor.ComposeAsync([], request, 100, Path.Combine(_dir, "empty.gif")));
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }
}

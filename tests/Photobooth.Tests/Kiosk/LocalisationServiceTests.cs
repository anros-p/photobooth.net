using Photobooth.App.Localisation;

namespace Photobooth.Tests.Kiosk;

public sealed class LocalisationServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), $"loc_{Guid.NewGuid():N}");

    public LocalisationServiceTests()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, "en.json"),
            """{"Hello": "Hello", "Bye": "Goodbye"}""");
        File.WriteAllText(Path.Combine(_dir, "fr.json"),
            """{"Hello": "Bonjour", "Bye": "Au revoir"}""");
    }

    public void Dispose() => Directory.Delete(_dir, recursive: true);

    [Fact]
    public void Indexer_ReturnsEnglishByDefault()
    {
        var svc = new LocalisationService(_dir);
        Assert.Equal("Hello", svc["Hello"]);
    }

    [Fact]
    public void Indexer_MissingKey_ReturnsKey()
    {
        var svc = new LocalisationService(_dir);
        Assert.Equal("Missing.Key", svc["Missing.Key"]);
    }

    [Fact]
    public void SetLanguage_SwitchesStrings()
    {
        var svc = new LocalisationService(_dir);
        svc.SetLanguage("fr");
        Assert.Equal("Bonjour", svc["Hello"]);
    }

    [Fact]
    public void SetLanguage_RaisesLanguageChanged()
    {
        var svc = new LocalisationService(_dir);
        var raised = 0;
        svc.LanguageChanged += (_, _) => raised++;

        svc.SetLanguage("fr");

        Assert.Equal(1, raised);
    }

    [Fact]
    public void SetLanguage_UpdatesCurrentLanguage()
    {
        var svc = new LocalisationService(_dir);
        svc.SetLanguage("fr");
        Assert.Equal("fr", svc.CurrentLanguage);
    }

    [Fact]
    public void SetLanguage_MissingFile_Throws()
    {
        var svc = new LocalisationService(_dir);
        Assert.Throws<FileNotFoundException>(() => svc.SetLanguage("zz"));
    }

    [Fact]
    public void NoEnglishFile_DoesNotThrow()
    {
        // Directory with no en.json
        var emptyDir = Path.Combine(Path.GetTempPath(), $"loc_empty_{Guid.NewGuid():N}");
        Directory.CreateDirectory(emptyDir);
        try
        {
            var svc = new LocalisationService(emptyDir);
            Assert.Equal("any.key", svc["any.key"]); // returns key as-is
        }
        finally { Directory.Delete(emptyDir, recursive: true); }
    }
}

public class JunkClassifierTests : IDisposable
{
    private readonly List<string> _tempFiles = [];

    private FileInfo TempFile(string name)
    {
        var path = Path.Combine(Path.GetTempPath(), name);
        File.WriteAllBytes(path, []);
        _tempFiles.Add(path);
        return new FileInfo(path);
    }

    public void Dispose()
    {
        foreach (var p in _tempFiles)
            try { File.Delete(p); } catch { }
    }

    // ── WhatsApp ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("IMG-20231215-WA0001.jpg")]
    [InlineData("VID-20231215-WA0032.mp4")]
    [InlineData("img-20231215-wa0001.jpg")]   // case-insensitive
    public void WhatsAppFilename_IsJunk(string filename)
    {
        Assert.True(JunkClassifier.IsJunk(TempFile(filename), DateSource.Filename));
    }

    // ── Screenshots ───────────────────────────────────────────────────────────

    [Theory]
    [InlineData("Screenshot_20231215_143022.jpg")]
    [InlineData("Screenshot 2023-12-15.png")]
    [InlineData("screenshot_20231215_143022.jpg")]   // case-insensitive
    public void ScreenshotFilename_IsJunk(string filename)
    {
        Assert.True(JunkClassifier.IsJunk(TempFile(filename), DateSource.Filename));
    }

    // ── non-media extensions ──────────────────────────────────────────────────

    [Theory]
    [InlineData("data.json")]
    [InlineData(".nomedia")]
    [InlineData("thumbs.db")]
    [InlineData("config.tmp")]
    [InlineData("settings.ini")]
    [InlineData("metadata.xml")]
    public void NonMediaExtension_IsJunk(string filename)
    {
        Assert.True(JunkClassifier.IsJunk(TempFile(filename), DateSource.Metadata));
    }

    // ── filesystem-only date ──────────────────────────────────────────────────

    [Fact]
    public void FilesystemDateOnly_IsJunk()
    {
        Assert.True(JunkClassifier.IsJunk(TempFile("unknown.jpg"), DateSource.FileSystem));
    }

    // ── real media files ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("IMG_20231215_143022.jpg",  DateSource.Filename)]
    [InlineData("VID_20231215_143022.mp4",  DateSource.Filename)]
    [InlineData("photo.jpg",               DateSource.Metadata)]
    [InlineData("video.mp4",               DateSource.Metadata)]
    [InlineData("image.heic",              DateSource.Metadata)]
    [InlineData("image.png",               DateSource.Metadata)]
    [InlineData("clip.mov",                DateSource.Metadata)]
    public void RealMediaFile_IsNotJunk(string filename, DateSource source)
    {
        Assert.False(JunkClassifier.IsJunk(TempFile(filename), source));
    }
}

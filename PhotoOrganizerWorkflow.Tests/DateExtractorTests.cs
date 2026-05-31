public class DateExtractorTests : IDisposable
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

    // ── filename pattern ──────────────────────────────────────────────────────

    [Theory]
    [InlineData("20231215_143022.jpg",          2023, 12, 15, 14, 30, 22)]
    [InlineData("IMG_20231215_143022.jpg",       2023, 12, 15, 14, 30, 22)]
    [InlineData("VID_20231215_143022.mp4",       2023, 12, 15, 14, 30, 22)]
    [InlineData("PANO_20231215_143022.jpg",      2023, 12, 15, 14, 30, 22)]
    [InlineData("PXL_20231215_143022.jpg",       2023, 12, 15, 14, 30, 22)]
    [InlineData("IMG_20010101_000000.jpg",       2001,  1,  1,  0,  0,  0)]
    public void FilenamePattern_ExtractsCorrectDate(string filename, int y, int mo, int d, int h, int mi, int s)
    {
        var result = DateExtractor.TryExtractDate(TempFile(filename));

        Assert.Equal(DateSource.Filename, result.Source);
        Assert.Equal(new DateTime(y, mo, d, h, mi, s), result.Date);
    }

    [Theory]
    [InlineData("random_photo.jpg")]
    [InlineData("IMG-20231215-WA0001.jpg")]      // WhatsApp — no standard timestamp
    [InlineData("Screenshot_20231215.jpg")]       // no time component
    [InlineData("nodate.mp4")]
    public void NonMatchingFilename_DoesNotUseFilenameSource(string filename)
    {
        var result = DateExtractor.TryExtractDate(TempFile(filename));

        // Empty file has no metadata, so falls through to FileSystem
        Assert.NotEqual(DateSource.Filename, result.Source);
    }

    // ── filesystem fallback ───────────────────────────────────────────────────

    [Fact]
    public void NoMetadata_FallsBackToFileSystemSource()
    {
        var result = DateExtractor.TryExtractDate(TempFile("no_metadata_file.jpg"));

        Assert.Equal(DateSource.FileSystem, result.Source);
    }

    [Fact]
    public void FileSystemFallback_UsesCreationTime()
    {
        var file = TempFile("creation_time_test.bin");
        var expected = file.CreationTime;

        var result = DateExtractor.TryExtractDate(file);

        Assert.Equal(DateSource.FileSystem, result.Source);
        Assert.Equal(expected, result.Date);
    }

    // ── always returns a result ───────────────────────────────────────────────

    [Fact]
    public void AlwaysReturnsResult_NeverNull()
    {
        // DateExtractor.TryExtractDate always returns a DateResult (never null)
        // because the filesystem date is the final fallback
        var result = DateExtractor.TryExtractDate(TempFile("any_file.xyz"));

        Assert.NotNull(result);
        Assert.True(result.Date > DateTime.MinValue);
    }
}

using System.Text.RegularExpressions;

public static class JunkClassifier
{
    // WhatsApp: IMG-20231215-WA0001, VID-20231215-WA0001
    private static readonly Regex WhatsAppPattern = new(
        @"^(?:IMG|VID)-\d{8}-WA\d+",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    // Screenshot_20231215_143022, Screenshot 2023-12-15 14.30.22
    private static readonly Regex ScreenshotPattern = new(
        @"^Screenshot[_\s\-]",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly HashSet<string> KnownMediaExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        // Common photos
        ".jpg", ".jpeg", ".png", ".heic", ".heif", ".bmp", ".webp", ".tiff", ".tif", ".gif",
        // RAW formats (Canon, Sony, Nikon, Olympus, Fuji, Panasonic, Pentax, Samsung, Sigma)
        ".dng", ".raw", ".cr2", ".cr3", ".arw", ".nef", ".nrw", ".orf", ".raf",
        ".rw2", ".pef", ".srw", ".x3f", ".erf",
        // Video
        ".mp4", ".mov", ".avi", ".mkv", ".3gp", ".m4v", ".wmv", ".mts", ".m2ts", ".ts"
    };

    // These extensions always come from dedicated cameras/camcorders and are never junk,
    // even when no embedded date is detected (MetadataExtractor has limited RAW support).
    private static readonly HashSet<string> AlwaysLegitimateExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cr2", ".cr3", ".arw", ".nef", ".nrw", ".orf", ".raf",
        ".rw2", ".pef", ".srw", ".x3f", ".erf", ".dng", ".raw",
        ".mts", ".m2ts",
        ".xmp"  // Lightroom/Photoshop edit sidecars — must stay next to their RAW files
    };

    public static bool IsJunk(FileInfo file, DateSource dateSource)
    {
        var stem = Path.GetFileNameWithoutExtension(file.Name);

        if (WhatsAppPattern.IsMatch(stem))
            return true;

        if (ScreenshotPattern.IsMatch(stem))
            return true;

        // Files in a WhatsApp directory
        if (file.DirectoryName?.Contains("WhatsApp", StringComparison.OrdinalIgnoreCase) == true)
            return true;

        // Explicitly legitimate extensions are never junk regardless of date source.
        // Checked before the media-extension gate so e.g. .xmp sidecars pass through.
        if (AlwaysLegitimateExtensions.Contains(file.Extension))
            return false;

        // Non-media extensions are junk (metadata files, thumbnails, etc.)
        if (!KnownMediaExtensions.Contains(file.Extension))
            return true;

        // For everything else, only a filesystem date means no real capture metadata.
        if (dateSource == DateSource.FileSystem)
            return true;

        return false;
    }
}

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
        ".jpg", ".jpeg", ".png", ".heic", ".heif", ".dng", ".raw",
        ".bmp", ".webp", ".tiff", ".tif", ".gif",
        ".mp4", ".mov", ".avi", ".mkv", ".3gp", ".m4v", ".wmv"
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

        // Non-media extensions are junk (metadata files, thumbnails, etc.)
        if (!KnownMediaExtensions.Contains(file.Extension))
            return true;

        // Only a filesystem date means no real capture metadata — treat as suspected junk
        if (dateSource == DateSource.FileSystem)
            return true;

        return false;
    }
}

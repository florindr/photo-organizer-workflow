using System.Text.RegularExpressions;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using MetadataExtractor.Formats.QuickTime;

public enum DateSource { Filename, Metadata, FileSystem }

public record DateResult(DateTime Date, DateSource Source);

public static class DateExtractor
{
    // Matches Samsung/Android filenames: 20231215_143022, VID_20231215_143022, IMG_20231215_143022, PXL_20231215_143022
    private static readonly Regex FilenamePattern = new(
        @"(?:^|(?:IMG|VID|PANO|PXL)_?)(\d{4})(\d{2})(\d{2})[_\-](\d{2})(\d{2})(\d{2})",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static DateResult TryExtractDate(FileInfo file)
    {
        // 1. Filename (most reliable for Samsung/Android devices)
        var match = FilenamePattern.Match(Path.GetFileNameWithoutExtension(file.Name));
        if (match.Success &&
            int.TryParse(match.Groups[1].Value, out int y) &&
            int.TryParse(match.Groups[2].Value, out int mo) &&
            int.TryParse(match.Groups[3].Value, out int d) &&
            int.TryParse(match.Groups[4].Value, out int h) &&
            int.TryParse(match.Groups[5].Value, out int mi) &&
            int.TryParse(match.Groups[6].Value, out int s))
        {
            try { return new DateResult(new DateTime(y, mo, d, h, mi, s), DateSource.Filename); } catch { }
        }

        // 2. Embedded metadata
        try
        {
            var dirs = ImageMetadataReader.ReadMetadata(file.FullName);

            // EXIF (photos)
            var exif = dirs.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            if (exif?.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var exifDate) == true)
                return new DateResult(exifDate, DateSource.Metadata);

            // QuickTime / MP4 movie header (videos)
            var qt = dirs.OfType<QuickTimeMovieHeaderDirectory>().FirstOrDefault();
            if (qt?.TryGetDateTime(QuickTimeMovieHeaderDirectory.TagCreated, out var qtDate) == true)
                return new DateResult(qtDate, DateSource.Metadata);
        }
        catch { }

        // 3. File creation time as last resort
        return new DateResult(file.CreationTime, DateSource.FileSystem);
    }
}

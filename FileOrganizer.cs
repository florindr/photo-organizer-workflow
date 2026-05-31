public class FileOrganizer(
    DirectoryInfo source,
    DirectoryInfo output,
    string format,
    bool dryRun,
    string[] extensions,
    bool copy)
{
    private readonly string[] _formatSegments = format.Split('/');
    private readonly HashSet<string> _extensions = new(extensions, StringComparer.OrdinalIgnoreCase);

    public void Run()
    {
        if (!source.Exists)
        {
            Console.Error.WriteLine($"Source directory does not exist: {source.FullName}");
            Environment.Exit(1);
        }

        if (dryRun)
            Console.WriteLine("DRY RUN — no files will be moved\n");

        var files = source.EnumerateFiles("*", SearchOption.AllDirectories)
            .Where(f => _extensions.Contains(f.Extension))
            .OrderBy(f => f.Name)
            .ToList();

        if (files.Count == 0)
        {
            Console.WriteLine("No matching files found.");
            return;
        }

        Console.WriteLine($"Found {files.Count} file(s) in {source.FullName}\n");

        int processed = 0, skipped = 0, errors = 0;

        foreach (var file in files)
        {
            try
            {
                var result = DateExtractor.TryExtractDate(file);
                var relPath = Path.Combine([.. _formatSegments.Select(s => result.Date.ToString(s))]);
                var destDir = new DirectoryInfo(Path.Combine(output.FullName, relPath));
                var destFile = new FileInfo(Path.Combine(destDir.FullName, file.Name));

                if (destFile.Exists)
                {
                    Console.WriteLine($"  SKIP  (exists)   {file.Name}  →  {relPath}");
                    skipped++;
                    continue;
                }

                var verb = copy ? "COPY" : "MOVE";
                Console.WriteLine($"  {verb}           {file.Name}  →  {relPath}");

                if (!dryRun)
                {
                    destDir.Create();
                    if (copy)
                        file.CopyTo(destFile.FullName);
                    else
                        file.MoveTo(destFile.FullName);
                }
                processed++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"  ERROR            {file.Name}  —  {ex.Message}");
                errors++;
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Done: {processed} {(copy ? "copied" : "moved")}, {skipped} skipped, {errors} errors");
    }
}

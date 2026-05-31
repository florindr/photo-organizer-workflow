public class Syncer(
    DirectoryInfo source,
    DirectoryInfo output,
    string format,
    bool dryRun,
    bool deleteSynced)
{
    private readonly string[] _formatSegments = format.Split('/');

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
            .OrderBy(f => f.Name)
            .ToList();

        if (files.Count == 0)
        {
            Console.WriteLine("No files found.");
            return;
        }

        Console.WriteLine($"Found {files.Count} file(s) in {source.FullName}\n");

        var junk = new List<FileInfo>();
        var confirmedAtDest = new List<FileInfo>();
        int synced = 0, alreadySynced = 0, errors = 0;

        foreach (var file in files)
        {
            try
            {
                var dateResult = DateExtractor.TryExtractDate(file);
                var relPath = Path.Combine([.. _formatSegments.Select(s => dateResult.Date.ToString(s))]);
                var destDir = new DirectoryInfo(Path.Combine(output.FullName, relPath));
                var destFile = new FileInfo(Path.Combine(destDir.FullName, file.Name));

                if (destFile.Exists)
                {
                    confirmedAtDest.Add(file);
                    alreadySynced++;
                    continue;
                }

                if (JunkClassifier.IsJunk(file, dateResult.Source))
                {
                    junk.Add(file);
                    continue;
                }

                Console.WriteLine($"  SYNC  {file.Name}  →  {relPath}");

                if (!dryRun)
                {
                    destDir.Create();
                    file.CopyTo(destFile.FullName);
                    confirmedAtDest.Add(file);
                }
                synced++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"  ERROR  {file.Name}  —  {ex.Message}");
                errors++;
            }
        }

        Console.WriteLine();
        Console.WriteLine($"Done: {synced} synced, {alreadySynced} already at destination, {junk.Count} suspected junk, {errors} errors");

        if (junk.Count > 0)
        {
            Console.WriteLine($"\nSuspected junk ({junk.Count} files):");
            foreach (var f in junk)
                Console.WriteLine($"  {f.FullName}");

            if (!dryRun)
                PromptDelete(junk, "suspected junk");
        }

        if (deleteSynced && confirmedAtDest.Count > 0 && !dryRun)
        {
            Console.WriteLine();
            PromptDelete(confirmedAtDest, "source files confirmed at destination");
        }
    }

    private static void PromptDelete(List<FileInfo> files, string label)
    {
        Console.Write($"Delete {files.Count} {label} from source? [y/N] ");
        var response = Console.ReadLine()?.Trim();
        if (!string.Equals(response, "y", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Skipped deletion.");
            return;
        }

        int deleted = 0, failed = 0;
        foreach (var file in files)
        {
            try
            {
                file.Delete();
                deleted++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"  ERROR deleting {file.Name}  —  {ex.Message}");
                failed++;
            }
        }
        Console.WriteLine($"Deleted {deleted} file(s){(failed > 0 ? $", {failed} failed" : "")}.");
    }
}

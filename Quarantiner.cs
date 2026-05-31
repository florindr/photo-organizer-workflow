public class Quarantiner(
    DirectoryInfo source,
    DirectoryInfo quarantineDir,
    bool dryRun,
    int fromYear,
    int toYear)
{
    public void Run()
    {
        if (!source.Exists)
        {
            Console.Error.WriteLine($"Source directory does not exist: {source.FullName}");
            Environment.Exit(1);
        }

        var yearDirs = source.EnumerateDirectories()
            .Where(d => int.TryParse(d.Name, out int y) && y >= fromYear && y <= toYear)
            .OrderBy(d => d.Name)
            .ToList();

        if (yearDirs.Count == 0)
        {
            Console.WriteLine($"No year folders ({fromYear}–{toYear}) found in {source.FullName}");
            return;
        }

        var allFiles = yearDirs
            .SelectMany(d => d.EnumerateFiles("*", SearchOption.AllDirectories))
            .ToList();

        Console.WriteLine($"Scanning {allFiles.Count} file(s) across years {fromYear}–{toYear} in {source.FullName}");
        Console.WriteLine($"Using {Environment.ProcessorCount} parallel threads\n");

        var junk = allFiles
            .AsParallel()
            .WithDegreeOfParallelism(Environment.ProcessorCount)
            .Select(f =>
            {
                var dr = DateExtractor.TryExtractDate(f);
                return (File: f, Rel: Path.GetRelativePath(source.FullName, f.FullName), IsJunk: JunkClassifier.IsJunk(f, dr.Source));
            })
            .Where(x => x.IsJunk)
            .Select(x => (x.File, x.Rel))
            .OrderBy(x => x.Rel)
            .ToList();

        Console.WriteLine($"Suspected junk: {junk.Count} of {allFiles.Count} file(s)\n");

        if (junk.Count == 0)
            return;

        foreach (var (_, rel) in junk)
            Console.WriteLine($"  {rel}");

        if (dryRun)
        {
            Console.WriteLine("\nDry run — no files moved.");
            return;
        }

        Console.WriteLine();
        Console.Write($"Move {junk.Count} suspected junk files to {quarantineDir.FullName}? [y/N] ");
        var response = Console.ReadLine()?.Trim();
        if (!string.Equals(response, "y", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("Skipped.");
            return;
        }

        int moved = 0, failed = 0;
        foreach (var (file, rel) in junk)
        {
            try
            {
                var dest = new FileInfo(Path.Combine(quarantineDir.FullName, rel));
                dest.Directory!.Create();
                file.MoveTo(dest.FullName);
                moved++;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"  ERROR  {file.Name}  —  {ex.Message}");
                failed++;
            }
        }

        Console.WriteLine($"Moved {moved} file(s) to {quarantineDir.FullName}{(failed > 0 ? $", {failed} failed" : "")}.");
    }
}

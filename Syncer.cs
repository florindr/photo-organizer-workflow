public class Syncer(
    DirectoryInfo source,
    DirectoryInfo output,
    string format,
    bool dryRun,
    bool deleteSynced)
{
    private readonly string[] _formatSegments = format.Split('/');

    private enum FileAction { Sync, AlreadyAtDest, Junk }

    private record FileDecision(FileInfo File, FileAction Action, string RelPath);

    public void Run()
    {
        if (!source.Exists)
        {
            Console.Error.WriteLine($"Source directory does not exist: {source.FullName}");
            Environment.Exit(1);
        }

        if (dryRun)
            Console.WriteLine("DRY RUN — no files will be moved\n");

        var files = source.EnumerateFiles("*", SearchOption.AllDirectories).ToList();

        if (files.Count == 0)
        {
            Console.WriteLine("No files found.");
            return;
        }

        Console.WriteLine($"Found {files.Count} file(s) in {source.FullName}");
        Console.WriteLine($"Classifying with {Environment.ProcessorCount} parallel threads...\n");

        // Parallel classification phase (date extraction is the bottleneck)
        var decisions = files
            .AsParallel()
            .WithDegreeOfParallelism(Environment.ProcessorCount)
            .Select(file =>
            {
                var dr = DateExtractor.TryExtractDate(file);
                var relPath = Path.Combine([.. _formatSegments.Select(s => dr.Date.ToString(s))]);
                var destFile = new FileInfo(Path.Combine(output.FullName, relPath, file.Name));

                var action = destFile.Exists        ? FileAction.AlreadyAtDest
                           : JunkClassifier.IsJunk(file, dr.Source) ? FileAction.Junk
                           : FileAction.Sync;

                return new FileDecision(file, action, relPath);
            })
            .OrderBy(d => d.File.Name)
            .ToList();

        // Sequential act phase
        var junk = new List<FileInfo>();
        var confirmedAtDest = new List<FileInfo>();
        int synced = 0, alreadySynced = 0, errors = 0;

        foreach (var decision in decisions)
        {
            try
            {
                switch (decision.Action)
                {
                    case FileAction.AlreadyAtDest:
                        confirmedAtDest.Add(decision.File);
                        alreadySynced++;
                        break;

                    case FileAction.Junk:
                        junk.Add(decision.File);
                        break;

                    case FileAction.Sync:
                        Console.WriteLine($"  SYNC  {decision.File.Name}  →  {decision.RelPath}");
                        if (!dryRun)
                        {
                            var destDir = new DirectoryInfo(Path.Combine(output.FullName, decision.RelPath));
                            var destFile = new FileInfo(Path.Combine(destDir.FullName, decision.File.Name));
                            destDir.Create();
                            decision.File.CopyTo(destFile.FullName);
                            confirmedAtDest.Add(decision.File);
                        }
                        synced++;
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"  ERROR  {decision.File.Name}  —  {ex.Message}");
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
